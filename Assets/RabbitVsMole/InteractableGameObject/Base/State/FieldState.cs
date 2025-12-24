using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace RabbitVsMole.InteractableGameObject.Base
{
    public abstract class FieldState
    {
        protected FieldBase Parent { get; private set; }
        public FieldState(FieldBase parent)
        {
            Parent = parent;
            OnStart();
        }
        protected abstract void OnCancelAction();
        public abstract bool CanInteract(Backpack backpack);

        internal bool InteractWithField(
            [NotNull] PlayerAvatar playerAvatar,
            [NotNull] Func<ActionType, float> onActionRequested,
            [NotNull] Action onActionCompleted, 
            out Action cancelAction)
        {
            cancelAction = null;

            if (playerAvatar == null
                || onActionRequested == null
                || onActionCompleted == null)
                return false;

            if (!CanInteract(playerAvatar.Backpack))
                return false;

            cancelAction = OnCancelAction;

            return Action(
                playerAvatar,
                onActionRequested, 
                onActionCompleted
                );
        }

        protected abstract void OnStart();
        protected abstract void OnDestroy();
        protected abstract bool Action(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted);

        public AI.AIPriority AIPriority { get; protected set; }

        internal void SetAIPriority(AI.AIPriority priority)
        {
            AIPriority = priority;
        }

        internal FieldState ChangeState(FieldState newFieldState)
        {
            if(newFieldState == null) 
                return this;
            OnDestroy();
            return newFieldState;
        }

        protected bool StandardAction(bool backpackAction, Func<ActionType, float> onActionRequested, Action onActionCompleted, ActionType actionType, FieldState newFieldState, Action nonStandardAction = null)
        {
            if (!backpackAction)
                return false;
            var actionTime = onActionRequested.Invoke(actionType);
            nonStandardAction?.Invoke();
            Parent.StartCoroutine(Parent.CompliteAction(onActionCompleted, actionTime, newFieldState));
            return true;
        }
    }
}