# C# реализация конфигурации

Пакет `AlienFruit.Otml.Configuration` подключает OTML-файл как обычный источник конфигурации `Microsoft.Extensions.Configuration` (`IConfiguration`). Читать такую конфигурацию можно так же, как `appsettings.json`, — а значения секретов при чтении подтягиваются из внешнего хранилища.

## Быстрый старт

1. Файл `settings.otml`:

```py
@database
	connString : "Server=db;Pool=5"
```

2. Подключаем его к приложению:

```c#
using AlienFruit.Otml.Configuration.Extensions;

builder.Configuration.AddOtmlConfigurationFile("settings.otml");
```

3. Читаем как обычно — одно значение напрямую:

```c#
var connString = configuration["database:connString"];
```

Или всю секцию одним объектом — обычный паттерн `IOptions`:

```c#
public class DatabaseOptions
{
	public string ConnString { get; set; }
}
```

Привязка и использование:

```c#
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("database"));

// дальше — внедрение в сервис
public MyService(IOptions<DatabaseOptions> options)
{
	var connString = options.Value.ConnString;
}
```

Готово. Дальше — про секреты и затем технические детали.

<br/>

## Секреты прямо в конфигурации

Пароли и токены не нужно хранить в файле — в OTML они записываются как маркеры `{{secret:<имя>}}`:

```py
@database
	password : {{secret:DB_PASSWORD}}
	connString : "Server=db;Password={{secret:DB_PASSWORD}};Pool=5"
```

При чтении значения маркер заменяется настоящим секретом из подключённого хранилища — независимо от того, читаете вы по ключу (`config["database:password"]`) или привязываете секцию к `IOptions<DatabaseOptions>`: секрет попадёт в свойство класса так же, как обычное значение. Если хранилище не подключено или секрет в нём не найден, чтение завершится исключением `SecretNotFoundException` — пустой пароль вы не получите.

**Keyraft.** Хранилище секретов на self-hosted сервере [Keyraft](https://github.com/keyraft/keyrafted). Подключение — один вызов до добавления файла:

```c#
builder.Services.AddKeyraftSecretProvider(options =>
{
	options.BaseUrl = "http://localhost:7200";
	options.Token = "secret-token";
	options.Namespace = "myapp/prod";
});
builder.Configuration.AddOtmlConfigurationFile("settings.otml");
```

Параметры подключения, поведение при сбоях и повторы запросов: [Хранилище секретов Keyraft](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/ru/keyraft-implementation.md).

**Переменные окружения.** Встроенный провайдер `EnvironmentVariableSecretProvider` берёт значение переменной окружения с именем из маркера: `{{secret:DB_PASSWORD}}` → переменная `DB_PASSWORD`.

```c#
using AlienFruit.Otml.Configuration.Extensions; // AddEnvironmentVariableSecretProvider

// WebApplicationBuilder / IHostApplicationBuilder:
builder.AddEnvironmentVariableSecretProvider();

// IServiceCollection:
services.AddEnvironmentVariableSecretProvider();
```

Отсутствие переменной — не ошибка: разрешение переходит к следующему хранилищу. Имена переменных читаются с семантикой ОС: без учёта регистра в Windows, с учётом — в Linux. Имя хранилища в сообщениях об ошибках — `environment`.

Регистрация работает как у Keyraft: провайдер попадает в реестр для разрешения маркеров и внедряется в DI как синглтон `ISecretProvider`.

**Порядок регистрации.** Провайдеры нужно зарегистрировать до вызова `AddOtmlConfigurationFile` / `AddOtmlConfiguration`: список хранилищ фиксируется в момент вызова.

<br/>

## Технические детали

### Ключи конфигурации

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

Результат — обычная `IConfiguration`. Работают и чтение по ключу (`config["database:password"]`), и разовая привязка (`config.GetSection("database").Get<DatabaseOptions>()`), и привязка через `Configure<DatabaseOptions>` + `IOptions<DatabaseOptions>` — всё как с `appsettings.json`.

`IConfigurationRoot.Reload()` перечитывает OTML-файл и очищает кэш разрешённых секретов, поэтому правки файла и ротация секретов подхватываются при следующем чтении. Учтите: `IOptions<T>` связывается один раз и живёт до конца процесса, так что после `Reload()` обновлений он не увидит. Чтобы видеть их, используйте `IOptionsSnapshot<T>` (перевязывается на каждый запрос) или `IOptionsMonitor<T>`.

### Как разрешаются маркеры

Любое значение свойства может содержать один или несколько маркеров.

Маркеры разрешаются **лениво**: `Load()` только разбирает файл, подстановка происходит при первом чтении значения (`config["database:password"]`, привязке и т.д.), результат кэшируется. Имя тега регистронезависимо, пробелы внутри маркера допустимы (`{{ SECRET:DB_PASSWORD }}`). Маркеры с неизвестным тегом (например `{{vault:key}}`) и значения без маркеров остаются без изменений.

Хранилища перебираются по очереди, побеждает первое непустое значение. Отсутствие секрета в хранилище — не ошибка: провайдер возвращает `null`, и разрешение переходит к следующему. Бросать исключение провайдер должен только при реальных сбоях (транспорт, аутентификация, таймаут) — это прерывает разрешение.

**Семантика ошибок**

| Ситуация | Результат |
|----------|-----------|
| Провайдер не зарегистрирован / секрета нет ни в одном хранилище | `SecretNotFoundException` (содержит `SecretName`, ключ конфигурации и имена просмотренных хранилищ) |
| Пустой маркер `{{secret: }}` | `ArgumentException` с именем ключа конфигурации |

### Своё хранилище секретов

Интерфейс для любого источника — файл, БД, внешний сервис:

```c#
public interface ISecretProvider
{
	string Name { get; }             // имя хранилища, используется в сообщениях об ошибках
	string? GetSecret(string name);  // null, если это хранилище не содержит ключ
}
```

Подключить экземпляр можно двумя способами.

Через `CustomSecretProviders` — хранилища, которые перебираются после провайдеров из реестра:

```c#
using AlienFruit.Otml.Configuration.Abstractions; // ISecretProvider
using AlienFruit.Otml.Configuration.Extensions;
using AlienFruit.Otml.Configuration.SecretProviders; // EnvironmentVariableSecretProvider

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

Через глобальный реестр — тогда провайдер увидит каждый последующий вызов `AddOtmlConfigurationFile` / `AddOtmlConfiguration`:

```c#
SecretProviderRegistry.Register(new EnvironmentVariableSecretProvider());
```

```c#
static void Register(ISecretProvider provider)   // бросит ArgumentNullException для null
static ISecretProvider[] GetAll()                // в порядке регистрации
```

### Параметры OtmlConfigurationOptions

| Свойство | Описание |
|----------|----------|
| `OtmlFile` | Путь к OTML-файлу. Обязательное: не задан — `ArgumentException` |
| `CustomSecretProviders` | `ISecretProvider[]` — хранилища, которые перебираются после провайдеров из `SecretProviderRegistry`. По умолчанию — пустой массив; присвоение `null` бросает `ArgumentNullException` |

### Подключение к IHostBuilder

```c#
Host.CreateDefaultBuilder(args)
	.AddOtmlConfiguration("settings.otml")
	.ConfigureServices((context, services) => { /* ... */ })
	.Build();
```

У обоих методов (`AddOtmlConfigurationFile` и `AddOtmlConfiguration`) есть перегрузка с `Action<OtmlConfigurationOptions>` — через неё подключаются собственные хранилища секретов.

### Нюанс регистрации для IHostBuilder

`AddOtmlConfiguration` фиксирует реестр в момент своего вызова, а `ConfigureServices` выполняется позже, внутри `Build()` — поэтому `services.AddKeyraftSecretProvider` внутри `ConfigureServices` не успеет повлиять на разрешение секретов в конфигурации (в сервисы провайдер при этом внедрится). Для `WebApplicationBuilder`/`ConfigurationManager` естественный порядок — сначала регистрация провайдеров, затем подключение файла — работает.

### Пространства имён и таргет-фреймворки

`AlienFruit.Otml.Configuration` таргетирует `netstandard2.1`, адаптер Keyraft — `net8.0`.

| Что | Пространство имён |
|-----|-------------------|
| Методы расширения (`AddOtmlConfigurationFile`, `AddOtmlConfiguration`, `AddEnvironmentVariableSecretProvider`) | `AlienFruit.Otml.Configuration.Extensions` |
| `ISecretProvider` | `AlienFruit.Otml.Configuration.Abstractions` |
| `EnvironmentVariableSecretProvider` | `AlienFruit.Otml.Configuration.SecretProviders` |

### Хранилище секретов Keyraft

Адаптер Keyraft — `KeyraftSecretProvider`, параметры подключения, поведение HTTP-ответов и регистрация в DI — описаны отдельно: [Хранилище секретов Keyraft](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/ru/keyraft-implementation.md).
