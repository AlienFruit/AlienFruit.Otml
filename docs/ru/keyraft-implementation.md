

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
(см. ниже): он создаёт автономный клиент с бесконечным таймаутом, а отмена отдельных запросов выполняется
по `KeyraftSecretProviderOptions.Timeout`.

Оба конструктора сразу валидируют опции. `Name` — `keyraft:{namespace}`, используется в ошибках разрешения.

**KeyraftSecretProviderOptions**

| Свойство | Обязательно | Описание |
|----------|-------------|----------|
| `BaseUrl` | да | абсолютный `http(s)` URL сервера Keyraft |
| `Token` | да | bearer-токен с правами на чтение `Namespace` |
| `Namespace` | да | namespace с секретами, например `myapp/prod` |
| `Timeout` | нет | таймаут запроса, по умолчанию 30 секунд |

**Поведение GetSecret / GetSecretAsync**

| Ответ Keyraft | Результат |
|---------------|-----------|
| `200` с `{"value": "..."}` | значение дословно (пробелы не обрезаются) |
| `404` | `null` — ключа нет в этом хранилище, разрешение переходит к следующему провайдеру |
| пустое/пробельное имя секрета | `null` без обращения к сети |
| `401` / `403` | `KeyraftCommunicationException` (доступ запрещён, `StatusCode` заполнен) |
| прочие коды статуса | `KeyraftCommunicationException` со статусом и (обрезанным) телом ответа |
| некорректный или пустой payload у `200` | `KeyraftCommunicationException` |
| таймаут / сервер недоступен | `KeyraftCommunicationException` |

```c#
string? GetSecret(string name)
Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
```

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
