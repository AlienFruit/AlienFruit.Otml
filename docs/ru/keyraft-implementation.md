

# Хранилище секретов Keyraft

<br/>

Пакет `AlienFruit.Otml.Configuration.Keyraft` реализует `ISecretProvider` поверх HTTP/JSON API self-hosted
сервера [Keyraft](https://github.com/keyraft/keyrafted), чтобы маркеры `{{secret:<имя>}}` в значениях OTML-конфигурации
разрешались из хранилищ Keyraft. Пакет таргетирует `net8.0`; синтаксис маркеров, порядок разрешения и
`SecretProviderRegistry` описаны в [C# реализация конфигурации](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/ru/configuration-implementation.md).

<br/>

## KeyraftSecretProvider

Реализация `ISecretProvider` поверх HTTP/JSON API Keyraft. Секрет читается запросом

```
GET {BaseUrl}/v1/kv/{namespace}/{secret-name}
Authorization: Bearer {token}
```

Экранируется только имя секрета; сегменты namespace передаются как есть (`project/environment`).

**Конструкторы**

```c#
KeyraftSecretProvider(IHttpClientFactory httpClientFactory, KeyraftSecretProviderOptions options)
```

Клиент разрешается по имени при каждом запросе — `KeyraftSecretProvider.HttpClientName` (`"keyraft"`). Используйте,
когда именованный клиент настроен в DI (polly, base address и т.п.).

```c#
KeyraftSecretProvider(HttpClient httpClient, KeyraftSecretProviderOptions options)
```

Клиент используется как есть всё время жизни провайдера. Этот путь использует `services.AddKeyraftSecretProvider`
(см. ниже): он создаёт автономный клиент с таймаутом `ClientTimeout` (по умолчанию бесконечным),
а отмена отдельных запросов выполняется по `KeyraftSecretProviderOptions.Timeout`.

Оба конструктора сразу валидируют опции. `Name` — `keyraft:{namespace}`, используется в ошибках разрешения.

**KeyraftSecretProviderOptions**

| Свойство | Обязательно | Описание |
|----------|-------------|----------|
| `BaseUrl` | да | абсолютный `http(s)` URL сервера Keyraft |
| `Token` | да | bearer-токен с правами на чтение `Namespace` |
| `Namespace` | да | namespace с секретами, например `myapp/prod` |
| `Timeout` | нет | таймаут запроса, по умолчанию 30 секунд |
| `ClientTimeout` | нет | таймаут автономного `HttpClient`, создаваемого при регистрации; по умолчанию бесконечный, обязателен положительный или бесконечный |
| `Retry` | нет | политика повторов преходящих сбоев (`KeyraftRetryOptions`), см. ниже |

**Поведение GetSecret / GetSecretAsync**

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

```c#
string? GetSecret(string name)
Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
```

<br/>

## Повторы запросов

Преходящие сбои повторяются по политике из `KeyraftSecretProviderOptions.Retry` (`KeyraftRetryOptions`):

| Свойство | По умолчанию | Описание |
|----------|--------------|----------|
| `MaxRetryAttempts` | 2 | число повторов после первого запроса (бюджет — не более `MaxRetryAttempts + 1` запросов) |
| `InitialDelay` | 200 мс | пауза перед первым повтором |
| `BackoffMultiplier` | 2.0 | во сколько раз растёт пауза перед каждым следующим повтором |
| `MaxDelay` | 5 с | верхняя граница паузы |

Пауза перед повтором N (с нуля): `InitialDelay * BackoffMultiplier^N`, но не более `MaxDelay`; джиттер не используется.

Повторяются: HTTP `408`, `429`, `5xx`, ошибки транспорта и превышение `Timeout`. Не повторяются:
`401`/`403`, `404` (промах по ключу, а не сбой), прочие коды и некорректные payload. Отмена по
токену вызывающей стороны прерывает разрешение сразу, без повторов. После исчерпания повторов
ошибка проявляется как обычно: транспорт/таймаут → `KeyraftCommunicationException`
(«Cannot reach…» / «timed out…»), преходящий статус → обычная ошибка со статусом и телом
последнего ответа.

<br/>

## Регистрация в DI

```c#
services.AddKeyraftSecretProvider(options =>
{
	options.BaseUrl = "http://localhost:7200";
	options.Token = builder.Configuration["Keyraft:Token"];
	options.Namespace = "myapp/prod";
});
```

Существуют перегрузки для `IServiceCollection` (с `Action<T>` или с готовым `KeyraftSecretProviderOptions`)
и для `IHostApplicationBuilder` (делегируют в `Services`).

Каждый вызов сразу валидирует опции, затем регистрирует экземпляр `KeyraftSecretProvider` как singleton
`ISecretProvider` **и** в `SecretProviderRegistry`. Несколько вызовов регистрируют несколько хранилищ
(например, разные namespace); они перебираются в порядке регистрации.

Учтите момент чтения реестра: `AddOtmlConfigurationFile` / `AddOtmlConfiguration` фиксируют реестр
в момент своего вызова, поэтому провайдеры нужно регистрировать до подключения OTML-файла.

<br/>

## Полный пример

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

var password = app.Configuration["database:password"]; // разрешённый секрет
```

Содержимое `appsettings.otml`:

```py
@database
	password : {{secret:DB_PASSWORD}}
```
