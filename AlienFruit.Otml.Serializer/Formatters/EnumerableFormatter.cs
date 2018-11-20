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

        public IEnumerable<INode> Serialize(IEnumerable<T> value, INodeFactory nodeFactory)
            => value is null ? Enumerable.Empty<INode>() : value.SelectMany(x => this.formatter.Serialize(x, nodeFactory));

        public IEnumerable<T> Deserialize(IEnumerable<INode> node)
            => node.Select(x => this.formatter.Deserialize(x.Singleton()));

        public IEnumerable<INode> SerializeObject(object value, INodeFactory nodeFactory) => Serialize((IEnumerable<T>)value, nodeFactory);

        public virtual object DeserializeObject(IEnumerable<INode> value) => Deserialize(value);
    }
}