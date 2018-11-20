using System;

namespace AlienFruit.Otml.Serializer.Exceptions
{
    internal class OtmlDeserializeException : Exception
    {
        public OtmlDeserializeException(string message) : base(message)
        {
        }
    }
}