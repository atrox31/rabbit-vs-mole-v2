namespace Extensions
{
    public static class IntExtensions
    {
        /// <summary>
        /// Maps an integer value from one range to another.
        /// </summary>
        /// <remarks>This method assumes that <paramref name="valueMin"/> is less than <paramref
        /// name="valueMax"/>  and that the source range is non-zero. If these conditions are not met, the behavior is
        /// undefined.</remarks>
        /// <param name="value">The value to be mapped.</param>
        /// <param name="valueMin">The minimum value of the source range.</param>
        /// <param name="valueMax">The maximum value of the source range.</param>
        /// <param name="targetMin">The minimum value of the target range.</param>
        /// <param name="targetMax">The maximum value of the target range.</param>
        /// <returns>The mapped value in the target range. The result is scaled proportionally based on the position of 
        /// <paramref name="value"/> within the source range.</returns>
        public static int Map(this int value, int valueMin, int valueMax, int targetMin, int targetMax)
        {
            var normalizedPosition = (value - valueMin) / (valueMax - valueMin);
            return targetMin + (targetMax - targetMin) * normalizedPosition;
        }
    }
}