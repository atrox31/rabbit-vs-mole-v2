using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Enums;
using GameObjects.FarmField.Systems;
using RabbitVsMole;
using UnityEngine;

namespace GameObjects.FarmField.States
{
    public abstract class FarmFieldStateBase : IFarmFieldState
    {
        public bool IsBusy;

        public virtual bool CanPlant => false;
        public virtual bool CanWater => false;
        public virtual bool CanHarvest => false;
        public virtual bool CanCollapseMound => false;
        public virtual bool CanDigMound => false;
        public virtual bool CanRemoveRoots => false;
        public virtual bool CanEnterMound => false;

        public virtual AIPriority AIPriority { get; protected set; }

        protected abstract IReadOnlyDictionary<PlayerType, FarmFieldActionMapEntry> GetActionMap();
        
        private CancellationTokenSource _actionCancellationTokenSource;

        public virtual IEnumerator Interact(
            FarmField field,
            PlayerType playerType,
            Func<ActionType, bool> notifyAndCheck,
            Action<IFarmFieldState> onDone)
        {
            if (IsBusy) yield break;

            IsBusy = true;
            var action = GetActionMapEntry(playerType);

            if (!notifyAndCheck(action.ActionType))
            {
                onDone(this);
                IsBusy = false;

                yield break;
            }

            DebugHelper.Log(null, "Waiting..");
            yield return new WaitForSeconds(3);
            
            DebugHelper.Log(null, $"Starting action {action.ActionType}..");
            var result = action.Func(field);
            onDone(result);
            IsBusy = false;
            DebugHelper.Log(null, "Done!");

            yield return null;
        }

        public void CancelAction()
        {
            _actionCancellationTokenSource?.Cancel();
        }

        protected virtual IFarmFieldState Plant(FarmField field)
        {
            field.PlantSeed();
            return new PlantedField();
        }

        protected virtual IFarmFieldState Water(FarmField field)
        {
            var wateringSystem = new WateringSystem(field);
            _actionCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            wateringSystem.StartWatering(null, _actionCancellationTokenSource.Token);
            // field.StartWatering();
            return new GrownField();
        }

        protected virtual IFarmFieldState Harvest(FarmField field)
        {
            field.HarvestCarrot();
            return new UntouchedField();
        }

        protected virtual IFarmFieldState CollapseMound(FarmField field)
        {
            field.DestroyMound();
            return new UntouchedField();
        }

        protected virtual IFarmFieldState DigMound(FarmField field)
        {
            field.CreateMound();
            return new MoundedField();
        }

        protected virtual IFarmFieldState RemoveRoots(FarmField field)
        {
            LogWarning(nameof(RemoveRoots));
            return this;
        }

        protected virtual IFarmFieldState Enter(FarmField field)
        {
            LogWarning(nameof(Enter));
            return this;
        }

        private void LogWarning(string action)
            => DebugHelper.LogWarning(null, $"You cannot {action} in the current state: {GetType().Name}");

        private FarmFieldActionMapEntry GetActionMapEntry(PlayerType playerType)
            => GetActionMap().GetValueOrDefault(playerType);
    }
}