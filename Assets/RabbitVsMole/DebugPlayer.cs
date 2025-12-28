using PlayerManagementSystem;
using RabbitVsMole;
using System;
using UnityEngine;

public class DebugPlayer : MonoBehaviour
{
    private void Awake()
    {
#if !UNITY_EDITOR
        // destroy object in release 
        DestroyImmediate(gameObject);
#endif
    }

    public static void DebugPlayDuelSplitscreen()
    {
        GameManager.PlayGame(
            gameMode: ScriptableObject.CreateInstance<GameModeData>(),
            map: GameSceneManager.SceneType.GamePlay_Duel,
            day: DayOfWeek.Monday,
            playerTypeForStory: PlayerType.Rabbit,
            rabbitControlAgent: PlayerControlAgent.Human,
            moleControlAgent: PlayerControlAgent.Human);
    }

    public static void DebugPlayDuelVsAiAsRabbit()
    {
        GameManager.PlayGame(
            gameMode: ScriptableObject.CreateInstance<GameModeData>(),
            map: GameSceneManager.SceneType.GamePlay_Duel,
            day: DayOfWeek.Monday,
            playerTypeForStory: PlayerType.Rabbit,
            rabbitControlAgent: PlayerControlAgent.Human,
            moleControlAgent: PlayerControlAgent.Bot);
    }

    public static void DebugPlayDuelVsAiAsMole()
    {
        GameManager.PlayGame(
            gameMode: ScriptableObject.CreateInstance<GameModeData>(),
            map: GameSceneManager.SceneType.GamePlay_Duel,
            day: DayOfWeek.Monday,
            playerTypeForStory: PlayerType.Rabbit,
            rabbitControlAgent: PlayerControlAgent.Bot,
            moleControlAgent: PlayerControlAgent.Human);
    }

    public static void DebugPlaySoloRabbit(int dayOfWeek)
    {
        GameManager.PlayGame(
            gameMode: ScriptableObject.CreateInstance<GameModeData>(),
            map: GameSceneManager.SceneType.Gameplay_RabbitSolo,
            day: (DayOfWeek)dayOfWeek,
            playerTypeForStory: PlayerType.Rabbit,
            rabbitControlAgent: PlayerControlAgent.Human,
            moleControlAgent: PlayerControlAgent.None);
    }

    public static void DebugPlayBot()
    {
            GameManager.PlayGame(
            gameMode: ScriptableObject.CreateInstance<GameModeData>(),
            map: GameSceneManager.SceneType.Debug_BotTest,
            day: DayOfWeek.Monday,
            playerTypeForStory: PlayerType.Rabbit,
            rabbitControlAgent: PlayerControlAgent.Bot,
            moleControlAgent: PlayerControlAgent.Bot);
    }
}