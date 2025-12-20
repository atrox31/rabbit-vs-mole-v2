using System;
using Enums;
using RabbitVsMole;
using System.Collections.Generic;
using System.Threading;
using GameObjects.FarmField.Systems;

namespace GameObjects.FarmField.States
{
    public sealed class PlantedField : FarmFieldStateBase
    {
        private CancellationTokenSource _actionCancellationTokenSource;
        
        private readonly WateringSystem _wateringSystem;

        public PlantedField(FarmField farmField) : base(farmField)
        {
            AIPriority = new AIPriority(priority: 70);
            _wateringSystem = new WateringSystem(farmField);
        }

        public override bool CanWater => true;
        public override bool CanDigMound => true;

        protected override IReadOnlyDictionary<PlayerType, FarmFieldActionMapEntry> GetActionMap()
        {
            return new Dictionary<PlayerType, FarmFieldActionMapEntry>
            {
                { PlayerType.Rabbit, new FarmFieldActionMapEntry(ActionType.Water, Water) },
                { PlayerType.Mole, new FarmFieldActionMapEntry(ActionType.DigMound, DigMound) }
            };
        }

        protected override IFarmFieldState Water()
        {
            _actionCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            _wateringSystem.StartWatering(_actionCancellationTokenSource.Token);
            
            return this;
        }
    }
}