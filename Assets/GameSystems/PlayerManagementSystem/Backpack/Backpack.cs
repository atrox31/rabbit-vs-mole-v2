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

            Carrot = new BackpackItem(playerType, BackpackItemType.Carrot, GameManager.CurrentGameStats.BackpackCapacityCarrot);

            if (playerType == PlayerType.Rabbit)
            {
                Seed = new BackpackItem(playerType, BackpackItemType.Seed, GameManager.CurrentGameStats.BackpackCapacitySeed);
                Water = new BackpackItem(playerType, BackpackItemType.Water, GameManager.CurrentGameStats.BackpackCapacityWater);
                return;
            }

            if (playerType == PlayerType.Mole)
            {
                Dirt = new BackpackItem(playerType, BackpackItemType.Dirt, GameManager.CurrentGameStats.BackpackCapacityDirt);
                Health = new BackpackItem(playerType, BackpackItemType.Health, GameManager.CurrentGameStats.FightMoleHealthPoints, true);
            }
        }

        public BackpackItem Seed;
        public BackpackItem Water;
        public BackpackItem Dirt;
        public BackpackItem Carrot;
        public BackpackItem Health;
    }
}

