using UnityEngine;


namespace RabbitVsMole
{
    public class SpeedController
    {
        public float SpeedMargin { get; set; } = 0.1f;
        public float Current { get; private set; }
        public bool HaveAnySpeed => Current > SpeedMargin;
        public float RotationSpeed => _baseRotationSpeed;

        private float _baseSpeed;
        private float _baseAcceleration;
        private float _baseDeceleration;
        private float _baseRotationSpeed;

        public SpeedController(PlayerType playerType)
        {
            switch (playerType)
            {
                case PlayerType.Rabbit:
                    _baseSpeed = GameInspector.GameStats.StatsBaseWalkingSpeedRabbit;
                    _baseAcceleration = GameInspector.GameStats.StatsBaseAccelerationRabbit;
                    _baseDeceleration = GameInspector.GameStats.StatsBaseDecelerationRabbit;
                    _baseRotationSpeed = GameInspector.GameStats.StatsBaseRotationSpeedRabbit;
                    break;
                case PlayerType.Mole:

                    _baseSpeed = GameInspector.GameStats.StatsBaseWalkingSpeedMole;
                    _baseAcceleration = GameInspector.GameStats.StatsBaseAccelerationMole;
                    _baseDeceleration = GameInspector.GameStats.StatsBaseDecelerationMole;
                    _baseRotationSpeed = GameInspector.GameStats.StatsBaseRotationSpeedMole;
                    break;
                default:

                    _baseSpeed = 0f;
                    _baseAcceleration = 0f;
                    _baseDeceleration = 0f;
                    _baseRotationSpeed = 0f;
                    break;
            }
        }

        public void HandleAcceleration(bool increase) =>
            Current = increase
                ? Mathf.MoveTowards(Current, _baseSpeed, _baseAcceleration * Time.fixedDeltaTime)
                : Mathf.MoveTowards(Current, 0f, _baseDeceleration * Time.fixedDeltaTime);
    }
}