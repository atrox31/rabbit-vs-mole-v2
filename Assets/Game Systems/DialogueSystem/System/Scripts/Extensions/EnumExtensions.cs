using System;

namespace DialogueSystem
{
    public static partial class EnumExtensions
    {
        /// <summary>
        /// Retrieves the integer value of an enumeration member.
        /// </summary>
        /// <typeparam name="T">The type of the enumeration.</typeparam>
        /// <param name="src">The enumeration member instance.</param>
        /// <returns>The integer value of the enumeration member.</returns>
        public static int i<T>(this T src) where T : Enum
        {
            // We use Convert.ToInt32, which safely handles the underlying type of the enum
            // and converts it to an integer.
            return Convert.ToInt32(src);
        }

        /// <summary>
        /// Get first enum element
        /// </summary>
        /// <typeparam name="T">The type of the enumeration.</typeparam>
        /// <param name="src">The enumeration member instance.</param>
        /// <returns>The integer value of the enumeration member.</returns>
        public static T GetFirstValue<T>(this T enumType) where T : Enum
        {
            Array values = Enum.GetValues(typeof(T));
            if (values.Length == 0)
            {
                throw new InvalidOperationException($"The enumeration type '{typeof(T).Name}' has no defined values.");
            }
            return (T)values.GetValue(0);
        }
    }
}