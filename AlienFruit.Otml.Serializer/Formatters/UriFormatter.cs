using AlienFruit.Otml.Serializer.Exceptions;
using AlienFruit.Otml.Serializer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Serializer.Formatters
{
    internal class UriFormatter : IFormatter<Uri>
    {
        public Uri Deserialize(IEnumerable<OtmlNode> node)
        {
            if (!node.Any())
                throw new OtmlDeserializeException("The node is empty");
            if (node.Count() > 1)
                throw new OtmlDeserializeException($"Uri value should be have only one node, but founded {node.Count()}");
            var valueNode = node.Single();
            if (valueNode.Children.Any())
                throw new OtmlDeserializeException($"Uri node cannot have any children, but founded {valueNode.Children.Count()}");
            if (!Uri.TryCreate(valueNode.Value, UriKind.RelativeOrAbsolute, out var result))
                throw new OtmlDeserializeException($"The value \"{valueNode.Value}\" is not valid uri");
            return result;
        }

        public object DeserializeObject(IEnumerable<OtmlNode> value) => Deserialize(value);

        public IEnumerable<OtmlNode> Serialize(Uri value, INodeFactory nodeFactory) => nodeFactory.CreateValue(value.ToString()).Singleton();

        public IEnumerable<OtmlNode> SerializeObject(object value, INodeFactory nodeFactory) => Serialize((Uri)value, nodeFactory);
    }
}