using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Serializer.Formatters
{
    internal class ListFormatter<T> : EnumerableFormatter<T>, IFormatter<List<T>>, IFormatter<IList<T>>, IFormatter<ICollection<T>>
    {
        private readonly IFormatter<T> formatter;

        public ListFormatter(IResolver resolver) : base(resolver)
        {
            this.formatter = resolver.GetFormatter<T>();
        }

        public IEnumerable<INode> Serialize(List<T> value, INodeFactory nodeFactory) => Serialize((IEnumerable<T>)value, nodeFactory);

        List<T> IFormatter<List<T>>.Deserialize(IEnumerable<INode> node) => Deserialize(node).ToList();

        public override object DeserializeObject(IEnumerable<INode> value) => Deserialize(value).ToList();

        public IEnumerable<INode> Serialize(IList<T> value, INodeFactory nodeFactory) => Serialize((IEnumerable<T>)value, nodeFactory);

        IList<T> IFormatter<IList<T>>.Deserialize(IEnumerable<INode> node) => Deserialize(node).ToList();

        public IEnumerable<INode> Serialize(ICollection<T> value, INodeFactory nodeFactory) => Serialize((IEnumerable<T>)value, nodeFactory);

        ICollection<T> IFormatter<ICollection<T>>.Deserialize(IEnumerable<INode> node) => Deserialize(node).ToArray();
    }
}