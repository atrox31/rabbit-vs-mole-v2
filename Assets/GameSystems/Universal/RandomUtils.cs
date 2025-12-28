using UnityEngine;

public static class RandomUtils
{
    /// <summary>
    /// Returns true if a random value is within the specified chance (0.0 to 1.0).
    /// Example: Chance(0.3f) returns true 30% of the time.
    /// </summary>
    public static bool Chance(float chance)
    {
        if (chance > 1.0f)
            DebugHelper.LogWarning(null, $"Trying to cast random chance but argument is invalid ({chance}) must be [0f..1f]");
        // Clamp to ensure values outside 0-1 don't break logic
        return Random.value <= Mathf.Clamp01(chance);
    }
}