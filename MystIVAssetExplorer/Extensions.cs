using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MystIVAssetExplorer;

internal static class Extensions
{
    public static bool TrySingle<T>(this IEnumerable<T> source, [MaybeNullWhen(false)] out T single)
    {
        using var enumerator = source.GetEnumerator();

        if (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            if (!enumerator.MoveNext())
            {
                single = current;
                return true;
            }
        }

        single = default;
        return false;
    }

    public static bool TrySingle<T>(this IEnumerable<T> source, Func<T, bool> predicate, [MaybeNullWhen(false)] out T single)
    {
        return source.Where(predicate).TrySingle(out single);
    }
}
