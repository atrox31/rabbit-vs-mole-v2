using RabbitVsMole.GameData.Mutator;

namespace RabbitVsMole.GameData
{
    [System.Serializable]
    public class EconomyIntModifier : IMutatorEffect
    {
        public enum IntType
        {
            CostDirtForMoleMound,
            CostRabbitForWaterAction,
            CostRabbitForSeedAction,
            SeedStorageValuePerAction,
            WaterSourceWaterToInventoryPerDrain,
        }

        public IntType targetType;
        public int value;
        public bool isMultiplier;

        public void Apply(GameStats stats)
        {
            switch (targetType)
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
                case IntType.SeedStorageValuePerAction:
                    stats.SeedStorageValuePerAction = isMultiplier ? stats.SeedStorageValuePerAction * value : stats.SeedStorageValuePerAction + value;
                    break;
                case IntType.WaterSourceWaterToInventoryPerDrain:
                    stats.WaterSourceWaterToInventoryPerDrain = isMultiplier ? stats.WaterSourceWaterToInventoryPerDrain * value : stats.WaterSourceWaterToInventoryPerDrain + value;
                    break;
            }
        }
    }
}
