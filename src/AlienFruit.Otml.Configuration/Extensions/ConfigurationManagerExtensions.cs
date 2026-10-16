using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace AlienFruit.Otml.Configuration.Extensions
{
    /// <summary>
    /// Extension methods for adding an OTML configuration file to <see cref="ConfigurationManager"/>.
    /// </summary>
    public static class ConfigurationManagerExtensions
    {
        /// <summary>
        /// Adds an OTML configuration file as a source of this <see cref="ConfigurationManager"/>.
        /// <c>{{secret:&lt;name&gt;}}</c> markers in property values are resolved from the providers
        /// registered in the <see cref="SecretProviderRegistry"/>.
        /// </summary>
        /// <param name="manager">The configuration manager to add the source to.</param>
        /// <param name="configurationFile">Path to the OTML file. Required.</param>
        /// <returns>The same <see cref="ConfigurationManager"/> for chaining.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="configurationFile"/> is <see langword="null"/> or whitespace.
        /// </exception>
        public static ConfigurationManager AddOtmlConfigurationFile(this ConfigurationManager manager, string configurationFile)
            => AddOtmlConfigurationFile(manager, options => options.OtmlFile = configurationFile);

        /// <summary>
        /// Adds an OTML configuration file as a source of this <see cref="ConfigurationManager"/>
        /// with full control over the options.
        /// </summary>
        /// <param name="manager">The configuration manager to add the source to.</param>
        /// <param name="editOptions">
        /// Configures the <see cref="OtmlConfigurationOptions"/>: the OTML file path and optional
        /// <see cref="OtmlConfigurationOptions.CustomSecretProviders"/>. Custom providers are appended to the
        /// ones already present in the <see cref="SecretProviderRegistry"/> (registry first), so this overload
        /// is the way to plug in your own storages without DI.
        /// The providers are captured at the call time; the file itself is read when the source is built.
        /// </param>
        /// <returns>The same <see cref="ConfigurationManager"/> for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="editOptions"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <see cref="OtmlConfigurationOptions.OtmlFile"/> was not set, or
        /// <see cref="OtmlConfigurationOptions.CustomSecretProviders"/> was set to <see langword="null"/>.
        /// </exception>
        /// <example>
        /// <code>
        /// builder.Configuration.AddOtmlConfigurationFile(o =>
        /// {
        ///     o.OtmlFile = "settings.otml";
        ///     o.CustomSecretProviders = new ISecretProvider[] { new EnvironmentVariableSecretProvider() };
        /// });
        /// </code>
        /// </example>
        public static ConfigurationManager AddOtmlConfigurationFile(this ConfigurationManager manager, Action<OtmlConfigurationOptions> editOptions)
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

            manager.Add<OtmlConfigurationSource>(source =>
            {
                source.OtmlFile = options.OtmlFile;
                source.SecretProviders = secretProviders;
            });
            return manager;
        }
    }
}
