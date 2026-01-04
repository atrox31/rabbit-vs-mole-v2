using System;

namespace GameSystems.Steam.Scripts
{
    /// <summary>
    /// Achievement definition that can register itself to listen for game events and report progress/unlocks via AchievementsWatcher.
    /// </summary>
    public interface IAchievement
    {
        string AchievementId { get; }
        void Register(AchievementsWatcher watcher);
        void Unregister();
        void OnSteamStatsReady(AchievementsWatcher watcher);
    }
}



