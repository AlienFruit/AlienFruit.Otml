using AlienFruit.Otml.Configuration.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Configuration
{
    /// <summary>
    /// Process-wide registry of secret providers used to resolve <c>{{secret:&lt;name&gt;}}</c>
    /// markers in OTML configuration values.
    /// </summary>
    /// <remarks>
    /// Providers are registered before the application reads configuration values.
    /// Configuration sources are built before the DI container, so instead of resolving
    /// providers from DI at configuration build time, they are registered here and resolved
    /// lazily when values containing markers are first accessed.
    /// </remarks>
    public static class SecretProviderRegistry
    {
        private static readonly List<ISecretProvider> providers = new List<ISecretProvider>();

        /// <summary>
        /// Registers a secret provider in the registry. Providers are tried in registration order
        /// when resolving <c>{{secret:...}}</c> markers.
        /// </summary>
        public static void Register(ISecretProvider provider)
        {
            if (provider is null)
                throw new ArgumentNullException(nameof(provider));

            providers.Add(provider);
        }

        /// <summary>
        /// Returns all registered providers in registration order.
        /// </summary>
        public static ISecretProvider[] GetAll() => providers.ToArray();

        /// <summary>
        /// Removes all registered providers. Intended for tests: the registry is process-wide
        /// and otherwise accumulates providers for the whole run.
        /// </summary>
        internal static void Clear() => providers.Clear();
    }
}
