
# C# реализация синтаксического анализатора OTML

<br/>

## Класс OtmlParser

OtmlParser считывает OTML  из потока/строки и формирует объектную модель документа.  
Для получения экземпляра парсера, необходимо использовать `OtmlParserFactory`.
Фабрика определяет версию документа OTML  и возвращает экземпляр парсера, соответствующего этой версии.

Использование:
```c
var otmlFactory = new OtmlParserFactory();

//чтение  из  потока  
using (var stream = File.OpenRead(@"C:\test.otml"))
using (var parser = otmlFactory.GetParser(stream))  
{
	var result = parser.Parse();
}

//чтение  из  строки
var text = File.ReadAllText(testDataFile);
using (var parser = otmlFactory.GetParser(text))
{
	var result = parser.Parse();
}
```

## Класс OtmlUnparser

OtmlUnarser  сериализует объектную модель документа в OTML  текст/поток.  
Для получения экземпляра OtmlUnarser, необходимо использовать OtmlUnparserFactory.Фабрика вернет OtmlUnarser  для OTML  максимальной версии, либо конкретной версии, которую вы укажите.  
Можно указать кодировку, в которой вы хотите получить OTML. Кодировкой умолчанию является UTF-8.

Использование:
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

Объектная модель документа представляет из себя коллекцию объектов OtmlNode

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

Свойства OtmlNode:
|    Type           |    NodeType.Object                                    |    NodeType.Property                                  |    NodeType.Value                        |
|-------------------|-------------------------------------------------------|-------------------------------------------------------|------------------------------------------|
|    Name           |    Имя объекта                                        |    Имя свойства                                       |    Всегда пустая строка                  |
|    Value          |    Всегда пустая строка                               |    Всегда пустая строка                               |    Содержит значение                     |
|    IsMultiline    |    Всегда false                                       |    Всегда false                                       |    True   для многострочных значений,    |
|    Children       |    Коллекция дочерних элементов   любого  NodeType    |    Коллекция дочерних элементов любого NodeType       |    Всегда пустая коллекция               |

Создавать элементы DOM  можно при помощи `OtmlNodeFactory`, которую можно получить через метод `GetNodeFactory()` класса `OtmlUnparser`. 
Либо написать свои реализации для элементов, не нарушая условия вышеуказанной таблицы.

