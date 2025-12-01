using System;
using System.Collections;
using Enums;

namespace GameObjects.FarmField.States
{
    public interface IFarmFieldState
    {
        bool CanPlant { get; }
        bool CanWater { get; }
        bool CanHarvest { get; }
        bool CanCollapseMound { get; }
        bool CanDigMound { get; }
        bool CanRemoveRoots { get; }
        bool CanEnterMound { get; }

        IEnumerator Interact(FarmField field,
            PlayerType playerType,
            Func<ActionType, bool> notifyAndCheck,
            Action<IFarmFieldState> onDone);

        void CancelAction();
    }
}