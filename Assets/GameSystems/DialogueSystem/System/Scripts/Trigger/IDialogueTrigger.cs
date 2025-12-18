namespace DialogueSystem.Trigger
{
    /// <summary>
    /// Interface for triggers that can be executed during dialogue sequences.
    /// </summary>
    public interface IDialogueTrigger
    {
        public object Arguments { get; set; }
        /// <summary>
        /// Executes the trigger. This method should be called when the trigger node is reached in the dialogue sequence.
        /// </summary>
        void Execute();
    }
}

