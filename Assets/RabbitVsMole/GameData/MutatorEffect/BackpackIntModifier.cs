using RabbitVsMole.GameData.Mutator;

namespace RabbitVsMole.GameData
{
    [System.Serializable]
    public class BackpackIntModifier : IMutatorEffect
    {
        public enum IntType
        {
            BackpackCapacitySeed,
            BackpackCapacityWater,
            BackpackCapacityDirt,
            BackpackCapacityCarrot,
        }

        public IntType targetType;
        public int value;
        public bool isMultiplier;

        public void Apply(GameStats stats)
        {
            switch (targetType)
            {
                case IntType.BackpackCapacitySeed:
                    stats.BackpackCapacitySeed = isMultiplier ? stats.BackpackCapacitySeed * value : stats.BackpackCapacitySeed + value;
                    break;
                case IntType.BackpackCapacityWater:
                    stats.BackpackCapacityWater = isMultiplier ? stats.BackpackCapacityWater * value : stats.BackpackCapacityWater + value;
                    break;
                case IntType.BackpackCapacityDirt:
                    stats.BackpackCapacityDirt = isMultiplier ? stats.BackpackCapacityDirt * value : stats.BackpackCapacityDirt + value;
                    break;
                case IntType.BackpackCapacityCarrot:
                    stats.BackpackCapacityCarrot = isMultiplier ? stats.BackpackCapacityCarrot * value : stats.BackpackCapacityCarrot + value;
                    break;
            }
        }
    }
}
