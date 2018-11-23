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

        public IEnumerable<OtmlNode> Serialize(List<T> value, INodeFactory nodeFactory) => Serialize((IEnumerable<T>)value, nodeFactory);

        List<T> IFormatter<List<T>>.Deserialize(IEnumerable<OtmlNode> node) => Deserialize(node).ToList();

        public override object DeserializeObject(IEnumerable<OtmlNode> value) => Deserialize(value).ToList();

        public IEnumerable<OtmlNode> Serialize(IList<T> value, INodeFactory nodeFactory) => Serialize((IEnumerable<T>)value, nodeFactory);

        IList<T> IFormatter<IList<T>>.Deserialize(IEnumerable<OtmlNode> node) => Deserialize(node).ToList();

        public IEnumerable<OtmlNode> Serialize(ICollection<T> value, INodeFactory nodeFactory) => Serialize((IEnumerable<T>)value, nodeFactory);

        ICollection<T> IFormatter<ICollection<T>>.Deserialize(IEnumerable<OtmlNode> node) => Deserialize(node).ToArray();
    }
}