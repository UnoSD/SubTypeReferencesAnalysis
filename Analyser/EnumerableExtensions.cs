using System;
using System.Collections.Generic;
using System.Linq;

namespace SubTypeReferencesAnalysis
{
    public static class EnumerableExtensions
    {
        public static TResult IncreamentalSearch<TResult, TKey>(this IEnumerable<TResult> source, Func<TResult, TKey, bool> selector, Action<IEnumerable<TResult>> feedback, Func<TKey> keySelector)
        {
            // Ensure no duplicates otherwise infinite loop...

            var sourceArray = source as TResult[] ?? source.ToArray();

            if (sourceArray.Length < 2) return sourceArray.FirstOrDefault();

            var key = keySelector();

            var results = sourceArray.Where(arg => selector(arg, key)).ToArray();

            feedback(results);

            return IncreamentalSearch(results, selector, feedback, keySelector);
        }

        public static void DisplayAll<T>(this IEnumerable<T> types, Func<T, string> selector)
        {
            types.Select(selector)
                 .ToList()
                 .ForEach(Console.WriteLine);
        }
    }
}