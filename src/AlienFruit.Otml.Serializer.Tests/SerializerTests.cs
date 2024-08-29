using AlienFruit.Otml.Serializer.Formatters;
using AlienFruit.Otml.Serializer.Utils;
using AutoFixture;
using FluentAssertions;
using Xunit;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace AlienFruit.Otml.Serializer.Tests
{
    public class SerializerTests
    {
        private Fixture fixture = new Fixture();

        private class ExpressioinContainer : ResolverContainer
        {
            protected override void Init()
            {
                RegisterCustomFormatter(typeof(ClassFormatter<>),
                x =>
                {
                    return x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(Expression<>);//x == typeof(Expression);
                },
                (f, v, args) =>
                {
                    return Activator.CreateInstance(f.MakeGenericType(v), args) as IFormatter;
                });

                RegisterCustomFormatter(typeof(ClassFormatter<>),
                x =>
                {
                    return x == typeof(Type);
                },
                (f, v, args) =>
                {
                    return Activator.CreateInstance(f.MakeGenericType(v), args) as IFormatter;
                });

                RegisterCustomFormatter(typeof(PrimitiveFormatter<>),
                x =>
                {
                    var result = x == typeof(MemberTypes);
                    return result;
                },
                (f, v, args) =>
                {
                    return Activator.CreateInstance(f.MakeGenericType(v), args) as IFormatter;
                });

                RegisterCustomFormatter(typeof(ObjectFormatter),
                x =>
                {
                    return x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(Func<,>);//x == typeof(Expression);
                },
                (f, v, args) =>
                {
                    return Activator.CreateInstance(f.MakeGenericType(v), args) as IFormatter;
                });
            }
        }

        public class Foo123
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }
        }

        public class Foo
        {
            public string Name { get; set; }
            public string StringValue { get; set; }
            public int IntValue { get; set; }
            public double DoubleValue { get; set; }

            public Color ColorValue { get; set; }

            public Dictionary<string, long> DictionaryValue { get; set; }

            public IEnumerable<InnerObject> ObjectsValues { get; set; }

            public class InnerObject
            {
                public enum FooEnum { First, Second }

                public string Name { get; set; }
                public string[] ArrayValue { get; set; }
                public FooEnum Type { get; set; }
            }
        }

        public static Foo CreateFoo()
            => new Foo
            {
                Name = "Test class name",
                StringValue = "This is a string value",
                IntValue = 2147483647,
                DoubleValue = 3.141592653589793238462643,
                ColorValue = Color.FromArgb(10, 200, 100, 20),
                DictionaryValue = new Dictionary<string, long>
                {
                    { "the first key", 9223372036854775807 },
                    { "the second key", -9223372036854775807 },
                },
                ObjectsValues = new[]
                {
                    new Foo.InnerObject
                    {
                        Name = "the first inner object name",
                        ArrayValue = new [] { "the first value", "the second value", "" },
                        Type = Foo.InnerObject.FooEnum.First
                    },
                    new Foo.InnerObject
                    {
                        Name = "the second inner object name",
                        ArrayValue = new [] { "the first value", "the second value" },
                        Type = Foo.InnerObject.FooEnum.Second
                    },
                    new Foo.InnerObject
                    {
                        Name = ""
                    }
                },
            };

        public static string GetOtmlString()
            => @"
@Foo
	Name : Test class name
	StringValue : This is a string value
	IntValue : 2147483647
	DoubleValue : ""3,14159265358979""
	DictionaryValue :
		the first key : 9223372036854775807
		the second key : -9223372036854775807
	ObjectsValues :
        @InnerObject
            Name : the first inner object name
            ArrayValue :
				the first value
                the second value
            Type : First
        @InnerObject
            Name : the second inner object name
            ArrayValue :
				the first value
                the second value
            Type : Second
        @InnerObject
            Name :
            ArrayValue :
            Type : Second";

        [Fact]
        public void Serialize_then_deserialize_should_return_same_object_that_the_sourse()
        {
            // Arrange
            ISerializer serializer = OtmlSerializer.Build().Create();
            var sourceObject = fixture.Build<TestObject>()
                .With(x => x.Comment, new[] { $@"asdasda""sdsd {Environment.NewLine} line2, asdasdasd""ad""s {Environment.NewLine} line4 adsasdasdasdasd", "asdasd" })
                .With(x => x.ColorValue, Color.FromArgb(fixture.Create<int>()))
                .Create();

            // Action
            var serializeResult = serializer.Serialize(sourceObject);
            var deserealizeResult = serializer.Deserialize<TestObject>(serializeResult);

            // Assert
            deserealizeResult.Should().BeEquivalentTo(sourceObject);
        }

        [Fact]
        public void Serialize_to_stream_then_deserialize_should_return_same_object_that_the_sourse()
        {
            // Arrange
            var serializer = OtmlSerializer.Build().WithEncoding(Encoding.UTF8).Create();
            var sourceObject = fixture.Build<TestObject>()
                .With(x => x.ColorValue, Color.FromArgb(fixture.Create<int>()))
                .Create();

            // Action
            TestObject deserealizeResult = null;
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(sourceObject, stream, true);
                stream.Seek(0, SeekOrigin.Begin);
                deserealizeResult = serializer.Deserialize<TestObject>(stream, false);
            }
            // Assert
            deserealizeResult.Should().BeEquivalentTo(sourceObject);
        }

        [Fact]
        public void Serialize_to_string_then_deserialize_should_return_same_object_that_the_sourse()
        {
            // Arrange
            var serializer = OtmlSerializer.Build().WithEncoding(Encoding.UTF8).Create();
            var sourceObject = fixture.Build<TestObject>()
                .With(x => x.Outercode, string.Empty)
                .With(x => x.ColorValue, Color.FromArgb(fixture.Create<int>()))
                .Create();

            // Action
            TestObject deserealizeResult = null;
            var serializedString = serializer.Serialize(sourceObject);
            deserealizeResult = serializer.Deserialize<TestObject>(serializedString);

            // Assert
            deserealizeResult.Should().BeEquivalentTo(sourceObject);
        }

        [Fact]
        public void Serialize_then_desererialize_with_custom_formatter_should_change_string_property()
        {
            // Arrange
            var serializer = OtmlSerializer.Build().WithContainer(new TestContainer()).Create();
            var sourceObject = fixture.Create<InnerClass>();
            sourceObject.DoubleValue = 654.123;

            // Action
            var serializeResult = serializer.Serialize(sourceObject);
            var deserealizeResult = serializer.Deserialize<InnerClass>(serializeResult);

            // Assert
            deserealizeResult.Should().BeEquivalentTo(sourceObject, x =>
                x.Using<string>(ctx => ctx.Subject.Should().Be("deserialize test")).WhenTypeIs<string>());
        }

        private class TestContainer : ResolverContainer
        {
            protected override void Init()
            {
                RegisterCustomFormatter(typeof(TestFormatter), x => x == typeof(string), (x, y, args) => new TestFormatter());
            }
        }

        private class TestFormatter : IFormatter<string>
        {
            public string Deserialize(IEnumerable<OtmlNode> node) => "deserialize test";

            public object DeserializeObject(IEnumerable<OtmlNode> value) => Deserialize(value);

            public IEnumerable<OtmlNode> Serialize(string value, INodeFactory nodeFactory) => new TestValue("serialize test").Singleton();

            public IEnumerable<OtmlNode> SerializeObject(object value, INodeFactory nodeFactory) => Serialize((string)value, nodeFactory);
        }

        private class TestValue : OtmlNode
        {
            private readonly string value;
            public override NodeType Type => NodeType.Value;

            public TestValue(string value)
            {
                this.value = value;
            }

            protected override string GetName() => string.Empty;

            protected override string GetValue() => this.value;

            protected override bool GetMultilineState() => false;

            protected override IEnumerable<OtmlNode> GetChildren() => Enumerable.Empty<OtmlNode>();
        }

        private class TestClass
        {
            public string Value1 { get; set; }
            public int Value2 { get; set; }
            public int[] Values { get; set; }

            public int? NullableValue { get; set; }

            public InnerClass InnerClass123 { get; set; }

            public InnerClass[] Values2 { get; set; }
        }

        private class InnerClass
        {
            public enum InnerType { FirstType, SecondType }

            public string Value { get; set; }
            public Dictionary<int, string> Collection { get; set; }
            public InnerType Type { get; set; }
            public double DoubleValue { get; set; }
        }

        private class TestObject
        {
            public long RouteId { get; set; }
            public long TemplateId { get; set; }
            public long Id { get; set; }
            public TestClass TestClass { get; set; }
            public Color ColorValue { get; set; }
            public Uri Uri { get; set; }
            public decimal Amount { get; set; }
            public string[] Comment { get; set; }
            public DateTime CrDate { get; set; }
            public DateTime DateTimeStamp { get; set; }
            public bool Deleted { get; set; }
            public long DistributorId { get; set; }
            public long ActionId { get; set; }
            public long BaseDocId { get; set; }
            public long? BusinessStatusId { get; set; }
            public long OutletId { get; set; }
            public long ClientId { get; set; }
            public long CreatorId { get; set; }
            public long? ExchangeStateId { get; set; }
            public long FirstVersionId { get; set; }
            public long OperationId { get; set; }
            public long PhysicalPersonId { get; set; }
            public long PositionId { get; set; }
            public long PreviousVersionId { get; set; }
            public long StatusId { get; set; }
            public long IdvDJGridFields { get; set; }
            public long VisitId { get; set; }
            public bool IsEditOut { get; set; }
            public bool? IsReserved { get; set; }
            public bool IsSent { get; set; }
            public int? ObjectType { get; set; }
            public DateTime OpDate { get; set; }
            public string Outercode { get; set; }
            public long PDADocNum { get; set; }
            public string PrintDocNum { get; set; }
            public string PrnDocNum { get; set; }
            public string StatusSentMail { get; set; }
            public IEnumerable<Item> Items { get; set; }
            public TimeSpan Duration { get; set; }

            public class Item
            {
                public long Id { get; set; }
                public long IdDoc { get; set; }
                public long IdPacket { get; set; }
                public long IdQuestion { get; set; }
                public long IdTemplateRow { get; set; }
                public long IdTopic { get; set; }
                public IEnumerable<ItemPhoto> Photos { get; set; }
                public IEnumerable<ItemValue> Values { get; set; }
            }

            public class ItemPhoto
            {
                public long Id { get; set; }
                public long IdDocRow { get; set; }
                public long IdDoc { get; set; }
                public long IdQuestion { get; set; }
                public long IdTemplateRow { get; set; }
                public long IdTemplate { get; set; }
                public long IdTopic { get; set; }
                public string PhotoFileName { get; set; }
                public DateTime PhotoTime { get; set; }
            }

            public class ItemValue
            {
                public DateTime AnswerDate { get; set; }
                public long AnswerId { get; set; }
                public decimal AnswerNumber { get; set; }
                public string AnswerStr { get; set; }
                public long Id { get; set; }
                public long IdDocRow { get; set; }
                public long IdDoc { get; set; }
                public long IdQuestion { get; set; }
                public long IdTemplateRow { get; set; }
                public long IdTemplate { get; set; }
                public long IdTopic { get; set; }
            }
        }
    }
}