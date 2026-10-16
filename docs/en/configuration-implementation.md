

# C# configuration implementation

<br/>

The `AlienFruit.Otml.Configuration` package loads an OTML file as a standard `Microsoft.Extensions.Configuration` source (`IConfiguration`).
The `AlienFruit.Otml.Configuration.Keyraft` package adds resolution of `{{secret:<name>}}` markers through a self-hosted [Keyraft](https://github.com/keyraft/keyrafted) server.

`AlienFruit.Otml.Configuration` targets `netstandard2.1`, the Keyraft adapter targets `net8.0`.

Extension methods live in the `AlienFruit.Otml.Configuration.Extensions` namespace, `ISecretProvider` —
in `AlienFruit.Otml.Configuration.Abstractions`, the built-in environment-variable provider
`EnvironmentVariableSecretProvider` — in `AlienFruit.Otml.Configuration.SecretProviders`,
its registration `AddEnvironmentVariableSecretProvider` — in the same `AlienFruit.Otml.Configuration.Extensions` as the configuration methods.

<br/>

## Adding an OTML file as a configuration source

### WebApplicationBuilder / ConfigurationManager

```c#
using AlienFruit.Otml.Configuration.Extensions;

builder.Configuration.AddOtmlConfigurationFile("settings.otml");
```

### IHostBuilder

```c#
Host.CreateDefaultBuilder(args)
	.AddOtmlConfiguration("settings.otml")
	.ConfigureServices((context, services) => { /* ... */ })
	.Build();
```

Both methods have an overload that takes `Action<OtmlConfigurationOptions>` — use it to plug in your own
secret storages.

### Custom secret providers

```c#
using AlienFruit.Otml.Configuration.Abstractions; // ISecretProvider
using AlienFruit.Otml.Configuration.Extensions;
using AlienFruit.Otml.Configuration.SecretProviders; // EnvironmentVariableSecretProvider

builder.Configuration.AddOtmlConfigurationFile(o =>
{
	o.OtmlFile = "settings.otml";
	o.CustomSecretProviders = new ISecretProvider[] { new EnvironmentVariableSecretProvider() };
});

// IHostBuilder works the same way:
hostBuilder.AddOtmlConfiguration(o =>
{
	o.OtmlFile = "settings.otml";
	o.CustomSecretProviders = myProviders;
});
```

**OtmlConfigurationOptions properties**

| Property | Description |
|----------|-------------|
| `OtmlFile` | Path to the OTML file. Required: not set — `ArgumentException` |
| `CustomSecretProviders` | `ISecretProvider[]` storages tried after the ones from `SecretProviderRegistry`. Default: empty array; assigning `null` throws `ArgumentNullException` |

Resolution order: registry providers first (in registration order), then custom providers; the first
non-empty answer wins. The complete provider list is captured **at the call time**, so register providers
(`AddKeyraftSecretProvider`, `AddEnvironmentVariableSecretProvider`, `SecretProviderRegistry.Register`) before calling the extension method.

<br/>

## How OTML maps to configuration keys

Nested objects become colon-separated key paths, array values of a property get numeric indexes:

```py
@database
	password : secret-value
	endpoints : first, second
	@pool
		size : 10
```

| Key | Value |
|-----|-------|
| `database:password` | `secret-value` |
| `database:endpoints:0` | `first` |
| `database:endpoints:1` | `second` |
| `database:pool:size` | `10` |

The result is an ordinary `IConfiguration`: sections and binding (`config.GetSection("database").Get<DatabaseOptions>()`)
work as with `appsettings.json`. `IConfigurationRoot.Reload()` re-parses the OTML file and clears the resolved-secret
cache, so edited files and rotated secrets are picked up on the next read.

<br/>

## Secret markers

Any property value may contain one or more markers:

```py
@database
	password : {{secret:DB_PASSWORD}}
	connString : "Server=db;Password={{secret:DB_PASSWORD}};Pool=5"
```

Markers are resolved **lazily**: `Load()` only parses the file, the substitution happens on the first read of the value
(`config["database:password"]`, binding, etc.) and the resolved value is cached. The tag name is case-insensitive,
spaces inside the marker are allowed (`{{ SECRET:DB_PASSWORD }}`). Markers with unknown tags (for example `{{vault:key}}`)
and values without markers are left untouched.

**ISecretProvider interface**

```c#
public interface ISecretProvider
{
	string Name { get; }             // storage name used in error messages
	string? GetSecret(string name);  // null when this storage does not contain the key
}
```

A missing secret is not an error: the provider returns `null` and resolution moves on to the next storage.
A provider should throw only for real failures (transport, auth, timeout), which abort the resolution.

**Error semantics**

| Situation | Result |
|-----------|--------|
| No provider registered / secret in no storage | `SecretNotFoundException` (contains `SecretName`, the configuration key and the searched storage names) |
| Empty marker `{{secret: }}` | `ArgumentException` naming the configuration key |

**SecretProviderRegistry**

Providers from the registry are merged with `CustomSecretProviders` when `AddOtmlConfigurationFile` /
`AddOtmlConfiguration` is called, and stay active for the created source:

```c#
SecretProviderRegistry.Register(new EnvironmentVariableSecretProvider());
```

```c#
static void Register(ISecretProvider provider)   // throws ArgumentNullException for null
static ISecretProvider[] GetAll()                // registration order
```

One timing note for `IHostBuilder`: `AddOtmlConfiguration` captures the registry at its own call, while
`ConfigureServices` runs later, during `Build()` — so `services.AddKeyraftSecretProvider` inside
`ConfigureServices` is too late to affect configuration resolution (the provider is still injected into services).
On `WebApplicationBuilder`/`ConfigurationManager` the natural order — register providers, then add the file — works.

<br/>

## Environment variable secrets storage

The built-in `EnvironmentVariableSecretProvider` resolves the `{{secret:NAME}}` marker to the value of
the environment variable `NAME`. A missing variable is not an error: the provider returns `null` and
resolution moves on to the next storage. Variable names follow the OS semantics: case-insensitive on
Windows, case-sensitive on Linux. The storage name in error messages is `environment`.

DI registration mirrors Keyraft: the provider is added to the `SecretProviderRegistry` (for marker
resolution in configuration) and injected into DI as an `ISecretProvider` singleton. Overloads are
available for `IServiceCollection` and `IHostApplicationBuilder`:

```c#
using AlienFruit.Otml.Configuration.Extensions; // AddEnvironmentVariableSecretProvider

// WebApplicationBuilder / IHostApplicationBuilder:
builder.AddEnvironmentVariableSecretProvider();

// IServiceCollection:
services.AddEnvironmentVariableSecretProvider();
```

<br/>

## Keyraft secrets storage

The Keyraft adapter — `KeyraftSecretProvider`, connection options, HTTP behavior and DI registration —
is documented separately: [Keyraft secrets storage](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/en/keyraft-implementation.md).
