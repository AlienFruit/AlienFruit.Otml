using AlienFruit.Otml.Serializer.Exceptions;
using AlienFruit.Otml.Serializer.Utils;
using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Serializer.Formatters
{
    internal class PrimitiveFormatter<T> : IFormatter<T>
    {
        public PrimitiveFormatter()
        {
        }

        public T Deserialize(IEnumerable<INode> node)
        {
            if (node.Count() > 1)
                throw new OtmlDeserializeException($"Primitive value should be have only one node, but founded {node.Count()}");
            var valueNode = node.Single();
            if (valueNode.Children.Any())
                throw new OtmlDeserializeException($"Primitive node cannot have any children, but founded {valueNode.Children.Count()}");
            return valueNode.Value.ChangeType<T>();
        }

        public object DeserializeObject(IEnumerable<INode> value) => Deserialize(value);

        public IEnumerable<INode> Serialize(T value, INodeFactory nodeFactory) => nodeFactory.CreateValue(value.ToString()).Singleton();

        public IEnumerable<INode> SerializeObject(object value, INodeFactory nodeFactory) => Serialize((T)value, nodeFactory);
    }
}