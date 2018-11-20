using AlienFruit.Otml.Models;
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

            public T Deserialize(IEnumerable<INode> node) => default(T);

            public object DeserializeObject(IEnumerable<INode> value) => default(T);

            public IEnumerable<INode> Serialize(T value, INodeFactory nodeFactory) => new INode[] { fixture.Create<StubNode>() };

            public IEnumerable<INode> SerializeObject(object value, INodeFactory nodeFactory) => new INode[] { fixture.Create<StubNode>() };
        }

        protected class StubNode : INode
        {
            public string Name { get; set; }

            public string Value { get; set; }

            public NodeType Type { get; set; }

            public bool IsMultiline { get; set; }

            public IEnumerable<INode> Children => throw new NotImplementedException();

            public string Description => string.Empty;

            public void AddChild(INode child)
            {
                throw new NotImplementedException();
            }
        }
    }
}