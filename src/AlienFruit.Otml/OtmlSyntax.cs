namespace AlienFruit.Otml
{
    internal static class OtmlSyntax
    {
        public static char TabChar => '\t';
        public static char LF => '\n';
        public static char CR => '\r';
        public static char SplitChar => ':';
        public static char CommentChar => '#';
        public static char ObjectChar => '@';
        public static char PropsListSeparator => ',';
        public static char MultilineChar = '+';
        public static char DoubleQuote => '"';
        public static char SingleQuote => '\'';
        public static char ShieldChar => '\\';
        public static char SpaceChar = ' ';

        private static readonly char[] spaceList = { ' ', OtmlSyntax.TabChar };

        public static char[] SpaceList => spaceList;
    }
}