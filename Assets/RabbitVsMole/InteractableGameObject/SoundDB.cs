using RabbitVsMole.InteractableGameObject.Enums;
using System.Collections;
using System.Collections.Generic;

namespace RabbitVsMole.InteractableGameObject.SoundDB
{
    public class SoundDB
    {
        private static readonly Dictionary<long, string> _fastLookup = new Dictionary<long, string>()
        {
                { GetKey(ActionType.None, PlayerType.Rabbit), "pizzicato down 1" },
                { GetKey(ActionType.Attack, PlayerType.Rabbit), "269230__johnfolker__punch-4" },
                { GetKey(ActionType.Stun, PlayerType.Rabbit), string.Empty },
                { GetKey(ActionType.PlantSeed, PlayerType.Rabbit), "CarrotPickUpSound" },
                { GetKey(ActionType.WaterField, PlayerType.Rabbit), "421184__inspectorj__water-pouring-a" },
                { GetKey(ActionType.HarvestCarrot, PlayerType.Rabbit), "CarrotPickUpSound" },
                { GetKey(ActionType.RemoveRoots, PlayerType.Rabbit), "shovel" },
                //{ GetKey(ActionType.StealCarrotFromUndergroundField, PlayerType.Rabbit), "stealth_grab" },
                //{ GetKey(ActionType.DigUndergroundWall, PlayerType.Rabbit), "dig_dirt" },
                //{ GetKey(ActionType.DigMound, PlayerType.Rabbit), "mound_up" },
                { GetKey(ActionType.CollapseMound, PlayerType.Rabbit), "shovel" },
                //{ GetKey(ActionType.EnterMound, PlayerType.Rabbit), "tunnel_enter" },
                //{ GetKey(ActionType.ExitMound, PlayerType.Rabbit), "tunnel_exit" },
                { GetKey(ActionType.PickSeed, PlayerType.Rabbit), "420875__inspectorj__gathering-ice-a" },
                { GetKey(ActionType.PickWater, PlayerType.Rabbit), "536111__eminyildirim__water-drop" },
                { GetKey(ActionType.PutDownCarrot, PlayerType.Rabbit), "pizzicato up 2" },
                //{ GetKey(ActionType.StealCarrotFromStorage, PlayerType.Rabbit), "storage_steal" },

                { GetKey(ActionType.None, PlayerType.Mole), "pizzicato down 1" },
                { GetKey(ActionType.Attack, PlayerType.Mole), string.Empty },
                { GetKey(ActionType.Stun, PlayerType.Mole), "Goblin Scream" },
                //{ GetKey(ActionType.PlantSeed, PlayerType.Mole), "plant_seed_sound" },
                //{ GetKey(ActionType.WaterField, PlayerType.Mole), "water_splash" },
                { GetKey(ActionType.HarvestCarrot, PlayerType.Mole), "CarrotPickUpSound" },
                { GetKey(ActionType.RemoveRoots, PlayerType.Mole), "shovel" },
                { GetKey(ActionType.StealCarrotFromUndergroundField, PlayerType.Mole), "tom" },
                { GetKey(ActionType.DigUndergroundWall, PlayerType.Mole), "Falling Rock" },
                { GetKey(ActionType.DigMound, PlayerType.Mole), "digging a mold" },
                //{ GetKey(ActionType.CollapseMound, PlayerType.Mole), "mound_collapse" },
                { GetKey(ActionType.EnterMound, PlayerType.Mole), "teleport" },
                { GetKey(ActionType.ExitMound, PlayerType.Mole), "wind short 2" },
                //{ GetKey(ActionType.PickSeed, PlayerType.Mole), "seed_pickup" },
                //{ GetKey(ActionType.PickWater, PlayerType.Mole), "water_pickup" },
                { GetKey(ActionType.PutDownCarrot, PlayerType.Mole), "pizzicato up 2" },
                { GetKey(ActionType.StealCarrotFromStorage, PlayerType.Mole), "pizzicato down 2" }
            };
        private static long GetKey(ActionType type, PlayerType player)
        {
            return ((long)type << 32) | (uint)player;
        }

        public static string GetSound(ActionType type, PlayerType player)
        {
            if (_fastLookup.TryGetValue(GetKey(type, player), out string value))
                return $"Assets/Audio/Sound/sfx/{value}.ogg";
            else
                return null;
            
        }

    }
}