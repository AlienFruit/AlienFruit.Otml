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
        }
    }
}
