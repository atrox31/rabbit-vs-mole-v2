namespace GameObjects.FarmField.States
{
    /// <summary>
    /// Represents AI priority configuration for farm field states.
    /// </summary>
    public class AIPriority
    {
        /// <summary>
        /// Gets the base priority value used to determine the execution order of AI tasks.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Gets the threshold for the number of fields that triggers <see cref="Critical"/> priority.
        /// </summary>
        /// <value>
        /// Default is 3. If set to -1, the conditional priority logic is ignored.
        /// </value>
        public int Conditional { get; }

        /// <summary>
        /// Gets the elevated priority value applied when the field count exceeds <see cref="Conditional"/>.
        /// </summary>
        /// <remarks>
        /// This value is ignored if <see cref="Conditional"/> is set to -1.
        /// </remarks>
        public int Critical { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AIPriority"/> class.
        /// </summary>
        /// <param name="priority">The base priority value.</param>
        /// <param name="critical">The elevated priority value when threshold is exceeded.</param>
        /// <param name="conditional">The threshold for number of fields. Default is 3. Use -1 to ignore conditional logic.</param>
        public AIPriority(int priority, int critical = -1, int conditional = -1)
        {
            Priority = priority;
            Critical = critical;
            Conditional = conditional;
        }

        /// <summary>
        /// Gets the effective priority based on the number of fields of this type.
        /// </summary>
        /// <param name="fieldCount">The number of fields of this state type.</param>
        /// <returns>The effective priority value to use.</returns>
        public int GetEffectivePriority(int fieldCount)
        {
            if (Conditional == -1 || fieldCount <= Conditional)
            {
                return Priority;
            }

            return Critical;
        }
    }
}

