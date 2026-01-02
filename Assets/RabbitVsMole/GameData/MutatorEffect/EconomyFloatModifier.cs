using RabbitVsMole.GameData.Mutator;
using UnityEngine;

namespace RabbitVsMole.GameData
{
    [System.Serializable]
    public class EconomyFloatModifier : IMutatorEffect
    {
        public enum FloatType
        {
            WaterSourceMaxWaterLevel,
            WaterSourceWaterPerSec,
            WaterSourceWaterDrainPerAction,
            CarrotGrowingTimeInSec,
            CarrotSpoilTimeInSec,
            FarmFieldMaxWaterLevel,
            FarmFieldWaterDrainPerSec,
            FarmFieldWaterDrainByCarrotPerSec,
            FarmFieldWaterInsertPerAction,
        }

        public FloatType targetType;
        public float value;
        public bool isMultiplier;

        public void Apply(GameStats stats)
        {
            switch (targetType)
            {
                case FloatType.WaterSourceMaxWaterLevel:
                    stats.WaterSourceMaxWaterLevel = isMultiplier ? stats.WaterSourceMaxWaterLevel * value : stats.WaterSourceMaxWaterLevel + value;
                    break;
                case FloatType.WaterSourceWaterPerSec:
                    stats.WaterSourceWaterPerSec = isMultiplier ? stats.WaterSourceWaterPerSec * value : stats.WaterSourceWaterPerSec + value;
                    break;
                case FloatType.WaterSourceWaterDrainPerAction:
                    stats.WaterSourceWaterDrainPerAction = isMultiplier ? stats.WaterSourceWaterDrainPerAction * value : stats.WaterSourceWaterDrainPerAction + value;
                    break;
                case FloatType.CarrotGrowingTimeInSec:
                    stats.CarrotGrowingTimeInSec = isMultiplier ? stats.CarrotGrowingTimeInSec * value : stats.CarrotGrowingTimeInSec + value;
                    break;
                case FloatType.CarrotSpoilTimeInSec:
                    stats.CarrotSpoilTimeInSec = isMultiplier ? stats.CarrotSpoilTimeInSec * value : stats.CarrotSpoilTimeInSec + value;
                    break;
                case FloatType.FarmFieldMaxWaterLevel:
                    stats.FarmFieldMaxWaterLevel = isMultiplier ? stats.FarmFieldMaxWaterLevel * value : stats.FarmFieldMaxWaterLevel + value;
                    break;
                case FloatType.FarmFieldWaterDrainPerSec:
                    stats.FarmFieldWaterDrainPerSec = isMultiplier ? stats.FarmFieldWaterDrainPerSec * value : stats.FarmFieldWaterDrainPerSec + value;
                    break;
                case FloatType.FarmFieldWaterDrainByCarrotPerSec:
                    stats.FarmFieldWaterDrainByCarrotPerSec = isMultiplier ? stats.FarmFieldWaterDrainByCarrotPerSec * value : stats.FarmFieldWaterDrainByCarrotPerSec + value;
                    break;
                case FloatType.FarmFieldWaterInsertPerAction:
                    stats.FarmFieldWaterInsertPerAction = isMultiplier ? stats.FarmFieldWaterInsertPerAction * value : stats.FarmFieldWaterInsertPerAction + value;
                    break;
            }
        }
    }
}
