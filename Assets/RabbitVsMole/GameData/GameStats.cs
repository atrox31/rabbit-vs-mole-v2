using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.AI;
using System.Diagnostics.Contracts;
using Unity.Behavior;
using UnityEditor.ShaderGraph.Internal;

namespace RabbitVsMole.GameData
{
    [System.Serializable]
    public class GameStats
    {
        // Avatar stats
        public float StatsBaseWalkingSpeedRabbit = 4.5f;
        public float StatsBaseRotationSpeedRabbit = 4.5f;
        public float StatsBaseAccelerationRabbit = 10f;
        public float StatsBaseDecelerationRabbit = 20f;

        public float StatsBaseWalkingSpeedMole = 5.5f;
        public float StatsBaseRotationSpeedMole = 5.5f;
        public float StatsBaseAccelerationMole = 10f;
        public float StatsBaseDecelerationMole = 20f;

        // Backpack
        public int BackpackCapacitySeed = 3;
        public int BackpackCapacityWater = 8;
        public int BackpackCapacityDirt = 5;
        public int BackpackCapacityCarrot = 1;

        // Cost - Mole
        public int CostDirtForMoleMound = 3;

        // Cost - Rabbit
        public int CostRabbitForWaterAction = 1;
        public int CostRabbitForSeedAction = 1;

        // Seed storage
        public int SeedStorageValuePerAction = 3;

        // WaterSource
        public float WaterSourceMaxWaterLevel = 10f;
        public float WaterSourceWaterPerSec = 0.5f;
        public float WaterSourceWaterDrainPerAction = 4f;
        public int WaterSourceWaterToInventoryPerDrain = 4;

        // Carrot
        public float CarrotGrowingTimeInSec = 6f;
        public float CarrotSpoilTimeInSec = 60f;

        // FarmField
        public float FarmFieldMaxWaterLevel = 4f;
        public float FarmFieldWaterDrainPerSec = 0f;
        public float FarmFieldWaterDrainByCarrotPerSec = 1f;
        public float FarmFieldWaterInsertPerAction = 3f;

        // Mound
        public float MoundCreateTime = 4f;
        public float MoundCreateTimePernalityForNotCleanUnderground = 3f;
        public int MoundHealthPoint = 2;
        public int MoundDamageByRabbit = 1;

        // Roots
        public int RootsHealthPoint = 10;
        public int RootsDamageByRabbit = 3;
        public int RootsDamageByMole = 5;
        public float RootsBirthChance = 0.33f;
        public float RootsTickRate = 0.1f;
        public float RootsSpreadChance = 0.05f;
        public float RootsSpreadIncreaseByNeibour = 0.10f;
        public int RootsSpreadRadius = 1;

        public bool RootsCanSpawnOnCleanField = true;
        public bool RootsCanSpawnOnPlantedField = true;
        public bool RootsCanSpawnOnWithCarrotField = true;

        public bool RootsCanSpawnOnWithCarrotFullGrowField = false;
        public bool RootsCanSpawnOnMoundedField = false;

        // Wall
        public int WallDirtCollectPerAction = 1;
        public float WallDestroyTime = 1.33f;
        public float WallBuildTime = 2f;

        // Times
        public float TimeActionPlantSeed = 2f;
        public float TimeActionWaterField = 3f;
        public float TimeActionHarvestCarrot = 2.5f;
        public float TimeActionRemoveRoots = 2.5f;
        public float TimeActionStealCarrotFromUndergroundField = 5f;
        public float TimeActionDigUndergroundWall = 1.33f;
        public float TimeActionDigMound = 2f;
        public float TimeActionCollapseMound = 2f;
        public float TimeActionEnterMound = 3f;
        public float TimeActionPickSeed = 3f;
        public float TimeActionPickWater = 3.5f;
        public float TimeActionPutDownCarrot = 1f;
        public float TimeActionStealCarrotFromStorage = 3.5f;


        public AI AIStats = new AI();
        public class AI
        {
            public AIPriority CleanField = new AIPriority(50);
            public AIPriority FarmFieldMounded = new AIPriority(priority: 60, critical: 80, conditional: 3);
            public AIPriority FarmFieldPlanted = new AIPriority(70);
            public AIPriority FarmFieldRooted = new AIPriority(priority: 40, critical: 90, conditional: 3);
            public AIPriority FarmFieldWithCarrot = new AIPriority(priority: 0);
            public AIPriority FarmFieldWithCarrotNeedWater = new AIPriority(priority: 75);
            public AIPriority FarmFieldWithCarrotWorking = new AIPriority(priority: 10);
            public AIPriority FieldWithCarrotReady = new AIPriority(priority: 100);

            internal AIPriority UndergroundFieldClean = new AIPriority(0);
            internal AIPriority UndergroundFieldCarrot = new AIPriority(0);
            internal AIPriority UndergroundFieldMounded = new AIPriority(0);
            internal AIPriority UndergroundFieldWall = new AIPriority(0);

        }
    }
}