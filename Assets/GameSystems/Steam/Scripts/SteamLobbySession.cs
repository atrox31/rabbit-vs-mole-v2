// Steam lobby session helper for hosting/joining and exchanging minimal lobby state.
// Designed to be driven by UI (MainMenu panels).
//
// Responsibilities:
// - Create/Join/Leave lobby
// - Store selected game mode (as lobby data)
// - Ready toggle (as lobby member data)
// - Swap player roles (as lobby data)
//
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace GameSystems.Steam.Scripts
{
    public class SteamLobbySession : MonoBehaviour
    {
        public enum Role
        {
            Rabbit = 0,
            Mole = 1
        }

        public const string LobbyData_GameModeAssetName = "gamemode_asset";
        public const string LobbyData_Mutators = "mutators";
        public const string LobbyData_HostRole = "host_role";
        public const string LobbyData_GuestRole = "guest_role";
        public const string LobbyData_RolesNonce = "roles_nonce";
        public const string LobbyData_StartNonce = "start_nonce";
        public const string LobbyData_BeginNonce = "begin_nonce";
        public const string LobbyData_MapScene = "map_scene";
        public const string LobbyData_Day = "day";
        public const string LobbyData_ProductTag = "rvsm_product";
        public const string LobbyData_ProductTagValue = "RabbitVsMole";
        public const string LobbyData_GameEndNonce = "game_end_nonce";
        public const string LobbyData_GameWinner = "game_winner";

        public const string MemberData_Ready = "ready";
        public const string MemberData_Loaded = "loaded"; // legacy bool (kept for backward safety)
        public const string MemberData_LoadedNonce = "loaded_nonce";
        public const string MemberData_ReadyRolesNonce = "ready_roles_nonce";

        private static SteamLobbySession _instance;
        public static SteamLobbySession Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject("SteamLobbySession").AddComponent<SteamLobbySession>();
                }
                return _instance;
            }
        }

        public event Action OnStateChanged;
        public event Action OnMembersChanged;
        public event Action OnLobbyDataChanged;

#if !DISABLESTEAMWORKS
        private CallResult<LobbyCreated_t> _lobbyCreatedCall;
        private CallResult<LobbyEnter_t> _lobbyEnterCall;
        private Callback<LobbyChatUpdate_t> _lobbyChatUpdateCb;
        private Callback<LobbyDataUpdate_t> _lobbyDataUpdateCb;
#endif

        public ulong CurrentLobbyId { get; private set; }

        public bool IsInLobby => CurrentLobbyId != 0;
        public bool IsHost
        {
            get
            {
#if !DISABLESTEAMWORKS
                if (!IsInLobby || !SteamManager.Initialized) return false;
                var lobby = new CSteamID(CurrentLobbyId);
                return SteamMatchmaking.GetLobbyOwner(lobby) == SteamUser.GetSteamID();
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

#if !DISABLESTEAMWORKS
            _lobbyChatUpdateCb = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
            _lobbyDataUpdateCb = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
#endif
        }

        public void CreateLobby(string gameModeAssetName, bool hasMutators, int maxMembers = 2)
        {
#if !DISABLESTEAMWORKS
            if (!SteamManager.Initialized)
            {
                Debug.LogWarning("Steam not initialized. Cannot create lobby.");
                return;
            }

            maxMembers = Mathf.Clamp(maxMembers, 2, 8);

            var call = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, maxMembers);
            _lobbyCreatedCall ??= CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            _lobbyCreatedCall.Set(call);

            // Stash desired data to set right after creation
            _pendingGameModeAssetName = gameModeAssetName ?? string.Empty;
            _pendingHasMutators = hasMutators;
#else
            Debug.LogWarning("Steamworks disabled. CreateLobby ignored.");
#endif
        }

        public void JoinLobby(ulong lobbyId)
        {
#if !DISABLESTEAMWORKS
            if (!SteamManager.Initialized)
            {
                Debug.LogWarning("Steam not initialized. Cannot join lobby.");
                return;
            }
            if (lobbyId == 0)
                return;

            var call = SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
            _lobbyEnterCall ??= CallResult<LobbyEnter_t>.Create(OnLobbyEnter);
            _lobbyEnterCall.Set(call);
#else
            Debug.LogWarning("Steamworks disabled. JoinLobby ignored.");
#endif
        }

        public void LeaveLobby()
        {
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized)
                return;

            var lobby = new CSteamID(CurrentLobbyId);
            SteamMatchmaking.LeaveLobby(lobby);
            CurrentLobbyId = 0;
            _pendingGameModeAssetName = null;
            _pendingHasMutators = false;

            OnStateChanged?.Invoke();
            OnMembersChanged?.Invoke();
            OnLobbyDataChanged?.Invoke();
#else
            CurrentLobbyId = 0;
            OnStateChanged?.Invoke();
#endif
        }

        public void SetReady(bool ready)
        {
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized)
                return;
            var lobby = new CSteamID(CurrentLobbyId);
            SteamMatchmaking.SetLobbyMemberData(lobby, MemberData_Ready, ready ? "1" : "0");
            // Bind readiness to the current roles version so host can require re-ready after swap.
            if (ready && TryGetRolesNonce(out int rolesNonce) && rolesNonce > 0)
                SteamMatchmaking.SetLobbyMemberData(lobby, MemberData_ReadyRolesNonce, rolesNonce.ToString());
            else
                SteamMatchmaking.SetLobbyMemberData(lobby, MemberData_ReadyRolesNonce, "0");
            OnMembersChanged?.Invoke();
#endif
        }

        public void SetLoaded(bool loaded)
        {
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized)
                return;
            var lobby = new CSteamID(CurrentLobbyId);
            SteamMatchmaking.SetLobbyMemberData(lobby, MemberData_Loaded, loaded ? "1" : "0");
            OnMembersChanged?.Invoke();
#endif
        }

        public void SetLoadedNonce(int startNonce)
        {
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized)
                return;
            var lobby = new CSteamID(CurrentLobbyId);
            SteamMatchmaking.SetLobbyMemberData(lobby, MemberData_LoadedNonce, startNonce.ToString());
            OnMembersChanged?.Invoke();
#endif
        }

        public int HostRequestStart(int mapScene, int dayOfWeek)
        {
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized || !IsHost)
                return 0;

            var lobby = new CSteamID(CurrentLobbyId);
            int next = 1;
            if (TryGetStartNonce(out int current) && current > 0)
                next = current + 1;

            SteamMatchmaking.SetLobbyData(lobby, LobbyData_MapScene, mapScene.ToString());
            SteamMatchmaking.SetLobbyData(lobby, LobbyData_Day, dayOfWeek.ToString());
            SteamMatchmaking.SetLobbyData(lobby, LobbyData_StartNonce, next.ToString());
            // reset begin/loaded for new start
            SteamMatchmaking.SetLobbyData(lobby, LobbyData_BeginNonce, "0");
            SteamMatchmaking.SetLobbyMemberData(lobby, MemberData_Loaded, "0");
            SteamMatchmaking.SetLobbyMemberData(lobby, MemberData_LoadedNonce, "0");

            OnLobbyDataChanged?.Invoke();
            OnMembersChanged?.Invoke();
            return next;
#else
            return 0;
#endif
        }

        public bool TryGetStartNonce(out int nonce)
        {
            nonce = 0;
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized) return false;
            var lobby = new CSteamID(CurrentLobbyId);
            var v = SteamMatchmaking.GetLobbyData(lobby, LobbyData_StartNonce) ?? "0";
            return int.TryParse(v, out nonce);
#else
            return false;
#endif
        }

        public bool TryGetBeginNonce(out int nonce)
        {
            nonce = 0;
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized) return false;
            var lobby = new CSteamID(CurrentLobbyId);
            var v = SteamMatchmaking.GetLobbyData(lobby, LobbyData_BeginNonce) ?? "0";
            return int.TryParse(v, out nonce);
#else
            return false;
#endif
        }

        public void SetBeginNonce(int nonce)
        {
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized || !IsHost) return;
            var lobby = new CSteamID(CurrentLobbyId);
            SteamMatchmaking.SetLobbyData(lobby, LobbyData_BeginNonce, nonce.ToString());
            OnLobbyDataChanged?.Invoke();
#endif
        }

        public bool TryGetStartMapAndDay(out int mapScene, out int dayOfWeek)
        {
            mapScene = 0;
            dayOfWeek = 0;
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized) return false;
            var lobby = new CSteamID(CurrentLobbyId);
            var mapS = SteamMatchmaking.GetLobbyData(lobby, LobbyData_MapScene) ?? "0";
            var dayS = SteamMatchmaking.GetLobbyData(lobby, LobbyData_Day) ?? "0";
            return int.TryParse(mapS, out mapScene) && int.TryParse(dayS, out dayOfWeek);
#else
            return false;
#endif
        }

        public bool AreAllMembersLoaded()
        {
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized) return false;
            var lobby = new CSteamID(CurrentLobbyId);
            int count = SteamMatchmaking.GetNumLobbyMembers(lobby);
            if (count < 2) return false;

            if (!TryGetStartNonce(out int startNonce) || startNonce <= 0)
                return false;

            for (int i = 0; i < count; i++)
            {
                var member = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
                var loadedNonceStr = SteamMatchmaking.GetLobbyMemberData(lobby, member, MemberData_LoadedNonce) ?? "0";
                if (!int.TryParse(loadedNonceStr, out int loadedNonce) || loadedNonce != startNonce)
                    return false;
            }
            return true;
#else
            return false;
#endif
        }

        public bool TryGetHostSteamId(out ulong hostSteamId)
        {
            hostSteamId = 0;
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized) return false;
            hostSteamId = SteamMatchmaking.GetLobbyOwner(new CSteamID(CurrentLobbyId)).m_SteamID;
            return hostSteamId != 0;
#else
            return false;
#endif
        }

        public bool TryGetRolesNonce(out int nonce)
        {
            nonce = 0;
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized) return false;
            var lobby = new CSteamID(CurrentLobbyId);
            var v = SteamMatchmaking.GetLobbyData(lobby, LobbyData_RolesNonce) ?? "0";
            return int.TryParse(v, out nonce);
#else
            return false;
#endif
        }

        public void SwapPlayers()
        {
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized || !IsHost)
                return;

            var lobby = new CSteamID(CurrentLobbyId);
            var hostRole = GetHostRole();
            var guestRole = GetGuestRole();

            // Swap
            SetLobbyRoleData(lobby, LobbyData_HostRole, guestRole);
            SetLobbyRoleData(lobby, LobbyData_GuestRole, hostRole);

            // Increment roles nonce to force both peers to re-confirm Ready under the new roles.
            int nextRolesNonce = 1;
            if (TryGetRolesNonce(out int currentRolesNonce) && currentRolesNonce > 0)
                nextRolesNonce = currentRolesNonce + 1;
            SteamMatchmaking.SetLobbyData(lobby, LobbyData_RolesNonce, nextRolesNonce.ToString());

            OnLobbyDataChanged?.Invoke();
#endif
        }

        public string GetGameModeAssetName()
        {
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized) return string.Empty;
            return SteamMatchmaking.GetLobbyData(new CSteamID(CurrentLobbyId), LobbyData_GameModeAssetName) ?? string.Empty;
#else
            return string.Empty;
#endif
        }

        public bool GetHasMutators()
        {
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized) return false;
            var v = SteamMatchmaking.GetLobbyData(new CSteamID(CurrentLobbyId), LobbyData_Mutators) ?? string.Empty;
            return v == "1";
#else
            return false;
#endif
        }

        public Role GetHostRole()
        {
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized) return Role.Rabbit;
            var v = SteamMatchmaking.GetLobbyData(new CSteamID(CurrentLobbyId), LobbyData_HostRole) ?? "rabbit";
            return ParseRole(v, Role.Rabbit);
#else
            return Role.Rabbit;
#endif
        }

        public Role GetGuestRole()
        {
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized) return Role.Mole;
            var v = SteamMatchmaking.GetLobbyData(new CSteamID(CurrentLobbyId), LobbyData_GuestRole) ?? "mole";
            return ParseRole(v, Role.Mole);
#else
            return Role.Mole;
#endif
        }

        public List<(ulong steamId, string name, Role role, bool ready)> GetPlayers()
        {
            var result = new List<(ulong, string, Role, bool)>();

#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized)
                return result;

            var lobby = new CSteamID(CurrentLobbyId);
            int count = SteamMatchmaking.GetNumLobbyMembers(lobby);
            var hostId = SteamMatchmaking.GetLobbyOwner(lobby);
            int rolesNonce = 0;
            _ = TryGetRolesNonce(out rolesNonce);

            for (int i = 0; i < count; i++)
            {
                var member = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
                string name = SteamFriends.GetFriendPersonaName(member) ?? "?";
                bool ready = IsMemberReady(lobby, member) && (rolesNonce <= 0 || IsMemberReadyForRolesNonce(lobby, member, rolesNonce));
                Role role = member == hostId ? GetHostRole() : GetGuestRole();
                result.Add((member.m_SteamID, name, role, ready));
            }
#endif
            return result;
        }

        public bool CanHostStart()
        {
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized || !IsHost) return false;
            var lobby = new CSteamID(CurrentLobbyId);
            int count = SteamMatchmaking.GetNumLobbyMembers(lobby);
            if (count < 2) return false;

            var hostId = SteamMatchmaking.GetLobbyOwner(lobby);
            int rolesNonce = 0;
            _ = TryGetRolesNonce(out rolesNonce);
            // Any non-host member ready => allow start (for now)
            for (int i = 0; i < count; i++)
            {
                var member = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
                if (member == hostId) continue;
                if (!IsMemberReady(lobby, member)) continue;
                if (rolesNonce > 0 && !IsMemberReadyForRolesNonce(lobby, member, rolesNonce)) continue;
                return true;
            }
            return false;
#else
            return false;
#endif
        }

        public bool TryGetGuestSteamId(out ulong guestSteamId)
        {
            guestSteamId = 0;
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized) return false;
            var lobby = new CSteamID(CurrentLobbyId);
            var hostId = SteamMatchmaking.GetLobbyOwner(lobby);
            int count = SteamMatchmaking.GetNumLobbyMembers(lobby);
            for (int i = 0; i < count; i++)
            {
                var member = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
                if (member == hostId) continue;
                guestSteamId = member.m_SteamID;
                return guestSteamId != 0;
            }
            return false;
#else
            return false;
#endif
        }

        public int GetMemberCount()
        {
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized) return 0;
            var lobby = new CSteamID(CurrentLobbyId);
            return SteamMatchmaking.GetNumLobbyMembers(lobby);
#else
            return 0;
#endif
        }

        public int HostPublishGameEnd(RabbitVsMole.WinConditionEvaluator.Winner winner)
        {
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized || !IsHost) return 0;
            var lobby = new CSteamID(CurrentLobbyId);
            int next = 1;
            var current = SteamMatchmaking.GetLobbyData(lobby, LobbyData_GameEndNonce) ?? "0";
            if (int.TryParse(current, out int c) && c > 0) next = c + 1;

            SteamMatchmaking.SetLobbyData(lobby, LobbyData_GameEndNonce, next.ToString());
            SteamMatchmaking.SetLobbyData(lobby, LobbyData_GameWinner, winner.ToString());
            OnLobbyDataChanged?.Invoke();
            return next;
#else
            return 0;
#endif
        }

        public bool TryGetGameEnd(out int nonce, out RabbitVsMole.WinConditionEvaluator.Winner winner)
        {
            nonce = 0;
            winner = RabbitVsMole.WinConditionEvaluator.Winner.None;
#if !DISABLESTEAMWORKS
            if (!IsInLobby || !SteamManager.Initialized) return false;
            var lobby = new CSteamID(CurrentLobbyId);
            var n = SteamMatchmaking.GetLobbyData(lobby, LobbyData_GameEndNonce) ?? "0";
            if (!int.TryParse(n, out nonce)) return false;
            var w = SteamMatchmaking.GetLobbyData(lobby, LobbyData_GameWinner) ?? string.Empty;
            if (!Enum.TryParse(w, out winner)) winner = RabbitVsMole.WinConditionEvaluator.Winner.None;
            return nonce > 0;
#else
            return false;
#endif
        }

#if !DISABLESTEAMWORKS
        private string _pendingGameModeAssetName;
        private bool _pendingHasMutators;

        private void OnLobbyCreated(LobbyCreated_t param, bool ioFailure)
        {
            if (ioFailure || param.m_eResult != EResult.k_EResultOK)
            {
                Debug.LogWarning($"CreateLobby failed: {param.m_eResult} ioFailure={ioFailure}");
                return;
            }

            CurrentLobbyId = param.m_ulSteamIDLobby;
            var lobby = new CSteamID(CurrentLobbyId);
            Debug.Log($"[SteamLobbySession] Lobby created successfully! LobbyID={CurrentLobbyId}");

            // Lobby data for browser
            SteamMatchmaking.SetLobbyData(lobby, LobbyData_GameModeAssetName, _pendingGameModeAssetName ?? string.Empty);
            SteamMatchmaking.SetLobbyData(lobby, LobbyData_Mutators, _pendingHasMutators ? "1" : "0");
            SteamMatchmaking.SetLobbyData(lobby, LobbyData_ProductTag, LobbyData_ProductTagValue);
            Debug.Log($"[SteamLobbySession] Set lobby data: {LobbyData_ProductTag}={LobbyData_ProductTagValue}, GameMode={_pendingGameModeAssetName}");

            // Default roles
            SetLobbyRoleData(lobby, LobbyData_HostRole, Role.Rabbit);
            SetLobbyRoleData(lobby, LobbyData_GuestRole, Role.Mole);
            SteamMatchmaking.SetLobbyData(lobby, LobbyData_RolesNonce, "1");

            // Host starts unready (not required), client will toggle ready
            SteamMatchmaking.SetLobbyMemberData(lobby, MemberData_Ready, "0");
            SteamMatchmaking.SetLobbyMemberData(lobby, MemberData_ReadyRolesNonce, "0");

            OnStateChanged?.Invoke();
            OnMembersChanged?.Invoke();
            OnLobbyDataChanged?.Invoke();
        }

        private void OnLobbyEnter(LobbyEnter_t param, bool ioFailure)
        {
            if (ioFailure)
            {
                Debug.LogWarning("JoinLobby failed: IO failure");
                return;
            }

            CurrentLobbyId = param.m_ulSteamIDLobby;
            var lobby = new CSteamID(CurrentLobbyId);

            // Reset ready on join
            SteamMatchmaking.SetLobbyMemberData(lobby, MemberData_Ready, "0");
            SteamMatchmaking.SetLobbyMemberData(lobby, MemberData_ReadyRolesNonce, "0");

            OnStateChanged?.Invoke();
            OnMembersChanged?.Invoke();
            OnLobbyDataChanged?.Invoke();
        }

        private void OnLobbyChatUpdate(LobbyChatUpdate_t param)
        {
            if (!IsInLobby || param.m_ulSteamIDLobby != CurrentLobbyId)
                return;
            OnMembersChanged?.Invoke();
        }

        private void OnLobbyDataUpdate(LobbyDataUpdate_t param)
        {
            if (!IsInLobby || param.m_ulSteamIDLobby != CurrentLobbyId)
                return;
            OnLobbyDataChanged?.Invoke();
            OnMembersChanged?.Invoke();
        }

        private static bool IsMemberReady(CSteamID lobby, CSteamID member)
        {
            try
            {
                var v = SteamMatchmaking.GetLobbyMemberData(lobby, member, MemberData_Ready) ?? "0";
                return v == "1";
            }
            catch
            {
                return false;
            }
        }

        private static bool IsMemberReadyForRolesNonce(CSteamID lobby, CSteamID member, int rolesNonce)
        {
            try
            {
                var v = SteamMatchmaking.GetLobbyMemberData(lobby, member, MemberData_ReadyRolesNonce) ?? "0";
                return int.TryParse(v, out int memberNonce) && memberNonce == rolesNonce;
            }
            catch
            {
                return false;
            }
        }

        private static Role ParseRole(string v, Role fallback)
        {
            if (string.IsNullOrWhiteSpace(v)) return fallback;
            if (v.Equals("rabbit", StringComparison.OrdinalIgnoreCase)) return Role.Rabbit;
            if (v.Equals("mole", StringComparison.OrdinalIgnoreCase)) return Role.Mole;
            return fallback;
        }

        private static void SetLobbyRoleData(CSteamID lobby, string key, Role role)
        {
            SteamMatchmaking.SetLobbyData(lobby, key, role == Role.Rabbit ? "rabbit" : "mole");
        }
#endif
    }
}


