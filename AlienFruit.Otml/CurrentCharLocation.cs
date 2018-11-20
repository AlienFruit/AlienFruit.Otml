namespace AlienFruit.Otml
{
    internal struct CurrentCharLocation
    {
        public ulong Line { get; }
        public int Position { get; private set; }

        public CurrentCharLocation(ulong line, int position)
        {
            this.Line = line;
            this.Position = position;
        }
    }
}