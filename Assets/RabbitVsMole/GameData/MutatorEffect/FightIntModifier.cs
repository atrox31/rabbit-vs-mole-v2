using RabbitVsMole.GameData.Mutator;

namespace RabbitVsMole.GameData
{
    [System.Serializable]
    public class FightIntModifier : IMutatorEffect
    {
        public enum IntType
        {
            FightRabbitDamageDeal,
            FightMoleHealthPoints,
            FightMoleHealthRegenerationPerSec,
        }

        public IntType targetType;
        public int value;
        public bool isMultiplier;

        public void Apply(GameStats stats)
        {
            switch (targetType)
            {
                case IntType.FightRabbitDamageDeal:
                    stats.FightRabbitDamageDeal = isMultiplier ? stats.FightRabbitDamageDeal * value : stats.FightRabbitDamageDeal + value;
                    break;
                case IntType.FightMoleHealthPoints:
                    stats.FightMoleHealthPoints = isMultiplier ? stats.FightMoleHealthPoints * value : stats.FightMoleHealthPoints + value;
                    break;
                case IntType.FightMoleHealthRegenerationPerSec:
                    stats.FightMoleHealthRegenerationPerSec = isMultiplier ? stats.FightMoleHealthRegenerationPerSec * value : stats.FightMoleHealthRegenerationPerSec + value;
                    break;
            }
        }
    }
}
