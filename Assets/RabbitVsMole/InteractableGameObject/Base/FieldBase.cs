using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Field;
using RabbitVsMole.InteractableGameObject.Field.Base;
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

        [SerializeField] private Outline _outline;
        private Coroutine _outlineCoroutine;

        public FieldState State =>
            _fieldState;

        public bool Interact(PlayerAvatar playerAvatar, Func<ActionType, float> OnActionRequested, Action OnActionCompleted, out Action CancelAction) =>
            _fieldState.InteractWithField(playerAvatar, OnActionRequested, OnActionCompleted, out CancelAction);

        public bool CanInteract(Backpack backpack) =>
            _fieldState.CanInteract(backpack);

        public IEnumerator CompliteAction(Action action, float time, FieldState newFieldState, FieldState newLinkedFieldState)
        {
            var currentTime = 0.0f;
            while (true)
            {
                currentTime += Time.deltaTime;
                if (currentTime >= time)
                {
                    SetNewState(newFieldState);
                    LinkedField.SetNewState(newLinkedFieldState);

                    action?.Invoke();
                    yield break;
                }
                yield return null;
            }
        }

        void Awake()
        {

        }

        public void SetNewState(FieldState fieldState) =>
            _fieldState = _fieldState.ChangeState(_fieldState);
        

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