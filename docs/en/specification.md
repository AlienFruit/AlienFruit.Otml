
# OTML specification

<br/>

**OTML** is an object tree model language.
It is a language that describes a tree of objects, which is easily readable by both man and machine. 
It is abstract, flexible and simple, there is no data standardization and no arrays in it. OTML supports comments and stream processing.
<br/><br/>

## OTML targets
 - To be easily understandable to a man
 - To be minimalistic: have a syntax consisting of a minimum set of rules
 - To be as abstract and flexible as possible, allowing to describe as many different data structures as possible
 - To support data stream processing
 - To develop and change easily, have a version system
 - Forget about it: `{}`, this: `[]` and this: `()`
<br/>

## Syntax

A list of service characters used in the language: <br/>
`@ : , # ' "  `<br/>
and another `tab` character, `space` and `newline character`

**OTML consists of 3 elements:**

- **Object:** start with `@`
- **Property:** has on the right side the symbol `:`, followed by the value of the property
- **Value:** everything else is interpreted as value

```py
# a comment
@object1
	property1 : value1
	property2 : value2
	@object2
		property3 : value3
```

### Document declarations

They are specified only at the beginning of the OTML document with the characters `@@`.

```
@@version : 1.0
```
At the moment, only the declaration is used to indicate the version of the OTML document.

### Hierarchy

The `Tab` character is responsible for the nesting level. 
> The use of spaces from the beginning of the line to objects, properties and values is not allowed.

The left side is solely responsible for nesting and allows the use of the character `Tab` only. The levels have a hard order, for example:
If the parent has 2 indents, then the child element must have 3 indents, if there are 4 indents, then OTML will report the error.


### An object

An object can contain both values and properties and always starts with the prefix `@`.
Values can be on the same line with the object, in this case the object is separated from the values by the `:` symbol.
Values are separated from each other with a comma `,`.

```
@objectWithValues : "first node value", "second node value"
```
or

```
@objectWithValues : first node value, second node value
```

When describing values in the new lines, the `:` symbol can be not used, but its use, in this case, will not be considered an error

```py
@objectWithValues
	first node value
	second node value
```

Object properties can only be declared with a new line.

```
@objectWithValue
	firstNodeProperty : first property value
	secondNodeProperty : second property value
```


### Property

A property always ends in `:`, even if the property has no values.
This is the main difference between a property and an object.
It may contain one or more values.


```
property name : propery value
```

or

```
property name : propery value1, property value2
```

or

```
property name:
	property value
```

or

```
property name :
	first value
	second value
```

or

```
property name : propery value1, property value2
	property value3
	property value4
```

It may contain other objects and properties.
Objects and properties are described only with a new line.

```
property :
	anyProperty: any property value
	@anyObject
```

An example of using a property to describe a dictionary of values:

```py
dictionary :
	first key : first value
	second key : second value
```


### Value

The value cannot have child elements. This is an atomic unit of OTML.
The value has no standardization. Using quotes when specifying a value is optional.
You can use both `'` and `"`.

When you need to use the quotes:

 - It is necessary to use whitespace at the beginning / end of the value
 
 ```" value with spaces " ```
 
 or 
 
 ```' value with spaces '```

 - It is necessary to use the `,` symbol (value separator) in the value
 
```"value, with, comma"``` 

or 

```'value, with, comma'```

- It is necessary to use the `#` symbol (comment character) in the value

```"value with # sharp char"```

or 

```'value with # sharp char'```

- It is necessary to use the + symbol (multi-line value) in the text of the value

```"value with plus character +"```

or

```'value with plus character +'```

- It is necessary to use the @ symbol at the beginning of the text value

```"@ value with at sign"```

or

```'@ value with at sign'```

- You just do not want to bother and always highlight the values with quotes



### Escaping special characters

Only 2 characters must be escaped : `" `,`'`
Escaping occurs with the `\` character.
The character itself `\` is not escaped .

```
\property \with \backslash : \value\with\backslash
```

The characters `" `,`'`may not be escaped  in the following cases:
- The symbol " is inside single quotes. Example: ```' " '```
- The symbol ' is inside double quotes. Example: ```" ' "```

In all other cases, quotes require escaping.

```
property :
	value 1 with \' char
	value 2 with \" char
	"value 3 with \" char"
	'value 4 with \' char'
```



### Multiline values

In **OTML** there is a way to present multi-line text.
It happens by concatenating the values with the insertion of a character or a sequence of newline characters between them.
To add a new line at the end of the value, the symbol `+` is indicated.
The next value will be added as a new line of the previous value.

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
The following example is a collection of three values, where the first value is a multi-line one,
second and third ones - single line.

```py
"Value 1, first line" +
"Value 1, second line" +
"Value 1, third line"
Value2
Value3
```


### Comments

A comment is everything what is located from the # character to the end of the line, if the `#` character
is not inside double or single quotes.

```
#a comment
@node
	nodeProperty : property value # a comment
```
