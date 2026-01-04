// Achievements watcher built on Steamworks.NET.
// Listens to EventBus events and updates Steam stats / unlocks achievements.

#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using GameSystems;
using GameSystems.Steam.Scripts;
using System;
using System.Collections.Generic;
using UnityEngine;
#if !DISABLESTEAMWORKS
using System.Reflection;
using Steamworks;
#endif

namespace GameSystems.Steam
{
    public partial class AchievementsWatcher : MonoBehaviour
    {
        protected static AchievementsWatcher _instance;
        protected static AchievementsWatcher Instance
        {
            get
            {
                if (_instance == null)
                    return new GameObject("AchievementsWatcher").AddComponent<AchievementsWatcher>();
                else
                    return _instance;
            }
        }

        private readonly List<IAchievement> _achievements = new();
        private readonly HashSet<string> _allAchievementIds = new();
        private readonly Dictionary<string, int> _statClampMax = new();

        private float _nextStatsRequestTime;
        private bool _steamStatsReady;
        private readonly Dictionary<string, int> _cachedIntStats = new();
        private readonly HashSet<string> _confirmedUnlocked = new();
        private readonly HashSet<string> _pendingUnlocks = new();
        private readonly Dictionary<string, int> _pendingStatIncrements = new();
        private Func<bool> _isTrackingAllowed;

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSteamCallbacks();
        }

        /// <summary>
        /// Initializes watcher with a list of achievements and an optional gate predicate.
        /// If gate returns false, watcher ignores events/stat updates/unlocks (useful for e.g. splitscreen).
        /// </summary>
        public static void Initialize(IAchievement[] achievements, Func<bool> isTrackingAllowed = null)
        {
            Instance.Configure(achievements, isTrackingAllowed);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;

            UnregisterAllAchievements();
        }

        private void Update()
        {
            // Steam init may happen in bootstrap; request stats once when available.
            TryRequestCurrentStats();
        }

        private void Configure(IAchievement[] achievements, Func<bool> isTrackingAllowed)
        {
            UnregisterAllAchievements();

            _isTrackingAllowed = isTrackingAllowed;
            _achievements.Clear();
            _allAchievementIds.Clear();
            _statClampMax.Clear();

            if (achievements != null)
            {
                foreach (var a in achievements)
                {
                    if (a == null)
                        continue;
                    _achievements.Add(a);
                    _allAchievementIds.Add(a.AchievementId);

                    if (a is IStatProgressInfo statInfo && !string.IsNullOrEmpty(statInfo.StatName))
                        _statClampMax[statInfo.StatName] = statInfo.ClampMax;
                }
            }

            foreach (var a in _achievements)
                a.Register(this);

            // If we already have stats ready (e.g. domain reload), refresh again.
            TryRequestCurrentStats();
            if (_steamStatsReady)
                NotifySteamStatsReady();
        }

        private void UnregisterAllAchievements()
        {
            for (int i = 0; i < _achievements.Count; i++)
            {
                try { _achievements[i]?.Unregister(); }
                catch (Exception ex) { DebugHelper.LogWarning(this, $"Unregister achievement failed: {ex}"); }
            }
        }

        public bool IsTrackingAllowedNow()
        {
            if (_isTrackingAllowed == null)
                return true;

            try { return _isTrackingAllowed(); }
            catch (Exception ex)
            {
                DebugHelper.LogWarning(this, $"IsTrackingAllowed callback threw: {ex}");
                return false;
            }
        }

        private void TryRequestCurrentStats()
        {
#if DISABLESTEAMWORKS
            return;
#else
            if (_steamStatsReady)
                return;
            if (!SteamManager.Initialized)
                return;
            if (Time.unscaledTime < _nextStatsRequestTime)
                return;

            bool ok = TryRequestCurrentStatsCompat();
            if (!ok)
            {
                DebugHelper.LogWarning(this, "SteamUserStats.RequestCurrentStats() returned false.");
                _nextStatsRequestTime = Time.unscaledTime + 10f;
            }
            else
            {
                // If callback doesn't arrive for any reason, retry later (very low frequency).
                _nextStatsRequestTime = Time.unscaledTime + 30f;
            }
#endif
        }

#if !DISABLESTEAMWORKS
        private bool TryRequestCurrentStatsCompat()
        {
            // Some Steamworks wrappers/versions don't expose RequestCurrentStats.
            // Use reflection to keep compilation compatible.
            var mi = typeof(SteamUserStats).GetMethod(
                "RequestCurrentStats",
                BindingFlags.Public | BindingFlags.Static);

            if (mi == null)
            {
                // No explicit request available -> assume stats can be queried immediately.
                _steamStatsReady = true;
                RefreshConfirmedUnlocksFromSteam();
                RefreshCachedStatsFromSteam();
                ApplyPendingProgress();
                NotifySteamStatsReady();
                return true;
            }

            try
            {
                return (bool)mi.Invoke(null, null);
            }
            catch (Exception ex)
            {
                DebugHelper.LogWarning(this, $"RequestCurrentStats reflection invoke failed: {ex}");
                return false;
            }
        }
#endif

        private static string PrefKeyUnlocked(string achievementId) =>
            $"SteamAchievementUnlocked::{achievementId}";

        /// <summary>
        /// Returns true if we already have a confirmed unlock state from Steam (cached + persisted).
        /// </summary>
        public bool IsAchievementConfirmedUnlocked(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId))
                return false;

            if (_confirmedUnlocked.Contains(achievementId))
                return true;

            return PlayerPrefs.GetInt(PrefKeyUnlocked(achievementId), 0) == 1;
        }

        public void TryUnlockAchievement(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId))
                return;
            if (IsAchievementConfirmedUnlocked(achievementId))
                return;
            if (!IsTrackingAllowedNow())
                return;

#if DISABLESTEAMWORKS
            return;
#else
            if (!SteamManager.Initialized)
                return;

            // If we don't have stats yet, queue it so we don't miss early triggers (e.g. fast carrot).
            if (!_steamStatsReady)
            {
                _pendingUnlocks.Add(achievementId);
                return;
            }

            SteamUserStats.SetAchievement(achievementId);
            SteamUserStats.StoreStats();
#endif
        }

        public void IncrementSteamStat(string statName, int increment, int clampMax)
        {
            if (string.IsNullOrEmpty(statName))
                return;
            if (increment == 0)
                return;
            if (!IsTrackingAllowedNow())
                return;

#if DISABLESTEAMWORKS
            return;
#else
            if (!SteamManager.Initialized)
                return;

            if (!_steamStatsReady)
            {
                _pendingStatIncrements.TryGetValue(statName, out int pending);
                _pendingStatIncrements[statName] = pending + increment;
                return;
            }

            int current = GetSteamStatIntOrDefault(statName, defaultValue: 0);
            int next = current + increment;
            if (clampMax > 0)
                next = Mathf.Clamp(next, 0, clampMax);

            SteamUserStats.SetStat(statName, next);
            _cachedIntStats[statName] = next;
            SteamUserStats.StoreStats();
#endif
        }

        public void TryUnlockAchievementIfStatReached(string achievementId, string statName, int max)
        {
            if (max <= 0)
                return;
            if (IsAchievementConfirmedUnlocked(achievementId))
                return;

            int current = GetSteamStatIntOrDefault(statName, defaultValue: 0);
            if (current >= max)
                TryUnlockAchievement(achievementId);
        }

        public int GetSteamStatIntOrDefault(string statName, int defaultValue)
        {
            if (string.IsNullOrEmpty(statName))
                return defaultValue;

            if (_cachedIntStats.TryGetValue(statName, out int cached))
                return cached;

#if DISABLESTEAMWORKS
            return defaultValue;
#else
            if (!SteamManager.Initialized || !_steamStatsReady)
                return defaultValue;

            bool ok = SteamUserStats.GetStat(statName, out int value);
            if (!ok)
                return defaultValue;

            _cachedIntStats[statName] = value;
            return value;
#endif
        }

        private void NotifySteamStatsReady()
        {
            foreach (var a in _achievements)
            {
                try { a?.OnSteamStatsReady(this); }
                catch (Exception ex) { DebugHelper.LogWarning(this, $"OnSteamStatsReady failed: {ex}"); }
            }
        }
    }
}