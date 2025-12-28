using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public class UndergroundFieldClean : UndergroundFieldStateBase
    {
        public UndergroundFieldClean(UndergroundFieldBase parent) : base(parent)
        {
        }

        protected override void OnStart()
        {
            AIPriority = GameInspector.GameStats.AIStats.UndergroundFieldClean;
            FieldParent.DestroyWall();
            FieldParent.DestroyCarrot();
        }

        protected override void OnDestroy()
        {
        }

        protected override bool CanInteractForMole(Backpack backpack)
        {
            return backpack.Dirt.CanGet(GameInspector.GameStats.CostDirtForMoleMound);
        }

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return StandardAction(
                playerAvatar.Backpack.Dirt.TryGet(GameInspector.GameStats.CostDirtForMoleMound),
                onActionRequested,
                onActionCompleted,
                ActionType.DigMound,
                FieldParent.CreateUndergroundMoundedState(),
                FieldParent.LinkedField.CreateFarmMoundedState()
                );
        }
    }
}