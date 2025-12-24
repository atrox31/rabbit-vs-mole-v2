using RabbitVsMole.GameData.Mutator;
using UnityEngine;

namespace RabbitVsMole.GameData
{
    [System.Serializable]
    public class StatModifier : IMutatorEffect
    {
        public enum TargetStat 
        { 
        }
        public TargetStat stat;
        public float value;
        public bool isMultiplier;

        public void Apply(GameStats stats)
        {
            switch (stat)
            {
                
                
            }
        }
    }
}