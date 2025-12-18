using AddressablesStaticDictionary;
using System;
using UnityEngine;

namespace PlayerManagementSystem
{
    /// <summary>
    /// Provides a base class for managing agent instantiation and lifecycle in Unity, parameterized by a player type
    /// enumeration.
    /// </summary>
    /// <remarks><para> <see cref="AgentController{TypeOfPlayer}"/> is intended to be inherited by controllers
    /// that manage the creation and setup of agent avatars based on player type. It handles the loading and disposal of
    /// agent and character prefabs using addressable assets, and provides utility methods for spawning avatars at
    /// appropriate locations. </para> <para> The generic type parameter <typeparamref name="TypeOfPlayer"/> must be an
    /// <see cref="Enum"/>, representing the set of possible player or agent types supported by the controller.
    /// </para></remarks>
    /// <typeparam name="TypeOfPlayer">The enumeration type representing the player identity or role.</typeparam>
    public abstract class AgentController<TypeOfPlayer> : MonoBehaviour where TypeOfPlayer : Enum
    {
        private static AddressablesStaticDictionary<TypeOfPlayer> _characterPrefabs = 
            new("Assets/Prefabs/Agents/Characters/", ".prefab");
        
        protected static AddressablesStaticDictionary<PlayerControlAgent> _agentPrefabs = 
            new("Assets/Prefabs/Agents/", "AgentController.prefab");

        private void OnApplicationQuit()
        {
            _characterPrefabs?.Dispose();
            _agentPrefabs?.Dispose();
        }

        /// <summary>
        /// Creates and instantiates an avatar for the specified player type at the appropriate spawn point.
        /// </summary>
        /// <typeparam name="TypeOfHumanAgent">The type of avatar component to retrieve.</typeparam>
        /// <param name="playerType">The type of player for which to create the avatar.</param>
        /// <returns>The avatar component if successful, default value otherwise.</returns>
        protected TypeOfHumanAgent CreateAvatar<TypeOfHumanAgent>(TypeOfPlayer playerType) where TypeOfHumanAgent : PlayerAvatarBase
        {
            if (_characterPrefabs == null)
            {
                DebugHelper.LogError(this, "Character prefabs dictionary is not initialized");
                return default;
            }

            var character = _characterPrefabs.GetPrefab(playerType);
            if (character == null)
            {
                DebugHelper.LogError(this, $"Character prefab for player type {playerType} not found");
                return default;
            }

            Transform spawnPoint = PlayerSpawnPoint<TypeOfPlayer>.FindSpawnPoint(playerType);
            if (spawnPoint == null)
            {
                DebugHelper.LogError(this, $"Spawn point for player type {playerType} not found");
                return default;
            }

            var go = Instantiate(character, spawnPoint.position, spawnPoint.rotation, transform);
            go.name = $"AgentController [{nameof(TypeOfHumanAgent)}] ({playerType})";
            if (go == null)
            {
                DebugHelper.LogError(this, "Failed to instantiate character prefab");
                return default;
            }

            var avatar = go.GetComponentInChildren<TypeOfHumanAgent>();
            if (avatar == null)
            {
                DebugHelper.LogError(this, $"PlayerAvatar component of type {typeof(TypeOfHumanAgent).Name} not found on instantiated character");
                return default;
            }

            return avatar;
        }
    }
}