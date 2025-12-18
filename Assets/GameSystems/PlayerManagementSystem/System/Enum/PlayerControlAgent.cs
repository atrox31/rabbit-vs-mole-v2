namespace PlayerManagementSystem
{
    /// <summary>
    /// Defines the type of control agent for a player.
    /// </summary>
    public enum PlayerControlAgent
    {
        None,   // Error
        Human,  // Human player (local)
        Bot,    // NPC AI
        Online  // Receive inputs from online player
    }
}
