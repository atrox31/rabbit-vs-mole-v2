using Enums;
using GameObjects.FarmField.States;
using System;

namespace GameObjects.FarmField
{
    public class FarmFieldActionMapEntry
    {
        public ActionType ActionType { get; }
        public Func<FarmField, IFarmFieldState> Func { get; }

        public FarmFieldActionMapEntry(ActionType actionType, Func<FarmField, IFarmFieldState> func)
        {
            ActionType = actionType;
            Func = func;
        }
    }
}