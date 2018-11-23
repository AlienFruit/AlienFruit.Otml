using System.Collections.Generic;

namespace AlienFruit.Otml.Serializer
{
    public interface IFormatter<T> : IFormatter
    {
        IEnumerable<OtmlNode> Serialize(T value, INodeFactory nodeFactory);

        T Deserialize(IEnumerable<OtmlNode> node);
    }

    public interface IFormatter
    {
        IEnumerable<OtmlNode> SerializeObject(object value, INodeFactory nodeFactory);

        object DeserializeObject(IEnumerable<OtmlNode> value);
    }
}