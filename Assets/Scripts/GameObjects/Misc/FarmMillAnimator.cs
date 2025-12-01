using UnityEngine;

namespace GameObjects.Misc
{
    public class FarmMillAnimator : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private float _animationSpeed = 60f; // Base rotation speed
        [SerializeField] private float _maxSafeSpeed = 120f;  // Speed above which shaking starts

        [Header("Shaking Settings")]
        [SerializeField] private float _shakeMagnitude = 0.08f; // How much the wings shake (position offset)
        [SerializeField] private float _shakeFrequency = 10f;  // How fast the wings shake

        private Transform _wings;
        private Vector3 _initialLocalPosition;   

        private void Awake()
        {
            if (transform.childCount > 0)
            {
                _wings = transform.GetChild(0);
            }

            if (_wings == null)
            {
                Debug.LogError("No wings object found for animation. Make sure it's a child of the object with this script.");
                enabled = false;
                return; // Exit Awake if no wings are found
            }

            _initialLocalPosition = _wings.localPosition;
        }

        void Update()
        {
            float currentRotationSpeed = Time.deltaTime * _animationSpeed;

            _wings.Rotate(Vector3.forward, currentRotationSpeed, Space.Self);

            _wings.localPosition = _initialLocalPosition;

            if (_animationSpeed > _maxSafeSpeed)
            {
                ApplyShaking();
            }
        }

        private void ApplyShaking()
        {
            float timeOffset = Time.time * _shakeFrequency;

            float posX = Mathf.PerlinNoise(timeOffset, 0f) * 2f - 1f; // Range from -1 to 1
            float posY = Mathf.PerlinNoise(0f, timeOffset) * 2f - 1f; // Range from -1 to 1

            Vector3 shakeOffset = new Vector3(posX, posY, 0) * _shakeMagnitude;
            _wings.localPosition += shakeOffset;
        }
    }
}