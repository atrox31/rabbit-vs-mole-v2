using RabbitVsMole;
using System.Collections.Generic;

namespace PlayerManagementSystem.Backpack
{
    public class Backpack
    {
        private PlayerType _playerType;
        public PlayerType PlayerType => _playerType;

        public Backpack(PlayerType playerType)
        {
            _playerType = playerType;
            Seed = new BackpackItem(playerType, BackpackItemType.Seed, GameInspector.GameStats.BackpackCapacitySeed);
            Water = new BackpackItem(playerType, BackpackItemType.Water, GameInspector.GameStats.BackpackCapacityWater);
            Dirt = new BackpackItem(playerType, BackpackItemType.Dirt, GameInspector.GameStats.BackpackCapacityDirt);
            Carrot = new BackpackItem(playerType, BackpackItemType.Carrot, GameInspector.GameStats.BackpackCapacityCarrot);
        }

        public BackpackItem Seed;
        public BackpackItem Water;
        public BackpackItem Dirt;
        public BackpackItem Carrot;
    }
}

