using AlienFruit.Otml.Serializer.Utils;
using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Serializer.Formatters
{
    internal class DictionaryFotmatter<TKey, TValue> : IFormatter<IDictionary<TKey, TValue>>, IFormatter<Dictionary<TKey, TValue>>
    {
        private readonly IFormatter<TValue> valueFormatter;

        public DictionaryFotmatter(IResolver resolver)
        {
            this.valueFormatter = resolver.GetFormatter<TValue>();
        }

        public IEnumerable<OtmlNode> Serialize(IDictionary<TKey, TValue> value, INodeFactory nodeFactory)
        => value is null
            ? Enumerable.Empty<OtmlNode>()
            : value.Select(x => nodeFactory.CreateNode(NodeType.Property, x.Key.ToString(), this.valueFormatter.Serialize(x.Value, nodeFactory)));

        public IDictionary<TKey, TValue> Deserialize(IEnumerable<OtmlNode> node)
        => DeserializeToPairEnum(node).ToDictionary(x => x.Key, x => x.Value);

        public object DeserializeObject(IEnumerable<OtmlNode> value) => Deserialize(value);

        public IEnumerable<OtmlNode> SerializeObject(object value, INodeFactory nodeFactory) => Serialize((Dictionary<TKey, TValue>)value, nodeFactory);

        public IEnumerable<OtmlNode> Serialize(Dictionary<TKey, TValue> value, INodeFactory nodeFactory) => Serialize((IDictionary<TKey, TValue>)value, nodeFactory);

        Dictionary<TKey, TValue> IFormatter<Dictionary<TKey, TValue>>.Deserialize(IEnumerable<OtmlNode> node)
            => DeserializeToPairEnum(node).ToDictionary(x => x.Key, x => x.Value);

        private IEnumerable<KeyValuePair<TKey, TValue>> DeserializeToPairEnum(IEnumerable<OtmlNode> node)
            => node.Select(x => new KeyValuePair<TKey, TValue>(x.Name.ChangeType<TKey>(), this.valueFormatter.Deserialize(x.Children)));
    }
}