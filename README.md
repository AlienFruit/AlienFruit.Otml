 ![Logo](https://raw.githubusercontent.com/AlienFruit/AlienFruit.Otml/master/design/icons/256w/Artboard1.png)

**OTML** is an object tree model language.
It is a language that describes a tree of objects, which is easily readable by both man and machine. 
It is abstract, flexible and simple, there is no data standardization and no arrays in it. OTML supports comments and stream processing.
<br/><br/>

- [Specification](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/en/specification.md)
- [Parser C# implementation](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/en/parser-implementation.md)
- [Serializer C# implementation](https://github.com/AlienFruit/AlienFruit.Otml/blob/master/docs/en/serializer-implementation.md)

## OTML targets
 - To be easily understandable to a man
 - To be minimalistic: have a syntax consisting of a minimum set of rules
 - To be as abstract and flexible as possible, allowing to describe as many different data structures as possible
 - To support data stream processing
 - To develop and change easily, have a version system
 - Forget about it: `{}`, this: `[]` and this: `()`
<br/>


OTML example:

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
```
</br>
YAML example:

```yaml
TemplateId: 21
Amount: 253
Comments:
- string71
- string137
- string201
CrDate: 2019-08-09T22:47:01.3653125
Deleted: true
DistributorId: 77
BusinessStatusId: 121
OpDate: 2018-01-17T03:14:59.5554985
Outercode: Outercodestring117
PDADocNum: 122
Values:
- AnswerDate: 2020-01-11T00:01:18.4028131
  AnswerNumber: 20
  AnswerStr: AnswerStrstring141
  Id: 2
- AnswerDate: 2018-01-31T16:21:57.8621476
  AnswerNumber: 222
  AnswerStr: AnswerStrstring206
  Id: 249
- AnswerDate: 2021-03-06T16:35:05.4552944
  AnswerNumber: 207
  AnswerStr: AnswerStrstring230
  Id: 165

```
</br>

JSON example
```json
{
  "TemplateId": 21,
  "Amount": 253.0,
  "Comments": [
    "string71",
    "string137",
    "string201"
  ],
  "CrDate": "2019-08-09T22:47:01.3653125",
  "Deleted": true,
  "DistributorId": 77,
  "BusinessStatusId": 121,
  "IsEditOut": false,
  "OpDate": "2018-01-17T03:14:59.5554985",
  "Outercode": "Outercodestring117",
  "PDADocNum": 122,
  "Values": [
    {
      "AnswerDate": "2020-01-11T00:01:18.4028131",
      "AnswerNumber": 20.0,
      "AnswerStr": "AnswerStrstring141",
      "Id": 2
    },
    {
      "AnswerDate": "2018-01-31T16:21:57.8621476",
      "AnswerNumber": 222.0,
      "AnswerStr": "AnswerStrstring206",
      "Id": 249
    },
    {
      "AnswerDate": "2021-03-06T16:35:05.4552944",
      "AnswerNumber": 207.0,
      "AnswerStr": "AnswerStrstring230",
      "Id": 165
    }
  ]
}
```