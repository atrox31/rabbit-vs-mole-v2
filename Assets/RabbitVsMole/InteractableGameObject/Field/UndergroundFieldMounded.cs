using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public class UndergroundFieldMounded : UndergroundFieldStateBase
    {
        public UndergroundFieldMounded(UndergroundFieldBase parent) : base(parent)
        {
        }

        protected override void OnStart()
        {
            AIPriority = GameInspector.GameStats.AIStats.UndergroundFieldMounded;
            FieldParent.DestroyCarrot();
            FieldParent.DestroyWall();

            FieldParent.CreateMound();
        }

        protected override void OnDestroy()
        {
            FieldParent.DestroyMound();
        }

        protected override bool CanInteractForMole(Backpack backpack)
        {
            return backpack.Carrot.Count == 0;
        }

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return StandardAction(
                true,
                onActionRequested,
                onActionCompleted,
                ActionType.EnterMound,
                null,
                null,
                () => { playerAvatar.MoveToLinkedField(Parent.LinkedField); });
        }
    }
}