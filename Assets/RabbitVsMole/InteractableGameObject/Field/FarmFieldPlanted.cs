using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;

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

        protected override bool CanInteractForRabbit(Backpack backpack)
        {
            return backpack.Water.CanGet(GameInspector.GameStats.CostRabbitForWaterAction);
        }

        protected override bool CanInteractForMole(Backpack backpack)
        {
            return true;
        }

        protected override bool ActionForRabbit(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return StandardAction(
                playerAvatar.Backpack.Water.TryGet(GameInspector.GameStats.CostRabbitForWaterAction),
                onActionRequested,
                onActionCompleted,
                ActionType.WaterField,
                FieldParent.CreateFarmWithCarrotState(),
                null,
                () => { FieldParent.AddWater(GameInspector.GameStats.FarmFieldWaterInsertPerAction); });
        }

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return StandardAction(
                true,
                onActionRequested,
                onActionCompleted,
                ActionType.DigMound,
                FieldParent.CreateFarmMoundedState(),
                FieldParent.LinkedField.CreateUndergroundMoundedState()
            );
        }

    }
}