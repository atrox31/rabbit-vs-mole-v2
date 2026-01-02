using RabbitVsMole.GameData.Mutator;
using UnityEngine;

namespace RabbitVsMole.GameData
{
    [System.Serializable]
    public class ActionTimesFloatModifier : IMutatorEffect
    {
        public enum FloatType
        {
            TimeActionPlantSeed,
            TimeActionWaterField,
            TimeActionHarvestCarrot,
            TimeActionRemoveRoots,
            TimeActionStealCarrotFromUndergroundField,
            TimeActionDigUndergroundWall,
            TimeActionDigMoundUnderground,
            TimeActionDigMoundOnSurface,
            TimeActionCollapseMound,
            TimeActionEnterMound,
            TimeActionExitMound,
            TimeActionPickSeed,
            TimeActionPickWater,
            TimeActionPutDownCarrot,
            TimeActionStealCarrotFromStorage,
        }

        public FloatType targetType;
        public float value;
        public bool isMultiplier;

        public void Apply(GameStats stats)
        {
            switch (targetType)
            {
                case FloatType.TimeActionPlantSeed:
                    stats.TimeActionPlantSeed = isMultiplier ? stats.TimeActionPlantSeed * value : stats.TimeActionPlantSeed + value;
                    break;
                case FloatType.TimeActionWaterField:
                    stats.TimeActionWaterField = isMultiplier ? stats.TimeActionWaterField * value : stats.TimeActionWaterField + value;
                    break;
                case FloatType.TimeActionHarvestCarrot:
                    stats.TimeActionHarvestCarrot = isMultiplier ? stats.TimeActionHarvestCarrot * value : stats.TimeActionHarvestCarrot + value;
                    break;
                case FloatType.TimeActionRemoveRoots:
                    stats.TimeActionRemoveRoots = isMultiplier ? stats.TimeActionRemoveRoots * value : stats.TimeActionRemoveRoots + value;
                    break;
                case FloatType.TimeActionStealCarrotFromUndergroundField:
                    stats.TimeActionStealCarrotFromUndergroundField = isMultiplier ? stats.TimeActionStealCarrotFromUndergroundField * value : stats.TimeActionStealCarrotFromUndergroundField + value;
                    break;
                case FloatType.TimeActionDigUndergroundWall:
                    stats.TimeActionDigUndergroundWall = isMultiplier ? stats.TimeActionDigUndergroundWall * value : stats.TimeActionDigUndergroundWall + value;
                    break;
                case FloatType.TimeActionDigMoundUnderground:
                    stats.TimeActionDigMoundUnderground = isMultiplier ? stats.TimeActionDigMoundUnderground * value : stats.TimeActionDigMoundUnderground + value;
                    break;
                case FloatType.TimeActionDigMoundOnSurface:
                    stats.TimeActionDigMoundOnSurface = isMultiplier ? stats.TimeActionDigMoundOnSurface * value : stats.TimeActionDigMoundOnSurface + value;
                    break;
                case FloatType.TimeActionCollapseMound:
                    stats.TimeActionCollapseMound = isMultiplier ? stats.TimeActionCollapseMound * value : stats.TimeActionCollapseMound + value;
                    break;
                case FloatType.TimeActionEnterMound:
                    stats.TimeActionEnterMound = isMultiplier ? stats.TimeActionEnterMound * value : stats.TimeActionEnterMound + value;
                    break;
                case FloatType.TimeActionExitMound:
                    stats.TimeActionExitMound = isMultiplier ? stats.TimeActionExitMound * value : stats.TimeActionExitMound + value;
                    break;
                case FloatType.TimeActionPickSeed:
                    stats.TimeActionPickSeed = isMultiplier ? stats.TimeActionPickSeed * value : stats.TimeActionPickSeed + value;
                    break;
                case FloatType.TimeActionPickWater:
                    stats.TimeActionPickWater = isMultiplier ? stats.TimeActionPickWater * value : stats.TimeActionPickWater + value;
                    break;
                case FloatType.TimeActionPutDownCarrot:
                    stats.TimeActionPutDownCarrot = isMultiplier ? stats.TimeActionPutDownCarrot * value : stats.TimeActionPutDownCarrot + value;
                    break;
                case FloatType.TimeActionStealCarrotFromStorage:
                    stats.TimeActionStealCarrotFromStorage = isMultiplier ? stats.TimeActionStealCarrotFromStorage * value : stats.TimeActionStealCarrotFromStorage + value;
                    break;
            }
        }
    }
}
