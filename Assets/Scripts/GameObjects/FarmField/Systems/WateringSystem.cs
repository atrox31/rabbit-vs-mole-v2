using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace GameObjects.FarmField.Systems
{
    public class WateringSystem
    {
        private readonly float _wateringRate;
        private readonly float _maxWaterLevel;

        private float _waterLevel = 0;
        private Coroutine _coroutine;

        public WateringSystem(
            float wateringRate = 0.1f,
            float maxWaterLevel = 1)
        {
            _wateringRate = wateringRate;
            _maxWaterLevel = maxWaterLevel;
        }

        public bool IsWatering => _coroutine != null;

        public void StartWatering(
            MonoBehaviour owner,
            Action<float> onWaterLevelChanged,
            CancellationToken cancellationToken)
        {
            _coroutine = owner.StartCoroutine(WateringProcess(cancellationToken));
        }

        private IEnumerator WateringProcess(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _waterLevel < _maxWaterLevel)
            {
                _waterLevel += _wateringRate * Time.deltaTime;
                yield return null;
            }

            if (_waterLevel > _maxWaterLevel)
                _waterLevel = _maxWaterLevel;
        }
    }
}
