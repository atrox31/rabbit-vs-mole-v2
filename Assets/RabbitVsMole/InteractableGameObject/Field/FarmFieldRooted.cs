using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;
using UnityEngine;
using RabbitVsMole.InteractableGameObject.Base;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public class FarmFieldRooted : FarmFieldStateBase
    {
        public FarmFieldRooted(FarmFieldBase parent) : base(parent)
        {
        }
        private int _hitCount;

        protected override void OnStart()
        {
            AIPriority = GameInspector.GameStats.AIStats.FarmFieldRooted;
            FieldParent.DestroyCarrot();
            FieldParent.DestroySeed();

            FieldParent.CreateRoots();
            _hitCount = 0;
        }

        protected override void OnDestroy()
        {
            FieldParent.DestroyRoots();
        }

        protected override bool CanInteractForRabbit(Backpack backpack) =>
            GameInspector.GameStats.GameRulesRootsAllowDamageRootsWithCarrotInHand switch
            {
                true => true,
                false => backpack.Carrot.IsEmpty
            };

        protected override bool CanInteractForMole(Backpack backpack) =>
            GameInspector.GameStats.GameRulesRootsAllowDamageRootsWithCarrotInHand switch
            {
                true => true,
                false => backpack.Carrot.IsEmpty
            };

        protected override bool ActionForRabbit(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            _hitCount += GameInspector.GameStats.RootsDamageByRabbit;
            return RemoveRootsAction(onActionRequested, onActionCompleted);
        }

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            _hitCount += GameInspector.GameStats.RootsDamageByMole;
            return RemoveRootsAction(onActionRequested, onActionCompleted);
        }

        private bool RemoveRootsAction(Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            var rootsIsDestroyed = _hitCount >= GameInspector.GameStats.RootsHealthPoint;

            return StandardAction(new InteractionConfig
            {
                ActionType = ActionType.RemoveRoots,
                //BackpackAction = true,

                NewFieldStateProvider = rootsIsDestroyed
                        ? () => FieldParent.CreateFarmCleanState()
                        : null,

                //NewLinkedFieldStateProvider = null,
                OnActionRequested = onActionRequested,
                //OnActionStart = null,
                OnActionCompleted = onActionCompleted,
                //FinalValidation = null,
                //OnPreStateChange = null,
                //OnPostStateChange = null
            });
        }

    }
}