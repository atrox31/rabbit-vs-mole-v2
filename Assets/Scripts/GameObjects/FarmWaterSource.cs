using Enums;
using RabbitVsMole;
using System;
using UnityEngine;

namespace GameObjects
{
    public class FarmWaterSource : MonoBehaviour, IInteractable
    {
        public void Interact(PlayerType type, Func<ActionType, bool> setActionType, Action<bool> changeIsBusy)
        {
            
        }
    }
}
