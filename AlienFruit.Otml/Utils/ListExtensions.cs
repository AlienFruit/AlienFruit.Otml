using System;
using System.Collections.Generic;

namespace AlienFruit.Otml.Utils
{
    internal static class ListExtensions
    {
        public static List<T> TrimEnd<T>(this List<T> self, Func<T, bool> predicate)
        {
            for (int a = self.Count - 1; a >= 0; a--)
            {
                var item = self[a];
                if (predicate(item))
                    self.RemoveAt(a);
                else
                    break;
            }
            return self;
        }
    }
}