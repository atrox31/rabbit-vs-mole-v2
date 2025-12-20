using Extensions;
using InputManager;
using PlayerManagementSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RabbitVsMole
{
    public partial class GameManager
    {
        public struct PlayGameSettings
        {
            public GameModeData gameMode;
            public GameSceneManager.SceneType map;
            public DayOfWeek day;
            public PlayerType playerTypeForStory;
            public Dictionary<PlayerType, PlayerControlAgent> playerControlAgent;
            public Dictionary<PlayerType, bool> playerGamepadUsing;

            public readonly bool IsAllHumanAgents => playerControlAgent.Select(kv => kv.Value).All(v => v == PlayerControlAgent.Human);

            public PlayGameSettings(
                GameModeData gameMode,
                GameSceneManager.SceneType map,
                DayOfWeek day,
                PlayerType playerTypeForStory,
                PlayerControlAgent rabbitControlAgent,
                PlayerControlAgent moleControlAgent)
            {
                this.gameMode = gameMode;
                this.map = map;
                this.day = day;
                this.playerTypeForStory = playerTypeForStory;
                this.playerGamepadUsing = new Dictionary<PlayerType, bool>().PopulateWithEnumValues();
                this.playerControlAgent = new Dictionary<PlayerType, PlayerControlAgent>()
                        {
                            { PlayerType.Rabbit, rabbitControlAgent },
                            { PlayerType.Mole, moleControlAgent }
                        };
            }
            public PlayerControlAgent GetPlayerControlAgent(PlayerType playerType)
            {
                if (playerControlAgent.ContainsKey(playerType))
                    return playerControlAgent[playerType];
                else
                    return PlayerControlAgent.None;
            }
            public bool IsGamepadUsing(PlayerType playerType)
            {
                if (playerGamepadUsing.ContainsKey(playerType))
                    return playerGamepadUsing[playerType];
                else
                    return false;
            }
            public PlayerType? GetSplitscreenOnlyGamepadPlayerType()
            {
                var gamepadUsers = playerGamepadUsing.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
                if (gamepadUsers.Count == 1)
                    return gamepadUsers[0];
                return null;
            }
            public PlayGameSettings SetGamepadForPlayer(PlayerType playerType)
            {
                playerGamepadUsing[PlayerType.Rabbit] = false;
                playerGamepadUsing[PlayerType.Mole] = false;

                if (InputDeviceManager.GamepadCount > 0)
                    playerGamepadUsing[playerType] = true;

                return this;
            }
            public PlayGameSettings SetGamepadForBoth()
            {
                if (InputDeviceManager.GamepadCount >= 2)
                {
                    playerGamepadUsing[PlayerType.Rabbit] = true;
                    playerGamepadUsing[PlayerType.Mole] = true;
                }
                return this;
            }
            public string ToStringDebug()
            {
                return $"PlayGameSettings: GameMode={gameMode}, Map={map}, Day={day}, PlayerTypeForStory={playerTypeForStory}, RabbitControlAgent={GetPlayerControlAgent(PlayerType.Rabbit)}, MoleControlAgent={GetPlayerControlAgent(PlayerType.Mole)}, RabbitGamepadUsing={playerGamepadUsing[PlayerType.Rabbit]}, MoleGamepadUsing={playerGamepadUsing[PlayerType.Mole]}";
            }
        }
    }
}