using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameObjects
{
    public class Mound : MonoBehaviour
    {
        [Header("Mound Settings")]
        [SerializeField] private Transform _undergroundPosition;
        [SerializeField] private Transform _upgroundPosition;
        [SerializeField] private float _morphingTime = 1.0f;

        // Components
        private Transform _model;
        private ParticleSystem _particles;

        // State
        public bool IsReady { get; private set; }
        private Coroutine _morphCoroutine;

        private void Awake()
        {
            // U�yj GetComponentInChilden, aby znale�� model, a nast�pnie jego ParticleSystem
            _model = transform.Find("Model");
            if (_model == null)
            {
                Debug.LogError("Error: Cannot find 'Model' child object.", this);
                return;
            }

            _particles = _model.GetComponentInChildren<ParticleSystem>();
            if (_particles == null)
            {
                Debug.LogWarning("Warning: No ParticleSystem found in 'Model' child object.", this);
            }
        }

        private void Start()
        {
            // Rozpoczynamy animacj� wzrostu
            MorphUp();
            AudioManager.PlaySound3D("Assets/Audio/Sound/mole/digging a mold.wav", transform.position);
        }

        public void Delete()
        {
            if (_morphCoroutine != null)
            {
                StopCoroutine(_morphCoroutine);
            }
            IsReady = false;
            MorphDownAndDestroy();
        }

        private void MorphUp()
        {
            if (_morphCoroutine != null)
            {
                StopCoroutine(_morphCoroutine);
            }
            _morphCoroutine = StartCoroutine(Morph(_undergroundPosition.localPosition, _upgroundPosition.localPosition, () =>
            {
                IsReady = true;
                _particles?.Stop();
            }));
        }

        private void MorphDownAndDestroy()
        {
            _particles?.Play();
            _morphCoroutine = StartCoroutine(Morph(_model.localPosition, _undergroundPosition.localPosition, () =>
            {
                Destroy(gameObject);
            }));
        }

        private IEnumerator Morph(Vector3 start, Vector3 end, Action onComplete = null)
        {
            float elapsedTime = 0f;
            Quaternion targetRotation = Quaternion.Euler(0f, Random.Range(0.0f, 359.0f), 0f);

            _particles?.Play();

            while (elapsedTime < _morphingTime)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / _morphingTime);

                _model.localPosition = Vector3.Slerp(start, end, progress);
                _model.localRotation = targetRotation;

                yield return null;
            }

            // Ustawienie ostatecznej pozycji, by unikn�� b��d�w zaokr�glania
            _model.localPosition = end;

            // Wywo�anie akcji po zako�czeniu animacji
            onComplete?.Invoke();
        }
    }
}