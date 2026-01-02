using RabbitVsMole.GameData.Mutator;

namespace RabbitVsMole.GameData
{
    [System.Serializable]
    public class GameObjectsIntModifier : IMutatorEffect
    {
        public enum IntType
        {
            MoundHealthPoint,
            MoundDamageByRabbit,
            RootsHealthPoint,
            RootsDamageByRabbit,
            RootsDamageByMole,
            RootsSpreadRadius,
            WallDirtHealthPoint,
            WallDirtDamageByMole,
            WallDirtCollectPerAction,
        }

        public IntType targetType;
        public int value;
        public bool isMultiplier;

        public void Apply(GameStats stats)
        {
            switch (targetType)
            {
                case IntType.MoundHealthPoint:
                    stats.MoundHealthPoint = isMultiplier ? stats.MoundHealthPoint * value : stats.MoundHealthPoint + value;
                    break;
                case IntType.MoundDamageByRabbit:
                    stats.MoundDamageByRabbit = isMultiplier ? stats.MoundDamageByRabbit * value : stats.MoundDamageByRabbit + value;
                    break;
                case IntType.RootsHealthPoint:
                    stats.RootsHealthPoint = isMultiplier ? stats.RootsHealthPoint * value : stats.RootsHealthPoint + value;
                    break;
                case IntType.RootsDamageByRabbit:
                    stats.RootsDamageByRabbit = isMultiplier ? stats.RootsDamageByRabbit * value : stats.RootsDamageByRabbit + value;
                    break;
                case IntType.RootsDamageByMole:
                    stats.RootsDamageByMole = isMultiplier ? stats.RootsDamageByMole * value : stats.RootsDamageByMole + value;
                    break;
                case IntType.RootsSpreadRadius:
                    stats.RootsSpreadRadius = isMultiplier ? stats.RootsSpreadRadius * value : stats.RootsSpreadRadius + value;
                    break;
                case IntType.WallDirtHealthPoint:
                    stats.WallDirtHealthPoint = isMultiplier ? stats.WallDirtHealthPoint * value : stats.WallDirtHealthPoint + value;
                    break;
                case IntType.WallDirtDamageByMole:
                    stats.WallDirtDamageByMole = isMultiplier ? stats.WallDirtDamageByMole * value : stats.WallDirtDamageByMole + value;
                    break;
                case IntType.WallDirtCollectPerAction:
                    stats.WallDirtCollectPerAction = isMultiplier ? stats.WallDirtCollectPerAction * value : stats.WallDirtCollectPerAction + value;
                    break;
            }
        }
    }
}
