using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Base;

namespace RabbitVsMole.InteractableGameObject.Field
{
    public class FarmFieldWithCarrot : FarmFieldStateBase
    {
        public FarmFieldWithCarrot(FarmFieldBase parent) : base(parent)
        {
        }

        protected override void OnStart()
        {
            AIPriority = GameInspector.GameStats.AIStats.FarmFieldWithCarrot;
            FieldParent.CreateCarrot();
        }

        protected override void OnDestroy()
        {
            FieldParent.DestroyCarrot();
        }

        protected override bool CanInteractForRabbit(Backpack backpack)
        {
            return FieldParent.IsCarrotReady switch
            {
                true => backpack.Carrot.IsEmpty,
                false => backpack.Water.CanGet(GameInspector.GameStats.CostRabbitForWaterAction)
            };
        }

        protected override bool CanInteractForMole(Backpack backpack)
        {
            return !FieldParent.IsCarrotReady;
        }

        protected override bool ActionForRabbit(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return FieldParent.IsCarrotReady switch
            {
                true => StandardAction(new InteractionConfig
                {
                    ActionType = ActionType.HarvestCarrot,
                    BackpackAction = playerAvatar.Backpack.Carrot.TryInsert(),

                    NewFieldStateProvider = RandomUtils.Chance(GameInspector.GameStats.RootsBirthChance)
                        ? () => FieldParent.CreateFarmRootedState()
                        : () => FieldParent.CreateFarmCleanState(),

                    NewLinkedFieldStateProvider = (FieldParent.LinkedField.State is UndergroundFieldCarrot undergroundFieldCarrot)
                        ? () => FieldParent.LinkedField.CreateUndergroundCleanState()
                        : null,

                    OnActionRequested = onActionRequested,
                    //OnActionStart = null,
                    OnActionCompleted = onActionCompleted,
                    //FinalValidation = null,
                    //OnPreStateChange = null,
                    //OnPostStateChange = null,
                }),

                false => StandardAction(new InteractionConfig
                {
                    ActionType = ActionType.WaterField,
                    BackpackAction = playerAvatar.Backpack.Water.TryGet(GameInspector.GameStats.CostRabbitForWaterAction),
                    //NewFieldStateProvider = null,
                    //NewLinkedFieldStateProvider = null,
                    OnActionRequested = onActionRequested,
                    OnActionStart = () => FieldParent.AddWater(GameInspector.GameStats.FarmFieldWaterInsertPerAction),
                    OnActionCompleted = onActionCompleted,
                    //FinalValidation = null,
                    //OnPreStateChange = null,
                    //OnPostStateChange = null,
                })
            };
        }

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return FieldParent.IsCarrotReady switch
            {
                true => false,

                false => StandardAction(new InteractionConfig
                {
                    ActionType = ActionType.DigMound,
                    //BackpackAction = true,
                    NewFieldStateProvider = () => FieldParent.CreateFarmMoundedState(),
                    NewLinkedFieldStateProvider = () => FieldParent.LinkedField.CreateUndergroundMoundedState(),
                    OnActionRequested = onActionRequested,
                    OnActionStart = null,
                    OnActionCompleted = onActionCompleted,
                    //FinalValidation = null,
                    //OnPreStateChange = null,
                    OnPostStateChange = () => playerAvatar.TryActionDown()
                })
            };
        }
    }
}