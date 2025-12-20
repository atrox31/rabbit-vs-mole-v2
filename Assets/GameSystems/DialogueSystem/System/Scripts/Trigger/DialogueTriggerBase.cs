namespace DialogueSystem.Trigger
{
    /// <summary>
    /// Base class for dialogue triggers that provides common functionality.
    /// </summary>
    /// <typeparam name="TArgs">The type of arguments for this trigger, must inherit from DialogueTriggerArguments</typeparam>
    public abstract class DialogueTriggerBase<TArgs> : IDialogueTrigger where TArgs : DialogueTriggerArguments
    {
        public object Arguments { get; set; }

        public TArgs Args
        {
            get => Arguments as TArgs;
            set => Arguments = value;
        }

        public void Execute()
        {
            if (Args == null)
            {
                DebugHelper.LogWarning(null, $"{GetType().Name}: Arguments error.");
                return;
            }

            if (!ValidateArguments())
            {
                return;
            }

            ExecuteInternal();
        }

        /// <summary>
        /// Validates the arguments before execution. Override to add custom validation.
        /// </summary>
        /// <returns>True if arguments are valid, false otherwise</returns>
        protected virtual bool ValidateArguments()
        {
            return true;
        }

        /// <summary>
        /// The actual execution logic. Override this method to implement trigger behavior.
        /// </summary>
        protected abstract void ExecuteInternal();
    }
}

