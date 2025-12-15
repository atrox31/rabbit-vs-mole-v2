using System.Collections.Generic;
using UnityEngine;

namespace PlayerManagementSystem
{
    /// <summary>
    /// Component that marks a spawn point for a specific player type.
    /// Should be attached to GameObjects with tag "spawnPoint".
    /// </summary>
    [AddComponentMenu("")] // Prevent adding from the component menu
    public abstract class PlayerSpawnPoint<T> : MonoBehaviour where T : System.Enum
    {
        [SerializeField] private T playerType = default;

        /// <summary>
        /// Gets the player type associated with this spawn point.
        /// </summary>
        public T PlayerType => playerType;

        /// <summary>
        /// Finds the spawn point transform for the specified player type.
        /// </summary>
        /// <param name="playerType">The player type to find a spawn point for.</param>
        /// <returns>The transform of the spawn point if found, null otherwise.</returns>
        public static Transform FindSpawnPoint(T playerType)
        {
            var spawnPoints = FindObjectsByType<PlayerSpawnPoint<T>>(FindObjectsSortMode.None);
            
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                DebugHelper.LogError(null, $"No spawn points found for player type: {playerType}");
                return null;
            }

            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint == null)
                {
                    continue;
                }

                if (EqualityComparer<T>.Default.Equals(spawnPoint.PlayerType, playerType))
                {
                    return spawnPoint.transform;
                }
            }

            DebugHelper.LogError(null, $"Spawn point not found for player type: {playerType}");
            return null;
        }
    }
}
