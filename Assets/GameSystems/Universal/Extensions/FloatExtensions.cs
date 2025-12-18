namespace Extensions
{
    public static class FloatExtensions
    {
        /// <summary>
        /// Maps a value from one range to a corresponding value in another range.
        /// </summary>
        /// <remarks>This method performs a linear transformation of the input value from the specified input
        /// range to the specified target range. Ensure that <paramref name="valueMin"/> is not equal to <paramref
        /// name="valueMax"/> to avoid division by zero.</remarks>
        /// <param name="value">The value to map, which is expected to be within the range defined by <paramref name="valueMin"/> and <paramref
        /// name="valueMax"/>.</param>
        /// <param name="valueMin">The minimum value of the input range.</param>
        /// <param name="valueMax">The maximum value of the input range.</param>
        /// <param name="targetMin">The minimum value of the target range.</param>
        /// <param name="targetMax">The maximum value of the target range.</param>
        /// <returns>The value mapped to the target range. If <paramref name="value"/> is outside the input range, the result is
        /// extrapolated based on the target range.</returns>
        public static float Map(this float value, float valueMin, float valueMax, float targetMin, float targetMax)
        {
            var normalizedPosition = (value - valueMin) / (valueMax - valueMin);
            return targetMin + (targetMax - targetMin) * normalizedPosition;
        }
    }
}