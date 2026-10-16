using AlienFruit.Otml.Configuration.Abstractions;
using AlienFruit.Otml.Configuration.Extensions;
using AlienFruit.Otml.Configuration.SecretProviders;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using Xunit;

namespace AlienFruit.Otml.Configuration.Tests
{
    /// <summary>
    /// Covers <see cref="EnvironmentVariableSecretProvider"/> semantics (hit, miss, empty name,
    /// registry name) and its DI registration through
    /// <see cref="EnvironmentVariableServiceCollectionExtensions.AddEnvironmentVariableSecretProvider(IServiceCollection)"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="SecretProviderRegistry"/> is a process-wide static; this class shares the
    /// "SecretProviderRegistry" collection with the other registry-touching tests (xUnit runs
    /// one collection serially) and clears the registry before and after every test.
    /// </remarks>
    [Collection("SecretProviderRegistry")]
    public class EnvironmentVariableSecretProviderTests : IDisposable
    {
        private const string SecretName = "OTML_TEST_ENV_SECRET";

        private readonly string tempFile;

        public EnvironmentVariableSecretProviderTests()
        {
            this.tempFile = Path.Combine(Path.GetTempPath(), $"otml-env-test-{Guid.NewGuid():N}.otml");
            File.WriteAllText(this.tempFile, "@app\n\tpassword : {{secret:OTML_TEST_ENV_SECRET}}\n");
            Environment.SetEnvironmentVariable(SecretName, "from-env");
            SecretProviderRegistry.Clear();
        }

        public void Dispose()
        {
            SecretProviderRegistry.Clear();
            Environment.SetEnvironmentVariable(SecretName, null);
            if (File.Exists(this.tempFile))
                File.Delete(this.tempFile);
        }

        [Fact]
        public void GetSecret_returns_value_of_existing_variable()
        {
            var provider = new EnvironmentVariableSecretProvider();

            provider.GetSecret(SecretName).Should().Be("from-env");
        }

        [Fact]
        public void GetSecret_returns_null_for_missing_variable()
        {
            var provider = new EnvironmentVariableSecretProvider();

            provider.GetSecret("OTML_TEST_ENV_SECRET_NOT_SET").Should().BeNull();
        }

        [Fact]
        public void GetSecret_returns_null_for_empty_name()
        {
            var provider = new EnvironmentVariableSecretProvider();

            provider.GetSecret(string.Empty).Should().BeNull();
            provider.GetSecret("  ").Should().BeNull();
        }

        [Fact]
        public void Name_is_environment()
        {
            var provider = new EnvironmentVariableSecretProvider();

            provider.Name.Should().Be("environment");
        }

        [Fact]
        public void AddEnvironmentVariableSecretProvider_resolves_ISecretProvider_as_environment_variable()
        {
            var services = new ServiceCollection();

            services.AddEnvironmentVariableSecretProvider();

            var provider = services.BuildServiceProvider().GetRequiredService<ISecretProvider>();
            provider.Should().BeOfType<EnvironmentVariableSecretProvider>();
        }

        [Fact]
        public void AddEnvironmentVariableSecretProvider_registers_the_same_instance_in_SecretProviderRegistry()
        {
            var services = new ServiceCollection();

            services.AddEnvironmentVariableSecretProvider();

            var provider = services.BuildServiceProvider().GetRequiredService<ISecretProvider>();
            SecretProviderRegistry.GetAll().Should().Contain(provider);
        }

        [Fact]
        public void AddEnvironmentVariableSecretProvider_resolves_markers_from_the_environment()
        {
            var services = new ServiceCollection();
            services.AddEnvironmentVariableSecretProvider();

            var manager = new ConfigurationManager();
            manager.AddOtmlConfigurationFile(this.tempFile);

            manager["app:password"].Should().Be("from-env");
        }
    }
}
