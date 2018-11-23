using AlienFruit.Otml.Serializer.Utils;
using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Serializer.Formatters
{
    internal class EnumerableFormatter<T> : IFormatter<IEnumerable<T>>
    {
        private readonly IFormatter<T> formatter;

        public EnumerableFormatter(IResolver resolver)
        {
            this.formatter = resolver.GetFormatter<T>();
        }

        public IEnumerable<OtmlNode> Serialize(IEnumerable<T> value, INodeFactory nodeFactory)
            => value is null ? Enumerable.Empty<OtmlNode>() : value.SelectMany(x => this.formatter.Serialize(x, nodeFactory));

        public IEnumerable<T> Deserialize(IEnumerable<OtmlNode> node)
            => node.Select(x => this.formatter.Deserialize(x.Singleton()));

        public IEnumerable<OtmlNode> SerializeObject(object value, INodeFactory nodeFactory) => Serialize((IEnumerable<T>)value, nodeFactory);

        public virtual object DeserializeObject(IEnumerable<OtmlNode> value) => Deserialize(value);
    }
}