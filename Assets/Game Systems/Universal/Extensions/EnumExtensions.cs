using System;

namespace Extensions
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

        // <summary>
        /// Gets the next value in the enumeration sequence. Wraps around to the first value if the current value is the last one.
        /// </summary>
        public static T Next<T>(this T src) where T : Enum
        {
            T[] Arr = (T[])Enum.GetValues(src.GetType());
            int currentIndex = Array.IndexOf(Arr, src);
            int nextIndex = (currentIndex + 1) % Arr.Length;

            return Arr[nextIndex];
        }

        /// <summary>
        /// Gets the previous value in the enumeration sequence. Wraps around to the last value if the current value is the first one.
        /// </summary>
        public static T Prev<T>(this T src) where T : Enum
        {
            T[] Arr = (T[])Enum.GetValues(src.GetType());
            int currentIndex = Array.IndexOf(Arr, src);
            // Adding Arr.Length before modulo handles the case where currentIndex is 0,
            // preventing a negative result before the modulo operation.
            int prevIndex = (currentIndex - 1 + Arr.Length) % Arr.Length;

            return Arr[prevIndex];
        }

        /// <summary>
        /// Get random enum element
        /// </summary>
        /// <returns>Random enum element</returns>
        public static T SelectRandom<T>(this T src) where T : Enum
        {
            T[] Arr = (T[])Enum.GetValues(src.GetType());
            Random random = new Random();
            int randomIndex = random.Next(Arr.Length);
            return Arr[randomIndex];
        }
    }
}