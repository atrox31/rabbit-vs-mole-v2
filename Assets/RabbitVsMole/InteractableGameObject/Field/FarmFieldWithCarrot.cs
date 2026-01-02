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
            AIPriority = GameManager.CurrentGameStats.AIStats.FarmFieldWithCarrot;
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
                false => backpack.Water.CanGet(GameManager.CurrentGameStats.CostRabbitForWaterAction)
            };
        }

        protected override bool CanInteractForMole(Backpack backpack)
        {
            return GameManager.CurrentGameStats.GameRulesAllowMolePickUpCarrotFromFarm switch
            {
                true => backpack.Carrot.IsEmpty,
                false => !FieldParent.IsCarrotReady
            };
        }

        protected override bool ActionForRabbit(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return FieldParent.IsCarrotReady switch
            {
                true => PickUpCarrotAction(playerAvatar, onActionRequested, onActionCompleted),

                false => StandardAction(new InteractionConfig
                {
                    ActionType = ActionType.WaterField,
                    BackpackAction = playerAvatar.Backpack.Water.TryGet(GameManager.CurrentGameStats.CostRabbitForWaterAction),
                    //NewFieldStateProvider = null,
                    //NewLinkedFieldStateProvider = null,
                    OnActionRequested = onActionRequested,
                    OnActionStart = () => FieldParent.AddWater(GameManager.CurrentGameStats.FarmFieldWaterInsertPerAction),
                    OnActionCompleted = onActionCompleted,
                    //FinalValidation = null,
                    //OnPreStateChange = null,
                    //OnPostStateChange = null,
                })
            };
        }

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            if (GameManager.CurrentGameStats.GameRulesAllowMolePickUpCarrotFromFarm)
            {
                return FieldParent.IsCarrotReady switch
                {
                    true => PickUpCarrotAction(playerAvatar, onActionRequested, onActionCompleted),

                    false => false // prevent digging mound
                };
            }
            else
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

        bool PickUpCarrotAction(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return StandardAction(new InteractionConfig
            {
                ActionType = ActionType.HarvestCarrot,
                BackpackAction = playerAvatar.Backpack.Carrot.TryInsert(),

                NewFieldStateProvider = RandomUtils.Chance(GameManager.CurrentGameStats.RootsBirthChance)
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
            });
        }
    }
}