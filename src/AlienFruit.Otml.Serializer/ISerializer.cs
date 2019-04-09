using System;
using System.IO;

namespace AlienFruit.Otml.Serializer
{
    public interface ISerializer
    {
        string Serialize<T>(T value);

        string Serialize(Type valueType, object value);

        void Serialize<T>(T value, Stream stream, bool leaveOpen = false);

        void Serialize(Type valueType, object value, Stream stream, bool leaveOpen = false);

        T Deserialize<T>(string value);

        object Deserialize(Type resultObjectType, string value);

        T Deserialize<T>(Stream stream, bool leaveOpen = false);

        object Deserialize(Type resultObjectType, Stream stream, bool leaveOpen = false);
    }
}