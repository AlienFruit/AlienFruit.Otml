using AlienFruit.Otml.Serializer.Exceptions;
using AlienFruit.Otml.Serializer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Serializer.Formatters
{
    internal class StringFormatter : IFormatter<string>
    {
        public StringFormatter()
        {
        }

        public string Deserialize(IEnumerable<OtmlNode> node)
        {
            if (node.Count() > 1)
                throw new OtmlDeserializeException($"The string value can build from only one node, but founded {node.Count()}");
            if (node.Single().Type != NodeType.Value)
                throw new OtmlDeserializeException($"The string nodes should be a ValueNode type");

            return node.Single().Value;
        }

        public object DeserializeObject(IEnumerable<OtmlNode> value) => Deserialize(value);

        public IEnumerable<OtmlNode> Serialize(string value, INodeFactory nodeFactory)
        {
            return value is null
                ? Enumerable.Empty<OtmlNode>()
                : nodeFactory.CreateValue(value, value.Contains(Environment.NewLine)).Singleton();
        }

        public IEnumerable<OtmlNode> SerializeObject(object value, INodeFactory nodeFactory) => Serialize((string)value, nodeFactory);
    }
}