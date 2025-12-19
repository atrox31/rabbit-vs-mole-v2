using Enums;
using RabbitVsMole;
using System.Collections.Generic;

namespace GameObjects.FarmField.States
{
    public sealed class PlantedField : FarmFieldStateBase
    {
        public override bool CanWater => true;
        public override bool CanDigMound => true;
        
        public PlantedField()
        {
            AIPriority = new AIPriority(priority: 70);
        }

        protected override IReadOnlyDictionary<PlayerType, FarmFieldActionMapEntry> GetActionMap()
        {
            return new Dictionary<PlayerType, FarmFieldActionMapEntry>
            {
                { PlayerType.Rabbit, new FarmFieldActionMapEntry(ActionType.Water, Water) },
                { PlayerType.Mole, new FarmFieldActionMapEntry(ActionType.DigMound, DigMound) }
            };
        }
    }
}