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
	TemplateId : 225
	Amount : 56
	Comments : 
		bfa5c2e9-d7ca-47d7-a409-5ba5039dc40a
		2ffa07a8-f546-4645-8a52-94ff42f88879
		56c54405-36c4-49f6-8a85-2702ff8468f7
	CrDate : "2018-05-30T11:31:11.6594689"
	Deleted : True
	DistributorId : 247
	BusinessStatusId : 117
	IsEditOut : False
	OpDate : "2019-01-11T05:12:47.9352083"
	Outercode : Outercode128a06ff-4e53-40d1-813d-4d68e958deee
	PDADocNum : 139
	Items : 
		@Item
			Id : 227
			Values : 
				@ItemValue
					AnswerDate : "2021-01-10T13:14:55.8943616"
					AnswerNumber : 47
					AnswerStr : AnswerStr0080161a-17ee-4423-ab1f-5af2dea77213
					Id : 170
				@ItemValue
					AnswerDate : "2019-12-04T14:04:00.9937222"
					AnswerNumber : 92
					AnswerStr : AnswerStre354895d-833a-48ec-8ee4-fa74209b6b99
					Id : 176
				@ItemValue
					AnswerDate : "2019-04-14T09:07:35.2940484"
					AnswerNumber : 195
					AnswerStr : AnswerStrf961f2d2-df5d-4c57-b7c4-617819a553ae
					Id : 81
		@Item
			Id : 251
			Values : 
				@ItemValue
					AnswerDate : "2018-09-06T05:04:53.4006795"
					AnswerNumber : 178
					AnswerStr : AnswerStr5ad3c3d6-512d-4de4-b54b-4d8f33650257
					Id : 236
				@ItemValue
					AnswerDate : "2021-02-01T18:59:52.0782417"
					AnswerNumber : 30
					AnswerStr : AnswerStr04266eaf-e80b-4173-a2ac-c2aa050a98ff
					Id : 122
				@ItemValue
					AnswerDate : "2021-01-24T06:59:20.7988498"
					AnswerNumber : 105
					AnswerStr : AnswerStr38cc732f-7ab2-4d4c-9f3e-75dac36f1dcb
					Id : 77
		@Item
			Id : 219
			Values : 
				@ItemValue
					AnswerDate : "2018-08-28T21:19:38.5946767"
					AnswerNumber : 221
					AnswerStr : AnswerStrf5b9a25d-0d47-4756-b106-8e15d5063cb6
					Id : 243
				@ItemValue
					AnswerDate : "2017-08-08T12:08:13.3004565"
					AnswerNumber : 120
					AnswerStr : AnswerStrc538723e-bd2c-456e-8167-38785c71fce8
					Id : 173
				@ItemValue
					AnswerDate : "2018-09-30T16:57:37.1859521"
					AnswerNumber : 75
					AnswerStr : AnswerStr7e8d5909-2e1a-412e-9f69-a2cec638e2da
					Id : 193

```
</br>
YAML example:

```yaml
TemplateId: 225
Amount: 56
Comments:
- bfa5c2e9-d7ca-47d7-a409-5ba5039dc40a
- 2ffa07a8-f546-4645-8a52-94ff42f88879
- 56c54405-36c4-49f6-8a85-2702ff8468f7
CrDate: 2018-05-30T11:31:11.6594689
Deleted: true
DistributorId: 247
BusinessStatusId: 117
OpDate: 2019-01-11T05:12:47.9352083
Outercode: Outercode128a06ff-4e53-40d1-813d-4d68e958deee
PDADocNum: 139
Items:
- Id: 227
  Values:
  - AnswerDate: 2021-01-10T13:14:55.8943616
    AnswerNumber: 47
    AnswerStr: AnswerStr0080161a-17ee-4423-ab1f-5af2dea77213
    Id: 170
  - AnswerDate: 2019-12-04T14:04:00.9937222
    AnswerNumber: 92
    AnswerStr: AnswerStre354895d-833a-48ec-8ee4-fa74209b6b99
    Id: 176
  - AnswerDate: 2019-04-14T09:07:35.2940484
    AnswerNumber: 195
    AnswerStr: AnswerStrf961f2d2-df5d-4c57-b7c4-617819a553ae
    Id: 81
- Id: 251
  Values:
  - AnswerDate: 2018-09-06T05:04:53.4006795
    AnswerNumber: 178
    AnswerStr: AnswerStr5ad3c3d6-512d-4de4-b54b-4d8f33650257
    Id: 236
  - AnswerDate: 2021-02-01T18:59:52.0782417
    AnswerNumber: 30
    AnswerStr: AnswerStr04266eaf-e80b-4173-a2ac-c2aa050a98ff
    Id: 122
  - AnswerDate: 2021-01-24T06:59:20.7988498
    AnswerNumber: 105
    AnswerStr: AnswerStr38cc732f-7ab2-4d4c-9f3e-75dac36f1dcb
    Id: 77
- Id: 219
  Values:
  - AnswerDate: 2018-08-28T21:19:38.5946767
    AnswerNumber: 221
    AnswerStr: AnswerStrf5b9a25d-0d47-4756-b106-8e15d5063cb6
    Id: 243
  - AnswerDate: 2017-08-08T12:08:13.3004565
    AnswerNumber: 120
    AnswerStr: AnswerStrc538723e-bd2c-456e-8167-38785c71fce8
    Id: 173
  - AnswerDate: 2018-09-30T16:57:37.1859521
    AnswerNumber: 75
    AnswerStr: AnswerStr7e8d5909-2e1a-412e-9f69-a2cec638e2da
    Id: 193
```
</br>

JSON example
```json
{
  "TemplateId": 225,
  "Amount": 56.0,
  "Comments": [
    "bfa5c2e9-d7ca-47d7-a409-5ba5039dc40a",
    "2ffa07a8-f546-4645-8a52-94ff42f88879",
    "56c54405-36c4-49f6-8a85-2702ff8468f7"
  ],
  "CrDate": "2018-05-30T11:31:11.6594689",
  "Deleted": true,
  "DistributorId": 247,
  "BusinessStatusId": 117,
  "IsEditOut": false,
  "OpDate": "2019-01-11T05:12:47.9352083",
  "Outercode": "Outercode128a06ff-4e53-40d1-813d-4d68e958deee",
  "PDADocNum": 139,
  "Items": [
    {
      "Id": 227,
      "Values": [
        {
          "AnswerDate": "2021-01-10T13:14:55.8943616",
          "AnswerNumber": 47.0,
          "AnswerStr": "AnswerStr0080161a-17ee-4423-ab1f-5af2dea77213",
          "Id": 170
        },
        {
          "AnswerDate": "2019-12-04T14:04:00.9937222",
          "AnswerNumber": 92.0,
          "AnswerStr": "AnswerStre354895d-833a-48ec-8ee4-fa74209b6b99",
          "Id": 176
        },
        {
          "AnswerDate": "2019-04-14T09:07:35.2940484",
          "AnswerNumber": 195.0,
          "AnswerStr": "AnswerStrf961f2d2-df5d-4c57-b7c4-617819a553ae",
          "Id": 81
        }
      ]
    },
    {
      "Id": 251,
      "Values": [
        {
          "AnswerDate": "2018-09-06T05:04:53.4006795",
          "AnswerNumber": 178.0,
          "AnswerStr": "AnswerStr5ad3c3d6-512d-4de4-b54b-4d8f33650257",
          "Id": 236
        },
        {
          "AnswerDate": "2021-02-01T18:59:52.0782417",
          "AnswerNumber": 30.0,
          "AnswerStr": "AnswerStr04266eaf-e80b-4173-a2ac-c2aa050a98ff",
          "Id": 122
        },
        {
          "AnswerDate": "2021-01-24T06:59:20.7988498",
          "AnswerNumber": 105.0,
          "AnswerStr": "AnswerStr38cc732f-7ab2-4d4c-9f3e-75dac36f1dcb",
          "Id": 77
        }
      ]
    },
    {
      "Id": 219,
      "Values": [
        {
          "AnswerDate": "2018-08-28T21:19:38.5946767",
          "AnswerNumber": 221.0,
          "AnswerStr": "AnswerStrf5b9a25d-0d47-4756-b106-8e15d5063cb6",
          "Id": 243
        },
        {
          "AnswerDate": "2017-08-08T12:08:13.3004565",
          "AnswerNumber": 120.0,
          "AnswerStr": "AnswerStrc538723e-bd2c-456e-8167-38785c71fce8",
          "Id": 173
        },
        {
          "AnswerDate": "2018-09-30T16:57:37.1859521",
          "AnswerNumber": 75.0,
          "AnswerStr": "AnswerStr7e8d5909-2e1a-412e-9f69-a2cec638e2da",
          "Id": 193
        }
      ]
    }
  ]
}
```