using System;

namespace AlienFruit.Otml.Serializer.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class SerializerIgnoreAttribute : Attribute
    {
    }
}