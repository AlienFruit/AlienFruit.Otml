

# C# реализация конфигурации

<br/>

Пакет `AlienFruit.Otml.Configuration` загружает OTML-файл как стандартный источник конфигурации `Microsoft.Extensions.Configuration` (`IConfiguration`).
Пакет `AlienFruit.Otml.Configuration.Keyraft` добавляет разрешение маркеров `{{secret:<имя>}}` через self-hosted сервер [Keyraft](https://github.com/keyraft/keyrafted).

`AlienFruit.Otml.Configuration` таргетирует `netstandard2.1`, адаптер Keyraft — `net8.0`.

Методы расширения находятся в пространстве имён `AlienFruit.Otml.Configuration.Extensions`, `ISecretProvider` —
в `AlienFruit.Otml.Configuration.Abstractions`.

<br/>

## Подключение OTML-файла как источника конфигурации

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

У обоих методов есть перегрузка с `Action<OtmlConfigurationOptions>` — через неё подключаются
собственные хранилища секретов.

### Собственные провайдеры секретов

```c#
using AlienFruit.Otml.Configuration.Abstractions; // ISecretProvider
using AlienFruit.Otml.Configuration.Extensions;

builder.Configuration.AddOtmlConfigurationFile(o =>
{
	o.OtmlFile = "settings.otml";
	o.CustomSecretProviders = new ISecretProvider[] { new EnvironmentVariableSecretProvider() };
});

// IHostBuilder — так же:
hostBuilder.AddOtmlConfiguration(o =>
{
	o.OtmlFile = "settings.otml";
	o.CustomSecretProviders = myProviders;
});
```

**Свойства OtmlConfigurationOptions**

| Свойство | Описание |
|----------|----------|
| `OtmlFile` | Путь к OTML-файлу. Обязательное: не задан — `ArgumentException` |
| `CustomSecretProviders` | `ISecretProvider[]` — хранилища, которые перебираются после провайдеров из `SecretProviderRegistry`. По умолчанию — пустой массив; присвоение `null` бросает `ArgumentNullException` |

Порядок разрешения: сначала провайдеры из реестра (в порядке регистрации), затем собственные;
побеждает первое непустое значение. Полный список провайдеров фиксируется **в момент вызова**,
поэтому провайдеры (`AddKeyraftSecretProvider`, `SecretProviderRegistry.Register`) нужно зарегистрировать
до вызова метода расширения.

<br/>

## Как OTML отображается на ключи конфигурации

Вложенные объекты становятся путями ключей через двоеточие, значения свойства-массива получают числовые индексы:

```py
@database
	password : secret-value
	endpoints : first, second
	@pool
		size : 10
```

| Ключ | Значение |
|------|----------|
| `database:password` | `secret-value` |
| `database:endpoints:0` | `first` |
| `database:endpoints:1` | `second` |
| `database:pool:size` | `10` |

Результат — обычная `IConfiguration`: секции и привязка (`config.GetSection("database").Get<DatabaseOptions>()`)
работают как с `appsettings.json`. `IConfigurationRoot.Reload()` перечитывает OTML-файл и очищает кэш разрешённых
секретов, поэтому правки файла и ротация секретов подхватываются при следующем чтении.

<br/>

## Маркеры секретов

Любое значение свойства может содержать один или несколько маркеров:

```py
@database
	password : {{secret:DB_PASSWORD}}
	connString : "Server=db;Password={{secret:DB_PASSWORD}};Pool=5"
```

Маркеры разрешаются **лениво**: `Load()` только разбирает файл, подстановка происходит при первом чтении значения
(`config["database:password"]`, привязке и т.д.), результат кэшируется. Имя тега регистронезависимо,
пробелы внутри маркера допустимы (`{{ SECRET:DB_PASSWORD }}`). Маркеры с неизвестным тегом
(например `{{vault:key}}`) и значения без маркеров остаются без изменений.

**Интерфейс ISecretProvider**

```c#
public interface ISecretProvider
{
	string Name { get; }             // имя хранилища, используется в сообщениях об ошибках
	string? GetSecret(string name);  // null, если это хранилище не содержит ключ
}
```

Отсутствие секрета — не ошибка: провайдер возвращает `null`, и разрешение переходит к следующему хранилищу.
Бросать исключение провайдер должен только при реальных сбоях (транспорт, аутентификация, таймаут) —
это прерывает разрешение.

**Семантика ошибок**

| Ситуация | Результат |
|----------|-----------|
| Провайдер не зарегистрирован / секрета нет ни в одном хранилище | `SecretNotFoundException` (содержит `SecretName`, ключ конфигурации и имена просмотренных хранилищ) |
| Пустой маркер `{{secret: }}` | `ArgumentException` с именем ключа конфигурации |

**SecretProviderRegistry**

Провайдеры из реестра объединяются с `CustomSecretProviders` в момент вызова
`AddOtmlConfigurationFile` / `AddOtmlConfiguration` и зафиксированными переходят в источник:

```c#
SecretProviderRegistry.Register(new EnvironmentVariableSecretProvider());
```

```c#
static void Register(ISecretProvider provider)   // бросит ArgumentNullException для null
static ISecretProvider[] GetAll()                // в порядке регистрации
```

Тайминг для `IHostBuilder`: `AddOtmlConfiguration` фиксирует реестр в момент своего вызова, а
`ConfigureServices` выполняется позже, внутри `Build()` — поэтому `services.AddKeyraftSecretProvider`
внутри `ConfigureServices` не успеет повлиять на разрешение секретов в конфигурации
(в сервисы провайдер при этом внедрится). Для `WebApplicationBuilder`/`ConfigurationManager`
естественный порядок — сначала регистрация провайдеров, затем подключение файла — работает.

<br/>

## Хранилище секретов Keyraft

Адаптер Keyraft — `KeyraftSecretProvider`, параметры подключения, поведение HTTP-ответов и регистрация в DI —
описаны отдельно: [Хранилище секретов Keyraft](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/ru/keyraft-implementation.md).
