using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public class FarmFieldClean : FarmFieldStateBase
    {
        public FarmFieldClean(FarmFieldBase parent) : base(parent)
        {
        }

        protected override void OnStart()
        {
            AIPriority = GameInspector.GameStats.AIStats.CleanField;
            FieldParent.DestroyCarrot();
            FieldParent.DestroyMound();
            FieldParent.DestroyRoots();
            FieldParent.DestroySeed();
        }

        protected override void OnDestroy()
        {
        }

        protected override bool CanInteractForRabbit(Backpack backpack)
        {
            return backpack.Seed.CanGet(GameInspector.GameStats.CostRabbitForSeedAction);
        }

        protected override bool CanInteractForMole(Backpack backpack)
        {
            return true;
        }

        protected override bool ActionForRabbit(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return StandardAction(
                playerAvatar.Backpack.Seed.TryGet(GameInspector.GameStats.CostRabbitForSeedAction),
                onActionRequested,
                onActionCompleted,
                ActionType.PlantSeed,
                FieldParent.CreateFarmPlantedState(),
                null);
        }

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return StandardAction(
                playerAvatar.Backpack.Seed.TryGet(GameInspector.GameStats.CostRabbitForSeedAction),
                onActionRequested,
                onActionCompleted,
                ActionType.DigMound,
                FieldParent.CreateFarmMoundedState(),
                FieldParent.LinkedField.CreateUndergroundMoundedState()
                );
        }
    }
}