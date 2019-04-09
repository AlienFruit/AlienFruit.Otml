
# OTML Спецификация

<br/>

**OTML** - язык модели дерева объектов.
Это язык описания дерева объектов, который легко читается как человеком, так и машиной. Он абстрактен, гибок и прост, в нем нет типизации данных и нет массивов. OTML поддерживает комментарии и потоковую обработку.
<br/><br/>

## Цели OTML
--------------------------------
 - Быть легко понятным человеку
 - Быть минималистичным: иметь синтаксис, состоящий из минимального набора правил
 - Быть максимально абстрактным и гибким, позволяя описывать максимально различные структуры данных
 - Поддерживать потоковую обработку данных
 - Развиваться и легко меняться, иметь систему версий
 - Забыть про это: `{ }`, это:`[ ]` и это: `( )`
<br/>

## Синтаксис

Список служебных символов, используемых в языке: <br/>
`@ : , # ' "  `<br/>
и  еще `tab` символ, `пробел`  и `символ новой строки`

**OTML состоит из 3-х элементов:**

- **Объект:** начинаются с символа `@`
- **Свойство:** имеет с правой стороны символ `:` , после которого идет значение свойства
- **Значение:** всё остальное интерпретируется как значение

```py
# this is comment
@object1
	property1 : value1
	property2 : value2
	@object2
		property3 : value3
```

### Декларации документа

Задаются только в начале OTML документа при помощи символов `@@`

```
@@version : 1.0
```
На данный момент используется только декларация для указания версии OTML документа.

### Иерархия

За уровень вложенности отвечает символ `Tab`. 
> Использование пробелов от начала строки до объектов, свойств и значений не допускается.

Левая сторона отвечает исключительно за вложенность и допускает только использование символа `Tab`. У уровней имеется жесткий порядок, например:
если у родителя 2 отступа, то дочерний элемент должен иметь 3 отступа, если будет 4 отступа, то OTML сообщит от об ошибке.


### Объект

Объект может содержать как значения, так и свойства и всегда начинается с префикса `@`
Значения могут находиться на одной строке с объектом, в этом случае объект отделяется от значений символом `:`.
Значения отделяются друг от друга при помощи запятой `,`.

```py
@objectWithValues : "this is first node value", "this is second node value"
```
или
```py
@objectWithValues : this is first node value, this is second node value
```
При описании значений в новых строках, символ `:` можно не использовать, но его использование, в данном случае, не будет считаться ошибкой

```py
@objectWithValues
	this is first node value
	this is second node value
```
Свойства объекта могут объявляться только с новой строки
```py
@objectWithValue
	firstNodeProperty : first property value
	secondNodeProperty : second property value
```


### Свойство

Свойство всегда заканчивается на `:`, даже если свойство не содержит значений.
Это является основным отличием от объекта.
Может содержать одно или несколько значений

```py
property name : propery value
```
или
```py
property name : propery value1, property value2
```
или
```py
property name:
	property value
```
или
```py
property name :
	first value
	second value
```
или
```py
property name : propery value1, property value2
	property value3
	property value4
```

Может содержать другие объекты и свойства.
Объекты и свойства описываются только с новой строки.

```py
property :
	anyProperty: any property value
	@anyObject
```
Пример использования свойства для описания словаря значений:

```py
dictionary :
	first key : first value
	second key : second value
```


### Значение

Значение не может иметь дочерних элементов. Это атомарная единица **OTML**.
Значение не имеет типизации. Использование кавычек при указании значения не является обязательным.
Можно использовать как  `'` так и `"`
Когда необходимо использовать кавычки:

 - Необходимо использовать пробельные символы в начале/конце значения 
 ```" value with spaces " ```
 или 
 ```' value with spaces '```

 - Необходимо использовать в значении символ `,` (разделитель значений)
```"value, with, comma"``` 
или 
```'value, with, comma'```

- Необходимо использовать символ `#` (символ комментария) в значении
```"value with # sharp char"```
или 
```'value with # sharp char'```

-  Необходимо использовать символ + (многострочное значение) в тексте значения
```"value with plus character +"```
или
```'value with plus character +'```

-  Необходимо использовать символ @ в начале текста значения
```"@ value with at sign"```
или
```'@ value with at sign'```

- Вы просто не хотите заморачиваться и всегда выделяете значения кавычками



### Экранирование

Нужно экранировать только 2 символа: `"`, `'`
Экранирование происходит при помощи символа  `\`
Сам символ `\` не экранируется.

```py
\property \with \backslash : \value\with\backslash
```
Символы `"`, `'` можно не экранировать в следующих случаях:
- Символ  " находится внутри одинарных кавычек. Пример:  ```' " '```

- Символ  ' находится внутри двойных кавычек. Пример: ```" ' "```

Во всех остальных случаях для кавычек необходимо экранирование

```py
property :
	value 1 with \' char
	value 2 with \" char
	"value 3 with \" char"
	'value 4 with \' char'
```



### Многострочные значения

В **OTML** есть способ представления многострочного текста.
Происходит это путем конкатенации значений со вставкой между ними символа или последовательности символов переноса строки.
Для добавления новой строки в конце значения указывается символ `+`
следующее значение будет добавлено как новая строка предыдущего значения.

```py
multilineProperty1 :
	first text line +
	second text line +
	third text line
	
multilineProperty2 :
	"first text line" +
	"second text line" +
	"third text line"

multilineProperty3 : first text line +, second text line +, third text line
multilineProperty4 : "first text line" +, "second text line" +, "third text line"
```
Следующий пример является коллекцией из трёх значений, где первое значение - многострочное,
второе и третье - однострочные
```py
"Value 1, first line" +
"Value 1, second line" +
"Value 1, third line"
Value2
Value3
```


### Комментарии

Комментарием является все что находится от символа # до конца строки, если символ `#`
не находится внутри двойных или одинарных кавычек.
```py
#this is comment
@node
	nodeProperty : property value # this is comment to
```