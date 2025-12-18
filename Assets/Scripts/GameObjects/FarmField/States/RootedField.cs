using Enums;
using RabbitVsMole;
using System.Collections.Generic;

namespace GameObjects.FarmField.States
{
    public sealed class RootedField : FarmFieldStateBase
    {
        public override bool CanRemoveRoots => true;

        protected override IReadOnlyDictionary<PlayerType, FarmFieldActionMapEntry> GetActionMap()
        {
            return new Dictionary<PlayerType, FarmFieldActionMapEntry>
            {
                { PlayerType.Rabbit, new FarmFieldActionMapEntry(ActionType.RemoveRoots, RemoveRoots) },
                { PlayerType.Mole, new FarmFieldActionMapEntry(ActionType.RemoveRoots, RemoveRoots) }
            };
        }
    }
}