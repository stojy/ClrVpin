using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Utils.Extensions;

public static class LinqExtensions
{
    [DebuggerStepThrough]
    public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
    {
        if (items == null)
            return;

        foreach (var item in items)
            action(item);
    }

    [DebuggerStepThrough]
    public static void ForEachParallel<T>(this IEnumerable<T> items, Action<T> action, int maxConcurrency = DegreeOfParallelism)
    {
        if (items == null)
            return;

        Parallel.ForEach(items, new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency }, action);
    }

    [DebuggerStepThrough]
    public static void ForEach<T>(this IEnumerable<T> items, Action<T, int> action)
    {
        if (items == null)
            return;

        var i = 0;
        foreach (var item in items)
            action(item, i++);
    }

    [DebuggerStepThrough]
    public static void ForEachParallel<T>(this IEnumerable<T> items, Action<T, int> action, int maxConcurrency = DegreeOfParallelism)
    {
        if (items == null)
            return;

        var i = 0;
        Parallel.ForEach(items, new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency }, item =>
            action(item, Interlocked.Increment(ref i)));
    }

    [DebuggerStepThrough]
    public static IEnumerable<TResult> SelectParallel<TSource, TResult>(this IEnumerable<TSource> items, Func<TSource, int, TResult> func, int maxConcurrency = DegreeOfParallelism)
    {
        if (items == null)
            return default;

        var i = 0;
        return items.AsParallel().WithDegreeOfParallelism(DegreeOfParallelism).Select(item => func(item, Interlocked.Increment(ref i)));
    }

    public static IEnumerable<T> Except<T>(this IEnumerable<T> items, T item) => items.Except(new List<T> { item });

    public static void Toggle<T>(this ICollection<T> source, T item)
    {
        if (source.Contains(item))
            source.Remove(item);
        else
            source.Add(item);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static void Remove<T>(this ICollection<T> items, T item)
    {
        if (items.Contains(item))
            items.Remove(item);
    }

    public static void Add<T>(this IList<T> items, T item)
    {
        // this will fail for arrays at runtime :(
        // - Array's a *VERY* 'special' beast.. it does NOT implement IList<T> or ICollection<T> as part of their declaration syntax
        //   - BUT those interfaces are 'injected' added at build/runtime.. and will thus fail at runtime because .Add is not supported by Array :(
        //   - https://stackoverflow.com/questions/4482557/what-interfaces-do-all-arrays-implement-in-c
        //     https://stackoverflow.com/questions/35653973/array-doesnt-implement-icollectiont-but-is-assignable
        // - the non-generic ICollection correctly(?) doesn't support .Add(), but the generic ICollection<T> does
        //   - https://stackoverflow.com/questions/11690147/why-does-icollection-not-contain-an-add-method
        if (!items.Contains(item))
            items.Add(item);
    }

    public static void AddOrRemove<T>(this IList<T> source, T item, bool add)
    {
        if (add)
            Add(source, item);
        else
            Remove(source, item);
    }

    public static void AddRange<T>(this ICollection<T> source, ICollection<T> items)
    {
        items.ForEach(source.Add);
    }

    public static bool In<T>(this T source, IEnumerable<T> items) => items.Contains(source);

    public static bool In<T>(this T source, params T[] items) => items.Contains(source);

    public static bool ContainsAll<T>(this IEnumerable<T> source, params T[] items) => items.Any() && items.All(source.Contains);

    public static bool ContainsAny<T>(this IEnumerable<T> source, params T[] items) => items.Any() && items.Any(source.Contains);

    public static bool ContainsAny<T>(this IEnumerable<T> source, IEnumerable<T> items)
    {
        var itemsArray = items as T[] ?? items.ToArray();
        return itemsArray.Any() && itemsArray.Any(source.Contains);
    }

    public static string StringJoin<T>(this IEnumerable<T> items, string separator = ", ") => string.Join(separator, items);

    public static IList<T> SelectUnique<T>(this IEnumerable<T> items)
    {
        return items.Where(x => x != null).Distinct().OrderBy(x => x).ToList();
    }

    public static IList<string> SelectUnique(this IEnumerable<string> items)
    {
        return items.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();
    }

    public static IList<T> SelectManyUnique<T>(this IEnumerable<IEnumerable<T>> items)
    {
        return items.SelectMany(x => x).Where(x => x != null).Distinct().OrderBy(x => x).ToList();
    }

    public static IList<string> SelectManyUnique(this IEnumerable<IEnumerable<string>> items)
    {
        return items.SelectMany(x => x).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();
    }

    // find index of a byte array within a byte array
    // - inspired from this post, but re-written as a linq extension.. https://stackoverflow.com/a/26880541/227110
    public static int IndexOf(this IEnumerable<byte> haystack, IEnumerable<byte> needle)
    {
        var needleArray = needle as byte[] ?? needle.ToArray();
        var haystackArray = haystack as byte[] ?? haystack.ToArray();

        var needleLength = needleArray.Length;
        var haystackLengthLimit = haystackArray.Length - needleLength;

        if (needleLength > 0)
        {
            for (var i = 0; i <= haystackLengthLimit; i++)
            {
                var j = 0;
                for (; j < needleLength; j++)
                {
                    if (needleArray[j] != haystackArray[i + j])
                        break;
                }

                if (j == needleLength)
                    return i;
            }
        }

        return -1;
    }

    private const int DegreeOfParallelism = 4;
}