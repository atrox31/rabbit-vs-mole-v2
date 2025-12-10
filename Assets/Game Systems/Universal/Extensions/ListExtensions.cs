using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class ListExtensions
    {
        /// <summary>
        /// Searches a List of Pair<TFirst, TSecond> for an element whose 'First' property matches the search value
        /// and returns the corresponding 'Second' property value.
        /// </summary>
        /// <typeparam name="TFirst">The type of the First element in the Pair.</typeparam>
        /// <typeparam name="TSecond">The type of the Second element in the Pair.</typeparam>
        /// <param name="source">The list of Pair objects to search.</param>
        /// <param name="searchFirst">The value to match against the 'First' property.</param>
        /// <param name="defaultValue">The value to return if no matching pair is found.</param>
        /// <returns>The 'Second' value of the first matching Pair, or the defaultValue if none is found.</returns>
        public static TSecond GetSecondByFirst<TFirst, TSecond>(
            this List<Universal.Pair<TFirst, TSecond>> source,
            TFirst searchFirst,
            TSecond defaultValue = default(TSecond))
        {
            Universal.Pair<TFirst, TSecond> resultPair = source.FirstOrDefault(pair =>
                EqualityComparer<TFirst>.Default.Equals(pair.First, searchFirst));

            if (resultPair == null)
            {
                return defaultValue;
            }

            return resultPair.Second;
        }

        // <summary>
        /// Returns a random element from the list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The source list.</param>
        /// <returns>A random element of type T, or default(T) if the list is empty.</returns>
        public static T GetRandomElement<T>(this List<T> list)
        {
            if (list == null || list.Count == 0)
            {
                return default; // Returns null for reference types, 0 for int, etc.
            }

            int randomIndex = UnityEngine.Random.Range(0, list.Count);
            return list[randomIndex];
        }
    }
}