using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public class FarmFieldMounded : FarmFieldStateBase
    {
        public FarmFieldMounded(FarmFieldBase parent) : base(parent) { }

        protected override void OnStart()
        {
            AIPriority = GameInspector.GameStats.AIStats.FarmFieldMounded;
            FieldParent.DestroyCarrot();
            FieldParent.DestroyRoots();
            FieldParent.CreateMound();
        }

        protected override void OnDestroy()
        {
            FieldParent.DestroyMound();
        }

        protected override bool CanInteractForRabbit(Backpack backpack)
        {
            return true;
        }

        protected override bool CanInteractForMole(Backpack backpack)
        {
            return true;
        }

        protected override bool ActionForRabbit(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return StandardAction(
                true,
                onActionRequested,
                onActionCompleted,
                ActionType.CollapseMound,
                FieldParent.CreatePlantedState());
        }

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return StandardAction(
                true,
                onActionRequested,
                onActionCompleted,
                ActionType.EnterMound,
                null,
                () => { playerAvatar.MoveToLinkedField(Parent.LinkedField); });
        }
    }
}