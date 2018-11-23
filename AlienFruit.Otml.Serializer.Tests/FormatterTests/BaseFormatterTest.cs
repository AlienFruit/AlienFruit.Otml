using AutoFixture;
using System;
using System.Collections.Generic;

namespace AlienFruit.Otml.Serializer.Tests.FormatterTests
{
    public class BaseFormatterTest
    {
        protected readonly Fixture fixture = new Fixture();

        protected class StubResolver<T> : IResolver
        {
            private readonly Fixture fixture;

            public StubResolver(Fixture fixture) => this.fixture = fixture;

            public IFormatter<T> GetFormatter<T>() => new StubFormatter<T>(this.fixture);

            public IFormatter GetFormatter(Type type) => new StubFormatter<T>(this.fixture);

            public T GetObject<T>() => throw new NotImplementedException();
        }

        protected class StubFormatter<T> : IFormatter<T>
        {
            private readonly Fixture fixture;

            public StubFormatter(Fixture fixture) => this.fixture = fixture;

            public T Deserialize(IEnumerable<OtmlNode> node) => default(T);

            public object DeserializeObject(IEnumerable<OtmlNode> value) => default(T);

            public IEnumerable<OtmlNode> Serialize(T value, INodeFactory nodeFactory) => new OtmlNode[] { fixture.Create<StubNode>().Complete() };

            public IEnumerable<OtmlNode> SerializeObject(object value, INodeFactory nodeFactory) => new OtmlNode[] { fixture.Create<StubNode>().Complete() };
        }

        protected class StubNode : OtmlNode
        {
            public string Name { get; set; }

            public string Value { get; set; }

            public NodeType NodeType { get; set; }

            public bool IsMultiline { get; set; }

            public IEnumerable<OtmlNode> Children => throw new NotImplementedException();

            public string Description => string.Empty;

            public override NodeType Type => NodeType;

            protected override IEnumerable<OtmlNode> GetChildren() => Children;

            protected override bool GetMultilineState() => IsMultiline;

            protected override string GetName() => Name;

            protected override string GetValue() => Value;

            public StubNode Complete()
            {
                switch (Type)
                {
                    case NodeType.Object:
                    case NodeType.Property:
                        Value = string.Empty;
                        return this;

                    default:
                        Name = string.Empty;
                        return this;
                }
            }
        }
    }
}