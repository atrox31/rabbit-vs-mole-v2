using UnityEngine;

namespace Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Replaces a target substring within the source string with a formatted number.
        /// </summary>
        /// <param name="source">The input string (this keyword).</param>
        /// <param name="target">The substring to be replaced (e.g., "%%").</param>
        /// <param name="number">The integer value to insert.</param>
        /// <param name="padWithZeros">If true, pads the number with leading zeros up to the length of the target string. If false, no padding is applied.</param>
        /// <returns>The modified string.</returns>
        public static string ReplaceWithNumber(this string source, string target, int number, bool padWithZeros = true)
        {
            int targetIndex = source.IndexOf(target);

            if (targetIndex == -1)
            {
                Debug.LogWarning($"Target substring '{target}' not found in the source string.");
                return source;
            }

            string numberString;

            if (padWithZeros)
            {
                int paddingLength = target.Length;
                numberString = number.ToString($"D{paddingLength}");

                if (numberString.Length > paddingLength)
                {
                    return source.Replace(target, number.ToString());
                }
            }
            else
            {
                numberString = number.ToString();
            }
            return source.Replace(target, numberString);
        }
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