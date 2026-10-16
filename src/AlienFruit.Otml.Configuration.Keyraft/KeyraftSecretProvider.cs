using AlienFruit.Otml.Configuration.Abstractions;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AlienFruit.Otml.Configuration.Keyraft
{
    /// <summary>
    /// <see cref="ISecretProvider"/> backed by a self-hosted Keyraft server
    /// (https://github.com/keyraft/keyrafted), resolved through its HTTP/JSON API.
    /// </summary>
    public class KeyraftSecretProvider : ISecretProvider
    {
        /// <summary>
        /// HTTP client name registered by <see cref="KeyraftServiceCollectionExtensions.AddKeyraftSecretProvider"/>.
        /// </summary>
        public const string HttpClientName = "keyraft";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IHttpClientFactory? httpClientFactory;
        private readonly HttpClient? ownedClient;
        private readonly KeyraftSecretProviderOptions options;

        /// <inheritdoc />
        public string Name => $"keyraft:{this.options.Namespace}";

        public KeyraftSecretProvider(IHttpClientFactory httpClientFactory, KeyraftSecretProviderOptions options)
        {
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.options.Validate();
        }

        /// <summary>
        /// Standalone constructor for non-DI usage; the provider owns no factory and
        /// uses the given client as-is for its whole lifetime.
        /// </summary>
        public KeyraftSecretProvider(HttpClient httpClient, KeyraftSecretProviderOptions options)
        {
            this.ownedClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.options.Validate();
        }

        private HttpClient CreateClient()
            => this.httpClientFactory?.CreateClient(HttpClientName) ?? this.ownedClient!;

        /// <inheritdoc />
        public string? GetSecret(string name)
        {
            // ConfigurationProvider.Load() is synchronous; every internal await uses ConfigureAwait(false),
            // so the sync-over-async bridge cannot deadlock on a caller synchronization context.
            return GetSecretAsync(name, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Reads the secret value from Keyraft asynchronously.
        /// </summary>
        /// <returns>The secret value, or <see langword="null"/> when the name is empty
        /// or the key does not exist in the configured namespace (HTTP 404).</returns>
        /// <exception cref="KeyraftCommunicationException">Auth, transport, timeout or malformed-payload failure.</exception>
        public async Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var response = await SendGetAsync(name, cancellationToken).ConfigureAwait(false);

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                await EnsureSuccessAsync(response, name, cancellationToken).ConfigureAwait(false);

                var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return ParseEntry(payload, name);
            }
        }

        private async Task<HttpResponseMessage> SendGetAsync(string name, CancellationToken cancellationToken)
        {
            var client = CreateClient();
            var requestUri = BuildRequestUri(this.options, name, client.BaseAddress);

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.options.Token);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(this.options.Timeout);

            try
            {
                return await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeoutCts.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new KeyraftCommunicationException(
                    $"Keyraft request for the secret '{name}' timed out after {this.options.Timeout}.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new KeyraftCommunicationException($"Cannot reach the Keyraft server at '{this.options.BaseUrl}'.", ex);
            }
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string name, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
                return;

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                throw new KeyraftCommunicationException(
                    $"Keyraft denied access to the secret '{name}' (HTTP {(int)response.StatusCode}). " +
                    "Check the token and its namespace permissions.")
                {
                    StatusCode = (int)response.StatusCode
                };

            var errorPayload = await ReadErrorPayloadAsync(response, cancellationToken).ConfigureAwait(false);
            throw new KeyraftCommunicationException(
                $"Keyraft responded with an unexpected status {(int)response.StatusCode} ({response.StatusCode}) for the secret '{name}'. {errorPayload}")
            {
                StatusCode = (int)response.StatusCode
            };
        }

        private string ParseEntry(string payload, string name)
        {
            KeyraftEntry? entry;
            try
            {
                entry = JsonSerializer.Deserialize<KeyraftEntry>(payload, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new KeyraftCommunicationException(
                    $"Keyraft returned a payload that cannot be parsed as a secret entry for the key '{name}'.", ex);
            }

            if (entry is null || string.IsNullOrEmpty(entry.Value))
                throw new KeyraftCommunicationException(
                    $"Keyraft returned an empty payload for the secret '{name}' in the namespace '{this.options.Namespace}'.");

            return entry.Value;
        }

        private static async Task<string> ReadErrorPayloadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return string.IsNullOrEmpty(body) ? string.Empty : $"Server response: {Truncate(body)}";
            }
            catch
            {
                return "Server response body is unavailable.";
            }
        }

        // Error payloads may echo request data; keep messages bounded.
        private static string Truncate(string value)
            => value.Length <= 512 ? value : value.Substring(0, 512) + "...";

        /// <summary>
        /// Builds <c>/v1/kv/{namespace}/{key}</c>; namespace segments stay literal (Keyraft grammar
        /// is <c>project/environment</c>: <c>[a-zA-Z0-9_-]</c> per segment), only the secret name is escaped.
        /// </summary>
        internal static Uri BuildRequestUri(KeyraftSecretProviderOptions options, string secretName, Uri? clientBaseAddress)
        {
            var segments = options.Namespace.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var path = "/v1/kv/" + string.Join("/", segments) + "/" + Uri.EscapeDataString(secretName);

            if (clientBaseAddress is not null)
                return new Uri(clientBaseAddress, path);

            var baseUrl = options.BaseUrl.TrimEnd('/');
            return new Uri(baseUrl + path, UriKind.Absolute);
        }

        private sealed class KeyraftEntry
        {
            [JsonPropertyName("key")]
            public string? Key { get; set; }

            [JsonPropertyName("value")]
            public string? Value { get; set; }

            [JsonPropertyName("type")]
            public string? Type { get; set; }
        }
    }
}
