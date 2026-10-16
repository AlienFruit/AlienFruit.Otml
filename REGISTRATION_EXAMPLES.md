# SecretsBuilder Registration Examples

## Пример 1: Keyraft в конфигурации (до построения DI)

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
        // DI сервисы, если нужны
        services.AddLogging();
    })
    .Build();

await host.RunAsync();
```

---

## Пример 2: Несколько провайдеров секретов

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
        // Второй провайдер (другой namespace)
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

## Пример 3: Конфигурация БЕЗ секретов (только статические значения)

```csharp
Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        // secrets = null, конфигурация без {{secret:...}} маркеров
        config.AddOtmlConfigurationFile("settings.otml");
    })
    .ConfigureServices((context, services) =>
    {
        services.AddScoped<IMyService, MyService>();
    })
    .Build();
```

---

## Пример 4: ConfigurationManager (без Host)

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

// Используем configManager напрямую
var connectionString = configManager["Database:ConnectionString"];
var apiKey = configManager["Api:Key"];
```

---

## Пример 5: Регистрация в DI (отдельно от конфигурации)

```csharp
// Конфигурация БЕЗ секретов
Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddOtmlConfigurationFile("settings.otml");  // ← без секретов
    })
    .ConfigureServices((context, services) =>
    {
        // Регистрируем Keyraft в DI для использования в сервисах
        services.AddKeyraftSecretProvider(options =>
        {
            options.BaseUrl = context.Configuration["Keyraft:BaseUrl"]!;
            options.Token = context.Configuration["Keyraft:Token"]!;
            options.Namespace = "myapp";
        });
        
        services.AddScoped<ISecretService, SecretService>();
    })
    .Build();

// В сервисе:
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

## Пример 6: Комбинация — конфигурация И DI регистрация

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
        // Используем секреты при загрузке конфигурации
        config.AddOtmlConfigurationFile("settings.otml", secrets);
    })
    .ConfigureServices((context, services) =>
    {
        // Также регистрируем в DI для других нужд
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

## Пример 7: Условная регистрация в зависимости от окружения

```csharp
var secretsBuilder = new SecretsBuilder();

// В продакшене используем Keyraft
if (!app.IsDevelopment())
{
    secretsBuilder.AddKeyraftSecretProvider(options =>
    {
        options.BaseUrl = Environment.GetEnvironmentVariable("KEYRAFT_URL")!;
        options.Token = Environment.GetEnvironmentVariable("KEYRAFT_TOKEN")!;
        options.Namespace = "app/prod";
    });
}
// В разработке можно использовать переменные окружения (добавить свой провайдер)

Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        // Использует secrets только если они были добавлены
        config.AddOtmlConfigurationFile("settings.otml", secretsBuilder);
    })
    .Build();
```

---

## Пример 8: Custom ISecretProvider

```csharp
public class EnvironmentVariableSecretProvider : ISecretProvider
{
    public string Name => "environment";
    
    public string? GetSecret(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }
}

// В Program.cs:
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

## Пример 9: Минимальный (только конфигурация)

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

## Итоговый паттерн

```
1. Создать SecretsBuilder() — если нужны секреты в конфигурации
   ↓
2. Добавить провайдеров .AddKeyraftSecretProvider(...)
   ↓
3. Передать в AddOtmlConfigurationFile(file, secrets)
   ↓
4. Опционально: отдельно зарегистрировать в DI сервисах
```
