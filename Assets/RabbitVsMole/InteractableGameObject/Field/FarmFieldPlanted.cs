using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Base;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public class FarmFieldPlanted : FarmFieldStateBase
    {
        public FarmFieldPlanted(FarmFieldBase parent) : base(parent)
        {
        }

        protected override void OnStart()
        {
            AIPriority = GameInspector.GameStats.AIStats.FarmFieldPlanted;
            FieldParent.CreateSeed();
        }

        protected override void OnDestroy()
        {
            FieldParent.DestroySeed();
        }

        protected override bool CanInteractForRabbit(Backpack backpack) => 
            backpack.Water.CanGet(GameInspector.GameStats.CostRabbitForWaterAction);

        protected override bool CanInteractForMole(Backpack backpack) =>
            true;

        protected override bool ActionForRabbit(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return StandardAction(new InteractionConfig
            {
                ActionType = ActionType.WaterField,
                BackpackAction = playerAvatar.Backpack.Water.TryGet(GameInspector.GameStats.CostRabbitForWaterAction),
                NewFieldStateProvider = () => FieldParent.CreateFarmWithCarrotState(),
                //NewLinkedFieldStateProvider = null,
                OnActionRequested = onActionRequested,
                //OnActionStart = null,
                OnActionCompleted = onActionCompleted,
                //FinalValidation = null,
                //OnPreStateChange = null,
                OnPostStateChange = () => FieldParent.AddWater(GameInspector.GameStats.FarmFieldWaterInsertPerAction),
            });
        }

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return StandardAction(new InteractionConfig
            {
                ActionType = ActionType.DigMound,
                //BackpackAction = true,
                NewFieldStateProvider = () => FieldParent.CreateFarmMoundedState(),
                NewLinkedFieldStateProvider = () => FieldParent.LinkedField.CreateUndergroundMoundedState(),
                OnActionRequested = onActionRequested,
                OnActionStart = null,
                OnActionCompleted = onActionCompleted,
                //FinalValidation = null,
                //OnPreStateChange = null,
                OnPostStateChange = () => playerAvatar.TryActionDown()
            });
        }

    }
}