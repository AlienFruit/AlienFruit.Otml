using System;

namespace AlienFruit.Otml.Serializer
{
    public interface IResolver
    {
        IFormatter<T> GetFormatter<T>();

        IFormatter GetFormatter(Type type);

        T GetObject<T>();
    }
}