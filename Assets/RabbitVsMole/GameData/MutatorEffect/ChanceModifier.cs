using RabbitVsMole.GameData.Mutator;

namespace RabbitVsMole.GameData
{
    [System.Serializable]
    public class ChanceModifier : IMutatorEffect
    {
        public enum ChanceType
        {
        }
        public ChanceType chanceType;
        public float chanceValue; // 0.0 to 1.0 (e.g., 0.1 = 10%, 0.3 = 30%)

        public void Apply(GameStats stats)
        {
            // Nieużywane - odkomentuj gdy wprowadzisz do gameplay
            // switch (chanceType)
            // {
            //     case ChanceType.InstantGrowChance:
            //         stats.instantGrowChance = chanceValue;
            //         break;
            // }
        }
    }
}

