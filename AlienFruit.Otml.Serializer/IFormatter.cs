using System.Collections.Generic;

namespace AlienFruit.Otml.Serializer
{
    public interface IFormatter<T> : IFormatter
    {
        IEnumerable<INode> Serialize(T value, INodeFactory nodeFactory);

        T Deserialize(IEnumerable<INode> node);
    }

    public interface IFormatter
    {
        IEnumerable<INode> SerializeObject(object value, INodeFactory nodeFactory);

        object DeserializeObject(IEnumerable<INode> value);
    }
}