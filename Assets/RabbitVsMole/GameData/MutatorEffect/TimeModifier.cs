using RabbitVsMole.GameData.Mutator;

namespace RabbitVsMole.GameData
{
    [System.Serializable]
    public class TimeModifier : IMutatorEffect
    {
        public enum TimeType
        {
        }
        public TimeType timeType;
        public float timeValue; // in seconds
        public bool isMultiplier;

        public void Apply(GameStats stats)
        {
            // Nieużywane - odkomentuj gdy wprowadzisz do gameplay
            // switch (timeType)
            // {
            //     case TimeType.RabbitTripDuration:
            //         stats.rabbitTripDuration = isMultiplier ? stats.rabbitTripDuration * timeValue : stats.rabbitTripDuration + timeValue;
            //         break;
            // }
        }
    }
}

