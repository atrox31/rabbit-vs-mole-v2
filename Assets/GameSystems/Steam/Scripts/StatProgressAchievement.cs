using GameSystems;
using System;

namespace GameSystems.Steam.Scripts
{
    /// <summary>
    /// Increments a Steam stat on specific EventBus events, and unlocks an achievement on reaching max.
    /// </summary>
    public sealed class StatProgressAchievement<TEvent> : IAchievement, IStatProgressInfo
    {
        private readonly string _achievementId;
        private readonly string _statName;
        private readonly int _max;
        private readonly int _incrementPerTrigger;
        private readonly Func<TEvent, bool> _condition;

        private AchievementsWatcher _watcher;
        private Action<TEvent> _handler;

        public string AchievementId => _achievementId;
        public string StatName => _statName;
        public int ClampMax => _max;

        public StatProgressAchievement(
            string achievementId,
            string statName,
            int max,
            int incrementPerTrigger,
            Func<TEvent, bool> condition)
        {
            _achievementId = achievementId;
            _statName = statName;
            _max = max;
            _incrementPerTrigger = incrementPerTrigger;
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
            if (watcher.IsAchievementConfirmedUnlocked(_achievementId))
                return;

            int current = watcher.GetSteamStatIntOrDefault(_statName, defaultValue: 0);
            if (current >= _max)
            {
                watcher.TryUnlockAchievement(_achievementId);
            }
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugHelper.Log(null, $"[Achievements] Progress event '{typeof(TEvent).Name}' -> '{_achievementId}' (stat '{_statName}' +{_incrementPerTrigger}/{_max})");
#endif
            _watcher.IncrementSteamStat(_statName, _incrementPerTrigger, clampMax: _max);
            _watcher.TryUnlockAchievementIfStatReached(_achievementId, _statName, _max);
        }
    }
}



