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

            Carrot = new BackpackItem(playerType, BackpackItemType.Carrot, GameInspector.GameStats.BackpackCapacityCarrot);

            if (playerType == PlayerType.Rabbit)
            {
                Seed = new BackpackItem(playerType, BackpackItemType.Seed, GameInspector.GameStats.BackpackCapacitySeed);
                Water = new BackpackItem(playerType, BackpackItemType.Water, GameInspector.GameStats.BackpackCapacityWater);
                return;
            }

            if (playerType == PlayerType.Mole)
            {
                Dirt = new BackpackItem(playerType, BackpackItemType.Dirt, GameInspector.GameStats.BackpackCapacityDirt);
                Health = new BackpackItem(playerType, BackpackItemType.Health, GameInspector.GameStats.FightMoleHealthPoints, true);
            }
        }

        public BackpackItem Seed;
        public BackpackItem Water;
        public BackpackItem Dirt;
        public BackpackItem Carrot;
        public BackpackItem Health;
    }
}

