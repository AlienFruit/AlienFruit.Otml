

# C# serializer implementation

<br/>

## OtmlSerializer class

You can use the `Serializer` to convert the .NET object to OTML and back.

### Creating a serializer

Getting serializer with default settings:

```c#
var serializer = OtmlSerializer.Create();
```
<br/>

To create a serializer, you can use the builder:

```c#
var serializer = OtmlSerializer.Build()
	.WihParserVersion(new Version(1, 0))
	.WithEncoding(Encoding.UTF8)
	.Create();
```

**Builder methods**

```c#
SerializerBuilder WihParserVersion(Version version)
```

Sets the version of the document to serialize to.
The version for deserialization is read from the document declaration, or the maximum version of the desalizer is taken from the package if there is no declaration at the beginning of the document.

<br/>

```c#
 public SerializerBuilder WithEncoding(Encoding encoding)
```

Specifies the encoding for serialization. When deserializing, the encoding is determined automatically.
If you do not specify the encoding, the default encoding will be selected: `UTF-8`


<br/>

```c#
SerializerBuilder WithContainer(ResolverContainer container)
```

Allows you to register your implementation to serialize / deserialize data types which the serializer does not support.


### Serialization/Deserialization

The next class will be used in examples:

```c#
public class Foo
{
	public string Name { get; set; }
	public string StringValue { get; set; }
	public int IntValue { get; set; }
}
```

Serialization to `string`:

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

Serialization to `Stream`:

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

### Deserialization

Deserialization  from `string`:

```c#
var serializer = OtmlSerializer.Create();

var source =
	"@Foo\n" +
	"\tName: Name\n" +
	"\tStringValue : Value\n" +
	"\tIntValue : 123";

Foo result = serializer.Deserialize<Foo>(source);
```

Deserialization  from `Stream`:

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