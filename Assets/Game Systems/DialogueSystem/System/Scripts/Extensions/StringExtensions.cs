using UnityEngine;

namespace DialogueSystem
{
    public static class StringExtensions
    {
        // <summary>
        /// Displays the string, truncating it to a specified maximum length if necessary, 
        /// and appending "[...]" if truncation occurs.
        /// </summary>
        /// <param name="source">The input string to process.</param>
        /// <param name="maxLength">The maximum desired length of the resulting string (excluding the suffix).</param>
        /// <param name="suffix">The suffix to append when the string is truncated (default is "[...]").</param>
        /// <returns>The original or truncated string with the suffix appended if truncated.</returns>
        public static string Truncate(this string source, int maxLength, string suffix = "[...]")
        {
            if (string.IsNullOrEmpty(source) || source.Length <= maxLength)
            {
                return source;
            }

            if (maxLength < 0)
            {
                Debug.LogError("Truncate: maxLength must be non-negative.");
                return source;
            }

            return source.Substring(0, maxLength) + suffix;
        }
    }
}