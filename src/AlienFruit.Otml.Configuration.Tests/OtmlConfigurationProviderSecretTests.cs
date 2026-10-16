using AlienFruit.Otml.Configuration.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace AlienFruit.Otml.Configuration.Tests
{
    /// <summary>
    /// Covers lazy <c>{{secret:...}}</c> resolution in <see cref="OtmlConfigurationProvider"/>:
    /// markers are resolved on the first value access (not at load), storages are tried in
    /// registration order, <see langword="null"/> means "not in this storage".
    /// </summary>
    public class OtmlConfigurationProviderSecretTests : IDisposable
    {
        private readonly string tempFile;
        private readonly List<string> extraFiles = new List<string>();

        public OtmlConfigurationProviderSecretTests()
        {
            this.tempFile = Path.Combine(Path.GetTempPath(), $"otml-secret-test-{Guid.NewGuid():N}.otml");
        }

        public void Dispose()
        {
            foreach (var file in this.extraFiles.Append(this.tempFile).Where(File.Exists))
                File.Delete(file);
        }

        private void Write(string otml) => File.WriteAllText(this.tempFile, otml);

        private string WriteExtra(string otml)
        {
            var file = Path.Combine(Path.GetTempPath(), $"otml-secret-test-{Guid.NewGuid():N}.otml");
            File.WriteAllText(file, otml);
            this.extraFiles.Add(file);
            return file;
        }

        private IConfigurationRoot Build(params ISecretProvider[] secrets)
        {
            var source = new OtmlConfigurationSource { OtmlFile = this.tempFile, SecretProviders = secrets };
            return new ConfigurationBuilder().Add(source).Build();
        }

        [Fact]
        public void Value_with_marker_is_replaced_by_the_secret()
        {
            Write("@database\n\tpassword : {{secret:DB_PASSWORD}}\n");
            var secrets = new FakeSecretProvider(new Dictionary<string, string> { ["DB_PASSWORD"] = "s3cret" });

            var config = Build(secrets);

            config["database:password"].Should().Be("s3cret");
            secrets.RequestedNames.Should().Equal("DB_PASSWORD");
        }

        [Fact]
        public void Marker_embedded_in_composite_value_is_substituted_in_place()
        {
            Write("@database\n\tconnString : \"Server=db;Password={{secret:DB_PASSWORD}};Pool=5\"\n");
            var secrets = new FakeSecretProvider(new Dictionary<string, string> { ["DB_PASSWORD"] = "s3cret" });

            var config = Build(secrets);

            config["database:connString"].Should().Be("Server=db;Password=s3cret;Pool=5");
        }

        [Fact]
        public void Multiple_markers_in_one_value_resolve_each_independently()
        {
            Write("@mail\n\tdsn : \"{{secret:SMTP_USER}}:{{secret:SMTP_PASS}}@smtp.example\"\n");
            var secrets = new FakeSecretProvider(new Dictionary<string, string>
            {
                ["SMTP_USER"] = "user",
                ["SMTP_PASS"] = "pass"
            });

            var config = Build(secrets);

            config["mail:dsn"].Should().Be("user:pass@smtp.example");
            secrets.RequestedNames.Should().Equal("SMTP_USER", "SMTP_PASS");
        }

        [Fact]
        public void Every_array_element_is_resolved()
        {
            Write("@app\n\ttokens : plain, {{secret:SECOND}}, {{secret:THIRD}}\n");
            var secrets = new FakeSecretProvider(new Dictionary<string, string>
            {
                ["SECOND"] = "two",
                ["THIRD"] = "three"
            });

            var config = Build(secrets);

            config["app:tokens:0"].Should().Be("plain");
            config["app:tokens:1"].Should().Be("two");
            config["app:tokens:2"].Should().Be("three");
        }

        [Fact]
        public void Marker_tolerates_spaces_and_case_insensitive_tag_name()
        {
            Write("@x\n\ta : {{ SECRET:K1 }}\n\tb : {{secret:K2}}\n");
            var secrets = new FakeSecretProvider(new Dictionary<string, string>
            {
                ["K1"] = "v1",
                ["K2"] = "v2"
            });

            var config = Build(secrets);

            config["x:a"].Should().Be("v1");
            config["x:b"].Should().Be("v2");
        }

        [Fact]
        public void Unknown_tag_marker_is_left_untouched_and_no_provider_lookup_happens()
        {
            Write("@x\n\ta : {{vault:K1}}\n\tb : {{secret:K2}}\n");
            var secrets = new FakeSecretProvider(new Dictionary<string, string> { ["K2"] = "v2" });

            var config = Build(secrets);

            config["x:a"].Should().Be("{{vault:K1}}");
            config["x:b"].Should().Be("v2");
            secrets.RequestedNames.Should().Equal("K2");
        }

        [Fact]
        public void Value_without_markers_passes_through_unchanged()
        {
            Write("@app\n\tname : My app\n\tcount : 42\n");
            var secrets = new FakeSecretProvider(new Dictionary<string, string>());

            var config = Build(secrets);

            config["app:name"].Should().Be("My app");
            config["app:count"].Should().Be("42");
            secrets.RequestedNames.Should().BeEmpty();
        }

        [Fact]
        public void Marker_without_secret_provider_fails_on_access_not_at_load()
        {
            Write("@database\n\tpassword : {{secret:DB_PASSWORD}}\n");

            var config = Build();

            var act = () => config["database:password"];

            act.Should().Throw<SecretNotFoundException>()
                .Where(e => e.SecretName == "DB_PASSWORD")
                .WithMessage("*database:password*no ISecretProvider is registered*");
        }

        [Fact]
        public void Missing_secret_throws_SecretNotFoundException_with_name_key_and_storages()
        {
            Write("@database\n\tpassword : {{secret:DB_PASSWORD}}\n");
            var secrets = new FakeSecretProvider(new Dictionary<string, string>());

            var config = Build(secrets);
            var act = () => config["database:password"];

            act.Should().Throw<SecretNotFoundException>()
                .Where(e => e.SecretName == "DB_PASSWORD")
                .WithMessage("*DB_PASSWORD*database:password*not found*searched storages: fake*");
        }

        [Fact]
        public void Provider_returning_null_falls_through_to_the_next_storage()
        {
            Write("@database\n\tpassword : {{secret:DB_PASSWORD}}\n");
            var miss = new FakeSecretProvider(new Dictionary<string, string>(), name: "miss");
            var hit = new FakeSecretProvider(new Dictionary<string, string> { ["DB_PASSWORD"] = "v" }, name: "hit");

            var config = Build(miss, hit);

            config["database:password"].Should().Be("v");
            miss.RequestedNames.Should().Equal("DB_PASSWORD");
            hit.RequestedNames.Should().Equal("DB_PASSWORD");
        }

        [Fact]
        public void First_non_empty_provider_wins_and_later_storages_are_not_queried()
        {
            Write("@database\n\tpassword : {{secret:DB_PASSWORD}}\n");
            var first = new FakeSecretProvider(new Dictionary<string, string> { ["DB_PASSWORD"] = "from-first" }, name: "first");
            var second = new FakeSecretProvider(new Dictionary<string, string> { ["DB_PASSWORD"] = "from-second" }, name: "second");

            var config = Build(first, second);

            config["database:password"].Should().Be("from-first");
            second.RequestedNames.Should().BeEmpty();
        }

        [Fact]
        public void Resolution_failure_never_materializes_placeholder_or_empty_value()
        {
            Write("@mail\n\tsubject : Hello {{secret:GREETING}}\n");
            var secrets = new FakeSecretProvider(new Dictionary<string, string> { ["GREETING"] = "world" });

            var config = Build(secrets);

            config["mail:subject"].Should().Be("Hello world");
            config["mail:subject"].Should().NotContain("{{");
        }

        [Fact]
        public void Marker_with_empty_name_throws_ArgumentException_naming_the_key()
        {
            Write("@database\n\tpassword : {{secret: }}\n");
            var secrets = new FakeSecretProvider(new Dictionary<string, string>());

            var config = Build(secrets);
            var act = () => config["database:password"];

            act.Should().Throw<ArgumentException>()
                .WithMessage("*database:password*empty*marker*");
            secrets.RequestedNames.Should().BeEmpty();
        }

        [Fact]
        public void Resolved_value_is_cached_and_provider_is_queried_only_once()
        {
            Write("@database\n\tpassword : {{secret:DB_PASSWORD}}\n");
            var secrets = new FakeSecretProvider(new Dictionary<string, string> { ["DB_PASSWORD"] = "s3cret" });

            var config = Build(secrets);

            config["database:password"].Should().Be("s3cret");
            config["database:password"].Should().Be("s3cret");

            secrets.RequestedNames.Should().Equal("DB_PASSWORD");
        }

        [Fact]
        public void Error_for_nested_object_uses_full_configuration_key()
        {
            Write("@outer\n\t@inner\n\t\tpass : {{secret:P}}\n");
            var secrets = new FakeSecretProvider(new Dictionary<string, string>());

            var config = Build(secrets);
            var act = () => config["outer:inner:pass"];

            act.Should().Throw<SecretNotFoundException>()
                .WithMessage("*outer:inner:pass*");
        }

        [Fact]
        public void Legacy_otml_without_secret_providers_loads_same_as_before()
        {
            Write("@section\n\tkey : value\n\tarr : a, b\n\t@nested\n\t\tnum : 7\n");

            var config = Build();

            config["section:key"].Should().Be("value");
            config["section:arr:0"].Should().Be("a");
            config["section:arr:1"].Should().Be("b");
            config["section:nested:num"].Should().Be("7");
        }

        [Fact]
        public void Multiple_otml_sources_resolve_markers_per_provider()
        {
            var appFile = this.WriteExtra("@app\n\tname : my-app\n");
            var dataFile = this.WriteExtra("@database\n\tpassword : {{secret:DB_PASSWORD}}\n");

            var secrets = new FakeSecretProvider(new Dictionary<string, string> { ["DB_PASSWORD"] = "top" });
            var config = new ConfigurationBuilder()
                .Add(new OtmlConfigurationSource { OtmlFile = appFile })
                .Add(new OtmlConfigurationSource { OtmlFile = dataFile, SecretProviders = new ISecretProvider[] { secrets } })
                .Build();

            config["database:password"].Should().Be("top");
            config["app:name"].Should().Be("my-app");
            secrets.RequestedNames.Should().Equal("DB_PASSWORD"); // the non-secret source touched no storage
        }
    }
}
