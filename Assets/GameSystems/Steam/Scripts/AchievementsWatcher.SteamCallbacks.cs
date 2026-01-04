// Steamworks callbacks and Steam-side synchronization logic for AchievementsWatcher.

#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace GameSystems.Steam
{
    public partial class AchievementsWatcher
    {
#if !DISABLESTEAMWORKS
        private Callback<UserStatsReceived_t> _cbUserStatsReceived;
        private Callback<UserStatsStored_t> _cbUserStatsStored;
#endif

        private void InitializeSteamCallbacks()
        {
#if DISABLESTEAMWORKS
            return;
#else
            // Callbacks can be set up even before stats are requested.
            _cbUserStatsReceived = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
            _cbUserStatsStored = Callback<UserStatsStored_t>.Create(OnUserStatsStored);
#endif
        }

#if !DISABLESTEAMWORKS
        private void OnUserStatsReceived(UserStatsReceived_t e)
        {
            if (!SteamManager.Initialized)
                return;
            if (e.m_eResult != EResult.k_EResultOK)
            {
                DebugHelper.LogWarning(this, $"Steam UserStatsReceived failed: {e.m_eResult}");
                return;
            }

            _steamStatsReady = true;

            RefreshConfirmedUnlocksFromSteam();
            RefreshCachedStatsFromSteam();
            ApplyPendingProgress();
            NotifySteamStatsReady();
        }

        private void OnUserStatsStored(UserStatsStored_t e)
        {
            if (!SteamManager.Initialized)
                return;
            if (e.m_eResult != EResult.k_EResultOK)
            {
                DebugHelper.LogWarning(this, $"Steam UserStatsStored failed: {e.m_eResult}");
                return;
            }

            // After store, refresh again so PlayerPrefs only updates when Steam confirms it.
            RefreshConfirmedUnlocksFromSteam();
            RefreshCachedStatsFromSteam();
        }

        private void RefreshConfirmedUnlocksFromSteam()
        {
            _confirmedUnlocked.Clear();

            foreach (var id in _allAchievementIds)
            {
                if (string.IsNullOrEmpty(id))
                    continue;

                bool ok = SteamUserStats.GetAchievement(id, out bool achieved);
                if (ok && achieved)
                {
                    _confirmedUnlocked.Add(id);
                    PlayerPrefs.SetInt(PrefKeyUnlocked(id), 1);
                }
            }

            PlayerPrefs.Save();
        }

        private void RefreshCachedStatsFromSteam()
        {
            // Only cache the stats that we have pending increments for, to avoid maintaining a list of all stats.
            foreach (var kv in _pendingStatIncrements)
            {
                if (string.IsNullOrEmpty(kv.Key))
                    continue;
                if (SteamUserStats.GetStat(kv.Key, out int value))
                    _cachedIntStats[kv.Key] = value;
            }
        }

        private void ApplyPendingProgress()
        {
            // Apply pending stat increments first.
            if (_pendingStatIncrements.Count > 0)
            {
                foreach (var kv in _pendingStatIncrements)
                {
                    string stat = kv.Key;
                    int inc = kv.Value;
                    if (string.IsNullOrEmpty(stat) || inc == 0)
                        continue;

                    int current = GetSteamStatIntOrDefault(stat, 0);
                    int next = current + inc;
                    if (_statClampMax.TryGetValue(stat, out int clampMax) && clampMax > 0)
                        next = Mathf.Clamp(next, 0, clampMax);
                    SteamUserStats.SetStat(stat, next);
                    _cachedIntStats[stat] = next;
                }
                _pendingStatIncrements.Clear();
                SteamUserStats.StoreStats();
            }

            // Then apply pending unlocks.
            if (_pendingUnlocks.Count > 0)
            {
                foreach (var id in _pendingUnlocks)
                {
                    if (IsAchievementConfirmedUnlocked(id))
                        continue;
                    SteamUserStats.SetAchievement(id);
                }
                _pendingUnlocks.Clear();
                SteamUserStats.StoreStats();
            }
        }
#endif
    }
}


