using UnityEngine;
using UnityEngine.Localization;

namespace RabbitVsMole
{
    public enum GameModeWinCondition
    {
        Sandbox,            // no win condition
        CarrotCollection,   // who collect target carrot number first
        TimeLimit,          // who has the most carrots when time runs out
        Rivalry,           // rabbit when collects carrots or mole when time runs out
        Cooperation         // players work together to achieve a common goal and win or lose together
    }

    [System.Serializable]
    [CreateAssetMenu(fileName = "GameModeData", menuName = "MainMenu/GameModeData", order = 1)]
    public class GameModeData : ScriptableObject
    {
        [Header("Game Mode Information")]
        public LocalizedString modeName;
        public Sprite modeImage;
        public LocalizedString modeDescription;
        public LocalizedString modeConfiguration;

        [Header("Game Mode Settings")]
        public bool allowMultiplayer = true;
        public int carrotGoal = 0;
        public float timeLimitInMinutes = 0;
        public GameModeWinCondition winCondition = GameModeWinCondition.Sandbox;
        public bool AllowFight =>
            winCondition != GameModeWinCondition.Cooperation;
    }
}