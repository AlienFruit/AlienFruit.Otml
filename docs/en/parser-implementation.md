
# C# implementation of OTML parser

<br/>

## OtmlParser class

OtmlParser reads OTML from the stream / string and forms the document object model.
To get an instance of the parser, you must use the `OtmlParserFactory`.
The factory determines the version of the OTML document and returns an instance of the parser corresponding to this version.

Example:

```c
var otmlFactory = new OtmlParserFactory();

//read from stream  
using (var stream = File.OpenRead(@"C:\test.otml"))
using (var parser = otmlFactory.GetParser(stream))  
{
	var result = parser.Parse();
}

//read from string
var text = File.ReadAllText(testDataFile);
using (var parser = otmlFactory.GetParser(text))
{
	var result = parser.Parse();
}
```

## OtmlUnparser class

OtmlUnarser serializes the document object model into OTML text/stream.
To obtain an instance of OtmlUnarser, you must use OtmlUnparserFactory. The factory will return OtmlUnarser for OTML maximum version, or a specific version that you specify. 
You can specify the encoding in which you want to get OTML. The default encoding is UTF-8.

Example:

```c
var otmlFactory = new OtmlUnparserFactory();
var otmlUnparser = otmlFactory.GetDefaultUnparser();

// or otmlFactory.GetUnparser(new Version(1, 0));
var otmlNodeFactory = otmlUnparser.GetNodeFactory();

var dom = new[]
{
	otmlNodeFactory.CreateNode(NodeType.Object, "testObject", new []
	{
		otmlNodeFactory.CreateNode(NodeType.Property, "testProperty", new[]
		{
			otmlNodeFactory.CreateValue("test value")
		})
	})
};

//unparce to stream
using (var stream = File.OpenWrite(@"writeTest.otml"))
{
	otmlUnparser.Unparse(dom, stream);
}

//unparce to string
var result = otmlUnparser.Unparse(dom);

```

## DOM

The document object model is a collection of OtmlNode objects.

```c
public abstract class OtmlNode
{
	string Name { get; }
	string Value { get; }
	NodeType Type { get; }
	bool IsMultiline { get; }
	IEnumerable<OtmlNode> Children { get; }
}  
  
public  enum  NodeType
{
	Object,
	Property,
	Value
}
```

OtmlNode properties:

|    Type           |    NodeType.Object                                    |    NodeType.Property                                  |    NodeType.Value                        |
|-------------------|-------------------------------------------------------|-------------------------------------------------------|------------------------------------------|
|    Name           |    The name of the object                             |    Property name                                       |    Always blank line                     |
|    Value          |    Always blank line                                  |    Always blank line                                  |    Contains value                        |
|    IsMultiline    |    Always false                                       |    Always false                                       |    True for multi-line values            |
|    Children       |    A collection of child elements of any NodeType     |    A collection of child elements of any NodeType     |    Always empty collection               |

You can create DOM elements using `OtmlNodeFactory`, which can be obtained through the` GetNodeFactory () `method of the` OtmlUnparser` class.
Or write your own implementation for the elements without violating the conditions of the above table.


