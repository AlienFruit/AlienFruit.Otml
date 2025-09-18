using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Serializer.Formatters
{
    internal class ArrayFormatter<T> : EnumerableFormatter<T>, IFormatter<T[]>
    {
        public ArrayFormatter(IResolver resolver) : base(resolver)
        {
        }

        public IEnumerable<OtmlNode> Serialize(T[] value, INodeFactory nodeFactory) => Serialize(value.AsEnumerable(), nodeFactory);

        T[] IFormatter<T[]>.Deserialize(IEnumerable<OtmlNode> node) => Deserialize(node).ToArray();

        public override object DeserializeObject(IEnumerable<OtmlNode> value) => Deserialize(value).ToArray();
    }
}