using System;
using System.Collections;
using System.Collections.Generic;
using Enums;
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
        
        private readonly FarmField _farmField;

        protected FarmFieldStateBase(FarmField farmField)
        {
            _farmField = farmField;
        }

        public virtual IEnumerator Interact(
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
            var result = action.Func();
            onDone(result);
            IsBusy = false;
            DebugHelper.Log(null, "Done!");

            yield return null;
        }

        public virtual void CancelAction() {}

        protected virtual IFarmFieldState Plant()
        {
            _farmField.PlantSeed();
            return new PlantedField(_farmField);
        }

        protected virtual IFarmFieldState Water()
        {
            return this;
        }

        protected virtual IFarmFieldState Harvest()
        {
            _farmField.HarvestCarrot();
            return new UntouchedField(_farmField);
        }

        protected virtual IFarmFieldState CollapseMound()
        {
            _farmField.DestroyMound();
            return new UntouchedField(_farmField);
        }

        protected virtual IFarmFieldState DigMound()
        {
            _farmField.CreateMound();
            return new MoundedField(_farmField);
        }

        protected virtual IFarmFieldState RemoveRoots()
        {
            LogWarning(nameof(RemoveRoots));
            return this;
        }

        protected virtual IFarmFieldState Enter()
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