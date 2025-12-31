using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Base;
using RabbitVsMole.InteractableGameObject.Enums;
using System;
using UnityEngine;

namespace RabbitVsMole.InteractableGameObject.Storages
{
    public class FarmSeedStorage : StorageBase
    {
        protected override void OnCancelAction(Action action) { }
        public override bool CanInteract(Backpack backpack)
        {
            if (backpack.PlayerType != PlayerType.Rabbit)
                return false;

            var canInsertSeed = backpack.Seed.CanInsert(GameInspector.GameStats.SeedStorageValuePerAction);
            
            return canInsertSeed;
        }

        protected override bool Action(Backpack backpack, Func<ActionType, float> OnActionRequested, Action OnActionCompleted)
        {
            /// we are sure that
            /// 1. Player is Rabbit
            /// 2. Rabbit can hold more seed

            if (!backpack.Seed.TryInsert(GameInspector.GameStats.SeedStorageValuePerAction, true))
                return false;

            var actionTime = OnActionRequested.Invoke(ActionType.PickSeed);
            StartCoroutine(CompliteAction(OnActionCompleted, actionTime));
            return true;
        }

    }
}