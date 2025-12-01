using System.Collections;
using Extensions;
using GameObjects.Base;
using UnityEngine;

namespace GameObjects
{
    public class FarmCarrot : CarrotBase
    {
        [Header("Growth and Water Settings")]
        [SerializeField] private float _growthDuration = 3.0f;
        [SerializeField] private float _waterDrainRate = 0.49f;

        // Zmienne do usuwania obiektu
        [SerializeField] private float _shrinkSpeed = 3.0f;
        private bool _isBeingDestroyed = false;

        // Komponenty
        private Transform _carrotModel;
        private Vector3 _startPosition;
        private Vector3 _endPosition;
        private Quaternion _upRotation;

        [Header("Positions & Effects")]
        [SerializeField] private Transform _undergroundPosition;
        [SerializeField] private Transform _upgroundPosition;
        [SerializeField] private ParticleSystem _grownCarrotParticleEffect;
        [SerializeField] private ParticleSystem _harvestCarrotParticleEffect;

        // Korutyny
        private Coroutine _growthCoroutine;
        private Coroutine _destructionCoroutine;

        private void Awake()
        {
            _carrotModel = transform.Find("Model");
            if (_carrotModel == null)
            {
                Debug.LogError("Error: Cannot find 'Model' child object.", this);
                return;
            }

            if (_grownCarrotParticleEffect != null) _grownCarrotParticleEffect.Stop();
        }

        private void Start()
        {
            _upRotation = Quaternion.Euler(0, Random.Range(0.0f, 359.0f), -90.0f);
        }

        /// <summary>
        /// Rozpoczyna proces wzrostu marchewki.
        /// </summary>
        /// <param name="parentField">Pole, na którym roœnie marchewka.</param>
        /// <returns>Zwraca true, jeœli wzrost siê rozpocz¹³, w przeciwnym razie false.</returns>
        public override bool Grow(FarmField.FarmField parentField)
        {
            if (_isBeingDestroyed || IsReady) return false;
            if (parentField == null)
            {
                Debug.LogError("Carrot error: Parent field is null.", this);
                return false;
            }

            // Zabezpieczenie przed wielokrotnym wywo³aniem wzrostu
            if (_growthCoroutine != null) return false;
            _growthCoroutine = StartCoroutine(GrowthProcess(parentField));
            return true;
        }

        /// <summary>
        /// Rozpoczyna proces usuwania marchewki (animacjê zanikania).
        /// </summary>
        /// <returns>Zwraca true, jeœli proces siê rozpocz¹³, w przeciwnym razie false.</returns>
        public override bool Delete()
        {
            if (_isBeingDestroyed) return false;
            if (_growthCoroutine != null)
            {
                StopCoroutine(_growthCoroutine);
                _growthCoroutine = null;
            }

            _isBeingDestroyed = true;
            if (_grownCarrotParticleEffect != null) _grownCarrotParticleEffect.Stop();

            AudioManager.PlaySound3D("Assets/Audio/Sound/rabbit/CarrotPickUpSound.wav", transform.position);
            PlayHarvestAnimation();
            _destructionCoroutine = StartCoroutine(DestructionProcess());
            return true;
        }

        /// <summary>
        /// Ustawia pocz¹tkow¹ i koñcow¹ pozycjê marchewki na podstawie pola, na którym siê pojawia.
        /// </summary>
        /// <param name="spawnPosition">Pozycja, w której obiekt jest tworzony.</param>
   
        public override void SetPosition(Vector3 spawnPosition)
        {
            _startPosition = new Vector3(spawnPosition.x, _undergroundPosition.localPosition.y, spawnPosition.z);
            _endPosition = new Vector3(spawnPosition.x, _upgroundPosition.localPosition.y, spawnPosition.z);

            if (_carrotModel != null)
            {
                _carrotModel.SetLocalPositionAndRotation(_startPosition, _upRotation);
            }
        }

        private void PlayHarvestAnimation()
        {
            if (_harvestCarrotParticleEffect != null)
            {
                _harvestCarrotParticleEffect.DetachAndPlay();
            }
        }

        // --- Metody Korutyn ---

        private IEnumerator GrowthProcess(FarmField.FarmField parentField)
        {
            float elapsedTime = 0f;
            while (elapsedTime < _growthDuration)
            {
                // Wstrzymujemy wzrost, jeœli pole nie ma wystarczaj¹cej iloœci wody
                if (!parentField.HasWater)
                {
                    yield return null;
                    continue;
                }

                elapsedTime += Time.deltaTime;
                parentField.DrainField(_waterDrainRate);

                float progress = Mathf.Clamp01(elapsedTime / _growthDuration);
                SetCarrotPosition(progress);

                yield return null;
            }

            IsReady = true;
            SetCarrotPosition(1.0f); // Zapewniamy, ¿e marchewka jest w pe³ni uformowana

            if (_grownCarrotParticleEffect != null) _grownCarrotParticleEffect.Play();
            if (parentField.HasLinkedField) parentField.GetLinkedField().CreateCarrot();

            _growthCoroutine = null;
        }

        private IEnumerator DestructionProcess()
        {
            float elapsedTime = 0f;
            while (elapsedTime < 1.0f / _shrinkSpeed)
            {
                elapsedTime += Time.deltaTime;
                float progress = 1.0f - Mathf.Clamp01(elapsedTime * _shrinkSpeed);
                SetCarrotPosition(progress);
                yield return null;
            }

            Destroy(gameObject);
        }

        private void SetCarrotPosition(float progress)
        {
            if (_carrotModel != null)
            {
                _carrotModel.SetLocalPositionAndRotation(
                    Vector3.Lerp(_startPosition, _endPosition, progress),
                    _upRotation);
            }
        }
    }
}