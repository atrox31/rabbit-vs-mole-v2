using System;

public static partial class EnumExtensions
{
    /// <summary>
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
}