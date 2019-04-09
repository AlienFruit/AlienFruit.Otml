

# C# реализация сериализатора

<br/>

## Класс OtmlSerializer

Дня конвертации .NET объекта в OTML и обратно можно использовать `Serializer`.

### Создание сериализатора

Получение сериализатора с настройками по умолчанию:
```c#
var serializer = OtmlSerializer.Create();
```
<br/>

Для создания сериализатора можно использовать билдер:
```c#
var serializer = OtmlSerializer.Build()
	.WihParserVersion(new Version(1, 0))
	.WithEncoding(Encoding.UTF8)
	.Create();
```

**Методы билдера**

```c#
SerializerBuilder WihParserVersion(Version version)
```
Устанавливает версию документа, в которую будет происходить **сериализация**.
Версия для десериализации  считывается из декларации документа, либо берется максимальная версия десеарилизатора из пакета, если отсутствует декларация в начале документа.

<br/>

```c#
 public SerializerBuilder WithEncoding(Encoding encoding)
```
Задает кодировку для сериализации.  При десериализации кодировка определяется автоматически.
Если не задать кодировку, то выберется кодировка по умолчанию : `UTF-8`

<br/>

```c#
SerializerBuilder WithContainer(ResolverContainer container)
```
Позволяет зарегистрировать свою реализацию для сериализации/десериализации типов данных, которые не поддерживает сериализатор. 


### Сериализация/Десериализация

В примерах будет исполтзоваться класс:

```c#
public class Foo
{
	public string Name { get; set; }
	public string StringValue { get; set; }
	public int IntValue { get; set; }
}
```

Сериализация в `string`:

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

Сериализация в `Stream`:

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

### Десериализация

Десериализация из `string`:

```c#
var serializer = OtmlSerializer.Create();

var source =
	"@Foo\n" +
	"\tName: Name\n" +
	"\tStringValue : Value\n" +
	"\tIntValue : 123";

Foo result = serializer.Deserialize<Foo>(source);
```

Десериализация из `Stream`:

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