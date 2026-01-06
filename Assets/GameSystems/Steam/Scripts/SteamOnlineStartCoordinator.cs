// Coordinates online game start between host and client using Steam lobby data.
//
// Flow:
// - Client clicks Ready => lobby member data "ready=1"
// - Host clicks Start => host sets lobby data "start_nonce" (+ map/day) and calls GameManager.PlayGame
// - Client observes start_nonce change => builds PlayGameSettings and calls GameManager.PlayGame
// - OnSceneShow in gameplay => both sides Pause + set member data "loaded=1"
// - Host observes both loaded => sets lobby data "begin_nonce" to current start_nonce
// - Both observe begin_nonce == start_nonce => Unpause + CurrentGameInspector.StartGame()
//
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System;
using UnityEngine;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace GameSystems.Steam.Scripts
{
    public class SteamOnlineStartCoordinator : MonoBehaviour
    {
        private static SteamOnlineStartCoordinator _instance;
        public static SteamOnlineStartCoordinator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject("SteamOnlineStartCoordinator").AddComponent<SteamOnlineStartCoordinator>();
                }
                return _instance;
            }
        }

        private int _localStartNonce = 0;
        private bool _localSceneShown = false;
        private bool _localStarted = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SteamLobbySession.Instance.OnLobbyDataChanged += OnLobbyDataChanged;
            SteamLobbySession.Instance.OnMembersChanged += OnMembersChanged;
        }

        private void OnDisable()
        {
            SteamLobbySession.Instance.OnLobbyDataChanged -= OnLobbyDataChanged;
            SteamLobbySession.Instance.OnMembersChanged -= OnMembersChanged;
        }

        public void ResetForNewSession()
        {
            _localStartNonce = 0;
            _localSceneShown = false;
            _localStarted = false;
        }

        /// <summary>
        /// Called by GameManager after gameplay scene is shown (online only).
        /// This pauses the game and marks this peer as loaded.
        /// </summary>
        public void OnGameplaySceneShown()
        {
            _localSceneShown = true;
            RabbitVsMole.GameManager.Pause();

            // Mark loaded for this specific start nonce to avoid stale ready state.
            if (SteamLobbySession.Instance.TryGetStartNonce(out int startNonce) && startNonce > 0)
                SteamLobbySession.Instance.SetLoadedNonce(startNonce);
            else
                SteamLobbySession.Instance.SetLoaded(true);

            TryHostSignalBeginIfReady();
            TryStartIfBeginArrived();
        }

        private void OnLobbyDataChanged()
        {
            TryStartIfBeginArrived();
        }

        private void OnMembersChanged()
        {
            TryHostSignalBeginIfReady();
        }

        public void SetLocalStartNonce(int nonce)
        {
            _localStartNonce = nonce;
        }

        private void TryHostSignalBeginIfReady()
        {
            var session = SteamLobbySession.Instance;
            if (!session.IsInLobby || !session.IsHost) return;

            if (!session.TryGetStartNonce(out int startNonce)) return;
            if (startNonce == 0) return;

            if (!session.AreAllMembersLoaded()) return;

            // Signal begin only once for a given start nonce.
            if (session.TryGetBeginNonce(out int beginNonce) && beginNonce == startNonce)
                return;

            session.SetBeginNonce(startNonce);
        }

        private void TryStartIfBeginArrived()
        {
            if (_localStarted) return;
            if (!_localSceneShown) return;

            var session = SteamLobbySession.Instance;
            if (!session.IsInLobby) return;

            if (!session.TryGetStartNonce(out int startNonce)) return;
            if (!session.TryGetBeginNonce(out int beginNonce)) return;
            if (startNonce == 0 || beginNonce == 0) return;
            if (beginNonce != startNonce) return;

            // Ensure this is the same game start this client is loading.
            if (_localStartNonce != 0 && _localStartNonce != startNonce)
                return;

            // Start game now
            _localStarted = true;
            RabbitVsMole.GameManager.Unpause();
            var inspector = RabbitVsMole.GameManager.CurrentGameInspector;
            inspector?.StartGame();
        }
    }
}


