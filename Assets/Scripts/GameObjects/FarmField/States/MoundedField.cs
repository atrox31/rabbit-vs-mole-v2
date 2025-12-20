using Enums;
using RabbitVsMole;
using System.Collections.Generic;

namespace GameObjects.FarmField.States
{
    public sealed class MoundedField : FarmFieldStateBase
    {
        public MoundedField(FarmField farmField) : base(farmField)
        {
            AIPriority = new AIPriority(priority: 60, critical: 80, conditional: 3);
        }

        public override bool CanCollapseMound => true;
        public override bool CanEnterMound => true;

        protected override IReadOnlyDictionary<PlayerType, FarmFieldActionMapEntry> GetActionMap()
        {
            return new Dictionary<PlayerType, FarmFieldActionMapEntry>
            {
                { PlayerType.Rabbit, new FarmFieldActionMapEntry(ActionType.CollapseMound, CollapseMound) },
                { PlayerType.Mole, new FarmFieldActionMapEntry(ActionType.EnterMound, Enter) }
            };
        }
    }
}