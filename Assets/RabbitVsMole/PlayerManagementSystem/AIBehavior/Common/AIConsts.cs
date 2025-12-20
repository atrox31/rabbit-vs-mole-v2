namespace PlayerManagementSystem.AIBehaviour.Common
{
    public static class AIConsts
    {
        public static readonly string   SUPPLY_TAG = "Supply";

        public static readonly int      MAX_INTELIGENCE = 100;
        public static readonly int      MIN_INTELIGENCE = 0;

        public static readonly float    MIN_THINKING_TIME = 1.0f;
        public static readonly float    MAX_THINKING_TIME = 5.0f;

        public static readonly float    MIN_AGGRO_SPHERE_RADIUS = 1.0f;
        public static readonly float    MAX_AGGRO_SPHERE_RADIUS = 5.0f;
        public static readonly float    AGGRO_ZERO = 0f;
        public static readonly float    AGGRO_MIN_TO_CHASE = 33f;
        public static readonly float    AGGRO_CALM_DOWN_RATION = 0.33f;
        public static readonly float    AGGRO_MAX = 100f;
        public static readonly float    AGGRO_INCREASE_RATIO_BY_INTELIIGENCE = .33f;
    }
}