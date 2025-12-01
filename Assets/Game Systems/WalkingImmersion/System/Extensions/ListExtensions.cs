using System.Collections.Generic;

namespace WalkingImmersionSystem
{
    public static class ListExtensions
    {

        /// <summary>
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