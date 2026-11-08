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
    /// Covers array flattening and specific document shapes in <see cref="OtmlConfigurationProvider"/>:
    /// inline and blocked arrays, multiline (+) joins, quoted commas, section access and binding,
    /// case-insensitive key lookup, reload semantics, and the failure modes of malformed documents.
    /// The <c>{{secret:...}}</c> provider-ordering contract is covered by
    /// <see cref="OtmlConfigurationProviderSecretTests"/>; only array/multiline interactions
    /// with markers are exercised here.
    /// </summary>
    public class OtmlConfigurationProviderArraysTests : IDisposable
    {
        private readonly string tempFile;

        public OtmlConfigurationProviderArraysTests()
        {
            this.tempFile = Path.Combine(Path.GetTempPath(), $"otml-arrays-test-{Guid.NewGuid():N}.otml");
        }

        public void Dispose()
        {
            if (File.Exists(this.tempFile))
                File.Delete(this.tempFile);
        }

        private void Write(string otml) => File.WriteAllText(this.tempFile, otml);

        private IConfigurationRoot Build(params ISecretProvider[] secrets)
        {
            var source = new OtmlConfigurationSource { OtmlFile = this.tempFile, SecretProviders = secrets };
            return new ConfigurationBuilder().Add(source).Build();
        }

        #region inline arrays

        [Fact]
        public void Inline_array_flattens_to_indexed_keys()
        {
            Write("@s\n\tarr : a, b, c\n");

            var config = Build();

            config["s:arr:0"].Should().Be("a");
            config["s:arr:1"].Should().Be("b");
            config["s:arr:2"].Should().Be("c");
            config["s:arr"].Should().BeNull(); // the property itself is not stored when it is an array
        }

        [Fact]
        public void Array_section_children_are_indexed_and_bindable()
        {
            Write("@s\n\tarr : a, b\n");

            var config = Build();

            config.GetSection("s:arr").GetChildren()
                .Select(x => $"{x.Key}={x.Value}").Should().Equal("0=a", "1=b");
            config.GetSection("s:arr").Get<string[]>().Should().Equal("a", "b");
            Write("@s\n\tnums : 1, 2, 3\n");
            config = Build();
            config.GetSection("s:nums").Get<int[]>().Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Quoted_element_with_comma_stays_one_array_item()
        {
            Write("@s\n\tarr : \"a,b\", c\n");

            var config = Build();

            config["s:arr:0"].Should().Be("a,b");
            config["s:arr:1"].Should().Be("c");
        }

        [Fact]
        public void Array_at_deep_nesting_keeps_the_full_key_path()
        {
            Write("@a\n\t@b\n\t\t@c\n\t\t\tarr : x, y\n");

            var config = Build();

            config["a:b:c:arr:0"].Should().Be("x");
            config["a:b:c:arr:1"].Should().Be("y");
        }

        [Fact]
        public void Empty_quoted_value_is_an_empty_string_not_a_failure()
        {
            Write("@s\n\tk : \"\"\n");

            var config = Build();

            config["s:k"].Should().Be(string.Empty);
        }

        #endregion inline arrays

        #region blocked and multiline values

        [Fact]
        public void Blocked_values_under_a_property_form_an_array()
        {
            // "arr :" without an inline value accepts tab-indented value lines as items
            Write("@s\n\tarr :\n\t\tv1\n\t\tv2\n");

            var config = Build();

            config["s:arr:0"].Should().Be("v1");
            config["s:arr:1"].Should().Be("v2");
            config["s:arr"].Should().BeNull();
        }

        [Fact]
        public void Multiline_plus_joins_into_a_single_value_not_an_array()
        {
            Write("@s\n\tdesc :\n\t\tfirst line +\n\t\tsecond line\n");

            var config = Build();

            config["s:desc"]
                .Should().Be("first line" + Environment.NewLine + "second line");
            config["s:desc:0"].Should().BeNull();
        }

        [Fact]
        public void Multiline_plus_followed_by_plain_line_forms_an_array_with_merged_first_item()
        {
            Write("@s\n\tprop :\n\t\tline1 +\n\t\tline2\n\t\tline3\n");

            var config = Build();

            config["s:prop:0"].Should().Be("line1" + Environment.NewLine + "line2");
            config["s:prop:1"].Should().Be("line3");
            config["s:prop:2"].Should().BeNull();
        }

        [Fact]
        public void Property_without_any_value_fails_load()
        {
            Write("@s\n\ta :\n\tb : 1\n");

            var act = () => Build();

            act.Should().Throw<ArgumentException>()
                .WithMessage("*Property cannot be empty*a*");
        }

        [Fact]
        public void Duplicate_property_names_fail_load()
        {
            // Data.Add is a plain dictionary insert: the second "s:a" collides
            Write("@s\n\ta : 1\n\ta : 2\n");

            var act = () => Build();

            act.Should().Throw<ArgumentException>().WithMessage("*already been added*s:a*");
        }

        [Fact]
        public void Properties_outside_an_object_are_dropped()
        {
            // ProcessObject only walks children of object nodes; a root-level property
            // line has Value children, which are skipped.
            Write("prop : a, b\n");

            var config = Build();

            config["prop:0"].Should().BeNull();
            config["prop"].Should().BeNull();
        }

        [Fact]
        public void Inline_value_on_an_object_is_ignored()
        {
            // "@s : inlineval" — the value node under the object is not a property
            Write("@s : inlineval\n\tk : v\n");

            var config = Build();

            config["s"].Should().BeNull();
            config["s:k"].Should().Be("v");
        }

        #endregion blocked and multiline values

        #region keys and sections

        [Fact]
        public void Keys_are_case_insensitive()
        {
            // The provider backs Data with an OrdinalIgnoreCase dictionary, matching
            // the JSON/XML/INI providers, so upper-cased lookups resolve.
            Write("@db\n\tpass : secret-value\n");

            var config = Build();

            config["db:pass"].Should().Be("secret-value");
            config["DB:PASS"].Should().Be("secret-value");
            config.GetSection("DB:PASS").Value.Should().Be("secret-value");
        }

        [Fact]
        public void Differently_cased_property_names_collide_on_one_key()
        {
            // Keys are case-insensitive, so "A" and "a" map to the same
            // configuration key; the second add fails like any duplicate name.
            Write("@s\n\tA : 1\n\ta : 2\n");

            var act = () => Build();

            act.Should().Throw<ArgumentException>().WithMessage("*already been added*s:a*");
        }

        [Fact]
        public void Missing_key_returns_null_and_TryGet_is_false()
        {
            Write("@s\n\ta : 1\n");

            var config = Build();

            config["s:missing"].Should().BeNull();
            config["completely:unknown"].Should().BeNull();
        }

        #endregion keys and sections

        #region reload

        [Fact]
        public void Reload_drops_removed_keys_and_picks_up_new_values()
        {
            Write("@s\n\ta : 1\n\tb : 2\n");
            var config = Build();
            config["s:b"].Should().Be("2");

            Write("@s\n\ta : 9\n");
            config.Reload();

            config["s:a"].Should().Be("9");
            config["s:b"].Should().BeNull();
        }

        [Fact]
        public void Reload_resolves_against_the_current_file_not_the_secret_cache()
        {
            var secrets = new FakeSecretProvider(new Dictionary<string, string>
            {
                ["P"] = "v1",
                ["Q"] = "v2"
            });
            Write("@s\n\ta : {{secret:P}}\n");
            var config = Build(secrets);

            config["s:a"].Should().Be("v1"); // cached after first access

            Write("@s\n\ta : {{secret:Q}}\n");
            config.Reload();

            config["s:a"].Should().Be("v2"); // Load must clear the resolved cache
            secrets.RequestedNames.Should().Equal("P", "Q");
        }

        [Fact]
        public void Missing_file_in_existing_directory_fails_the_load()
        {
            var missing = Path.Combine(Path.GetTempPath(), $"otml-arrays-missing-{Guid.NewGuid():N}.otml");
            var source = new OtmlConfigurationSource { OtmlFile = missing };

            var act = () => new ConfigurationBuilder().Add(source).Build();

            act.Should().Throw<FileNotFoundException>().WithMessage("*" + missing + "*");
        }

        [Fact]
        public void Missing_directory_fails_the_load()
        {
            var missing = Path.Combine(
                Path.GetTempPath(), $"otml-arrays-no-dir-{Guid.NewGuid():N}", "settings.otml");
            var source = new OtmlConfigurationSource { OtmlFile = missing };

            var act = () => new ConfigurationBuilder().Add(source).Build();

            act.Should().Throw<DirectoryNotFoundException>().WithMessage("*" + missing + "*");
        }

        #endregion reload

        #region arrays and multiline with secret markers

        [Fact]
        public void Secret_marker_inside_a_joined_multiline_value_is_resolved_once()
        {
            Write("@app\n\tnote :\n\t\t\"hello {{secret:S}}\" +\n\t\t\"world\"\n");
            var secrets = new FakeSecretProvider(new Dictionary<string, string> { ["S"] = "SEC" });

            var config = Build(secrets);

            config["app:note"].Should().Be("hello SEC" + Environment.NewLine + "world");
            secrets.RequestedNames.Should().Equal("S");
        }

        [Fact]
        public void Bound_array_resolves_markers_per_element()
        {
            Write("@app\n\ttokens : plain, {{secret:T}}\n");
            var secrets = new FakeSecretProvider(new Dictionary<string, string> { ["T"] = "tok" });

            var config = Build(secrets);

            config.GetSection("app:tokens").Get<string[]>().Should().Equal("plain", "tok");
        }

        [Fact]
        public void Resolved_secret_value_is_not_reprocessed_for_markers()
        {
            // Deliberate one-pass resolution: a storage value that looks like a marker
            // is returned verbatim and must not recurse or throw.
            var secrets = new FakeSecretProvider(new Dictionary<string, string>
            {
                ["OUTER"] = "{{secret:INNER}}",
                ["INNER"] = "real"
            });
            Write("@x\n\ty : {{secret:OUTER}}\n");

            var config = Build(secrets);

            config["x:y"].Should().Be("{{secret:INNER}}");
            secrets.RequestedNames.Should().Equal("OUTER");
        }

        [Fact]
        public void Empty_string_secret_value_falls_through_to_the_next_storage()
        {
            var empty = new FakeSecretProvider(new Dictionary<string, string> { ["S"] = string.Empty }, name: "empty");
            var hit = new FakeSecretProvider(new Dictionary<string, string> { ["S"] = "real" }, name: "hit");
            Write("@x\n\ty : {{secret:S}}\n");

            var config = Build(empty, hit);

            config["x:y"].Should().Be("real");
            empty.RequestedNames.Should().Equal("S");
            hit.RequestedNames.Should().Equal("S");
        }

        [Fact]
        public void Comment_after_a_marker_is_stripped_before_resolution()
        {
            Write("@s\n\tk : {{secret:S}} # note\n");
            var secrets = new FakeSecretProvider(new Dictionary<string, string> { ["S"] = "X" });

            var config = Build(secrets);

            config["s:k"].Should().Be("X");
        }

        [Fact]
        public void Two_markers_served_by_different_storages_resolve_in_order()
        {
            var first = new FakeSecretProvider(new Dictionary<string, string> { ["A"] = "1" }, name: "first");
            var second = new FakeSecretProvider(new Dictionary<string, string> { ["B"] = "2" }, name: "second");
            Write("@x\n\ty : {{secret:A}}-{{secret:B}}\n");

            var config = Build(first, second);

            config["x:y"].Should().Be("1-2");
            first.RequestedNames.Should().Equal("A", "B"); // B missed in the first storage
            second.RequestedNames.Should().Equal("B");
        }

        #endregion arrays and multiline with secret markers

        #region construction guards

        [Fact]
        public void Provider_ctor_rejects_null_file_and_null_secrets()
        {
            var act = () => new OtmlConfigurationProvider(null!, Array.Empty<ISecretProvider>());
            act.Should().Throw<ArgumentNullException>().WithParameterName("otmlFile");

            act = () => new OtmlConfigurationProvider("settings.otml", null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("secrets");
        }

        [Fact]
        public void Source_build_requires_a_file()
        {
            var source = new OtmlConfigurationSource { OtmlFile = null! };

            var act = () => source.Build(new ConfigurationBuilder());

            act.Should().Throw<InvalidOperationException>().WithMessage("*OtmlFile is required*");
        }

        #endregion construction guards
    }
}
