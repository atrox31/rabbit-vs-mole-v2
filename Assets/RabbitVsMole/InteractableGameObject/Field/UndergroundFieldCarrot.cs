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
            AIPriority = GameManager.CurrentGameStats.AIStats.UndergroundFieldCarrot;
            FieldParent.CreateCarrot();
        }

        protected override void OnDestroy()
        {
            FieldParent.DestroyCarrot();
        }

        protected override void OnCancelAction(Action OnActionCompleted)
        {
            (FieldParent.LinkedField as FarmFieldBase)?.StopStealCarrot();
            OnActionCompleted?.Invoke();
        }

        protected override bool CanInteractForMole(Backpack backpack) =>
            backpack.Carrot.IsEmpty;

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            var linkedFarmField = FieldParent.LinkedField as FarmFieldBase;
            
            return FieldParent.IsCarrotReady switch
            {
                true => StandardAction(new InteractionConfig
                {
                    ActionType = ActionType.DigMound,
                    BackpackAction = true,
                    NewFieldStateProvider = () => FieldParent.CreateUndergroundCleanState(),
                    NewLinkedFieldStateProvider = FieldParent.LinkedField != null 
                        ? () => FieldParent.LinkedField.CreateFarmCleanState() 
                        : null,
                    OnActionRequested = onActionRequested,
                    OnActionStart = () => linkedFarmField?.StartStealCarrot(),
                    OnActionCompleted = onActionCompleted,
                    FinalValidation = () => linkedFarmField?.IsCarrotReady ?? true,
                    OnPreStateChange = () => playerAvatar.Backpack.Carrot.TryInsert(),
                    OnPostStateChange = () => linkedFarmField?.StopStealCarrot(),
                    DelayedStatusChange = true
                }),

                false => StandardAction(new InteractionConfig
                {
                    ActionType = ActionType.DigMound,
                    BackpackAction = playerAvatar.Backpack.Dirt.TryGet(GameManager.CurrentGameStats.CostDirtForMoleMound),
                    NewFieldStateProvider = () => FieldParent.CreateUndergroundMoundedState(),
                    NewLinkedFieldStateProvider = FieldParent.LinkedField != null 
                        ? () => FieldParent.LinkedField.CreateFarmMoundedState() 
                        : null,
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