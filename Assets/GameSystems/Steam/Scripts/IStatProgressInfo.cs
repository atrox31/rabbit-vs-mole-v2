namespace GameSystems.Steam.Scripts
{
    /// <summary>
    /// Internal helper interface for achievements driven by Steam stat progress.
    /// Used by AchievementsWatcher to discover stat clamp limits during initialization.
    /// </summary>
    internal interface IStatProgressInfo
    {
        string StatName { get; }
        int ClampMax { get; }
    }
}



