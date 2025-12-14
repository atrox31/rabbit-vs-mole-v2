using UnityEngine;

namespace GameObjects.Misc
{
    public class SpeedController
    {
        public float SpeedMargin { get; set; } = 0.1f;
        public float Current { get; private set; }
        public bool HaveAnySpeed => Current > SpeedMargin;
        private AvatarStats _avatarStats;

        public SpeedController(AvatarStats avatarStats)
        {
            _avatarStats = avatarStats;
        }

        public void HandleAcceleration(bool increase) =>
            Current = increase
                ? Mathf.MoveTowards(Current, _avatarStats.MaxWalkSpeed, _avatarStats .Acceleration * Time.fixedDeltaTime)
                : Mathf.MoveTowards(Current, 0f, _avatarStats.Deceleration * Time.fixedDeltaTime);
    }
}