using System;
using System.Collections.Generic;

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
    }
}
