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

                if(_waterLevel > GameManager.CurrentGameStats.WaterSourceMaxWaterLevel)
                    _waterLevel = GameManager.CurrentGameStats.WaterSourceMaxWaterLevel;

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
            if(WaterCurrentLevel < GameManager.CurrentGameStats.WaterSourceMaxWaterLevel)
            {
                WaterCurrentLevel += GameManager.CurrentGameStats.WaterSourceWaterPerSec * Time.deltaTime;
            }
        }

        protected override void OnCancelAction(Action action) { }

        private void Start()
        {
            WaterCurrentLevel = GameManager.CurrentGameStats.WaterSourceMaxWaterLevel;
        }

        private void SetWaterLevelVisual()
        {
            var waterLevel = Mathf.Clamp01(WaterCurrentLevel / GameManager.CurrentGameStats.WaterSourceMaxWaterLevel);
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

            var canDrain = CanDrain(GameManager.CurrentGameStats.WaterSourceWaterDrainPerAction);
            var canPutToBackpack = backpack.Water.CanInsert(GameManager.CurrentGameStats.WaterSourceWaterToInventoryPerDrain);
            return (canDrain && canPutToBackpack);
        }

        protected override bool Action(Backpack backpack, Func<ActionType, float> OnActionRequested, Action OnActionCompleted)
        {
            /// we are sure that
            /// 1. Player is Rabbit
            /// 2. Water level is ok
            /// 3. Rabbit can hold more water

            if (!backpack.Water.TryInsert(GameManager.CurrentGameStats.WaterSourceWaterToInventoryPerDrain, true))
                return false;

            WaterCurrentLevel -= GameManager.CurrentGameStats.WaterSourceWaterDrainPerAction;

            var actionTime = OnActionRequested.Invoke(ActionType.PickWater);
            StartCoroutine(CompliteAction(OnActionCompleted, actionTime));
            return true;
        }

    }
}