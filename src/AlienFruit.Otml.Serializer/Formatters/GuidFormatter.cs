using AlienFruit.Otml.Serializer.Exceptions;
using AlienFruit.Otml.Serializer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Serializer.Formatters
{
    internal class GuidFormatter : IFormatter<Guid>
    {
        public Guid Deserialize(IEnumerable<OtmlNode> node)
        {
            if (node.Count() > 1)
                throw new OtmlDeserializeException($"The Guid value can be built from only one node, but founded {node.Count()}");

            if (node.Count() == 0)
                throw new OtmlDeserializeException("The Guid value cannot be null");

            if (node.First().Type != NodeType.Value)
                throw new OtmlDeserializeException("The Guid nodes should be a ValueNode type");

            var value = node.First().Value;
            if (!Guid.TryParse(value, out var result))
            {
                throw new OtmlDeserializeException($"The Guid value has incorrect format");
            }

            return result;
        }

        public object DeserializeObject(IEnumerable<OtmlNode> value) => DeserializeObject(value);

        public IEnumerable<OtmlNode> Serialize(Guid value, INodeFactory nodeFactory)
            => nodeFactory.CreateValue(value.ToString()).Singleton();

        public IEnumerable<OtmlNode> SerializeObject(object value, INodeFactory nodeFactory)
            => SerializeObject((Guid)value, nodeFactory);
    }
}
