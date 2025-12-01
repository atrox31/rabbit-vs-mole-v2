using System.Collections.Generic;
using System.Linq;

namespace DialogueSystem
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
    }
}