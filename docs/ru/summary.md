 ![Logo](https://raw.githubusercontent.com/AlienFruit/AlienFruit.Otml/master/design/icons/256w/Artboard1.png)

**OTML** - язык модели дерева объектов.
Это язык описания дерева объектов, который легко читается как человеком, так и машиной. Он абстрактен, гибок и прост, в нем нет типизации данных и нет массивов. OTML поддерживает комментарии и потоковую обработку.
<br/><br/>

- [Спецификация](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/ru/specification.md)
- [C# реализация парсера](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/ru/parser-implementation.md)
- [C# реализация сериализатора](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/ru/serializer-implementation.md)
- [C# реализация конфигурации](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/ru/configuration-implementation.md)
- [Хранилище секретов Keyraft](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/ru/keyraft-implementation.md)

## Цели OTML
 - Быть легко понятным человеку
 - Быть минималистичным: иметь синтаксис, состоящий из минимального набора правил
 - Быть максимально абстрактным и гибким, позволяя описывать максимально различные структуры данных
 - Поддерживать потоковую обработку данных
 - Развиваться и легко меняться, иметь систему версий
 - Забыть про это: `{ }`, это:`[ ]` и это: `( )`
<br/>

## Пример OTML:

```py
@FooObject
	TemplateId : 252
	Amount : 226
	Comments : 
		string129
		string93
		string190
	CrDate : "2020-03-29T16:04:35.4042938"
	Deleted : True
	DistributorId : 191
	BusinessStatusId : 253
	IsEditOut : False
	OpDate : "2020-08-22T08:31:25.9223099"
	Outercode : Outercodestring48
	PDADocNum : 51
	Values : 
		@ItemValue
			AnswerDate : "2017-08-21T10:32:51.6801390"
			AnswerNumber : 15
			AnswerStr : AnswerStrstring229
			Id : 30
		@ItemValue
			AnswerDate : "2017-12-17T04:07:10.4458521"
			AnswerNumber : 178
			AnswerStr : AnswerStrstring69
			Id : 225
		@ItemValue
			AnswerDate : "2017-09-20T03:52:18.1241429"
			AnswerNumber : 222
			AnswerStr : AnswerStrstring145
			Id : 204
	MultilineStringValue : 
		"Когда взрыв происходит из газа в непосредственной близости от черной дыры," +
		"выбрасываемый газ сталкивается с материалом, утекающим от массивных звезд на ветру," +
		"отталкивая этот материал назад и вызывая его свечение в рентгеновских лучах." +
		"Когда вспышка стихает, ветер возвращается в норму, и рентгеновские лучи исчезают."
```
## Сериализация

В примерах будет использован следующий класс:

```c#
public class Foo
{
	public string Name { get; set; }
	public string StringValue { get; set; }
	public int IntValue { get; set; }
}
```

### Сериализация в `string`:

```c#
var fooClass = new Foo
{
	Name = "Name",
	StringValue =  "Value",
	IntValue = 123
};

var serializer = OtmlSerializer.Create();

string result = serializer.Serialize(fooClass);
```

### Сериализация в `Stream`:

```c#
var fooClass = new Foo
{
	Name = "Name",
	StringValue =  "Value",
	IntValue = 123
};

var serializer = OtmlSerializer.Create();

using (var stream = File.OpenWrite("C:\\result.otml"))
{
	serializer.Serialize(fooClass, stream);
}
```

## Десериализация

### Десериализация из `string`:

```c#
var serializer = OtmlSerializer.Create();

var source =
	"@Foo\n" +
	"\tName: Name\n" +
	"\tStringValue : Value\n" +
	"\tIntValue : 123";

Foo result = serializer.Deserialize<Foo>(source);
```

### Десериализация из `Stream`:

```c#
var source =
	"@Foo\n" +
	"\tName: Name\n" +
	"\tStringValue : Value\n" +
	"\tIntValue : 123";

using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(source)))
{
	Foo result = serializer.Deserialize<Foo>(stream);
}
```

## Конфигурация и секреты

Пакет `AlienFruit.Otml.Configuration` загружает OTML-файл как источник `IConfiguration`.
Значения свойств могут ссылаться на внешние секреты через маркер `{{secret:<имя>}}`:

```py
@database
	password : {{secret:DB_PASSWORD}}
	connString : "Server=db;Password={{secret:DB_PASSWORD}};Pool=5"
```

Маркеры разрешаются лениво, при первом чтении значения (не в `Load()`).
Зарегистрированные хранилища перебираются в порядке регистрации, побеждает первое непустое
значение; хранилище возвращает `null`, если ключа в нём нет. Если значение не найдено ни в одном
хранилище, чтение завершается исключением `SecretNotFoundException`. Значения без маркеров
и маркеры с неизвестным тегом (например `{{vault:key}}`) остаются без изменений.

```c#
public interface ISecretProvider
{
    string Name { get; }             // имя хранилища, используется в сообщениях об ошибках
    string? GetSecret(string name);  // null, если это хранилище не содержит ключ
}
```

```c#
builder.Services.AddKeyraftSecretProvider(options => { /* BaseUrl, Token, Namespace */ });
builder.Configuration.AddOtmlConfigurationFile("settings.otml"); // провайдеры должны быть зарегистрированы до этого вызова

// собственные хранилища, перебираются после зарегистрированных:
builder.Configuration.AddOtmlConfigurationFile(o =>
{
    o.OtmlFile = "settings.otml";
    o.CustomSecretProviders = new ISecretProvider[] { new EnvironmentVariableSecretProvider() };
});
```

Пакет `AlienFruit.Otml.Configuration.Keyraft` реализует `ISecretProvider` поверх HTTP API
self-hosted сервера [Keyraft](https://github.com/keyraft/keyrafted):

```c#
services.AddKeyraftSecretProvider(options =>
{
    options.BaseUrl = "http://localhost:7200";
    options.Token = configuration["Keyraft:Token"];
    options.Namespace = "myapp/prod";
});
```

Полное справочное описание API: [C# реализация конфигурации](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/ru/configuration-implementation.md),
[Хранилище секретов Keyraft](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/ru/keyraft-implementation.md).