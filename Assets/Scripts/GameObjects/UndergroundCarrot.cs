using GameObjects.Base;
using UnityEngine;

namespace GameObjects
{
    public class UndergroundCarrot : CarrotBase
    {
        private float _carrotScale;
        private float _carrotGrowTime = 1.63f;
        private bool _carrotGrow;
        private bool _carrotShrink;

        private void Update()
        {
            if (_carrotGrow)
            {
                _carrotScale += Time.deltaTime * (1.0f / _carrotGrowTime);
                transform.localScale = Vector3.Slerp(Vector3.zero, new Vector3(_carrotScale, _carrotScale, _carrotScale), Mathf.Clamp01(_carrotScale));

                if (_carrotScale >= 1.0f)
                {
                    _carrotScale = 1.0f;
                    _carrotGrow = false;
                }
            }
            if (_carrotShrink)
            {
                _carrotScale -= Time.deltaTime * (1.0f / _carrotGrowTime);
                transform.localScale = Vector3.Slerp(Vector3.zero, new Vector3(_carrotScale, _carrotScale, _carrotScale), Mathf.Clamp01(_carrotScale));

                if (_carrotScale <= 0.0f)
                {
                    _carrotShrink = false;
                    _carrotScale = 0.0f;
                    Destroy(gameObject);
                }
            }
        }

        public override bool Grow(FarmField.FarmField parrentField)
        {
            return false;
        }

        public override bool Delete()
        {
            if (_carrotShrink)
                return false;

            _carrotShrink = true;

            return true;
        }

        private void Start()
        {
            _carrotScale = 0.0f;
            _carrotGrow = true;
            _carrotShrink = false;
            transform.localScale = Vector3.zero;
        }
    }
}
