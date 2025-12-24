using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Enums;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RabbitVsMole.InteractableGameObject.Base
{
    public abstract class FieldBase : MonoBehaviour, IInteractableGameObject
    {
        protected abstract FieldState _fieldState { get; set; }
        public FieldBase LinkedField { get; set; }
        public AIPriority AIPriority => _fieldState?.AIPriority;

        public FieldState State =>
            _fieldState;

        public bool Interact(PlayerAvatar playerAvatar, Func<ActionType, float> OnActionRequested, Action OnActionCompleted, out Action CancelAction) =>
            _fieldState.InteractWithField(playerAvatar, OnActionRequested, OnActionCompleted, out CancelAction);

        public bool CanInteract(Backpack backpack) =>
            _fieldState.CanInteract(backpack);

        public IEnumerator CompliteAction(Action action, float time, FieldState newFieldState)
        {
            var currentTime = 0.0f;
            while (true)
            {
                currentTime += Time.deltaTime;
                if (currentTime >= time)
                {
                    _fieldState = _fieldState.ChangeState(newFieldState);

                    action?.Invoke();
                    yield break;
                }
                yield return null;
            }
        }

        public bool StandardAction(bool backpackAction, Func<ActionType, float> OnActionRequested, Action OnActionCompleted, ActionType actionType, FieldState newFieldState)
        {
            if (!backpackAction)
                return false;
            var actionTime = OnActionRequested.Invoke(actionType);
            StartCoroutine(CompliteAction(OnActionCompleted, actionTime, newFieldState));
            return true;
        }

        void Awake()
        {

        }

        public void LightUp() { }
        public void LightDown() { }


    }
}