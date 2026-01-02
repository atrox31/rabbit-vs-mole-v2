using RabbitVsMole.GameData.Mutator;
using UnityEngine;

namespace RabbitVsMole.GameData
{
    [System.Serializable]
    public class GameObjectsFloatModifier : IMutatorEffect
    {
        public enum FloatType
        {
            MoundCreateTime,
            MoundCreateTimePernalityForNotCleanUnderground,
            RootsBirthChance,
            RootsTickRate,
            RootsSpreadChance,
            RootsSpreadIncreaseByNeibour,
            WallDestroyTime,
            WallBuildTime,
        }

        public FloatType targetType;
        public float value;
        public bool isMultiplier;

        public void Apply(GameStats stats)
        {
            switch (targetType)
            {
                case FloatType.MoundCreateTime:
                    stats.MoundCreateTime = isMultiplier ? stats.MoundCreateTime * value : stats.MoundCreateTime + value;
                    break;
                case FloatType.MoundCreateTimePernalityForNotCleanUnderground:
                    stats.MoundCreateTimePernalityForNotCleanUnderground = isMultiplier ? stats.MoundCreateTimePernalityForNotCleanUnderground * value : stats.MoundCreateTimePernalityForNotCleanUnderground + value;
                    break;
                case FloatType.RootsBirthChance:
                    stats.RootsBirthChance = isMultiplier ? stats.RootsBirthChance * value : stats.RootsBirthChance + value;
                    break;
                case FloatType.RootsTickRate:
                    stats.RootsTickRate = isMultiplier ? stats.RootsTickRate * value : stats.RootsTickRate + value;
                    break;
                case FloatType.RootsSpreadChance:
                    stats.RootsSpreadChance = isMultiplier ? stats.RootsSpreadChance * value : stats.RootsSpreadChance + value;
                    break;
                case FloatType.RootsSpreadIncreaseByNeibour:
                    stats.RootsSpreadIncreaseByNeibour = isMultiplier ? stats.RootsSpreadIncreaseByNeibour * value : stats.RootsSpreadIncreaseByNeibour + value;
                    break;
                case FloatType.WallDestroyTime:
                    stats.WallDestroyTime = isMultiplier ? stats.WallDestroyTime * value : stats.WallDestroyTime + value;
                    break;
                case FloatType.WallBuildTime:
                    stats.WallBuildTime = isMultiplier ? stats.WallBuildTime * value : stats.WallBuildTime + value;
                    break;
            }
        }
    }
}
