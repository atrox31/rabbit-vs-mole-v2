namespace GameSystems
{
    /// <summary>
    /// Event published when dialogue system needs to freeze or unfreeze player movement.
    /// </summary>
    public struct DialogueFreezeEvent
    {
        public bool IsFrozen;

        public DialogueFreezeEvent(bool isFrozen)
        {
            IsFrozen = isFrozen;
        }
    }
}

