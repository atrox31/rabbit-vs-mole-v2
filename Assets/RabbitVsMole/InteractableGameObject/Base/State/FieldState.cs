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
        protected virtual void OnCancelAction(Action action)
        {
            Parent.StopAllCoroutines();
            action?.Invoke();
        }
        public abstract bool CanInteract(Backpack backpack);

        internal bool InteractWithField(
            [NotNull] PlayerAvatar playerAvatar,
            [NotNull] Func<ActionType, float> onActionRequested,
            [NotNull] Action onActionCompleted, 
            out Action<Action> cancelAction)
        {
            cancelAction = null;

            if (playerAvatar == null
                || onActionRequested == null
                || onActionCompleted == null)
                return false;

            if (!CanInteract(playerAvatar.Backpack))
            {
                var playErrorSound = playerAvatar.PlayerType switch
                {
                    PlayerType.Rabbit => GameManager.CurrentGameInspector.MoleControlAgent != PlayerControlAgent.Bot,
                    PlayerType.Mole => GameManager.CurrentGameInspector.MoleControlAgent != PlayerControlAgent.Bot,
                    _ => false,
                };

                if (playErrorSound)
                    AudioManager.PlaySound3D(SoundDB.SoundDB.GetSound(ActionType.None, playerAvatar.PlayerType), Parent.transform.position, AudioManager.AudioChannel.SFX);
                return false;
            }

            cancelAction = OnCancelAction;

            return Action(
                playerAvatar,
                (type) =>
                {
                    AudioManager.PlaySound3D(SoundDB.SoundDB.GetSound(type, playerAvatar.PlayerType), Parent.transform.position, AudioManager.AudioChannel.SFX);
                    return onActionRequested(type);
                }, 
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

        protected bool StandardAction(InteractionConfig config)
        {
            if (!config.BackpackAction)
                return false;
            var actionTime = config.OnActionRequested.Invoke(config.ActionType);
            config.OnActionStart?.Invoke();
            Parent.StartCoroutine(Parent.CompliteAction(config, actionTime));
            return true;
        }
    }
}