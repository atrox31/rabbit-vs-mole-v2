using UnityEngine;

/// <summary>
/// Component that marks a spawn point for a specific player type.
/// Should be attached to GameObjects with tag "spawnPoint".
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    [SerializeField] private PlayerType playerType = PlayerType.Rabbit;

    public PlayerType PlayerType => playerType;
    public static Transform FindSpawnPoint(PlayerType playerType)
    {
        var spawnPoints = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
        foreach (var spawnPoint in spawnPoints)
        {
            if (spawnPoint.PlayerType == playerType)
                return spawnPoint.transform;
        }
        DebugHelper.LogError(null, $"Spawn point not found for player type: {playerType}");
        return null;
    }
}

