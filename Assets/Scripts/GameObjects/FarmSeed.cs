using UnityEngine;

namespace GameObjects
{
    public class FarmSeed : MonoBehaviour
    {
        [SerializeField] private Transform _undergroundPosition;
        [SerializeField] private Transform _upgroundPosition;
        private Vector3 _startPosition;
        private Vector3 _endPosition;
        private Quaternion _upRotation;

        private Transform _SeedModel;
        private float _currentPosition = 0.0f;
        [SerializeField] private float _growSpeedInSeccond = 1.0f;
        private bool _isReady = false;
        private const float _randomPositionRange = .2f;
        private bool _isShrinking = false;

        private void Awake()
        {
            Transform child = transform.Find("Model")?.transform;
            if (child != null)
            {
                _SeedModel = child.GetComponent<Transform>();
            }
            else
            {
                Debug.LogError("Error, can not find seed model");
            }
        }

        void Start()
        {
            float random_x_move = Random.Range(-_randomPositionRange, _randomPositionRange);
            float random_y_move = Random.Range(-_randomPositionRange, _randomPositionRange);

            _startPosition = new Vector3(
                random_x_move,
                _undergroundPosition.localPosition.y,
                random_y_move
            );

            _endPosition = new Vector3(
                random_x_move,
                _upgroundPosition.localPosition.y,
                random_y_move
            );

            _upRotation = Quaternion.Euler(
                0,
                Random.Range(0.0f, 359.0f),
                0.0f);

            _SeedModel.transform.SetLocalPositionAndRotation(_startPosition, _upRotation);
        }

        public bool IsReady()
        {
            return _isReady;
        }

        public void Shrink()
        {
            _isShrinking = true;
        }

        public Vector3 GetSeedPosition()
        {
            return _endPosition;
        }

        void Update()
        {
            if (!_isReady)
            {
                _currentPosition += Time.deltaTime * (1.0f / _growSpeedInSeccond);
                if (_currentPosition >= 1.0f)
                {
                    _currentPosition = 1.0f;
                    _isReady = true;
                }

                _SeedModel.transform.SetLocalPositionAndRotation(
                    Vector3.Slerp(_startPosition, _endPosition, Mathf.Clamp01(_currentPosition)),
                    _upRotation);
                return;
            }
            if (_isShrinking)
            {
                _currentPosition -= Time.deltaTime * (1.0f / _growSpeedInSeccond);
                if (_currentPosition <= 0.0f)
                {
                    Destroy(gameObject);
                }

                _SeedModel.transform.SetLocalPositionAndRotation(
                    Vector3.Slerp(_startPosition, _endPosition, Mathf.Clamp01(_currentPosition)),
                    _upRotation);
            }
        }
    }
}