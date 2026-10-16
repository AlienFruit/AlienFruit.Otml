using AlienFruit.Otml.Configuration.Abstractions;
using AlienFruit.Otml.Configuration.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace AlienFruit.Otml.Configuration.Tests
{
    /// <summary>
    /// Covers the extension-method API: file wiring (regression: a shadowed lambda parameter dropped
    /// <see cref="OtmlConfigurationOptions.OtmlFile"/>, so every call failed at source build),
    /// custom providers, merge order registry-first-then-custom, and options validation.
    /// </summary>
    /// <remarks>
    /// <see cref="SecretProviderRegistry"/> is a process-wide static; this class shares the
    /// "SecretProviderRegistry" collection with the Keyraft DI tests (xUnit runs one collection
    /// serially) and clears the registry before and after every test.
    /// </remarks>
    [Collection("SecretProviderRegistry")]
    public class OtmlConfigurationExtensionsTests : IDisposable
    {
        private readonly string tempFile;

        public OtmlConfigurationExtensionsTests()
        {
            this.tempFile = Path.Combine(Path.GetTempPath(), $"otml-ext-test-{Guid.NewGuid():N}.otml");
            File.WriteAllText(this.tempFile, "@app\n\tplain : v\n\tsec : {{secret:MARKER}}\n");
            SecretProviderRegistry.Clear();
        }

        public void Dispose()
        {
            SecretProviderRegistry.Clear();
            if (File.Exists(this.tempFile))
                File.Delete(this.tempFile);
        }

        [Fact]
        public void AddOtmlConfigurationFile_string_passes_the_file_to_the_source()
        {
            var manager = new ConfigurationManager();

            var act = () => manager.AddOtmlConfigurationFile(this.tempFile);

            act.Should().NotThrow();
            manager["app:plain"].Should().Be("v");
        }

        [Fact]
        public void AddOtmlConfigurationFile_string_resolves_registry_provider()
        {
            SecretProviderRegistry.Register(
                new FakeSecretProvider(new Dictionary<string, string> { ["MARKER"] = "from-registry" }));

            var manager = new ConfigurationManager();
            manager.AddOtmlConfigurationFile(this.tempFile);

            manager["app:sec"].Should().Be("from-registry");
        }

        [Fact]
        public void CustomSecretProviders_resolve_markers()
        {
            var custom = new FakeSecretProvider(
                new Dictionary<string, string> { ["MARKER"] = "from-custom" }, name: "custom");

            var manager = new ConfigurationManager();
            manager.AddOtmlConfigurationFile(o =>
            {
                o.OtmlFile = this.tempFile;
                o.CustomSecretProviders = new ISecretProvider[] { custom };
            });

            manager["app:sec"].Should().Be("from-custom");
            custom.RequestedNames.Should().Equal("MARKER");
        }

        [Fact]
        public void Registry_providers_are_tried_before_custom()
        {
            var fromRegistry = new FakeSecretProvider(
                new Dictionary<string, string> { ["MARKER"] = "registry-value" }, name: "registry");
            var fromCustom = new FakeSecretProvider(
                new Dictionary<string, string> { ["MARKER"] = "custom-value" }, name: "custom");
            SecretProviderRegistry.Register(fromRegistry);

            var manager = new ConfigurationManager();
            manager.AddOtmlConfigurationFile(o =>
            {
                o.OtmlFile = this.tempFile;
                o.CustomSecretProviders = new ISecretProvider[] { fromCustom };
            });

            manager["app:sec"].Should().Be("registry-value");
            fromCustom.RequestedNames.Should().BeEmpty();
        }

        [Fact]
        public void Registry_without_the_key_falls_through_to_custom()
        {
            var miss = new FakeSecretProvider(new Dictionary<string, string>(), name: "registry");
            var hit = new FakeSecretProvider(
                new Dictionary<string, string> { ["MARKER"] = "custom-value" }, name: "custom");
            SecretProviderRegistry.Register(miss);

            var manager = new ConfigurationManager();
            manager.AddOtmlConfigurationFile(o =>
            {
                o.OtmlFile = this.tempFile;
                o.CustomSecretProviders = new ISecretProvider[] { hit };
            });

            manager["app:sec"].Should().Be("custom-value");
            hit.RequestedNames.Should().Equal("MARKER");
        }

        [Fact]
        public void AddOtmlConfiguration_host_overload_resolves_custom_provider()
        {
            var custom = new FakeSecretProvider(
                new Dictionary<string, string> { ["MARKER"] = "host-value" }, name: "custom");

            using var host = new HostBuilder()
                .AddOtmlConfiguration(o =>
                {
                    o.OtmlFile = this.tempFile;
                    o.CustomSecretProviders = new ISecretProvider[] { custom };
                })
                .Build();

            var config = host.Services.GetRequiredService<IConfiguration>();

            config["app:sec"].Should().Be("host-value");
        }

        [Fact]
        public void Null_or_missing_options_are_rejected_at_the_call()
        {
            var manager = new ConfigurationManager();

            var act = () => manager.AddOtmlConfigurationFile((Action<OtmlConfigurationOptions>)null);
            act.Should().Throw<ArgumentNullException>().WithParameterName("editOptions");

            act = () => manager.AddOtmlConfigurationFile(o => o.OtmlFile = " ");
            act.Should().Throw<ArgumentException>().WithMessage("*OtmlFile must be specified*");

            act = () => manager.AddOtmlConfigurationFile(o =>
            {
                o.OtmlFile = this.tempFile;
                o.CustomSecretProviders = null!;
            });
            act.Should().Throw<ArgumentNullException>().WithParameterName("CustomSecretProviders");
        }
    }
}
