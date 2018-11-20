using AlienFruit.Otml.Serializer.Formatters;
using AlienFruit.Otml.Serializer.Utils;
using AutoFixture;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
                //RegisterCustomFormatter(typeof(ClassFormatter), x => x == typeof(Expression), (x, y, args) => new ClassFormatter());
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
                    var result = x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(Func<,>);//x == typeof(Expression);
                    if (result)
                    {
                        int a = 0;
                    }

                    if (x == typeof(Func<,>))
                    {
                        int b = 0;
                    }
                    return result;
                },
                (f, v, args) =>
                {
                    return Activator.CreateInstance(f.MakeGenericType(v), args) as IFormatter;
                });
            }
        }

        public class Foo
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }
        }

        [Test]
        public void Test123()
        {
            var serializer = Serializer.Build().WithContainer(new ExpressioinContainer()).Create();

            Expression<Func<Foo, bool>> expression = (x) => x.IntValue == 234;

            var result = serializer.Serialize(expression);
        }

        [Test]
        public void Serialize_then_deserialize_should_return_same_object_that_the_sourse()
        {
            // Arrange
            ISerializer serializer = Serializer.Build().Create();
            var sourceObject = fixture.Build<TestObject>()
                .With(x => x.Comment, new[] { $@"asdasdasdsd {Environment.NewLine} line2, asdasdasdads {Environment.NewLine} line4 adsasdasdasdasd", "asdasd" })
                .Create();

            // Action
            var serializeResult = serializer.Serialize(sourceObject);
            var deserealizeResult = serializer.Deserialize<TestObject>(serializeResult);

            // Assert
            deserealizeResult.Should().BeEquivalentTo(sourceObject);
        }

        [Test]
        public void Serialize_to_stream_then_deserialize_should_return_same_object_that_the_sourse2()
        {
            // Arrange
            var serializer = Serializer.Build().WithEncoding(Encoding.UTF8).Create();
            var sourceObject = fixture.Create<TestObject>();

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

        [Test]
        public void Serialize_to_string_then_deserialize_should_return_same_object_that_the_sourse2()
        {
            // Arrange
            var serializer = Serializer.Build().WithEncoding(Encoding.UTF8).Create();
            var sourceObject = fixture.Create<TestObject>();

            // Action
            TestObject deserealizeResult = null;
            var serializedString = serializer.Serialize(sourceObject);
            deserealizeResult = serializer.Deserialize<TestObject>(serializedString);

            // Assert
            deserealizeResult.Should().BeEquivalentTo(sourceObject);
        }

        [Test]
        public void Test()
        {
            var serializer = Serializer.Build().Create();
        }

        [Test]
        public void Serialize_then_desererialize_with_custom_formatter_should_change_string_property()
        {
            // Arrange
            var serializer = Serializer.Build().WithContainer(new TestContainer()).Create();
            var sourceObject = fixture.Create<InnerClass>();

            // Action
            var serializeResult = serializer.Serialize(sourceObject);
            var deserealizeResult = serializer.Deserialize<InnerClass>(serializeResult);

            // Assert
            deserealizeResult.Should().BeEquivalentTo(sourceObject);
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
            public string Deserialize(IEnumerable<INode> node) => "deserialize test";

            public object DeserializeObject(IEnumerable<INode> value) => Deserialize(value);

            public IEnumerable<INode> Serialize(string value, INodeFactory nodeFactory) => new TestValue("serialize test").Singleton();

            public IEnumerable<INode> SerializeObject(object value, INodeFactory nodeFactory) => Serialize((string)value, nodeFactory);
        }

        private class TestValue : INode
        {
            public string Name => string.Empty;

            public string Value { get; set; }

            public NodeType Type => NodeType.Value;

            public bool IsMultiline => false;

            public IEnumerable<INode> Children => Enumerable.Empty<INode>();

            public TestValue(string value)
            {
                this.Value = value;
            }

            public void AddChild(INode child)
            {
                throw new NotImplementedException();
            }
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
            public string Value { get; set; }
            public Dictionary<int, string> Collection { get; set; }
        }

        private class TestObject
        {
            public long RouteId { get; set; }
            public long TemplateId { get; set; }
            public long Id { get; set; }
            public TestClass TestClass { get; set; }
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