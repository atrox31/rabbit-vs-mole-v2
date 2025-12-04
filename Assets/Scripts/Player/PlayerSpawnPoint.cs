using UnityEngine;

/// <summary>
/// Component that marks a spawn point for a specific player type.
/// Should be attached to GameObjects with tag "spawnPoint".
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    [SerializeField] private PlayerType playerType = PlayerType.Rabbit;

    public PlayerType PlayerType => playerType;
}

