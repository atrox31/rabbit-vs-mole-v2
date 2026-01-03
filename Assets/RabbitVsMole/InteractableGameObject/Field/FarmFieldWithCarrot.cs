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
            if (FieldParent.IsCarrotReady)
                return GameManager.CurrentGameStats.SystemAllowToPickCarrot && backpack.Carrot.IsEmpty;
            else
                return GameManager.CurrentGameStats.SystemAllowToWaterField && backpack.Water.CanGet(GameManager.CurrentGameStats.CostRabbitForWaterAction);
        }

        protected override bool CanInteractForMole(Backpack backpack)
        {
            if (GameManager.CurrentGameStats.GameRulesAllowMolePickUpCarrotFromFarm)
                return GameManager.CurrentGameStats.SystemAllowToPickCarrot && backpack.Carrot.IsEmpty;
            else
                return !FieldParent.IsCarrotReady; // kret może kopać kopiec tylko gdy marchewka nie jest gotowa
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
                    NewLinkedFieldStateProvider = FieldParent.LinkedField != null 
                        ? () => FieldParent.LinkedField.CreateUndergroundMoundedState() 
                        : null,
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

                NewLinkedFieldStateProvider = (FieldParent.LinkedField != null && FieldParent.LinkedField.State is UndergroundFieldCarrot)
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