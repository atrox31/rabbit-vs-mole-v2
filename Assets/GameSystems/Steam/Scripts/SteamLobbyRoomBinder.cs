// Binds SteamLobbySession -> GUICustomElement_GameModeSelector in "lobby room" mode.
// Updates player list and center text while active.
//
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace GameSystems.Steam.Scripts
{
    public class SteamLobbyRoomBinder : MonoBehaviour
    {
        [SerializeField] private RabbitVsMole.GUICustomElement_GameModeSelector _selector;
        [SerializeField] private bool _isHostView = false;
        [SerializeField] private List<RabbitVsMole.GameModeData> _availableGameModes = new();

        [Header("Localization")]
        [SerializeField] private LocalizedString _roleRabbit = new LocalizedString("Interface", "interface_role_rabbit");
        [SerializeField] private LocalizedString _roleMole = new LocalizedString("Interface", "interface_role_mole");
        [SerializeField] private LocalizedString _readySuffix = new LocalizedString("Interface", "interface_lobby_ready_suffix");
        [SerializeField] private LocalizedString _modePrefix = new LocalizedString("Interface", "interface_lobby_mode_prefix");

        private void Awake()
        {
            if (_selector == null)
            {
                _selector = GetComponent<RabbitVsMole.GUICustomElement_GameModeSelector>();
            }
        }

        public void Configure(RabbitVsMole.GUICustomElement_GameModeSelector selector, bool isHostView, List<RabbitVsMole.GameModeData> availableGameModes = null)
        {
            _selector = selector;
            _isHostView = isHostView;
            _availableGameModes = availableGameModes ?? new List<RabbitVsMole.GameModeData>();
        }

        private void OnEnable()
        {
            SteamLobbySession.Instance.OnMembersChanged += Refresh;
            SteamLobbySession.Instance.OnLobbyDataChanged += Refresh;
            SteamLobbySession.Instance.OnStateChanged += Refresh;
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            Refresh();
        }

        private void OnDisable()
        {
            SteamLobbySession.Instance.OnMembersChanged -= Refresh;
            SteamLobbySession.Instance.OnLobbyDataChanged -= Refresh;
            SteamLobbySession.Instance.OnStateChanged -= Refresh;
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        private void OnLocaleChanged(UnityEngine.Localization.Locale _)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_selector == null) return;

            var session = SteamLobbySession.Instance;
            var players = session.GetPlayers();

            string rabbitTag = ResolveNow(_roleRabbit, "[Rabbit]");
            string moleTag = ResolveNow(_roleMole, "[Mole]");
            string readySuffix = ResolveNow(_readySuffix, " (Ready)");

            var lines = new List<string>();
            foreach (var p in players)
            {
                string roleTag = p.role == SteamLobbySession.Role.Rabbit ? rabbitTag : moleTag;
                string readyTag = p.ready ? readySuffix : "";
                lines.Add($"{p.name} {roleTag}{readyTag}");
            }

            // Ensure we always show at least host line even if Steam not ready
            if (lines.Count == 0)
            {
                lines.Add("...");
            }

            string gm = session.GetGameModeAssetName();
            if (string.IsNullOrWhiteSpace(gm)) gm = "-";

            _selector.SetLobbyMode(lines);

            // Apply selected mode visuals (icon + description + configuration).
            var modeData = _availableGameModes?.Find(m => m != null && m.name == gm);
            if (modeData != null)
            {
                _selector.SetModeVisuals(modeData);
            }
            else
            {
                // Fallback: show at least the mode name in center, keep config empty.
                var modeLabel = new LocalizedString("Interface", "interface_lobby_mode_prefix")
                {
                    Arguments = new object[] { gm }
                };
                _selector.SetCenterOverrideLocalized(modeLabel, new LocalizedString());
                _selector.SetModeVisuals(null);
            }
        }

        private static string ResolveNow(LocalizedString localized, string fallback)
        {
            if (localized == null || localized.IsEmpty)
                return fallback ?? string.Empty;

            try
            {
                var op = localized.GetLocalizedStringAsync();
                if (op.IsDone && !string.IsNullOrEmpty(op.Result))
                    return op.Result;
            }
            catch { }

            return fallback ?? string.Empty;
        }
    }
}


