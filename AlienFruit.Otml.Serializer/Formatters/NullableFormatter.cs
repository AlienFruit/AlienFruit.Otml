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

        public T? Deserialize(IEnumerable<OtmlNode> node) => formatter.Deserialize(node);

        public object DeserializeObject(IEnumerable<OtmlNode> value) => Deserialize(value);

        public IEnumerable<OtmlNode> Serialize(T? value, INodeFactory nodeFactory)
            => value is null ? Enumerable.Empty<OtmlNode>() : this.formatter.Serialize(value.Value, nodeFactory);

        public IEnumerable<OtmlNode> SerializeObject(object value, INodeFactory nodeFactory) => Serialize((T?)value, nodeFactory);
    }
}