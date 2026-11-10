namespace AlienFruit.Otml
{
    /// <summary>
    /// Position of a character in the parsed document: 1-based line, 0-based
    /// in-line position. Public so that consumers (for example the configuration
    /// provider) can report errors with coordinates, like <see cref="Exceptions.OtmlParseException"/> does.
    /// </summary>
    public struct SourceLocation
    {
        public ulong Line { get; }
        public int Position { get; private set; }

        public SourceLocation(ulong line, int position)
        {
            this.Line = line;
            this.Position = position;
        }
    }
}