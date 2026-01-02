using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Base;
using UnityEditor.Build.Pipeline;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public class FarmFieldMounded : FarmFieldStateBase
    {
        public FarmFieldMounded(FarmFieldBase parent) : base(parent) { }
        private int _hitCount;

        protected override void OnStart()
        {
            AIPriority = GameManager.CurrentGameStats.AIStats.FarmFieldMounded;
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

        protected override bool CanInteractForRabbit(Backpack backpack) =>
            true;

        protected override bool CanInteractForMole(Backpack backpack) =>
            true;

        protected override bool ActionForRabbit(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            _hitCount += GameManager.CurrentGameStats.MoundDamageByRabbit;
            var moundIsDestroyed = _hitCount >= GameManager.CurrentGameStats.MoundHealthPoint;

            return StandardAction(new InteractionConfig
            {
                ActionType = ActionType.CollapseMound,
                //BackpackAction = true,

                NewFieldStateProvider = moundIsDestroyed
                    ? () => FieldParent.CreateFarmCleanState()
                    : null,

                NewLinkedFieldStateProvider = moundIsDestroyed
                    ? () => FieldParent.LinkedField.CreateUndergroundWallState()
                    : null,

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
                ActionType = ActionType.EnterMound,
                //BackpackAction = true,
                //NewFieldStateProvider = null,
                //NewLinkedFieldStateProvider = null,
                OnActionRequested = onActionRequested,
                OnActionStart = () => playerAvatar.MoveToLinkedField(Parent.LinkedField),
                OnActionCompleted = onActionCompleted,
                //FinalValidation = null,
                //OnPreStateChange = null,
                //OnPostStateChange = null
            });
        }
    }
}