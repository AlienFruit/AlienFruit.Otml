using System;
using System.Collections.Generic;
using System.Globalization;

namespace AlienFruit.Otml.Serializer.Utils
{
    internal static class ObjectExtensions
    {
        public static IEnumerable<T> Singleton<T>(this T self) => new[] { self };

        public static T[] InArray<T>(this T self) => new[] { self };
        public static T ChangeType<T>(this object obj) => (T)Convert.ChangeType(obj, typeof(T), CultureInfo.InvariantCulture);

        public static T ThrowIfNull<T>(this T value)
            => value != null ? value : throw new NullReferenceException();

        public static T ThrowIfNull<T>(this T value, string message)
            => value != null ? value : throw new NullReferenceException(message);
    }
}