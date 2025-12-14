using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static GameManager;

public class AgentController : MonoBehaviour
{
    static AddressablesStaticDictionary<PlayerType> _characterPrefabs = new("Assets/Prefabs/Agents/Characters/", ".prefab");
    protected static AddressablesStaticDictionary<PlayerControlAgent> _agentPrefabs = new("Assets/Prefabs/Agents/", "AgentController.prefab");

    private void OnApplicationQuit()
    { 
        _characterPrefabs.Dispose();
        _agentPrefabs.Dispose();
    }

    protected PlayerAvatar CreateAvatar(PlayerType playerType)
    {
        var character = _characterPrefabs.GetPrefab(playerType);
        if (character == null)
        {
            DebugHelper.LogError(this, $"Character prefab for player type {playerType} not found");
            return null;
        }

        Transform spawnPoint = PlayerSpawnPoint.FindSpawnPoint(playerType);
        if (spawnPoint == null)
        {
            DebugHelper.LogError(this, $"Spawn point for player type {playerType} not found");
            return null;
        }

        var go = Instantiate(character, spawnPoint.position, spawnPoint.rotation, transform);
        if (go == null)
        {
            DebugHelper.LogError(this, "Failed to instantiate character prefab");
            return null;
        }

        var avatar = go.GetComponentInChildren<PlayerAvatar>();
        if (avatar == null)
        {
            DebugHelper.LogError(this, "PlayerAvatar component not found on instantiated character");
            return null;
        }

        return avatar;
    }

    public static void CreateAgentControllerForAllPlayerTypes(GameManager.PlayGameSettings playGameSettings)
    {
        foreach (var playerType in Enum.GetValues(typeof(PlayerType)))
        {
            CreateAC((PlayerType)playerType, playGameSettings);
        }
    }

    private static void CreateAC(PlayerType playerType, PlayGameSettings playGameSettings)
    {
        switch (playGameSettings.GetPlayerControlAgent(playerType))
        {
            case PlayerControlAgent.None:
                break;
            case PlayerControlAgent.Human:
                HumanAgentController.CreateInstance(playerType, playGameSettings.IsGamepadUsing(playerType));
                break;
            case PlayerControlAgent.Bot:
                //AIPlayerAgentController.CreateInstance(playerType);
                break;
            case PlayerControlAgent.Online:
                //OnlineAgentController.CreateInstance(playerType);
                break;
            default:
                break;
        }
    }
}
