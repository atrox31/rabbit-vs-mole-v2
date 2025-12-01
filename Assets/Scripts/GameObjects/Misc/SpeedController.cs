using UnityEngine;

namespace GameObjects.Misc
{
    public class SpeedController
    {
        private float _acceleration; 
        private float _deceleration;
        private float _maxWalkSpeed;

        public float SpeedMargin { get; set; } = 0.1f;
        public float Current { get; private set; }
        public bool HaveAnySpeed => Current > SpeedMargin;

        public SpeedController(float acceleration, float deceleration, float maxWalkSpeed)
        {
            _acceleration = acceleration;
            _deceleration = deceleration;
            _maxWalkSpeed = maxWalkSpeed;
        }

        public void HandleAcceleration(bool increase) =>
            Current = increase
                ? Mathf.MoveTowards(Current, _maxWalkSpeed, _acceleration * Time.fixedDeltaTime)
                : Mathf.MoveTowards(Current, 0f, _deceleration * Time.fixedDeltaTime);
    }
}