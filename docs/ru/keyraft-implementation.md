# Хранилище секретов Keyraft

Пакет `AlienFruit.Otml.Configuration.Keyraft` читает секреты из self-hosted сервера [Keyraft](https://github.com/keyraft/keyrafted): маркеры `{{secret:<имя>}}` в значениях OTML-конфигурации разрешаются запросом к Keyraft. Пакет таргетирует `net8.0`; синтаксис маркеров и порядок разрешения описаны в [C# реализация конфигурации](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/ru/configuration-implementation.md).

## Подключение

```c#
var builder = WebApplication.CreateBuilder(args);

// 1. хранилище секретов
builder.Services.AddKeyraftSecretProvider(options =>
{
	options.BaseUrl = "http://localhost:7200";
	options.Token = "secret-token";
	options.Namespace = "myapp/prod";
});

// 2. OTML-файл как конфигурация (после регистрации хранилища)
builder.Configuration.AddOtmlConfigurationFile("appsettings.otml");

// 3. как обычно, привязываем секцию к классу настроек
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("database"));

var app = builder.Build();

var password = app.Configuration["database:password"]; // разрешённый секрет — чтение по ключу

// то же значение через IOptions — секрет попадёт в свойство класса при привязке
var dbOptions = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
var samePassword = dbOptions.Password;
```

Содержимое `appsettings.otml`:

```py
@database
	password : {{secret:DB_PASSWORD}}
```

Класс настроек и маркер связаны по имени свойства: `Password` ← `password`.

```c#
public class DatabaseOptions
{
	public string Password { get; set; }
}
```

<br/>

## Параметры

Три поля обязательны — без них регистрация сразу бросит исключение:

| Свойство | Обязательно | По умолчанию | Описание |
|----------|-------------|--------------|----------|
| `BaseUrl` | да | — | абсолютный `http(s)` URL сервера Keyraft |
| `Token` | да | — | bearer-токен с правами на чтение `Namespace` |
| `Namespace` | да | — | namespace с секретами, например `myapp/prod` |
| `Timeout` | нет | 30 секунд | таймаут одного запроса к Keyraft |
| `ClientTimeout` | нет | бесконечный | таймаут создаваемого `HttpClient`; обязателен положительный или бесконечный |
| `Retry` | нет | см. ниже | политика повторов преходящих сбоев |

Секрет читается простым GET-запросом, никакой дополнительной настройки не нужно:

```
GET {BaseUrl}/v1/kv/{namespace}/{secret-name}
Authorization: Bearer {token}
```

## Повторы при сбоях

Преходящие сбои (сервер недоступен, таймаут, `408`, `429`, `5xx`) повторяются автоматически, с экспоненциальной паузой:

| Свойство `Retry` | По умолчанию | Описание |
|------------------|--------------|----------|
| `MaxRetryAttempts` | 2 | число повторов после первого запроса (всего не более `MaxRetryAttempts + 1` запросов) |
| `InitialDelay` | 200 мс | пауза перед первым повтором |
| `BackoffMultiplier` | 2.0 | во сколько раз растёт пауза перед каждым следующим повтором |
| `MaxDelay` | 5 с | верхняя граница паузы |

Пауза перед повтором N (с нуля): `InitialDelay * BackoffMultiplier^N`, но не более `MaxDelay`; джиттер не используется.

`404` (секрета нет в этом хранилище) и ошибки доступа `401`/`403` не повторяются: первое — не сбой, второе от повторов не становится лучше. После исчерпания повторов ошибка — `KeyraftCommunicationException`: для сбоя транспорта/таймаута в сообщении будет «Cannot reach…» / «timed out…», для преходящего статуса — статус и тело последнего ответа.

<br/>

## Технические детали

### Поведение GetSecret / GetSecretAsync

```c#
string? GetSecret(string name)
Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
```

| Ответ Keyraft | Результат |
|---------------|-----------|
| `200` с `{"value": "..."}` | значение дословно (пробелы не обрезаются) |
| `404` | `null` — ключа нет в этом хранилище, разрешение переходит к следующему провайдеру |
| пустое/пробельное имя секрета | `null` без обращения к сети |
| `401` / `403` | `KeyraftCommunicationException` (доступ запрещён, `StatusCode` заполнен) |
| `408` / `429` / `5xx` | ретраи по `Retry`; после исчерпания — как «прочие коды статуса» |
| прочие (непреходящие) коды статуса | `KeyraftCommunicationException` со статусом и (обрезанным) телом ответа |
| некорректный или пустой payload у `200` | `KeyraftCommunicationException` |
| таймаут / сервер недоступен | ретраи по `Retry`, затем `KeyraftCommunicationException` |

Отмена по токену вызывающей стороны прерывает разрешение сразу, без повторов. Экранируется только имя секрета; сегменты namespace передаются как есть (`project/environment`).

### Регистрация в DI

Существуют перегрузки для `IServiceCollection` (с `Action<T>` или с готовым `KeyraftSecretProviderOptions`) и для `IHostApplicationBuilder` (делегируют в `Services`).

Каждый вызов сразу валидирует опции, затем регистрирует экземпляр `KeyraftSecretProvider` как singleton `ISecretProvider` **и** в `SecretProviderRegistry`. Несколько вызовов регистрируют несколько хранилищ (например, разные namespace); они перебираются в порядке регистрации.

Учтите момент чтения реестра: `AddOtmlConfigurationFile` / `AddOtmlConfiguration` фиксируют реестр в момент своего вызова, поэтому провайдеры нужно регистрировать до подключения OTML-файла.

### Конструкторы

```c#
KeyraftSecretProvider(HttpClient httpClient, KeyraftSecretProviderOptions options)
```

Клиент используется как есть всё время жизни провайдера. Этот путь использует `services.AddKeyraftSecretProvider`: он создаёт автономный клиент с таймаутом `ClientTimeout` (по умолчанию бесконечным), а отмена отдельных запросов выполняется по `KeyraftSecretProviderOptions.Timeout`.

```c#
KeyraftSecretProvider(IHttpClientFactory httpClientFactory, KeyraftSecretProviderOptions options)
```

Клиент разрешается по имени при каждом запросе — `KeyraftSecretProvider.HttpClientName` (`"keyraft"`). Используйте, когда именованный клиент настроен в DI (polly, base address и т.п.).

Оба конструктора сразу валидируют опции. `Name` — `keyraft:{namespace}`, используется в ошибках разрешения.
