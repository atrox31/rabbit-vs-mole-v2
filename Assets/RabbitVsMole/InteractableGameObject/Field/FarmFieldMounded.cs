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
        private int _hitCount = 0;

        protected override void OnStart()
        {
            AIPriority = GameInspector.GameStats.AIStats.FarmFieldMounded;
            FieldParent.DestroyCarrot();
            FieldParent.DestroyRoots();
            FieldParent.DestroySeed();

            FieldParent.CreateMound();
            _hitCount = 0;
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
            _hitCount += GameInspector.GameStats.MoundDamageByRabbit;
            var moundIsDestroyed = _hitCount >= GameInspector.GameStats.MoundHealthPoint;

            return StandardAction(
                backpackAction: true,
                onActionRequested: onActionRequested,
                onActionCompleted: onActionCompleted,
                actionType: ActionType.CollapseMound,
                newFieldState: moundIsDestroyed 
                    ? FieldParent.CreateFarmCleanState()
                    : null,
                newLinkedFieldState: moundIsDestroyed
                    ? FieldParent.LinkedField.CreateUndergroundWallState()
                    : null,
                nonStandardAction: null );
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