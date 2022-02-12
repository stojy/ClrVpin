using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Utils.Extensions
{
    public static class LinqExtensions
    {
        [DebuggerStepThrough]
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null)
                return;

            foreach (var item in source)
                action(item);
        }

        [DebuggerStepThrough]
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
            return source.Except(new List<T> {item});
        }

        public static void Toggle<T>(this ICollection<T> source, T item)
        {
            if (source.Contains(item))
                source.Remove(item);
            else
                source.Add(item);
        }
        
        public static void Remove<T>(this ICollection<T> source, T item)
        {
            if (source.Contains(item))
                source.Remove(item);
        }

        public static void Add<T>(this ICollection<T> source, T item)
        {
            if (!source.Contains(item))
                source.Add(item);
        }

        public static void AddOrRemove<T>(this ICollection<T> source, T item, bool add)
        {
            if (add)
                Add(source, item);
            else
                Remove(source, item);
        }

        public static bool In<T>(this T item, IEnumerable<T> source)
        {
            return source.Contains(item);
        }

        public static bool In<T>(this T item, params T[] source)
        {
            return source.Contains(item);
        }

        public static string StringJoin<T>(this IEnumerable<T> items, string separator = ",")
        {
            return string.Join(separator, items);
        }
    }
}
