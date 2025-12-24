using PlayerManagementSystem;
using PlayerManagementSystem.Backpack;
using RabbitVsMole.InteractableGameObject.Base;
using RabbitVsMole.InteractableGameObject.Enums;
using System;
using UnityEngine;

namespace RabbitVsMole.InteractableGameObject.Storages
{
    public class FarmWaterStorage : StorageBase
    {
        private float _waterLevel;
        float WaterCurrentLevel
        {
            get => _waterLevel;
            set
            {
                _waterLevel = value;

                if(_waterLevel > GameInspector.GameStats.WaterSourceMaxWaterLevel)
                    _waterLevel = GameInspector.GameStats.WaterSourceMaxWaterLevel;

                if(_waterLevel < 0)
                    _waterLevel = 0;

                SetWaterLevelVisual();
            }
        }

        [Header("Visuals")]
        [SerializeField] Transform _waterLevel0;
        [SerializeField] Transform _waterLevel1;
        [SerializeField] GameObject _waterGameObject;

        void Update()
        {
            if(WaterCurrentLevel < GameInspector.GameStats.WaterSourceMaxWaterLevel)
            {
                WaterCurrentLevel += GameInspector.GameStats.WaterSourceWaterPerSec * Time.deltaTime;
            }
        }

        protected override void OnCancelAction() { }

        private void Start()
        {
            WaterCurrentLevel = GameInspector.GameStats.WaterSourceMaxWaterLevel;
        }

        private void SetWaterLevelVisual()
        {
            var waterLevel = Mathf.Clamp01(WaterCurrentLevel / GameInspector.GameStats.WaterSourceMaxWaterLevel);
            _waterGameObject.transform.localPosition = Vector3.Lerp(_waterLevel0.localPosition, _waterLevel1.localPosition, waterLevel);
        }

        private bool CanDrain(float value)
        {
            return (WaterCurrentLevel - value) > 0;
        }

        public override bool CanInteract(Backpack backpack)
        {
            if(backpack.PlayerType != PlayerType.Rabbit)
                return false;
            //TODO: add mutator flag "water spoliage"

            var canDrain = CanDrain(GameInspector.GameStats.WaterSourceWaterDrainPerAction);
            var canPutToBackpack = backpack.Water.CanInsert(GameInspector.GameStats.WaterSourceWaterToInventoryPerDrain);
            return (canDrain && canPutToBackpack);
        }

        protected override bool Action(Backpack backpack, Func<ActionType, float> OnActionRequested, Action OnActionCompleted)
        {
            /// we are sure that
            /// 1. Player is Rabbit
            /// 2. Water level is ok
            /// 3. Rabbit can hold more water

            if (!backpack.Water.TryInsert(GameInspector.GameStats.WaterSourceWaterToInventoryPerDrain, true))
                return false;

            WaterCurrentLevel -= GameInspector.GameStats.WaterSourceWaterDrainPerAction;

            var actionTime = OnActionRequested.Invoke(ActionType.PickWater);
            StartCoroutine(CompliteAction(OnActionCompleted, actionTime));
            return true;
        }

    }
}