using AlienFruit.Otml.Configuration.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;

namespace AlienFruit.Otml.Configuration.Keyraft
{
    public static class KeyraftServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="KeyraftSecretProvider"/> as a singleton <see cref="ISecretProvider"/>
        /// in the DI container. The provider is registered in the <see cref="SecretProviderRegistry"/>
        /// for use during configuration loading. Each call adds one more storage to the
        /// <see cref="IEnumerable{ISecretProvider}"/> collection, so several Keyraft namespaces
        /// can be registered; consumers requesting the collection receive them all.
        /// </summary>
        /// <example>
        /// <code>
        /// Host.CreateDefaultBuilder(args)
        ///     .AddOtmlConfiguration("config.otml")
        ///     .ConfigureServices((context, services) =>
        ///     {
        ///         services.AddKeyraftSecretProvider(options =>
        ///         {
        ///             options.BaseUrl = "http://localhost:7200";
        ///             options.Token = context.Configuration["Keyraft:Token"];
        ///             options.Namespace = "myapp/prod";
        ///         });
        ///     })
        ///     .Build();
        /// </code>
        /// </example>
        public static IServiceCollection AddKeyraftSecretProvider(
            this IServiceCollection services,
            Action<KeyraftSecretProviderOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            var providerOptions = new KeyraftSecretProviderOptions();
            configure(providerOptions);

            return AddProviderCore(services, providerOptions);
        }

        /// <summary>
        /// Registers <see cref="KeyraftSecretProvider"/> as <see cref="ISecretProvider"/>
        /// using an already-configured options instance.
        /// </summary>
        public static IServiceCollection AddKeyraftSecretProvider(
            this IServiceCollection services,
            KeyraftSecretProviderOptions options)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(options);

            // Snapshot: the registered provider keeps its own clone, so later mutation
            // of the caller's instance cannot change resolution behavior.
            return AddProviderCore(services, options.Clone());
        }

        private static IServiceCollection AddProviderCore(
            IServiceCollection services,
            KeyraftSecretProviderOptions providerOptions)
        {
            providerOptions.Validate();

            var client = CreateStandaloneClient(providerOptions);
            var provider = new KeyraftSecretProvider(client, providerOptions);

            // Register in static registry for configuration resolution
            SecretProviderRegistry.Register(provider);

            // Register in DI for consumers
            services.AddSingleton<ISecretProvider>(provider);

            return services;
        }

        /// <summary>
        /// Registers <see cref="KeyraftSecretProvider"/> in the dependency injection container
        /// and secret provider registry for <see cref="IHostApplicationBuilder"/>
        /// (works with both IHostBuilder and WebApplicationBuilder).
        /// </summary>
        /// <example>
        /// <code>
        /// var builder = WebApplication.CreateBuilder(args);
        /// builder.Configuration.AddOtmlConfigurationFile("settings.otml");
        /// builder.AddKeyraftSecretProvider(opts =>
        /// {
        ///     opts.BaseUrl = "http://localhost:7200";
        ///     opts.Token = builder.Configuration["Keyraft:Token"];
        ///     opts.Namespace = "myapp/prod";
        /// });
        /// var app = builder.Build();
        /// </code>
        /// </example>
        public static IHostApplicationBuilder AddKeyraftSecretProvider(
            this IHostApplicationBuilder builder,
            Action<KeyraftSecretProviderOptions> configure)
        {
            builder.Services.AddKeyraftSecretProvider(configure);
            return builder;
        }

        /// <summary>
        /// Registers <see cref="KeyraftSecretProvider"/> in the dependency injection container
        /// using an already-configured options instance.
        /// </summary>
        public static IHostApplicationBuilder AddKeyraftSecretProvider(
            this IHostApplicationBuilder builder,
            KeyraftSecretProviderOptions options)
        {
            builder.Services.AddKeyraftSecretProvider(options);
            return builder;
        }

        // No BaseAddress: BuildRequestUri composes the full URL from options.BaseUrl.
        // Client lifetime timeout comes from options.ClientTimeout (default infinite);
        // individual requests are bounded by options.Timeout via a linked cancellation token.
        internal static HttpClient CreateStandaloneClient(KeyraftSecretProviderOptions options)
            => new() { Timeout = options.ClientTimeout };
    }
}
