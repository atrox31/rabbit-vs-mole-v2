using GameSystems;
using System;

namespace GameSystems.Steam.Scripts
{
    /// <summary>
    /// Unlocks an achievement when a specific EventBus event matches the provided condition.
    /// </summary>
    public sealed class EventAchievement<TEvent> : IAchievement
    {
        private readonly string _achievementId;
        private readonly Func<TEvent, bool> _condition;
        private AchievementsWatcher _watcher;
        private Action<TEvent> _handler;

        public string AchievementId => _achievementId;

        public EventAchievement(string achievementId, Func<TEvent, bool> condition)
        {
            _achievementId = achievementId;
            _condition = condition ?? (_ => true);
        }

        public void Register(AchievementsWatcher watcher)
        {
            _watcher = watcher;
            _handler = OnEvent;
            EventBus.Subscribe(_handler);
        }

        public void Unregister()
        {
            if (_handler != null)
                EventBus.Unsubscribe(_handler);
            _handler = null;
            _watcher = null;
        }

        public void OnSteamStatsReady(AchievementsWatcher watcher)
        {
            // nothing
        }

        private void OnEvent(TEvent e)
        {
            if (_watcher == null)
                return;
            if (!_watcher.IsTrackingAllowedNow())
                return;
            if (_watcher.IsAchievementConfirmedUnlocked(_achievementId))
                return;
            if (_condition != null && !_condition(e))
                return;

            _watcher.TryUnlockAchievement(_achievementId);
        }
    }
}



