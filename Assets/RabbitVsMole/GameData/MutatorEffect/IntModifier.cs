using RabbitVsMole.GameData.Mutator;

namespace RabbitVsMole.GameData
{
    [System.Serializable]
    public class IntModifier : IMutatorEffect
    {
        public enum IntType
        {
            CostDirtForMoleMound,
            CostRabbitForWaterAction,
            CostRabbitForSeedAction,
            BackpackCapacitySeed,
            BackpackCapacityWater,
            BackpackCapacityDirt,
            BackpackCapacityCarrot,
        }
        public IntType intType;
        public int value;
        public bool isMultiplier;

        public void Apply(GameStats stats)
        {
            switch (intType)
            {
                case IntType.CostDirtForMoleMound:
                    stats.CostDirtForMoleMound = isMultiplier ? stats.CostDirtForMoleMound * value : stats.CostDirtForMoleMound + value;
                    break;
                case IntType.CostRabbitForWaterAction:
                    stats.CostRabbitForWaterAction = isMultiplier ? stats.CostRabbitForWaterAction * value : stats.CostRabbitForWaterAction + value;
                    break;
                case IntType.CostRabbitForSeedAction:
                    stats.CostRabbitForSeedAction = isMultiplier ? stats.CostRabbitForSeedAction * value : stats.CostRabbitForSeedAction + value;
                    break;
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

