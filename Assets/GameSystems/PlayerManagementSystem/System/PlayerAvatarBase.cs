using UnityEngine;

namespace PlayerManagementSystem
{
    /// <summary>
    /// Represents a player-controlled avatar within the game environment.
    /// </summary>
    /// <remarks>This abstract base class provides a foundation for implementing player avatars in the game. 
    /// Inherit from <see cref="PlayerAvatarBase"/> to define custom avatar behavior and appearance.  Attach derived
    /// components to GameObjects in the Unity scene to enable player interaction.</remarks>
    public abstract class PlayerAvatarBase : MonoBehaviour
    {
    }
}

