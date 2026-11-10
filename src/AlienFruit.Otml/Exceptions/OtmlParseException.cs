using System;

namespace AlienFruit.Otml.Exceptions
{
    public class OtmlParseException : Exception
    {
        internal OtmlParseException(string message, SourceLocation location)
            : base($"{message}, in line: {location.Line}, position: {location.Position}")
        {
        }

        internal OtmlParseException(string message, ulong line, int position)
            : base($"{message}, in line: {line}, position: {position}")
        {
        }
    }
}