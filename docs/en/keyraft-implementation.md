# Keyraft secrets storage

The `AlienFruit.Otml.Configuration.Keyraft` package reads secrets from a self-hosted [Keyraft](https://github.com/keyraft/keyrafted) server: `{{secret:<name>}}` markers in OTML configuration values are resolved with a request to Keyraft. The package targets `net8.0`; marker syntax and resolution order are described in the [C# configuration implementation](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/en/configuration-implementation.md).

## Getting started

```c#
var builder = WebApplication.CreateBuilder(args);

// 1. the secret storage
builder.Services.AddKeyraftSecretProvider(options =>
{
	options.BaseUrl = "http://localhost:7200";
	options.Token = "secret-token";
	options.Namespace = "myapp/prod";
});

// 2. the OTML file as configuration (after the storage is registered)
builder.Configuration.AddOtmlConfigurationFile("appsettings.otml");

// 3. bind the section to an options class, as usual
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("database"));

var app = builder.Build();

var password = app.Configuration["database:password"]; // the resolved secret — read by key

// the same value through IOptions — the secret lands in the class property at binding time
var dbOptions = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
var samePassword = dbOptions.Password;
```

Given `appsettings.otml`:

```py
@database
	password : {{secret:DB_PASSWORD}}
```

The options class binds by property name: `Password` ← `password`.

```c#
public class DatabaseOptions
{
	public string Password { get; set; }
}
```

<br/>

## Options

Three fields are required — the registration throws immediately if any of them is missing:

| Property | Required | Default | Description |
|----------|----------|---------|-------------|
| `BaseUrl` | yes | — | absolute `http(s)` URL of the Keyraft server |
| `Token` | yes | — | bearer token with read access to the `Namespace` |
| `Namespace` | yes | — | namespace with the secrets, for example `myapp/prod` |
| `Timeout` | no | 30 seconds | timeout of a single request to Keyraft |
| `ClientTimeout` | no | infinite | timeout of the `HttpClient` created by the registration; must be positive or infinite |
| `Retry` | no | see below | retry policy for transient failures |

A secret is read with a plain GET request, no further configuration is needed:

```
GET {BaseUrl}/v1/kv/{namespace}/{secret-name}
Authorization: Bearer {token}
```

## Retries on failures

Transient failures (server unreachable, timeout, `408`, `429`, `5xx`) are retried automatically with an exponential backoff:

| `Retry` property | Default | Description |
|------------------|---------|-------------|
| `MaxRetryAttempts` | 2 | retries after the initial request (at most `MaxRetryAttempts + 1` requests in total) |
| `InitialDelay` | 200 ms | pause before the first retry |
| `BackoffMultiplier` | 2.0 | factor by which the pause grows for every next retry |
| `MaxDelay` | 5 s | upper bound of the pause |

The delay before retry N (0-based) is `InitialDelay * BackoffMultiplier^N`, capped at `MaxDelay`; there is no jitter.

`404` (the key is not in this storage) and access errors `401`/`403` are not retried: the first is not a failure, the second doesn't get better with retries. Once retries are exhausted the failure surfaces as `KeyraftCommunicationException`: for a transport/timeout failure the message is "Cannot reach…" / "timed out…", for a transient status — the status and body of the last response.

<br/>

## Technical details

### GetSecret / GetSecretAsync behavior

```c#
string? GetSecret(string name)
Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
```

| Keyraft response | Result |
|------------------|--------|
| `200` with `{"value": "..."}` | the value, verbatim (spaces are not trimmed) |
| `404` | `null` — the key is not in this storage, resolution falls through to the next provider |
| empty/whitespace secret name | `null` without calling the network |
| `401` / `403` | `KeyraftCommunicationException` (access denied, `StatusCode` set) |
| `408` / `429` / `5xx` | retried per `Retry`; after exhaustion — like "other status codes" |
| other (non-transient) status codes | `KeyraftCommunicationException` with status and (truncated) server body |
| malformed or empty `200` payload | `KeyraftCommunicationException` |
| timeout / unreachable server | retried per `Retry`, then `KeyraftCommunicationException` |

Cancellation through the caller token aborts resolution immediately, without retries. Only the secret name is URL-escaped; namespace segments are sent as-is (`project/environment`).

### DI registration

Overloads exist for `IServiceCollection` (with `Action<T>` or with a prepared `KeyraftSecretProviderOptions`) and for `IHostApplicationBuilder` (delegating to `Services`).

Each call validates the options immediately, then registers one `KeyraftSecretProvider` instance as a singleton `ISecretProvider` **and** in the `SecretProviderRegistry`. Calling it several times registers several storages (for example, different namespaces); they are tried in registration order.

Remember the registry timing: `AddOtmlConfigurationFile` / `AddOtmlConfiguration` capture the registry at their own call, so register providers before adding the OTML file.

### Constructors

```c#
KeyraftSecretProvider(HttpClient httpClient, KeyraftSecretProviderOptions options)
```

The client is used as-is for the provider lifetime. This is the path used by `services.AddKeyraftSecretProvider`: it creates a standalone client with the `ClientTimeout` timeout (infinite by default), while per-request cancellation is handled by `KeyraftSecretProviderOptions.Timeout`.

```c#
KeyraftSecretProvider(IHttpClientFactory httpClientFactory, KeyraftSecretProviderOptions options)
```

Resolves the HTTP client by name on every request — `KeyraftSecretProvider.HttpClientName` (`"keyraft"`). Use it when the named client is configured in DI (polly handlers, base address, etc.).

Both constructors validate the options immediately. `Name` is `keyraft:{namespace}`, used in resolution errors.
