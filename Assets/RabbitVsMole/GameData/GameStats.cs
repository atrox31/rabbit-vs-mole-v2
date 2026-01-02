using RabbitVsMole.InteractableGameObject.AI;

namespace RabbitVsMole.GameData
{
    [System.Serializable]
    public class GameStats
    {
        #region Fight
        // Fight
        public float FightRabbitAttackActionTime = 2.25f;
        public float FightMoleDeath = 2f;
        public float FightMoleStunTime = 2f;
        public float FightMoleRespawnTime = 2f;
        public int FightRabbitDamageDeal = 42;
        public int FightMoleHealthPoints = 100;
        public int FightMoleHealthRegenerationPerSec = 12;
        #endregion

        #region Avatar
        // Avatar stats
        public float StatsBaseWalkingSpeedRabbit = 4.5f;
        public float StatsBaseRotationSpeedRabbit = 4.5f;
        public float StatsBaseAccelerationRabbit = 10f;
        public float StatsBaseDecelerationRabbit = 20f;

        public float StatsBaseWalkingSpeedMole = 5.5f;
        public float StatsBaseRotationSpeedMole = 5.5f;
        public float StatsBaseAccelerationMole = 10f;
        public float StatsBaseDecelerationMole = 20f;

        #endregion

        #region Backpack
        // Backpack
        public int BackpackCapacitySeed = 3;
        public int BackpackCapacityWater = 8;
        public int BackpackCapacityDirt = 5;
        public int BackpackCapacityCarrot = 1;

        #endregion

        #region Economy
        // Cost - Mole
        public int CostDirtForMoleMound = 3;

        // Cost - Rabbit
        public int CostRabbitForWaterAction = 1;
        public int CostRabbitForSeedAction = 1;

        // Seed storage
        public int SeedStorageValuePerAction = 3;

        // WaterSource
        public float WaterSourceMaxWaterLevel = 10f;
        public float WaterSourceWaterPerSec = 0.2f;
        public float WaterSourceWaterDrainPerAction = 4f;
        public int WaterSourceWaterToInventoryPerDrain = 4;

        // Carrot
        public float CarrotGrowingTimeInSec = 12f;
        public float CarrotSpoilTimeInSec = 120f;

        // FarmField
        public float FarmFieldMaxWaterLevel = 6f;
        public float FarmFieldWaterDrainPerSec = 0f;
        public float FarmFieldWaterDrainByCarrotPerSec = 1f;
        public float FarmFieldWaterInsertPerAction = 4f;

        #endregion

        #region GameObjects
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

        // Wall
        public int WallDirtHealthPoint = 1;
        public int WallDirtDamageByMole = 1;
        public int WallDirtCollectPerAction = 1;
        public float WallDestroyTime = 1.33f;
        public float WallBuildTime = 2f;

        #endregion

        #region ActionTimes
        // Times
        public float TimeActionPlantSeed = 1.33f;
        public float TimeActionWaterField = 2.5f;
        public float TimeActionHarvestCarrot = 2.5f;
        public float TimeActionRemoveRoots = 2.5f;
        public float TimeActionStealCarrotFromUndergroundField = 5f;
        public float TimeActionDigUndergroundWall = 2.85f;
        public float TimeActionDigMoundUnderground = 3f;
        public float TimeActionDigMoundOnSurface = 4f;
        public float TimeActionCollapseMound = 2f;
        public float TimeActionEnterMound = 2f;
        public float TimeActionExitMound = 2f;
        public float TimeActionPickSeed = 3f;
        public float TimeActionPickWater = 2.5f;
        public float TimeActionPutDownCarrot = 1.2f;
        public float TimeActionStealCarrotFromStorage = 3.8f;

        #endregion

        #region GameRules
        //Game rules
        public bool GameRulesFightMoleAllowRegenerationOnSurface = false;

        public bool GameRulesRootsCanSpawnOnCleanField = true;
        public bool GameRulesRootsCanSpawnOnPlantedField = true;
        public bool GameRulesRootsCanSpawnOnWithCarrotField = true;

        public bool GameRulesRootsCanSpawnOnWithCarrotFullGrowField = false;
        public bool GameRulesRootsCanSpawnOnMoundedField = false;

        public bool GameRulesRootsAllowDamageRootsWithCarrotInHand = true;
        public bool GameRulesMoleCanEnterUndergroundMoundWithCarrotInHand = false;
        public bool GameRulesFarmFieldStartsWithRoots = false;

        public bool GameRulesAllowMoleToStealFromRabbitStorage = true;
        public bool GameRulesAllowMolePickUpCarrotFromFarm = true;

        #endregion

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