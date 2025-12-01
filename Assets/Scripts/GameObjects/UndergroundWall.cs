using System.Collections;
using UnityEngine;

namespace GameObjects
{
    public class UndergroundWall : MonoBehaviour
    {
        // Pola
        [SerializeField] private Transform _undergroundPosition;
        [SerializeField] private Transform _upgroundPosition;
        [SerializeField] private float _animationDuration = 2.0f;

        private Transform _model;
        private UndergroundField _linkedField;
        private bool _isAnimating = false;

        [SerializeField] private ParticleSystem _animationParticleEffect;

        private void Awake()
        {
            _model = transform.Find("Model");
            if (_model == null)
            {
                Debug.LogError("B³¹d: Nie mo¿na znaleŸæ obiektu dziecka o nazwie 'Model'.", this);
            }
        }

        /// <summary>
        /// Rozpoczyna proces wznoszenia œciany z animacj¹.
        /// </summary>
        /// <param name="linkedField">Pole, z którym ma zostaæ po³¹czona œciana.</param>
        public bool RiseWithAnimation(UndergroundField linkedField)
        {
            // Blokujemy wywo³anie, jeœli ju¿ trwa animacja
            if (_isAnimating) return false;

            _isAnimating = true;
            _linkedField = linkedField;
            _linkedField.LinkBlocker(this);

            StartCoroutine(RiseProcessWithAnimation());
            if(_animationParticleEffect != null)
            {
                _animationParticleEffect.Play();
            }
            return true;
        }

        private IEnumerator RiseProcessWithAnimation()
        {
            float elapsedTime = 0.0f;
            while (elapsedTime < _animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / _animationDuration);

                if (_model != null)
                {
                    _model.transform.localPosition = Vector3.Slerp(
                        _undergroundPosition.localPosition,
                        _upgroundPosition.localPosition,
                        progress
                    );
                }
                yield return null;
            }

            // Upewnij siê, ¿e œciana jest dok³adnie na pozycji koñcowej
            if (_model != null)
            {
                _model.transform.localPosition = _upgroundPosition.localPosition;
            }

            if (_animationParticleEffect != null)
            {
                _animationParticleEffect.Stop();
            }
            _isAnimating = false;
        }

        /// <summary>
        /// Natychmiast usuwa œcianê.
        /// </summary>
        public bool DeleteWallImmediately()
        {
            if (_isAnimating) return false;

            if (_linkedField != null)
            {
                _linkedField.UnlinkBlocker();
            }

            Destroy(gameObject);
            return true;
        }

        /// <summary>
        /// Rozpoczyna proces usuwania œciany z animacj¹.
        /// </summary>
        public bool DestroyWallWithAnimation()
        {
            if (_isAnimating) return false;
            _isAnimating = true;

            StartCoroutine(DestroyProcessWithAnimation());
            if (_animationParticleEffect != null)
            {
                _animationParticleEffect.Play();
            }
            return true;
        }

        private IEnumerator DestroyProcessWithAnimation()
        {
            float elapsedTime = 0.0f;
            while (elapsedTime < _animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / _animationDuration);

                if (_model != null)
                {
                    _model.transform.localPosition = Vector3.Lerp(
                        _upgroundPosition.localPosition,
                        _undergroundPosition.localPosition,
                        progress
                    );
                }
                yield return null;
            }
            _isAnimating = false;

            // Upewnij siê, ¿e obiekt jest w pe³ni schowany
            if (_model != null)
            {
                _model.transform.localPosition = _undergroundPosition.localPosition;
            }

            if (_linkedField != null)
            {
                _linkedField.UnlinkBlocker();
            }

            if (_animationParticleEffect != null)
            {
                // Od³¹czamy system cz¹steczek, aby móg³ zakoñczyæ swoje dzia³anie po zniszczeniu obiektu.
                _animationParticleEffect.transform.SetParent(null);
                var mainModule = _animationParticleEffect.main;
                mainModule.stopAction = ParticleSystemStopAction.Destroy;
                _animationParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            Destroy(gameObject);
        }

        /// <summary>
        /// £¹czy œcianê z danym polem.
        /// </summary>
        /// <param name="link">Pole, z którym ma zostaæ po³¹czona œciana.</param>
        public void LinkField(UndergroundField link)
        {
            _linkedField = link;
        }
    }
}