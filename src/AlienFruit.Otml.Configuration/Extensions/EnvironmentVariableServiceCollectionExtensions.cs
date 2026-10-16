using AlienFruit.Otml.Configuration.Abstractions;
using AlienFruit.Otml.Configuration.SecretProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace AlienFruit.Otml.Configuration.Extensions
{
    /// <summary>
    /// DI registration extensions for <see cref="EnvironmentVariableSecretProvider"/>.
    /// </summary>
    public static class EnvironmentVariableServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="EnvironmentVariableSecretProvider"/> as a singleton <see cref="ISecretProvider"/>
        /// in the DI container. The provider is registered in the <see cref="SecretProviderRegistry"/>
        /// for use during configuration loading. Each call adds one more storage to the
        /// <see cref="IEnumerable{ISecretProvider}"/> collection.
        /// </summary>
        /// <example>
        /// <code>
        /// var builder = WebApplication.CreateBuilder(args);
        /// builder.AddEnvironmentVariableSecretProvider();
        /// builder.Configuration.AddOtmlConfigurationFile("settings.otml");
        /// </code>
        /// </example>
        public static IServiceCollection AddEnvironmentVariableSecretProvider(this IServiceCollection services)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));

            var provider = new EnvironmentVariableSecretProvider();

            // Register in static registry for configuration resolution
            SecretProviderRegistry.Register(provider);

            // Register in DI for consumers
            services.AddSingleton<ISecretProvider>(provider);

            return services;
        }

        /// <summary>
        /// Registers <see cref="EnvironmentVariableSecretProvider"/> in the dependency injection container
        /// and secret provider registry for <see cref="IHostApplicationBuilder"/>
        /// (works with both IHostBuilder and WebApplicationBuilder).
        /// </summary>
        public static IHostApplicationBuilder AddEnvironmentVariableSecretProvider(this IHostApplicationBuilder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.AddEnvironmentVariableSecretProvider();
            return builder;
        }
    }
}
