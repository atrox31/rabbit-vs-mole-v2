using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Base;
using UnityEngine;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public class UndergroundFieldWall : UndergroundFieldStateBase
    {
        public UndergroundFieldWall(UndergroundFieldBase parent) : base(parent)
        {
        }
        private int _hitCount = 0;

        protected override void OnStart()
        {
            AIPriority = GameInspector.GameStats.AIStats.UndergroundFieldWall;
            FieldParent.DestroyCarrot();
            FieldParent.DestroyMound();

            FieldParent.CreateWall();
            _hitCount = 0;

            if(FieldParent.TryGetComponent<BoxCollider>(out var boxCollider))
            {
                var boxSize = boxCollider.size;
                boxSize.y = 5f;
                boxCollider.size = boxSize;
            }
            PlayerAvatar.MoleStaticInstance?.MoundCollapse(FieldParent);
        }

        protected override void OnDestroy()
        {
            FieldParent.DestroyWall();

            if (FieldParent.TryGetComponent<BoxCollider>(out var boxCollider))
            {
                var boxSize = boxCollider.size;
                boxSize.y = 0.5f;
                boxCollider.size = boxSize;
            }
        }


        protected override bool CanInteractForMole(Backpack backpack) => 
            backpack.Dirt.CanInsert(GameInspector.GameStats.WallDirtCollectPerAction);

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            _hitCount += GameInspector.GameStats.WallDirtDamageByMole;
            var wallIsDestroyed = _hitCount >= GameInspector.GameStats.WallDirtHealthPoint;
            return StandardAction(new InteractionConfig
            {
                ActionType = ActionType.DigUndergroundWall,
                BackpackAction = playerAvatar.Backpack.Dirt.TryInsert(GameInspector.GameStats.WallDirtCollectPerAction),
                NewFieldStateProvider = () => DetermineNewFieldState(wallIsDestroyed),
                //NewLinkedFieldStateProvider = null,
                OnActionRequested = onActionRequested,
                //OnActionStart = null,
                OnActionCompleted = onActionCompleted,
                //FinalValidation = null,
                //OnPreStateChange = null,
                //OnPostStateChange = null,
            });
        }

        FieldState DetermineNewFieldState(bool wallIsDestroyed)
        {
            if (!wallIsDestroyed)
                return null;

            if (FieldParent.LinkedField is FarmFieldBase farmField)
            {
                if (farmField.IsCarrotReady)
                    return FieldParent.CreateUndergroundCarrotState();
                else 
                    return FieldParent.CreateUndergroundCleanState();
            }
            else
            {
                DebugHelper.LogError(FieldParent, "Cannot find farm field linked field");
            }
            return null;    
        }

    }
}