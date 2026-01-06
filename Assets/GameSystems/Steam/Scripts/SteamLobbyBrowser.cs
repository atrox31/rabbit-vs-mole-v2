// Background lobby list fetcher built on Steamworks.NET.
// Updates a GUITable while the table (panel) is visible.
//
// Note: Steam lobby "ping" is not directly available from the lobby list API.
// For now we set Ping = -1 (displayed as "-"). We'll refine this later if needed.
//
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using Interface.Element;
using UnityEngine;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace GameSystems.Steam.Scripts
{
    public class SteamLobbyBrowser : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _refreshIntervalSeconds = 2f;
        [SerializeField] private int _maxResults = 50;

        [Header("Lobby Data Keys (optional)")]
        [SerializeField] private string _keyGameMode = SteamLobbySession.LobbyData_GameModeAssetName;
        [SerializeField] private string _keyMutators = SteamLobbySession.LobbyData_Mutators; // "1"/"true"/"yes" => HasMutators=true

        private GUITable _table;
        private Coroutine _routine;

#if !DISABLESTEAMWORKS
        private CallResult<LobbyMatchList_t> _lobbyMatchListCall;
        private bool _waitingForList;
#endif

        public void Bind(GUITable table)
        {
            _table = table;
        }

        /// <summary>
        /// Trigger an immediate refresh (e.g., user pressed Refresh button).
        /// </summary>
        public void RefreshNow()
        {
            if (!isActiveAndEnabled)
                return;

            // Immediate UI feedback: clear the table right away so users see that refresh was triggered.
            _table?.SetRows(Array.Empty<GUITable.RowData>(), keepSelectionIfPossible: false);

            if (!SteamManager.Initialized)
            {
                return;
            }

#if !DISABLESTEAMWORKS
            if (_waitingForList)
                return;
            RequestLobbyList();
#endif
        }

        private void Awake()
        {
            if (_table == null)
            {
                _table = GetComponent<GUITable>();
            }
        }

        private void OnEnable()
        {
            if (_routine == null)
            {
                _routine = StartCoroutine(RefreshRoutine());
            }
        }

        private void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        private IEnumerator RefreshRoutine()
        {
            // Keep running while this component is enabled (panel visible).
            while (isActiveAndEnabled)
            {
                if (!SteamManager.Initialized)
                {
                    // Steam not ready yet, keep table empty.
                    _table?.SetRows(Array.Empty<GUITable.RowData>(), keepSelectionIfPossible: false);
                    yield return new WaitForSecondsRealtime(Mathf.Max(0.25f, _refreshIntervalSeconds));
                    continue;
                }

#if !DISABLESTEAMWORKS
                RequestLobbyList();

                float timeout = 5f;
                float start = Time.unscaledTime;
                while (_waitingForList && isActiveAndEnabled && (Time.unscaledTime - start) < timeout)
                {
                    yield return null;
                }
                _waitingForList = false;
#endif

                yield return new WaitForSecondsRealtime(Mathf.Max(0.25f, _refreshIntervalSeconds));
            }
        }

#if !DISABLESTEAMWORKS
        private void RequestLobbyList()
        {
            _waitingForList = true;

            // Apply filters for this request
            // Spacewar (480) is shared across many projects; tag-filter to show only our game's lobbies.
            Debug.Log($"[SteamLobbyBrowser] Requesting lobby list with filter: {SteamLobbySession.LobbyData_ProductTag}={SteamLobbySession.LobbyData_ProductTagValue}");
            
            SteamMatchmaking.AddRequestLobbyListStringFilter(
                SteamLobbySession.LobbyData_ProductTag,
                SteamLobbySession.LobbyData_ProductTagValue,
                ELobbyComparison.k_ELobbyComparisonEqual);

            if (_maxResults > 0)
            {
                SteamMatchmaking.AddRequestLobbyListResultCountFilter(_maxResults);
            }

            SteamAPICall_t call = SteamMatchmaking.RequestLobbyList();
            _lobbyMatchListCall ??= CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
            _lobbyMatchListCall.Set(call);
        }

        private void OnLobbyMatchList(LobbyMatchList_t param, bool ioFailure)
        {
            try
            {
                _waitingForList = false;

                if (ioFailure)
                {
                    Debug.LogWarning("[SteamLobbyBrowser] RequestLobbyList IO failure!");
                    _table?.SetRows(Array.Empty<GUITable.RowData>(), keepSelectionIfPossible: false);
                    return;
                }

                int count = (int)param.m_nLobbiesMatching;
                Debug.Log($"[SteamLobbyBrowser] Found {count} lobbies matching filter");
                var rows = new List<GUITable.RowData>(Mathf.Max(0, count));

                for (int i = 0; i < count; i++)
                {
                    CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
                    CSteamID ownerId = SteamMatchmaking.GetLobbyOwner(lobbyId);
                    
                    // Debug: show lobby details
                    string productTag = SafeGetLobbyData(lobbyId, SteamLobbySession.LobbyData_ProductTag);
                    Debug.Log($"[SteamLobbyBrowser] Lobby[{i}]: ID={lobbyId.m_SteamID}, Owner={ownerId.m_SteamID}, ProductTag='{productTag}'");

                    string hostName = SteamFriends.GetFriendPersonaName(ownerId);
                    if (string.IsNullOrWhiteSpace(hostName) || hostName == "?")
                    {
                        // Ensure persona info is requested; it may be missing on Spacewar if owner is not in friends list.
                        try { SteamFriends.RequestUserInformation(ownerId, true); } catch { }

                        if (ownerId == SteamUser.GetSteamID())
                        {
                            hostName = SteamFriends.GetPersonaName();
                        }

                        if (string.IsNullOrWhiteSpace(hostName) || hostName == "?")
                        {
                            hostName = ownerId.m_SteamID.ToString();
                        }
                    }
                    string gm = SafeGetLobbyData(lobbyId, _keyGameMode);
                    string mut = SafeGetLobbyData(lobbyId, _keyMutators);

                    rows.Add(new GUITable.RowData
                    {
                        GameMode = string.IsNullOrWhiteSpace(gm) ? "?" : gm,
                        HasMutators = ParseBool(mut),
                        HostName = string.IsNullOrWhiteSpace(hostName) ? "?" : hostName,
                        Ping = -1,
                        // For now we use LobbyId string as "Ip"/connection key. Later we can connect via Steam lobby.
                        Ip = lobbyId.m_SteamID.ToString()
                    });
                }

                _table?.SetRows(rows, keepSelectionIfPossible: true);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        private static string SafeGetLobbyData(CSteamID lobbyId, string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            try
            {
                return SteamMatchmaking.GetLobbyData(lobbyId, key) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool ParseBool(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            value = value.Trim();
            return value == "1"
                   || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("y", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("tak", StringComparison.OrdinalIgnoreCase);
        }
#endif
    }
}


