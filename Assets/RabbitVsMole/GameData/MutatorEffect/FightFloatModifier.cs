using RabbitVsMole.GameData.Mutator;
using UnityEngine;

namespace RabbitVsMole.GameData
{
    [System.Serializable]
    public class FightFloatModifier : IMutatorEffect
    {
        public enum FloatType
        {
            FightRabbitAttackActionTime,
            FightMoleDeath,
            FightMoleStunTime,
            FightMoleRespawnTime,
        }

        public FloatType targetType;
        public float value;
        public bool isMultiplier;

        public void Apply(GameStats stats)
        {
            switch (targetType)
            {
                case FloatType.FightRabbitAttackActionTime:
                    stats.FightRabbitAttackActionTime = isMultiplier ? stats.FightRabbitAttackActionTime * value : stats.FightRabbitAttackActionTime + value;
                    break;
                case FloatType.FightMoleDeath:
                    stats.FightMoleDeath = isMultiplier ? stats.FightMoleDeath * value : stats.FightMoleDeath + value;
                    break;
                case FloatType.FightMoleStunTime:
                    stats.FightMoleStunTime = isMultiplier ? stats.FightMoleStunTime * value : stats.FightMoleStunTime + value;
                    break;
                case FloatType.FightMoleRespawnTime:
                    stats.FightMoleRespawnTime = isMultiplier ? stats.FightMoleRespawnTime * value : stats.FightMoleRespawnTime + value;
                    break;
            }
        }
    }
}
