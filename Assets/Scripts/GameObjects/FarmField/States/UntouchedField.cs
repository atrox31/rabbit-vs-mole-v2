using System.Collections.Generic;
using Enums;
using RabbitVsMole;

namespace GameObjects.FarmField.States
{
    public sealed class UntouchedField : FarmFieldStateBase
    {
        public UntouchedField(FarmField farmField) : base(farmField)
        {
            AIPriority = new AIPriority(priority: 50);
        }

        public override bool CanPlant => true;
        public override bool CanDigMound => true;

        protected override IReadOnlyDictionary<PlayerType, FarmFieldActionMapEntry> GetActionMap()
        {
            return new Dictionary<PlayerType, FarmFieldActionMapEntry>
            {
                { PlayerType.Rabbit, new FarmFieldActionMapEntry(ActionType.Plant, Plant) },
                { PlayerType.Mole, new FarmFieldActionMapEntry(ActionType.DigMound, DigMound) }
            };
        }
    }
}