using Enums;
using GameObjects.FarmField.States;
using System;

namespace GameObjects.FarmField
{
    public class FarmFieldActionMapEntry
    {
        public ActionType ActionType { get; }
        public Func<IFarmFieldState> Func { get; }

        public FarmFieldActionMapEntry(ActionType actionType, Func<IFarmFieldState> func)
        {
            ActionType = actionType;
            Func = func;
        }
    }
}