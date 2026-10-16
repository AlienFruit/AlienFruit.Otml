# C# configuration implementation

The `AlienFruit.Otml.Configuration` package loads an OTML file as a standard `Microsoft.Extensions.Configuration` source (`IConfiguration`). You read it exactly like `appsettings.json` — and secret values are pulled from an external storage at read time.

## Quick start

1. A file `settings.otml`:

```py
@database
	connString : "Server=db;Pool=5"
```

2. Hook it up to the application:

```c#
using AlienFruit.Otml.Configuration.Extensions;

builder.Configuration.AddOtmlConfigurationFile("settings.otml");
```

3. Read it as usual — a single value:

```c#
var connString = configuration["database:connString"];
```

Or bind the whole section with the familiar `IOptions` pattern:

```c#
public class DatabaseOptions
{
	public string ConnString { get; set; }
}
```

Wiring and use:

```c#
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("database"));

// then inject into your service
public MyService(IOptions<DatabaseOptions> options)
{
	var connString = options.Value.ConnString;
}
```

That's it. Secrets next, then the technical details.

<br/>

## Secrets right in the configuration

Passwords and tokens don't have to live in the file — in OTML they are written as `{{secret:<name>}}` markers:

```py
@database
	password : {{secret:DB_PASSWORD}}
	connString : "Server=db;Password={{secret:DB_PASSWORD}};Pool=5"
```

When the value is read, the marker is replaced with the real secret from a connected storage — whether you read by key (`config["database:password"]`) or bind the section to an `IOptions<DatabaseOptions>`: the secret lands in the class property just like an ordinary value. If no storage is connected or the secret is not found, the read fails with `SecretNotFoundException` — you will never silently get an empty password.

**Keyraft.** A secret storage backed by a self-hosted [Keyraft](https://github.com/keyraft/keyrafted) server. One call before adding the file:

```c#
builder.Services.AddKeyraftSecretProvider(options =>
{
	options.BaseUrl = "http://localhost:7200";
	options.Token = "secret-token";
	options.Namespace = "myapp/prod";
});
builder.Configuration.AddOtmlConfigurationFile("settings.otml");
```

Connection options, failure behavior and retries: [Keyraft secrets storage](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/en/keyraft-implementation.md).

**Environment variables.** The built-in `EnvironmentVariableSecretProvider` resolves the marker to an environment variable of the same name: `{{secret:DB_PASSWORD}}` → the `DB_PASSWORD` variable.

```c#
using AlienFruit.Otml.Configuration.Extensions; // AddEnvironmentVariableSecretProvider

// WebApplicationBuilder / IHostApplicationBuilder:
builder.AddEnvironmentVariableSecretProvider();

// IServiceCollection:
services.AddEnvironmentVariableSecretProvider();
```

A missing variable is not an error: resolution moves on to the next storage. Variable names follow the OS semantics: case-insensitive on Windows, case-sensitive on Linux. The storage name in error messages is `environment`.

Registration works like Keyraft's: the provider is added to the registry for marker resolution and injected into DI as an `ISecretProvider` singleton.

**Registration order.** Register providers before calling `AddOtmlConfigurationFile` / `AddOtmlConfiguration`: the storage list is captured at the call time.

<br/>

## Technical details

### Configuration keys

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

The result is an ordinary `IConfiguration`. Reading by key (`config["database:password"]`), one-shot binding (`config.GetSection("database").Get<DatabaseOptions>()`) and `Configure<DatabaseOptions>` + `IOptions<DatabaseOptions>` binding all work exactly as with `appsettings.json`.

`IConfigurationRoot.Reload()` re-parses the OTML file and clears the resolved-secret cache, so edited files and rotated secrets are picked up on the next read. Note: `IOptions<T>` binds once and lives for the process lifetime, so it will not see changes made after a `Reload()`. To see them, use `IOptionsSnapshot<T>` (rebinds per request) or `IOptionsMonitor<T>`.

### How markers are resolved

Any property value may contain one or more markers.

Markers are resolved **lazily**: `Load()` only parses the file, the substitution happens on the first read of the value (`config["database:password"]`, binding, etc.) and the resolved value is cached. The tag name is case-insensitive, spaces inside the marker are allowed (`{{ SECRET:DB_PASSWORD }}`). Markers with unknown tags (for example `{{vault:key}}`) and values without markers are left untouched.

Storages are tried one after another, the first non-empty answer wins. A missing secret in a storage is not an error: the provider returns `null` and resolution moves on to the next one. A provider should throw only for real failures (transport, auth, timeout), which abort the resolution.

**Error semantics**

| Situation | Result |
|-----------|--------|
| No provider registered / secret in no storage | `SecretNotFoundException` (contains `SecretName`, the configuration key and the searched storage names) |
| Empty marker `{{secret: }}` | `ArgumentException` naming the configuration key |

### Your own secret storage

The interface for any source — a file, a database, an external service:

```c#
public interface ISecretProvider
{
	string Name { get; }             // storage name used in error messages
	string? GetSecret(string name);  // null when this storage does not contain the key
}
```

There are two ways to plug an instance in.

Through `CustomSecretProviders` — storages tried after the ones from the registry:

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

Through the global registry — every later `AddOtmlConfigurationFile` / `AddOtmlConfiguration` call then sees the provider:

```c#
SecretProviderRegistry.Register(new EnvironmentVariableSecretProvider());
```

```c#
static void Register(ISecretProvider provider)   // throws ArgumentNullException for null
static ISecretProvider[] GetAll()                // registration order
```

### OtmlConfigurationOptions properties

| Property | Description |
|----------|-------------|
| `OtmlFile` | Path to the OTML file. Required: not set — `ArgumentException` |
| `CustomSecretProviders` | `ISecretProvider[]` storages tried after the ones from `SecretProviderRegistry`. Default: empty array; assigning `null` throws `ArgumentNullException` |

### Hooking up to IHostBuilder

```c#
Host.CreateDefaultBuilder(args)
	.AddOtmlConfiguration("settings.otml")
	.ConfigureServices((context, services) => { /* ... */ })
	.Build();
```

Both methods (`AddOtmlConfigurationFile` and `AddOtmlConfiguration`) have an `Action<OtmlConfigurationOptions>` overload — use it to plug in your own secret storages.

### Registration timing note for IHostBuilder

`AddOtmlConfiguration` captures the registry at its own call, while `ConfigureServices` runs later, during `Build()` — so `services.AddKeyraftSecretProvider` inside `ConfigureServices` is too late to affect configuration resolution (the provider is still injected into services). On `WebApplicationBuilder`/`ConfigurationManager` the natural order — register providers, then add the file — works.

### Namespaces and target frameworks

`AlienFruit.Otml.Configuration` targets `netstandard2.1`, the Keyraft adapter targets `net8.0`.

| What | Namespace |
|------|-----------|
| Extension methods (`AddOtmlConfigurationFile`, `AddOtmlConfiguration`, `AddEnvironmentVariableSecretProvider`) | `AlienFruit.Otml.Configuration.Extensions` |
| `ISecretProvider` | `AlienFruit.Otml.Configuration.Abstractions` |
| `EnvironmentVariableSecretProvider` | `AlienFruit.Otml.Configuration.SecretProviders` |

### Keyraft secrets storage

The Keyraft adapter — `KeyraftSecretProvider`, connection options, HTTP behavior and DI registration — is documented separately: [Keyraft secrets storage](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/en/keyraft-implementation.md).
