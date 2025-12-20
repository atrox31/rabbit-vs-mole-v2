using Enums;
using RabbitVsMole;
using System.Collections.Generic;

namespace GameObjects.FarmField.States
{
    public sealed class GrownField : FarmFieldStateBase
    {
        public GrownField(FarmField farmField) : base(farmField)
        {
            AIPriority = new AIPriority(priority: 100);
        }

        public override bool CanHarvest => false;

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