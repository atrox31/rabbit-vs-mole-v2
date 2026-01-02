using RabbitVsMole.GameData.Mutator;

namespace RabbitVsMole.GameData
{
    [System.Serializable]
    public class GameRulesBoolModifier : IMutatorEffect
    {
        public enum BoolType
        {
            GameRulesFightMoleAllowRegenerationOnSurface,
            GameRulesRootsCanSpawnOnCleanField,
            GameRulesRootsCanSpawnOnPlantedField,
            GameRulesRootsCanSpawnOnWithCarrotField,
            GameRulesRootsCanSpawnOnWithCarrotFullGrowField,
            GameRulesRootsCanSpawnOnMoundedField,
            GameRulesRootsAllowDamageRootsWithCarrotInHand,
            GameRulesMoleCanEnterUndergroundMoundWithCarrotInHand,
            GameRulesFarmFieldStartsWithRoots, 
            GameRulesAllowMoleToStealFromRabbitStorage,
            GameRulesAllowMolePickUpCarrotFromFarm 
        }                                     

        public BoolType targetType;
        public bool value;

        public void Apply(GameStats stats)
        {
            switch (targetType)
            {
                case BoolType.GameRulesFightMoleAllowRegenerationOnSurface:
                    stats.GameRulesFightMoleAllowRegenerationOnSurface = value;
                    break;
                case BoolType.GameRulesRootsCanSpawnOnCleanField:
                    stats.GameRulesRootsCanSpawnOnCleanField = value;
                    break;
                case BoolType.GameRulesRootsCanSpawnOnPlantedField:
                    stats.GameRulesRootsCanSpawnOnPlantedField = value;
                    break;
                case BoolType.GameRulesRootsCanSpawnOnWithCarrotField:
                    stats.GameRulesRootsCanSpawnOnWithCarrotField = value;
                    break;
                case BoolType.GameRulesRootsCanSpawnOnWithCarrotFullGrowField:
                    stats.GameRulesRootsCanSpawnOnWithCarrotFullGrowField = value;
                    break;
                case BoolType.GameRulesRootsCanSpawnOnMoundedField:
                    stats.GameRulesRootsCanSpawnOnMoundedField = value;
                    break;
                case BoolType.GameRulesRootsAllowDamageRootsWithCarrotInHand:
                    stats.GameRulesRootsAllowDamageRootsWithCarrotInHand = value;
                    break;
                case BoolType.GameRulesMoleCanEnterUndergroundMoundWithCarrotInHand:
                    stats.GameRulesMoleCanEnterUndergroundMoundWithCarrotInHand = value;
                    break;
                case BoolType.GameRulesFarmFieldStartsWithRoots:
                    stats.GameRulesFarmFieldStartsWithRoots = value;
                    break;
                case BoolType.GameRulesAllowMoleToStealFromRabbitStorage:
                    stats.GameRulesAllowMoleToStealFromRabbitStorage = value;
                    break;
                case BoolType.GameRulesAllowMolePickUpCarrotFromFarm:
                    stats.GameRulesAllowMolePickUpCarrotFromFarm = value;
                    break;
            }
        }
    }
}