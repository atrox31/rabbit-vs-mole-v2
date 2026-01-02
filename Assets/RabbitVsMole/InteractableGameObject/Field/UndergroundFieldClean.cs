using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole;
using RabbitVsMole.InteractableGameObject.Base;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public class UndergroundFieldClean : UndergroundFieldStateBase
    {
        public UndergroundFieldClean(UndergroundFieldBase parent) : base(parent)
        {
        }

        protected override void OnStart()
        {
            AIPriority = GameManager.CurrentGameStats.AIStats.UndergroundFieldClean;
            FieldParent.DestroyWall();
            FieldParent.DestroyCarrot();
            FieldParent.DestroyMound();
        }

        protected override void OnDestroy() {}

        protected override bool CanInteractForMole(Backpack backpack) =>
            backpack.Dirt.CanGet(GameManager.CurrentGameStats.CostDirtForMoleMound);

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return StandardAction(new InteractionConfig
            {
                ActionType = ActionType.DigMound,
                BackpackAction = playerAvatar.Backpack.Dirt.TryGet(GameManager.CurrentGameStats.CostDirtForMoleMound),
                NewFieldStateProvider = () => FieldParent.CreateUndergroundMoundedState(),
                NewLinkedFieldStateProvider = () => FieldParent.LinkedField.CreateFarmMoundedState(),
                OnActionRequested = onActionRequested,
                //OnActionStart = null,
                OnActionCompleted = onActionCompleted,
                //FinalValidation = null,
                //OnPreStateChange = null,
                //OnPostStateChange = null,
            });
        }
    }
}