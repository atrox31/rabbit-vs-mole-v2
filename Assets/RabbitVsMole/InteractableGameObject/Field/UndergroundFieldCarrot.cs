using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Base;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public class UndergroundFieldCarrot : UndergroundFieldStateBase
    {
        public UndergroundFieldCarrot(UndergroundFieldBase parent) : base(parent)
        {
        }

        protected override void OnStart()
        {
            AIPriority = GameInspector.GameStats.AIStats.UndergroundFieldCarrot;
            FieldParent.CreateCarrot();
        }

        protected override void OnDestroy()
        {
            FieldParent.DestroyCarrot();
        }

        protected override void OnCancelAction(Action OnActionCompleted)
        {
            (FieldParent.LinkedField as FarmFieldBase).StopStealCarrot();
            OnActionCompleted?.Invoke();
        }

        protected override bool CanInteractForMole(Backpack backpack) =>
            backpack.Carrot.IsEmpty;

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return FieldParent.IsCarrotReady switch
            {
                true => StandardAction(new InteractionConfig
                {
                    ActionType = ActionType.DigMound,
                    BackpackAction = true,
                    NewFieldStateProvider = () => FieldParent.CreateUndergroundCleanState(),
                    NewLinkedFieldStateProvider = () => FieldParent.LinkedField.CreateFarmCleanState(),
                    OnActionRequested = onActionRequested,
                    OnActionStart = () => (FieldParent.LinkedField as FarmFieldBase).StartStealCarrot(),
                    OnActionCompleted = onActionCompleted,
                    FinalValidation = () => (FieldParent.LinkedField as FarmFieldBase).IsCarrotReady,
                    OnPreStateChange = () => playerAvatar.Backpack.Carrot.TryInsert(),
                    OnPostStateChange = () => (FieldParent.LinkedField as FarmFieldBase).StopStealCarrot(),
                    DelayedStatusChange = true
                }),

                false => StandardAction(new InteractionConfig
                {
                    ActionType = ActionType.DigMound,
                    BackpackAction = playerAvatar.Backpack.Dirt.TryGet(GameInspector.GameStats.CostDirtForMoleMound),
                    NewFieldStateProvider = () => FieldParent.CreateUndergroundMoundedState(),
                    NewLinkedFieldStateProvider = () => FieldParent.LinkedField.CreateFarmMoundedState(),
                    OnActionRequested = onActionRequested,
                    //OnActionStart = null,
                    OnActionCompleted = onActionCompleted,
                    //FinalValidation = null,
                    //OnPreStateChange = null,
                    //OnPostStateChange = null,
                })
            };
        }
    }
}