using UnityEngine;

namespace WalkingImmersionSystem
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
    }
}