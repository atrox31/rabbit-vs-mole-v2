using Enums;
using Extensions;
using PlayerManagementSystem.AIBehaviour.Common;
using RabbitVsMole;
using System;
using System.Collections;
using UnityEngine;

namespace GameObjects
{
    public class FarmWaterSource : MonoBehaviour, IInteractable
    {
        public void Interact(PlayerType type, Func<ActionType, bool> setActionType, Action<bool> changeIsBusy)
        {
            StartCoroutine(EnedueLikeFakeIWorkingState(setActionType, changeIsBusy));
        }

        IEnumerator EnedueLikeFakeIWorkingState(Func<ActionType, bool> setActionType, Action<bool> changeIsBusy)
        {
            setActionType(ActionType.Harvest);
            yield return new WaitForSeconds(2f);
            changeIsBusy?.Invoke(true);
        }

            void Awake()
        {
            gameObject.UpdateTag(AIConsts.SUPPLY_TAG);
        }
    }
}
