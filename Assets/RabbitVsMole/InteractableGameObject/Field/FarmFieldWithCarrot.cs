using RabbitVsMole.InteractableGameObject.AI;
using RabbitVsMole.InteractableGameObject.Field.Base;
using System;
using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Enums;
using UnityEngine.Rendering.UI;

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
                true => backpack.Carrot.Count == 0,
                false => backpack.Water.CanGet(GameInspector.GameStats.CostRabbitForWaterAction)
            };
        }

        protected override bool CanInteractForMole(Backpack backpack)
        {
            return !FieldParent.IsCarrotReady;
        }

        protected override bool ActionForRabbit(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            var spawnRoots = GameInspector.GameStats.RootsBirthChance > UnityEngine.Random.value;

            return FieldParent.IsCarrotReady switch
            {
                true => StandardAction(
                    playerAvatar.Backpack.Carrot.TryInsert(),
                    onActionRequested,
                    onActionCompleted,
                    ActionType.HarvestCarrot,
                    spawnRoots 
                        ? FieldParent.CreateRootedState()
                        : FieldParent.CreateCleanState()
                    ),

                false => StandardAction(
                    playerAvatar.Backpack.Water.TryGet(GameInspector.GameStats.CostRabbitForWaterAction),
                    onActionRequested,
                    onActionCompleted,
                    ActionType.WaterField,
                    null,
                    () => { FieldParent.AddWater(GameInspector.GameStats.FarmFieldWaterInsertPerAction); })
            };
        }

        protected override bool ActionForMole(PlayerAvatar playerAvatar, Func<ActionType, float> onActionRequested, Action onActionCompleted)
        {
            return false;
        }
    }
}