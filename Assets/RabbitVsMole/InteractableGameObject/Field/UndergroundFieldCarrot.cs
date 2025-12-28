using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public class UndergroundFieldCarrot : UndergroundFieldStateBase
    {
        public UndergroundFieldCarrot(UndergroundFieldBase parent) : base(parent)
        {
        }

        protected override void OnStart()
        {
            AIPriority = GameInspector.GameStats.AIStats.UndergroundFieldCarrot;
            FieldParent.CreateCarrot();
        }

        protected override void OnDestroy()
        {
            FieldParent.DestroyCarrot();
        }


        protected override bool CanInteractForMole(Backpack backpack)
        {
            return backpack.Carrot.Count == 0;
        }

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return FieldParent.IsCarrotReady switch
            {
                true => StandardAction(
                        true,
                        onActionRequested,
                        onActionCompleted,
                        ActionType.HarvestCarrot,
                        FieldParent.CreateUndergroundCleanState(),
                        FieldParent.LinkedField.CreateFarmCleanState()
                        ),

                false => StandardAction(
                        playerAvatar.Backpack.Seed.TryGet(GameInspector.GameStats.CostRabbitForSeedAction),
                        onActionRequested,
                        onActionCompleted,
                        ActionType.DigMound,
                        FieldParent.CreateUndergroundMoundedState(),
                        FieldParent.LinkedField.CreateFarmMoundedState()
                        )
            };
        }
    }
}