using AlienFruit.Otml.Serializer.Exceptions;
using AlienFruit.Otml.Serializer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Serializer.Formatters
{
    internal class TimeSpanFormatter : IFormatter<TimeSpan>
    {
        public TimeSpan Deserialize(IEnumerable<OtmlNode> node)
        {
            if (node.Count() > 1)
                throw new OtmlDeserializeException($"The SimeSpan value can be built from only one node, but founded {node.Count()}");

            if (node.Count() == 0)
                throw new OtmlDeserializeException("The TimeSpan value cannot be null");

            if (node.First().Type != NodeType.Value)
                throw new OtmlDeserializeException("The TimeSpan nodes should be a ValueNode type");

            var value = node.First().Value;
            if(!TimeSpan.TryParse(value, out var result))
            {
                throw new OtmlDeserializeException($"The TimeSpan value has incorrect format");
            }

            return result;
        }

        public object DeserializeObject(IEnumerable<OtmlNode> value) => Deserialize(value);

        public IEnumerable<OtmlNode> Serialize(TimeSpan value, INodeFactory nodeFactory)
            => nodeFactory.CreateValue(value.ToString()).Singleton();

        public IEnumerable<OtmlNode> SerializeObject(object value, INodeFactory nodeFactory) => Serialize((TimeSpan)value, nodeFactory);
    }
}
