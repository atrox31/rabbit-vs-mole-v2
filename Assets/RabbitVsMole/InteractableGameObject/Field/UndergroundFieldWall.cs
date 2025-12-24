using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public class UndergroundFieldWall : UndergroundFieldStateBase
    {
        public UndergroundFieldWall(UndergroundFieldBase parent) : base(parent)
        {
        }

        protected override void OnStart()
        {
            AIPriority = GameInspector.GameStats.AIStats.UndergroundFieldWall;
            FieldParent.DestroyCarrot();
            FieldParent.DestroyMound();
            FieldParent.CreateWall();
        }

        protected override void OnDestroy()
        {
            FieldParent.DestroyWall();
        }

        protected override bool CanInteractForRabbit(Backpack backpack)
        {
            return false;
        }

        protected override bool CanInteractForMole(Backpack backpack)
        {
            return backpack.Dirt.CanInsert(GameInspector.GameStats.WallDirtCollectPerAction);
        }

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return StandardAction(
                playerAvatar.Backpack.Dirt.TryInsert(GameInspector.GameStats.WallDirtCollectPerAction),
                onActionRequested,
                onActionCompleted,
                ActionType.DigUndergroundWall,
                FieldParent.CreateUndergroundCleanState());
        }

    }
}