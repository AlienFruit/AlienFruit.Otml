namespace AlienFruit.Otml.Utils
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Split string only two parts by left position of delemiter
        /// </summary>
        public static string[] SplitLeft(this string self, char delemiter)
        {
            var delemiterPos = self.IndexOf(delemiter);
            if (delemiterPos < 0)
                return new string[] { self };
            return new string[] { self.Substring(0, delemiterPos), self.Substring(delemiterPos + 1) };
        }

        /// <summary>
        /// Get last right position of specefied char value
        /// </summary>
        public static int LastRightIndexOf(this string self, int startIndex, char value)
        {
            var delemiterPos = -1;
            for (int a = self.Length - 1; a >= startIndex; a--)
                if (self[a] == value)
                {
                    delemiterPos = a;
                    break;
                }
            return delemiterPos;
        }

        /// <summary>
        /// Split string only two parts by right position of delemiter
        /// </summary>
        public static string[] SplitRight(this string self, char delemiter)
        {
            var delemiterPos = LastRightIndexOf(self, 0, delemiter);
            if (delemiterPos < 0)
                return new string[] { self };
            return new string[] { self.Substring(0, delemiterPos), self.Substring(delemiterPos + 1) };
        }
    }
}