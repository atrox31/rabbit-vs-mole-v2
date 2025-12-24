using RabbitVsMole.GameData.Mutator;

namespace RabbitVsMole.GameData
{
    [System.Serializable]
    public class FlagModifier : IMutatorEffect
    {
        public enum Toggleable 
        { 
        }
        public Toggleable feature;
        public bool state = true;

        public void Apply(GameStats stats)
        {
            // Nieużywane - odkomentuj gdy wprowadzisz do gameplay
            // switch (feature)
            // {
            //     case Toggleable.SeeVibrations:
            //         stats.rabbitCanSeeVibrations = state;
            //         break;
            // }
        }
    }
}