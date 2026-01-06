using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Field;
using RabbitVsMole.InteractableGameObject.Field.Base;
using RabbitVsMole.Online;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace RabbitVsMole.InteractableGameObject.Base
{
    public abstract class FieldBase : MonoBehaviour, IInteractableGameObject
    {
        protected abstract FieldState _fieldState { get; set; }
        public FieldBase LinkedField { get; set; }
        public AIPriority AIPriority => _fieldState?.AIPriority;

        public FieldState State =>
            _fieldState;

        public bool Active { get; set; } = true;

        public bool Interact(PlayerAvatar playerAvatar, Func<ActionType, float> OnActionRequested, Action OnActionCompleted, out Action<Action> CancelAction) =>
            _fieldState.InteractWithField(playerAvatar, OnActionRequested, OnActionCompleted, out CancelAction);

        public bool CanInteract(Backpack backpack) => Active 
            && _fieldState.CanInteract(backpack);

        public IEnumerator CompliteAction(InteractionConfig config, float time)
        {
            if (!config.DelayedStatusChange)
            {
                config.OnPreStateChange?.Invoke();

                SetNewState(config.NewFieldStateProvider?.Invoke());
                LinkedField?.SetNewState(config.NewLinkedFieldStateProvider?.Invoke());

                config.OnPostStateChange?.Invoke();
            }

            var currentTime = 0.0f;
            while (true)
            {
                currentTime += Time.deltaTime;
                if (currentTime >= time)
                {
                    if (config.FinalValidation?.Invoke() == false)
                    {
                        config.OnActionCompleted?.Invoke();
                        yield break;
                    }

                    if (config.DelayedStatusChange)
                    {
                        config.OnPreStateChange?.Invoke();

                        SetNewState(config.NewFieldStateProvider?.Invoke());
                        LinkedField?.SetNewState(config.NewLinkedFieldStateProvider?.Invoke());

                        config.OnPostStateChange?.Invoke();
                    }
                    config.OnActionCompleted?.Invoke();

                    yield break;
                }
                yield return null;
            }
        }

        void Awake()
        {
           
        }

        public void SetNewState(FieldState fieldState)
        {
            if (!OnlineAuthority.CanChangeFieldState(this))
                return;

            _fieldState = _fieldState.ChangeState(fieldState);
            OnlineAuthority.NotifyHostFieldStateChanged(this, _fieldState);
        }
        

        public abstract void LightUp(PlayerType playerType);
        public abstract void LightDown(PlayerType playerType);

        internal FieldState CreateFarmCleanState() => new FarmFieldClean(this as FarmFieldBase);
        internal FieldState CreateFarmPlantedState() => new FarmFieldPlanted(this as FarmFieldBase);
        internal FieldState CreateFarmMoundedState() => new FarmFieldMounded(this as FarmFieldBase);
        internal FieldState CreateFarmRootedState() => new FarmFieldRooted(this as FarmFieldBase);
        internal FieldState CreateFarmWithCarrotState() => new FarmFieldWithCarrot(this as FarmFieldBase);


        internal FieldState CreateUndergroundWallState() => new UndergroundFieldWall(this as UndergroundFieldBase);
        internal FieldState CreateUndergroundMoundedState() => new UndergroundFieldMounded(this as UndergroundFieldBase);
        internal FieldState CreateUndergroundCarrotState() => new UndergroundFieldCarrot(this as UndergroundFieldBase);
        internal FieldState CreateUndergroundCleanState() => new UndergroundFieldClean(this as UndergroundFieldBase);

    }
}