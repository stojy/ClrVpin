using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    public static class LinqExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null)
                return;

            foreach (var item in source)
                action(item);
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            if (source == null)
                return;

            var i = 0;
            foreach (var item in source)
                action(item, i++);
        }

        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, T item)
        {
            return System.Linq.Enumerable.Except(source, new List<T> {item});
        }

        public static void Toggle<T>(this ICollection<T> source, T item)
        {
            if (source.Contains(item))
                source.Remove(item);
            else
                source.Add(item);
        }

        public static void ToggleOff<T>(this ICollection<T> source, T item)
        {
            if (source.Contains(item))
                source.Remove(item);
        }

        public static bool In<T>(this T item, IEnumerable<T> source)
        {
            return source.Contains(item);
        }

        public static bool In<T>(this T item, params T[] source)
        {
            return source.Contains(item);
        }
    }
}
