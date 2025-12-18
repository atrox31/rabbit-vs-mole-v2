using System;
using Enums;
using RabbitVsMole;

public interface IInteractable
{
    void Interact(PlayerType type, Func<ActionType, bool> setActionType, Action<bool> changeIsBusy);
}