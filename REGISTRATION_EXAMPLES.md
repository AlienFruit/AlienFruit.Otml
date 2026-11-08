# SecretsBuilder Registration Examples

## Example 1: Keyraft in configuration (before DI is built)

```csharp
using AlienFruit.Otml.Configuration;
using AlienFruit.Otml.Configuration.Keyraft;
using Microsoft.Extensions.Hosting;

var secrets = new SecretsBuilder()
    .AddKeyraftSecretProvider(options =>
    {
        options.BaseUrl = "http://localhost:7200";
        options.Token = "secret-token-123";
        options.Namespace = "myapp/prod";
    });

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddOtmlConfigurationFile("settings.otml", secrets);
    })
    .ConfigureServices((context, services) =>
    {
        // DI services, if needed
        services.AddLogging();
    })
    .Build();

await host.RunAsync();
```

---

## Example 2: Multiple secret providers

```csharp
var secrets = new SecretsBuilder()
    .AddKeyraftSecretProvider(options =>
    {
        options.BaseUrl = Environment.GetEnvironmentVariable("KEYRAFT_URL")!;
        options.Token = Environment.GetEnvironmentVariable("KEYRAFT_TOKEN")!;
        options.Namespace = "app/prod";
    })
    .AddKeyraftSecretProvider(options =>
    {
        // Second provider (different namespace)
        options.BaseUrl = Environment.GetEnvironmentVariable("KEYRAFT_URL")!;
        options.Token = Environment.GetEnvironmentVariable("KEYRAFT_TOKEN")!;
        options.Namespace = "shared/common";
    });

Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddOtmlConfigurationFile("settings.otml", secrets);
    })
    .Build();
```

---

## Example 3: Configuration WITHOUT secrets (static values only)

```csharp
Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        // secrets = null, configuration without {{secret:...}} markers
        config.AddOtmlConfigurationFile("settings.otml");
    })
    .ConfigureServices((context, services) =>
    {
        services.AddScoped<IMyService, MyService>();
    })
    .Build();
```

---

## Example 4: ConfigurationManager (without Host)

```csharp
using AlienFruit.Otml.Configuration;
using Microsoft.Extensions.Configuration;

var secrets = new SecretsBuilder()
    .AddKeyraftSecretProvider(options =>
    {
        options.BaseUrl = "http://localhost:7200";
        options.Token = "token";
        options.Namespace = "myapp";
    });

var configManager = new ConfigurationManager();
configManager.AddOtmlConfigurationFile("config.otml", secrets);

// Use configManager directly
var connectionString = configManager["Database:ConnectionString"];
var apiKey = configManager["Api:Key"];
```

---

## Example 5: Registration in DI (separate from configuration)

```csharp
// Configuration WITHOUT secrets
Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddOtmlConfigurationFile("settings.otml");  // ← no secrets
    })
    .ConfigureServices((context, services) =>
    {
        // Register Keyraft in DI for use in services
        services.AddKeyraftSecretProvider(options =>
        {
            options.BaseUrl = context.Configuration["Keyraft:BaseUrl"]!;
            options.Token = context.Configuration["Keyraft:Token"]!;
            options.Namespace = "myapp";
        });
        
        services.AddScoped<ISecretService, SecretService>();
    })
    .Build();

// In the service:
public class SecretService
{
    private readonly IEnumerable<ISecretProvider> providers;
    
    public SecretService(IEnumerable<ISecretProvider> providers)
    {
        this.providers = providers;
    }
    
    public string GetSecret(string name)
    {
        foreach (var provider in this.providers)
        {
            var value = provider.GetSecret(name);
            if (!string.IsNullOrEmpty(value))
                return value;
        }
        throw new SecretNotFoundException(name, "Secret not found");
    }
}
```

---

## Example 6: Combination — configuration AND DI registration

```csharp
var secrets = new SecretsBuilder()
    .AddKeyraftSecretProvider(options =>
    {
        options.BaseUrl = "http://localhost:7200";
        options.Token = Environment.GetEnvironmentVariable("KEYRAFT_TOKEN")!;
        options.Namespace = "app/prod";
    });

Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        // Use secrets when loading configuration
        config.AddOtmlConfigurationFile("settings.otml", secrets);
    })
    .ConfigureServices((context, services) =>
    {
        // Also register in DI for other purposes
        services.AddKeyraftSecretProvider(options =>
        {
            options.BaseUrl = context.Configuration["Keyraft:BaseUrl"]!;
            options.Token = context.Configuration["Keyraft:Token"]!;
            options.Namespace = "app/runtime";
        });
        
        services.AddScoped<IMyService, MyService>();
    })
    .Build();
```

---

## Example 7: Conditional registration depending on environment

```csharp
var secretsBuilder = new SecretsBuilder();

// In production use Keyraft
if (!app.IsDevelopment())
{
    secretsBuilder.AddKeyraftSecretProvider(options =>
    {
        options.BaseUrl = Environment.GetEnvironmentVariable("KEYRAFT_URL")!;
        options.Token = Environment.GetEnvironmentVariable("KEYRAFT_TOKEN")!;
        options.Namespace = "app/prod";
    });
}
// In development you can use environment variables (add your own provider)

Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        // Uses secrets only if they were added
        config.AddOtmlConfigurationFile("settings.otml", secretsBuilder);
    })
    .Build();
```

---

## Example 8: Custom ISecretProvider

```csharp
public class EnvironmentVariableSecretProvider : ISecretProvider
{
    public string Name => "environment";
    
    public string? GetSecret(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }
}

// In Program.cs:
var secrets = new SecretsBuilder()
    .Add(new EnvironmentVariableSecretProvider())
    .AddKeyraftSecretProvider(options =>
    {
        options.BaseUrl = "http://localhost:7200";
        options.Token = Environment.GetEnvironmentVariable("KEYRAFT_TOKEN")!;
        options.Namespace = "app/prod";
    });

Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddOtmlConfigurationFile("settings.otml", secrets);
    })
    .Build();
```

---

## Example 9: Minimal (configuration only)

```csharp
using AlienFruit.Otml.Configuration;

Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddOtmlConfigurationFile("config.otml");
    })
    .Build()
    .Run();
```

---

## Final pattern

```
1. Create SecretsBuilder() — if secrets are needed in configuration
   ↓
2. Add providers .AddKeyraftSecretProvider(...)
   ↓
3. Pass to AddOtmlConfigurationFile(file, secrets)
   ↓
4. Optional: separately register in DI services
```
