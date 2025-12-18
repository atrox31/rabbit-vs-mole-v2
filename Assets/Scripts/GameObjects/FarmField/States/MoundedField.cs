using Enums;
using RabbitVsMole;
using System.Collections.Generic;

namespace GameObjects.FarmField.States
{
    public sealed class MoundedField : FarmFieldStateBase
    {
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