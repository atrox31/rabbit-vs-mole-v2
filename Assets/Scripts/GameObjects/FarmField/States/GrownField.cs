using Enums;
using RabbitVsMole;
using System.Collections.Generic;

namespace GameObjects.FarmField.States
{
    public sealed class GrownField : FarmFieldStateBase
    {
        public override bool CanHarvest => false;
        
        public GrownField()
        {
            AIPriority = new AIPriority(priority: 100);
        }

        protected override IReadOnlyDictionary<PlayerType, FarmFieldActionMapEntry> GetActionMap()
        {
            return new Dictionary<PlayerType, FarmFieldActionMapEntry>
            {
                { PlayerType.Rabbit, new FarmFieldActionMapEntry(ActionType.Harvest, Harvest) },
                { PlayerType.Mole, new FarmFieldActionMapEntry(ActionType.Harvest, Harvest) }
            };
        }
    }
}