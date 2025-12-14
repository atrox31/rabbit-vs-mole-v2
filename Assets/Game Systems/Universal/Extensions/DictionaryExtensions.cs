using System.Collections.Generic;

namespace Extensions
{
    public static partial class DictionaryExtensions
    {
        /// <summary>
        /// Populates the dictionary with all possible values of the <typeparamref name="TKey"/> enumeration as keys,
        /// each mapped to the default value of <typeparamref name="TValue"/>.
        /// </summary>
        /// <remarks>This method clears the dictionary before adding entries. After calling this method,
        /// the dictionary will contain all possible values of <typeparamref name="TKey"/> as keys. If <typeparamref
        /// name="TKey"/> is a flags enumeration, all defined values (not all possible flag combinations) are
        /// added.</remarks>
        /// <typeparam name="TKey">The enumeration type to use as dictionary keys. Must be an <see cref="System.Enum"/>.</typeparam>
        /// <typeparam name="TValue">The type of the values to associate with each enumeration key.</typeparam>
        /// <param name="original">The dictionary to populate. All existing entries are removed before the enumeration values are added.</param>
        /// <returns>The original dictionary instance, containing one entry for each value of <typeparamref name="TKey"/>, each
        /// mapped to the default value of <typeparamref name="TValue"/>.</returns>
        public static Dictionary<TKey, TValue> PopulateWithEnumValues<TKey, TValue>(this Dictionary<TKey, TValue> original) where TKey : System.Enum
        {
            original.Clear();
            foreach (TKey key in System.Enum.GetValues(typeof(TKey)))
            {
                original[key] = default(TValue);
            }
            return original;
        }
    }
}