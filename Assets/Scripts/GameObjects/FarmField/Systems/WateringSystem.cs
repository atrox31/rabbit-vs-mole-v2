using System.Collections;
using System.Threading;
using UnityEngine;

namespace GameObjects.FarmField.Systems
{
    public class WateringSystem
    {
        private readonly FarmField _farmField;
        private readonly float _wateringRate;
        private readonly float _maxWaterLevel;

        private float _waterLevel;
        private Coroutine _coroutine;

        public WateringSystem(
            FarmField farmField,
            float wateringRate = 0.1f,
            float maxWaterLevel = 1)
        {
            _farmField = farmField;
            _wateringRate = wateringRate;
            _maxWaterLevel = maxWaterLevel;
        }

        public bool IsWatering => _coroutine != null;

        public float WaterLevel
        {
            get => _waterLevel;
            private set
            {
                _waterLevel = value;
                
                if (_waterLevel > _maxWaterLevel)
                    _waterLevel = _maxWaterLevel;
                
                var fillAmount = Mathf.Clamp01(_waterLevel / _maxWaterLevel);
                _farmField.WaterIndicator.fillAmount = fillAmount;
            }
        }

        public void DrainWater(float amount)
        {
            WaterLevel -= amount;
        }

        public void StartWatering(CancellationToken cancellationToken)
        {
            _coroutine = _farmField.StartCoroutine(WateringProcess(cancellationToken));
        }

        private IEnumerator WateringProcess(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _waterLevel < _maxWaterLevel)
            {
                WaterLevel += _wateringRate * Time.deltaTime;

                if (WaterLevel >= _maxWaterLevel)
                    yield break;
                
                yield return null;
            }
        }
    }
}
