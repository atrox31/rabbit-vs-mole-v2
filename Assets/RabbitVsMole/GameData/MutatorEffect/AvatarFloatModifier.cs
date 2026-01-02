using RabbitVsMole.GameData.Mutator;
using UnityEngine;

namespace RabbitVsMole.GameData
{
    [System.Serializable]
    public class AvatarFloatModifier : IMutatorEffect
    {
        public enum FloatType
        {
            StatsBaseWalkingSpeedRabbit,
            StatsBaseRotationSpeedRabbit,
            StatsBaseAccelerationRabbit,
            StatsBaseDecelerationRabbit,
            StatsBaseWalkingSpeedMole,
            StatsBaseRotationSpeedMole,
            StatsBaseAccelerationMole,
            StatsBaseDecelerationMole,
        }

        public FloatType targetType;
        public float value;
        public bool isMultiplier;

        public void Apply(GameStats stats)
        {
            switch (targetType)
            {
                case FloatType.StatsBaseWalkingSpeedRabbit:
                    stats.StatsBaseWalkingSpeedRabbit = isMultiplier ? stats.StatsBaseWalkingSpeedRabbit * value : stats.StatsBaseWalkingSpeedRabbit + value;
                    break;
                case FloatType.StatsBaseRotationSpeedRabbit:
                    stats.StatsBaseRotationSpeedRabbit = isMultiplier ? stats.StatsBaseRotationSpeedRabbit * value : stats.StatsBaseRotationSpeedRabbit + value;
                    break;
                case FloatType.StatsBaseAccelerationRabbit:
                    stats.StatsBaseAccelerationRabbit = isMultiplier ? stats.StatsBaseAccelerationRabbit * value : stats.StatsBaseAccelerationRabbit + value;
                    break;
                case FloatType.StatsBaseDecelerationRabbit:
                    stats.StatsBaseDecelerationRabbit = isMultiplier ? stats.StatsBaseDecelerationRabbit * value : stats.StatsBaseDecelerationRabbit + value;
                    break;
                case FloatType.StatsBaseWalkingSpeedMole:
                    stats.StatsBaseWalkingSpeedMole = isMultiplier ? stats.StatsBaseWalkingSpeedMole * value : stats.StatsBaseWalkingSpeedMole + value;
                    break;
                case FloatType.StatsBaseRotationSpeedMole:
                    stats.StatsBaseRotationSpeedMole = isMultiplier ? stats.StatsBaseRotationSpeedMole * value : stats.StatsBaseRotationSpeedMole + value;
                    break;
                case FloatType.StatsBaseAccelerationMole:
                    stats.StatsBaseAccelerationMole = isMultiplier ? stats.StatsBaseAccelerationMole * value : stats.StatsBaseAccelerationMole + value;
                    break;
                case FloatType.StatsBaseDecelerationMole:
                    stats.StatsBaseDecelerationMole = isMultiplier ? stats.StatsBaseDecelerationMole * value : stats.StatsBaseDecelerationMole + value;
                    break;
            }
        }
    }
}
