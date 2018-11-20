using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Serializer.Formatters
{
    internal class NullableFormatter<T> : IFormatter<T?> where T : struct
    {
        private readonly IFormatter<T> formatter;

        public NullableFormatter(IResolver resolver)
        {
            this.formatter = resolver.GetFormatter<T>();
        }

        public T? Deserialize(IEnumerable<INode> node) => formatter.Deserialize(node);

        public object DeserializeObject(IEnumerable<INode> value) => Deserialize(value);

        public IEnumerable<INode> Serialize(T? value, INodeFactory nodeFactory)
            => value is null ? Enumerable.Empty<INode>() : this.formatter.Serialize(value.Value, nodeFactory);

        public IEnumerable<INode> SerializeObject(object value, INodeFactory nodeFactory) => Serialize((T?)value, nodeFactory);
    }
}