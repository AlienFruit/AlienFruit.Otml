

# Keyraft secrets storage

<br/>

The `AlienFruit.Otml.Configuration.Keyraft` package implements `ISecretProvider` over the HTTP/JSON API of a
self-hosted [Keyraft](https://github.com/keyraft/keyrafted) server, so `{{secret:<name>}}` markers in OTML
configuration values are resolved from Keyraft storages. The package targets `net8.0`; marker syntax, resolution
order and the `SecretProviderRegistry` are described in the
[C# configuration implementation](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/en/configuration-implementation.md).

<br/>

## KeyraftSecretProvider

`ISecretProvider` implementation over the Keyraft HTTP/JSON API. A secret is read with

```
GET {BaseUrl}/v1/kv/{namespace}/{secret-name}
Authorization: Bearer {token}
```

Only the secret name is URL-escaped; namespace segments are sent as-is (`project/environment`).

**Constructors**

```c#
KeyraftSecretProvider(IHttpClientFactory httpClientFactory, KeyraftSecretProviderOptions options)
```

Resolves the HTTP client by name on every request — `KeyraftSecretProvider.HttpClientName` (`"keyraft"`). Use it when
the named client is configured in DI (polly handlers, base address, etc.).

```c#
KeyraftSecretProvider(HttpClient httpClient, KeyraftSecretProviderOptions options)
```

The client is used as-is for the provider lifetime. This is the path used by `services.AddKeyraftSecretProvider`
(see below), which creates a standalone client with infinite timeout; per-request cancellation is handled by
`KeyraftSecretProviderOptions.Timeout`.

Both constructors validate the options immediately. `Name` is `keyraft:{namespace}`, used in resolution errors.

**KeyraftSecretProviderOptions**

| Property | Required | Description |
|----------|----------|-------------|
| `BaseUrl` | yes | absolute `http(s)` URL of the Keyraft server |
| `Token` | yes | bearer token with read access to the `Namespace` |
| `Namespace` | yes | namespace with the secrets, for example `myapp/prod` |
| `Timeout` | no | request timeout, default 30 seconds |

**GetSecret / GetSecretAsync behavior**

| Keyraft response | Result |
|------------------|--------|
| `200` with `{"value": "..."}` | the value, verbatim (spaces are not trimmed) |
| `404` | `null` — the key is not in this storage, resolution falls through to the next provider |
| empty/whitespace secret name | `null` without calling the network |
| `401` / `403` | `KeyraftCommunicationException` (access denied, `StatusCode` set) |
| other status codes | `KeyraftCommunicationException` with status and (truncated) server body |
| malformed or empty `200` payload | `KeyraftCommunicationException` |
| timeout / unreachable server | `KeyraftCommunicationException` |

```c#
string? GetSecret(string name)
Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
```

<br/>

## DI registration

```c#
services.AddKeyraftSecretProvider(options =>
{
	options.BaseUrl = "http://localhost:7200";
	options.Token = builder.Configuration["Keyraft:Token"];
	options.Namespace = "myapp/prod";
});
```

The overloads exist for `IServiceCollection` (with `Action<T>` or with a prepared
`KeyraftSecretProviderOptions`) and for `IHostApplicationBuilder` (delegating to `Services`).

Each call validates the options immediately, then registers one `KeyraftSecretProvider` instance as a singleton
`ISecretProvider` **and** in the `SecretProviderRegistry`. Calling it several times registers several storages
(for example, different namespaces); they are tried in registration order.

Remember the registry timing: `AddOtmlConfigurationFile` / `AddOtmlConfiguration` capture the
registry at their own call, so register providers before adding the OTML file.

<br/>

## Full example

```c#
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKeyraftSecretProvider(options =>
{
	options.BaseUrl = "http://localhost:7200";
	options.Token = "secret-token";
	options.Namespace = "myapp/prod";
});
builder.Configuration.AddOtmlConfigurationFile("appsettings.otml");

var app = builder.Build();

var password = app.Configuration["database:password"]; // the resolved secret
```

Given `appsettings.otml`:

```py
@database
	password : {{secret:DB_PASSWORD}}
```
