@@version : 1.0
@@encoding : UTF-16

value1, 
"@value2",
vlue3

property1 : @va\r\nlue1 +, @value2 +, value3
porperty2  :  val\'ue1, value2, value3
property3 : "value'1", value2
property4 : value1\s, value2, va\'lue3 #comment
property5 : value1, value2, value3
	value1
	value2,
	value3, 
	value4

@testClass # level 0
	"prope:rty1" :	asdasd\'asdas 
		property2: # asdasdasdads :
			property3 : 24234234 # level 3
	property4 : #level 1
		property5 : 45654645645

@class name with spaces : "class,  value", 'second value'
	@class2
		property : hello \"asshole\" asdasd, "hello, asdadadad"
		property2 : 
			@class3
				property123 : asdasdasdasd

#	Otml test file
@class1
	#prop1  #adsasdasad, adasdaadadasd
	innerProp1: adasdasdasd 
	innerProp1: adasdasdasd
	innerProp1: adasdasdas
	innerProp1: adasdasdasd
	prop2 : asdasdasds
	item :
		@class2
			prop : asdadad
			prop2 : asdadasdd
		@class2
			perty2 : asdasdad

@pprop2 : "prop\"asd'asd'asd\"erty2", 'prope"rty2.1',  'pro\'perty2.2'  
@pprop2: 'property2'+, 'property2.1+', 'property2.2'
@pprop3: 
	'property2'+ #������������
	'property2.1'+
	'property2.2'
@meta
	style : 'C:\adads#ad\style.css'
	head : 'asdadasdd', 'adasdasd', 'asdasdasdasd'
	pprop2 : 'property2'+, 'property2.1'+, 'property2.2'
@tree
	@multilineClass:
		firstProperty: 'asdasdasda'
		prop1: 'property1'
		'��������� �����1'+
		prop3: property3+
		'��������� �����2'
		'���������� ������ � ���� ������������ ��������-��������'+ #comment
		'social-engineer.org ���� ������� �� PHDays � ������� ����������� ��������� � ����� � �������'+
		'��������� ������ ������ ������� �� ���� �������, ������������ ��������������� ������. ���� �� ���'  +
		'������� ���� ��� ���������� �������, ������� ������ ���������� � ���������� ������ ����� ����� � � ��������� �������,' +  
		'���������� ���������� ���������� ��������������.'+
		'�� ������, �� ����� �� ��, ����� ����, ������ ��� ���� ���������������� �����������, ����������� ���������� ��������,'+
		'���� ������� �������, ��� �������� ������ ����������, � ���� ������ ��������� ������, � ������ ��� ���� ������ �� ����� ��������.'+
		'� ����� �� �������� �� ������������� ����� ������ ���� ����� ��������� ����������� � ������ ���. � � ������ ���������� ����,' +
		'����� ������������� ������ ���� �� ���������� IT- ��� HR-������ � ����� �������� ���������������� ����������, ��������� ����������'+
		'���� ������� ���������������� ����������� ������ ���. �� ���������� �������� ��� ����� � ����, ������������ �������� ������' +
		'���� � � ������ ������� �������, � � ���� ��� ������ �������.'
		testProperty: 'asdasdad', 'Adasdad'
		'����� ������ �������� ������'
	# class
	@item1 : class1 #asdasdasdasd
		# property
		test:
			100px 
			200px
			400px
		testString: " asdasdas' d \"asd\\ asd 'asd ads"
			@asdad:class1313
				test:123
		width: 100px
		height: 50px
		sizes: 50px, '10#0px', 240px, 4px
		longText: 
			'firstString      ' +
			'secondString'
			100px
		@item2 : class1 
			inner: 
				@item123
					@item123 : class1, class2
	@item3 : class2
		x: <item1.x + 2>