using System;

namespace AlienFruit.Otml.Configuration.Keyraft
{
    /// <summary>
    /// Connection options for <see cref="KeyraftSecretProvider"/>.
    /// </summary>
    public class KeyraftSecretProviderOptions
    {
        /// <summary>
        /// Keyraft base address, for example <c>http://localhost:7200</c>. Required.
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Bearer token with read access to the <see cref="Namespace"/>. Required.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Keyraft namespace where the secrets are stored, for example <c>myapp/prod</c>. Required.
        /// Secret names from <c>{{secret:name}}</c> markers are resolved as keys inside this namespace.
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// Request timeout. Default: 30 seconds.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Timeout of the standalone <see cref="System.Net.Http.HttpClient"/> created by the DI
        /// registration path (<c>AddKeyraftSecretProvider</c>). Default:
        /// <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> — individual requests are bounded
        /// by <see cref="Timeout"/>; this value caps the whole client lifetime per request round-trip
        /// and matters when a custom handler pipeline can hang outside the cancellation token.
        /// Must be a positive <see cref="TimeSpan"/> or <see cref="System.Threading.Timeout.InfiniteTimeSpan"/>.
        /// </summary>
        public TimeSpan ClientTimeout { get; set; } = System.Threading.Timeout.InfiniteTimeSpan;

        /// <summary>
        /// Retry policy for transient Keyraft failures (HTTP 408/429/5xx, transport errors,
        /// request timeouts). Assigning <see langword="null"/> fails validation.
        /// </summary>
        public KeyraftRetryOptions Retry { get; set; } = new KeyraftRetryOptions();

        /// <summary>
        /// Memberwise snapshot used by the options overload of
        /// <see cref="KeyraftServiceCollectionExtensions.AddKeyraftSecretProvider(IServiceCollection, KeyraftSecretProviderOptions)"/>:
        /// later mutation of the caller's instance does not leak into the registered provider.
        /// </summary>
        internal KeyraftSecretProviderOptions Clone()
        {
            var clone = (KeyraftSecretProviderOptions)MemberwiseClone();
            clone.Retry = Retry?.Clone()!; // null stays null: Validate reports it as required
            return clone;
        }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.BaseUrl))
                throw new InvalidOperationException($"'{nameof(KeyraftSecretProviderOptions)}.{nameof(this.BaseUrl)}' is required");

            if (!Uri.TryCreate(this.BaseUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new InvalidOperationException(
                    $"'{nameof(KeyraftSecretProviderOptions)}.{nameof(this.BaseUrl)}' must be an absolute http(s) URL");

            if (string.IsNullOrWhiteSpace(this.Token))
                throw new InvalidOperationException($"'{nameof(KeyraftSecretProviderOptions)}.{nameof(this.Token)}' is required");

            if (string.IsNullOrWhiteSpace(this.Namespace))
                throw new InvalidOperationException($"'{nameof(KeyraftSecretProviderOptions)}.{nameof(this.Namespace)}' is required");

            if (this.ClientTimeout != System.Threading.Timeout.InfiniteTimeSpan && this.ClientTimeout <= TimeSpan.Zero)
                throw new InvalidOperationException(
                    $"'{nameof(KeyraftSecretProviderOptions)}.{nameof(this.ClientTimeout)}' must be positive or Timeout.InfiniteTimeSpan");

            if (this.Retry is null)
                throw new InvalidOperationException(
                    $"'{nameof(KeyraftSecretProviderOptions)}.{nameof(this.Retry)}' is required");

            this.Retry.Validate();
        }
    }
}
