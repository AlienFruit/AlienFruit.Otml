using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;

namespace AlienFruit.Otml.Configuration.Extensions
{
    /// <summary>
    /// Extension methods for adding an OTML configuration file to <see cref="IHostBuilder"/>.
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Adds an OTML configuration file as a source of the host configuration.
        /// <c>{{secret:&lt;name&gt;}}</c> markers in property values are resolved from the providers
        /// registered in the <see cref="SecretProviderRegistry"/>.
        /// </summary>
        /// <remarks>
        /// The provider list (from <see cref="SecretProviderRegistry"/> plus the options' custom providers)
        /// is captured at this call, so register providers before calling this method.
        /// The OTML file itself is read later, when the host configuration is built.
        /// </remarks>
        /// <param name="builder">The host builder to add the source to.</param>
        /// <param name="configurationFile">Path to the OTML file. Required.</param>
        /// <returns>The same <see cref="IHostBuilder"/> for chaining.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="configurationFile"/> is <see langword="null"/> or whitespace.
        /// </exception>
        public static IHostBuilder AddOtmlConfiguration(this IHostBuilder builder, string configurationFile)
            => AddOtmlConfiguration(builder, options => options.OtmlFile = configurationFile);

        /// <summary>
        /// Adds an OTML configuration file as a source of the host configuration with full control
        /// over the options.
        /// </summary>
        /// <remarks>
        /// <paramref name="editOptions"/> runs immediately, and the resulting provider list
        /// (<see cref="SecretProviderRegistry"/> first, then <see cref="OtmlConfigurationOptions.CustomSecretProviders"/>)
        /// is captured at that moment; the OTML file itself is read when the host configuration is built.
        /// </remarks>
        /// <param name="builder">The host builder to add the source to.</param>
        /// <param name="editOptions">
        /// Configures the <see cref="OtmlConfigurationOptions"/>: the OTML file path and optional
        /// <see cref="OtmlConfigurationOptions.CustomSecretProviders"/>. Custom providers are appended to the
        /// ones already present in the <see cref="SecretProviderRegistry"/>, so this overload is the way to
        /// plug in your own storages without DI.
        /// </param>
        /// <returns>The same <see cref="IHostBuilder"/> for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="editOptions"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <see cref="OtmlConfigurationOptions.OtmlFile"/> was not set, or
        /// <see cref="OtmlConfigurationOptions.CustomSecretProviders"/> was set to <see langword="null"/>.
        /// </exception>
        /// <example>
        /// <code>
        /// hostBuilder.AddOtmlConfiguration(o =>
        /// {
        ///     o.OtmlFile = "settings.otml";
        ///     o.CustomSecretProviders = new ISecretProvider[] { new EnvironmentVariableSecretProvider() };
        /// });
        /// </code>
        /// </example>
        public static IHostBuilder AddOtmlConfiguration(this IHostBuilder builder, Action<OtmlConfigurationOptions> editOptions)
        {
            if (editOptions == null)
                throw new ArgumentNullException(nameof(editOptions));

            var options = new OtmlConfigurationOptions();
            editOptions.Invoke(options);

            if (string.IsNullOrWhiteSpace(options.OtmlFile))
                throw new ArgumentException("OtmlFile must be specified.", nameof(options.OtmlFile));

            if (options.CustomSecretProviders == null)
                throw new ArgumentNullException(nameof(options.CustomSecretProviders));

            var secretProviders = SecretProviderRegistry
                .GetAll()
                .Concat(options.CustomSecretProviders)
                .ToArray();

            return builder.ConfigureAppConfiguration((context, config) =>
            {
                config.Sources.Add(
                    new OtmlConfigurationSource
                    {
                        OtmlFile = options.OtmlFile,
                        SecretProviders = secretProviders
                    });
            });
        }
    }
}
