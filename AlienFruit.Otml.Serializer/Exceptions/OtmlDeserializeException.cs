using System;

namespace AlienFruit.Otml.Serializer.Exceptions
{
    public class OtmlDeserializeException : Exception
    {
        public OtmlDeserializeException(string message) : base(message)
        {
        }
    }
}