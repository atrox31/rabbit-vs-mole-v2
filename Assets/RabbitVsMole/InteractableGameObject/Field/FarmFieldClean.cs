using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Base;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public class FarmFieldClean : FarmFieldStateBase
    {
        public FarmFieldClean(FarmFieldBase parent) : base(parent)
        {
        }

        protected override void OnStart()
        {
            AIPriority = GameManager.CurrentGameStats.AIStats.CleanField;
            FieldParent.DestroyCarrot();
            FieldParent.DestroyMound();
            FieldParent.DestroyRoots();
            FieldParent.DestroySeed();
        }

        protected override void OnDestroy()
        {
        }

        protected override bool CanInteractForRabbit(Backpack backpack) =>
            GameManager.CurrentGameStats.SystemAllowToPlantSeed && backpack.Seed.CanGet(GameManager.CurrentGameStats.CostRabbitForSeedAction);
        
        protected override bool CanInteractForMole(Backpack backpack) =>
            true;

        protected override bool ActionForRabbit(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return StandardAction(new InteractionConfig
            {
                ActionType = ActionType.PlantSeed,
                BackpackAction = playerAvatar.Backpack.Seed.TryGet(GameManager.CurrentGameStats.CostRabbitForSeedAction),
                NewFieldStateProvider = () => FieldParent.CreateFarmPlantedState(),
                //NewLinkedFieldStateProvider = null,
                OnActionRequested = onActionRequested,
                //OnActionStart = null,
                OnActionCompleted = onActionCompleted,
                //FinalValidation = null,
                //OnPreStateChange = null,
                //OnPostStateChange = null,
            });
        }

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return StandardAction(new InteractionConfig
            {
                ActionType = ActionType.DigMound,
                //BackpackAction = true,
                NewFieldStateProvider = () => FieldParent.CreateFarmMoundedState(),
                NewLinkedFieldStateProvider = FieldParent.LinkedField != null 
                    ? () => FieldParent.LinkedField.CreateUndergroundMoundedState() 
                    : null,
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