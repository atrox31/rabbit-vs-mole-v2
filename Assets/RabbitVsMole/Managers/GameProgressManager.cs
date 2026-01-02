using System;
using System.Collections.Generic;
using UnityEngine;

namespace RabbitVsMole
{
    /// <summary>
    /// Manages persistent player progress across game sessions.
    /// Handles golden carrot collection state and story progress.
    /// </summary>
    public class GameProgressManager : MonoBehaviour
    {
        private Dictionary<DayOfWeek, bool> _goldenCarrotPickStateRabbit = new Dictionary<DayOfWeek, bool>();
        private Dictionary<DayOfWeek, bool> _goldenCarrotPickStateMole = new Dictionary<DayOfWeek, bool>();
        private Dictionary<DayOfWeek, bool> _playerStoryProgressRabbit = new Dictionary<DayOfWeek, bool>();
        private Dictionary<DayOfWeek, bool> _playerStoryProgressMole = new Dictionary<DayOfWeek, bool>();

        /// <summary>
        /// Loads all progress data from PlayerPrefs.
        /// Should be called during initialization.
        /// </summary>
        public void LoadProgress()
        {
            _goldenCarrotPickStateRabbit = GameProgressDataManager.LoadStatus(PlayerPrefsConst.RABBIT_GOLDEN_CARROT_DATA);
            _goldenCarrotPickStateMole = GameProgressDataManager.LoadStatus(PlayerPrefsConst.MOLE_GOLDEN_CARROT_DATA);
            _playerStoryProgressRabbit = GameProgressDataManager.LoadStatus(PlayerPrefsConst.RABBIT_STORY_PROGRESS);
            _playerStoryProgressMole = GameProgressDataManager.LoadStatus(PlayerPrefsConst.MOLE_STORY_PROGRESS);
        }

        /// <summary>
        /// Saves all progress data to PlayerPrefs.
        /// </summary>
        public void SaveProgress()
        {
            GameProgressDataManager.SaveStatus(_goldenCarrotPickStateRabbit, PlayerPrefsConst.RABBIT_GOLDEN_CARROT_DATA);
            GameProgressDataManager.SaveStatus(_goldenCarrotPickStateMole, PlayerPrefsConst.MOLE_GOLDEN_CARROT_DATA);
            GameProgressDataManager.SaveStatus(_playerStoryProgressRabbit, PlayerPrefsConst.RABBIT_STORY_PROGRESS);
            GameProgressDataManager.SaveStatus(_playerStoryProgressMole, PlayerPrefsConst.MOLE_STORY_PROGRESS);
        }

        /// <summary>
        /// Sets story progress for a specific day and player.
        /// Automatically saves after setting.
        /// </summary>
        public void SetStoryProgress(DayOfWeek dayOfWeek, PlayerType playerType, bool value)
        {
            switch (playerType)
            {
                case PlayerType.Rabbit:
                    _playerStoryProgressRabbit[dayOfWeek] = value;
                    break;
                case PlayerType.Mole:
                    _playerStoryProgressMole[dayOfWeek] = value;
                    break;
                default:
                    DebugHelper.LogWarning(this, $"GameProgressManager.SetStoryProgress: Unknown player type '{playerType}'");
                    return;
            }

            SaveProgress();
        }

        /// <summary>
        /// Gets story progress for a specific day and player.
        /// </summary>
        public bool GetStoryProgress(DayOfWeek dayOfWeek, PlayerType playerType)
        {
            switch (playerType)
            {
                case PlayerType.Rabbit:
                    return _playerStoryProgressRabbit.ContainsKey(dayOfWeek) && _playerStoryProgressRabbit[dayOfWeek];
                case PlayerType.Mole:
                    return _playerStoryProgressMole.ContainsKey(dayOfWeek) && _playerStoryProgressMole[dayOfWeek];
                default:
                    DebugHelper.LogWarning(this, $"GameProgressManager.GetStoryProgress: Unknown player type '{playerType}'");
                    return false;
            }
        }

        /// <summary>
        /// Marks a golden carrot as collected for the current session.
        /// Should be called from game session via GameInspector.
        /// </summary>
        public void SetGoldenCarrotCollected(DayOfWeek dayOfWeek, PlayerType playerType)
        {
            switch (playerType)
            {
                case PlayerType.Rabbit:
                    _goldenCarrotPickStateRabbit[dayOfWeek] = true;
                    break;
                case PlayerType.Mole:
                    _goldenCarrotPickStateMole[dayOfWeek] = true;
                    break;
                default:
                    DebugHelper.LogWarning(this, $"GameProgressManager.SetGoldenCarrotCollected: Unknown player type '{playerType}'");
                    return;
            }

            SaveProgress();
        }

        /// <summary>
        /// Checks if a golden carrot was collected for a specific day and player.
        /// </summary>
        public bool IsGoldenCarrotCollected(DayOfWeek dayOfWeek, PlayerType playerType)
        {
            switch (playerType)
            {
                case PlayerType.Rabbit:
                    return _goldenCarrotPickStateRabbit.ContainsKey(dayOfWeek) && _goldenCarrotPickStateRabbit[dayOfWeek];
                case PlayerType.Mole:
                    return _goldenCarrotPickStateMole.ContainsKey(dayOfWeek) && _goldenCarrotPickStateMole[dayOfWeek];
                default:
                    DebugHelper.LogWarning(this, $"GameProgressManager.IsGoldenCarrotCollected: Unknown player type '{playerType}'");
                    return false;
            }
        }

        /// <summary>
        /// Resets all story progress for a specific player.
        /// </summary>
        public void ResetStoryProgress(PlayerType playerType)
        {
            switch (playerType)
            {
                case PlayerType.Rabbit:
                    foreach (var day in Enum.GetValues(typeof(DayOfWeek)))
                    {
                        _playerStoryProgressRabbit[(DayOfWeek)day] = false;
                    }
                    break;
                case PlayerType.Mole:
                    foreach (var day in Enum.GetValues(typeof(DayOfWeek)))
                    {
                        _playerStoryProgressMole[(DayOfWeek)day] = false;
                    }
                    break;
                default:
                    DebugHelper.LogWarning(this, $"GameProgressManager.ResetStoryProgress: Unknown player type '{playerType}'");
                    return;
            }

            SaveProgress();
        }

        /// <summary>
        /// Resets all golden carrot progress for a specific player.
        /// </summary>
        public void ResetGoldenCarrotProgress(PlayerType playerType)
        {
            switch (playerType)
            {
                case PlayerType.Rabbit:
                    foreach (var day in Enum.GetValues(typeof(DayOfWeek)))
                    {
                        _goldenCarrotPickStateRabbit[(DayOfWeek)day] = false;
                    }
                    break;
                case PlayerType.Mole:
                    foreach (var day in Enum.GetValues(typeof(DayOfWeek)))
                    {
                        _goldenCarrotPickStateMole[(DayOfWeek)day] = false;
                    }
                    break;
                default:
                    DebugHelper.LogWarning(this, $"GameProgressManager.ResetGoldenCarrotProgress: Unknown player type '{playerType}'");
                    return;
            }

            SaveProgress();
        }

        /// <summary>
        /// Marks the current day as completed for the specified player.
        /// This sets story progress to true for the current day.
        /// </summary>
        public void CompleteCurrentDay(DayOfWeek dayOfWeek, PlayerType playerType)
        {
            SetStoryProgress(dayOfWeek, playerType, true);
        }
    }
}
