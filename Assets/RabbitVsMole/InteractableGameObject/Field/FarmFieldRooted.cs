using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public class FarmFieldRooted : FarmFieldStateBase
    {
        public FarmFieldRooted(FarmFieldBase parent) : base(parent)
        {
        }

        protected override void OnStart()
        {
            AIPriority = GameInspector.GameStats.AIStats.FarmFieldRooted;
            FieldParent.DestroyCarrot();
            FieldParent.CreateRoots();
        }

        protected override void OnDestroy()
        {
            FieldParent.DestroyRoots();
        }

        protected override bool CanInteractForRabbit(Backpack backpack)
        {
            return backpack.Carrot.Count == 0;
        }

        protected override bool CanInteractForMole(Backpack backpack)
        {
            return backpack.Carrot.Count == 0;
        }

        protected override bool ActionForRabbit(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return RemoveRootsAction(onActionRequested, onActionCompleted);
        }

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return RemoveRootsAction(onActionRequested, onActionCompleted);
        }

        private bool RemoveRootsAction(Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return StandardAction(
                true,
                onActionRequested,
                onActionCompleted,
                ActionType.RemoveRoots,
                FieldParent.CreateCleanState());
        }
    }
}