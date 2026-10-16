<div align="center">
    <img src="https://raw.githubusercontent.com/AlienFruit/AlienFruit.Otml/master/design/icons/256w/Artboard1.png">
</div>
<br>
<br>

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=AlienFruit_AlienFruit.Otml&metric=alert_status)](https://sonarcloud.io/dashboard?id=AlienFruit_AlienFruit.Otml)
[![GitHub](https://img.shields.io/github/license/keenanwoodall/Deform.svg)](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/LICENSE.MIT)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-blue.svg)](https://github.com/AlienFruit/AlienFruit.Otml)

**OTML** is an object tree model language.
It is a language that describes a tree of objects, which is easily readable by both man and machine. 
It is abstract, flexible and simple. There is no data standardization and no arrays in it. OTML supports comments and stream processing.
<br/><br/>

## OTML targets
 - To be easily understandable to a man
 - To be minimalistic: have a syntax consisting of a minimum set of rules
 - To be as abstract and flexible as possible, allowing to describe as many different data structures as possible
 - To support data stream processing
 - To develop and change easily, have a versioning system
 - Forget about it: `{}`, this: `[]` and this: `()`
<br/>

## Packages
|  Package name | Package version            |
|---------------|----------------------------|
|    AlienFruit.Otml      |    [![Nuget](https://img.shields.io/nuget/v/AlienFruit.Otml.svg)](https://www.nuget.org/packages/AlienFruit.Otml)   |
|    AlienFruit.Otml.Serializer   | [![Nuget](https://img.shields.io/nuget/v/AlienFruit.Otml.Serializer.svg)](https://www.nuget.org/packages/AlienFruit.Otml.Serializer) |
|    AlienFruit.Otml.Configuration   | [![Nuget](https://img.shields.io/nuget/v/AlienFruit.Otml.Configuration.svg)](https://www.nuget.org/packages/AlienFruit.Otml.Configuration) |
|    AlienFruit.Otml.Configuration.Keyraft   | [![Nuget](https://img.shields.io/nuget/v/AlienFruit.Otml.Configuration.Keyraft.svg)](https://www.nuget.org/packages/AlienFruit.Otml.Configuration.Keyraft) |



## Docs
- [Specification](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/en/specification.md)
- [Parser C# implementation](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/en/parser-implementation.md)
- [Serializer C# implementation](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/en/serializer-implementation.md)
- [Configuration C# implementation](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/en/configuration-implementation.md)
- [Keyraft secrets storage](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/en/keyraft-implementation.md)

## OTML example:

```py
@FooObject
	TestId : 252
	Amount : 226
	Comments : 
		string129
		string93
		string190
	Date : "2020-03-29T16:04:35.4042938"
	StatusId : 253
	IsReadOnly : False
	NextDate : "2020-08-22T08:31:25.9223099"
	Code : Codestring48
	Values : 
		@ItemValue
			Date : "2017-08-21T10:32:51.6801390"
			Number : 15
			SomeStr : AnswerStrstring229
			Id : 30
		@ItemValue
			Date : "2017-12-17T04:07:10.4458521"
			Number : 178
			SomeStr : AnswerStrstring69
			Id : 225
		@ItemValue
			Date : "2017-09-20T03:52:18.1241429"
			Number : 222
			SomeStr : AnswerStrstring145
			Id : 204
	MultilineStringValue : 
		"When an outburst occurs from gas very near the black hole," +
		"the ejected gas collides with material flowing away from the massive stars in winds," +
		"pushing this material backwards and causing it to glow in X-rays." +
		"When the outburst dies down the winds return to normal and the X-rays fade."
```
## Serialization

The next class will be used in examples:

```c#
public class Foo
{
	public string Name { get; set; }
	public string StringValue { get; set; }
	public int IntValue { get; set; }
}
```

### Serialization to **string**:

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

### Serialization to **Stream**:

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

## Deserialization

### Deserialization  from **string**:

```c#
var serializer = OtmlSerializer.Create();

var source =
	"@Foo\n" +
	"\tName: Name\n" +
	"\tStringValue : Value\n" +
	"\tIntValue : 123";

Foo result = serializer.Deserialize<Foo>(source);
```

### Deserialization  from **Stream**:

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

## Configuration with secrets

Secrets don't belong in configuration files — they should never end up in a repository or be copied between environments. The `AlienFruit.Otml.Configuration` package loads an OTML file as an ordinary `IConfiguration` source — just like `appsettings.json` — and lets you reference a secret storage directly in the file:

```py
@database
	password : {{secret:DB_PASSWORD}}
	connString : "Server=db;Password={{secret:DB_PASSWORD}};Pool=5"
```

When the value is read, the `{{secret:DB_PASSWORD}}` marker is replaced with the real secret from a connected storage: a self-hosted [Keyraft](https://github.com/keyraft/keyrafted) server, environment variables, or — through the `ISecretProvider` interface — any system you run yourself (Vault, Azure Key Vault, a database...).

```c#
// register the storage first, then add the file — the order matters
builder.Services.AddKeyraftSecretProvider(options => { /* BaseUrl, Token, Namespace */ });
builder.Configuration.AddOtmlConfigurationFile("settings.otml");

var password = configuration["database:password"]; // the secret comes from the storage

// or bind a whole section with IOptions, as with appsettings.json:
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("database"));
// in the service constructor: IOptions<DatabaseOptions> → options.Value.Password
```

Worth knowing:
 - sections and binding work exactly like with `appsettings.json`;
 - storages are tried one after another, the first found value wins;
 - if no storage contains the secret, reading fails with a clear `SecretNotFoundException` instead of silently passing an empty password.

Details and API reference: [Configuration C# implementation](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/en/configuration-implementation.md), [Keyraft secrets storage](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/en/keyraft-implementation.md).
