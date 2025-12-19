using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace GameObjects.FarmField.Systems
{
    public class WateringSystem
    {
        private readonly FarmField _field;
        private readonly float _wateringRate;
        private readonly float _maxWaterLevel;

        private float _waterLevel;
        private Coroutine _coroutine;

        public WateringSystem(
            FarmField field,
            float wateringRate = 0.1f,
            float maxWaterLevel = 1)
        {
            _field = field;
            _wateringRate = wateringRate;
            _maxWaterLevel = maxWaterLevel;
        }

        public bool IsWatering => _coroutine != null;

        private float WaterLevel
        {
            get => _waterLevel;
            set
            {
                _waterLevel = value;
                
                if (_waterLevel > _maxWaterLevel)
                    _waterLevel = _maxWaterLevel;
                
                var fillAmount = Mathf.Clamp01(_waterLevel / _maxWaterLevel);
                _field.WaterIndicator.fillAmount = fillAmount;
            }
        }

        public void StartWatering(
            Action<float> onWaterLevelChanged,
            CancellationToken cancellationToken)
        {
            _coroutine = _field.StartCoroutine(WateringProcess(cancellationToken));
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
