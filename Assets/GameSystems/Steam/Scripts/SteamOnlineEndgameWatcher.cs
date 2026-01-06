// Watches Steam lobby state during an online gameplay session.
// - Client: waits for host-published game result via lobby data and shows GameOver.
// - Host/Client: if peer disconnects (lobby member count drops), remaining player wins automatically.
//
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using UnityEngine;

namespace GameSystems.Steam.Scripts
{
    public class SteamOnlineEndgameWatcher : MonoBehaviour
    {
        private static SteamOnlineEndgameWatcher _instance;
        public static SteamOnlineEndgameWatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject("SteamOnlineEndgameWatcher").AddComponent<SteamOnlineEndgameWatcher>();
                }
                return _instance;
            }
        }

        private int _lastSeenGameEndNonce = 0;

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
            SteamLobbySession.Instance.OnLobbyDataChanged += Tick;
            SteamLobbySession.Instance.OnMembersChanged += Tick;
            SteamLobbySession.Instance.OnStateChanged += Tick;
        }

        private void OnDisable()
        {
            SteamLobbySession.Instance.OnLobbyDataChanged -= Tick;
            SteamLobbySession.Instance.OnMembersChanged -= Tick;
            SteamLobbySession.Instance.OnStateChanged -= Tick;
        }

        private void Update()
        {
            Tick();
        }

        private void Tick()
        {
            var inspector = RabbitVsMole.GameManager.CurrentGameInspector;
            if (inspector == null || !inspector.IsOnlineSession)
                return;

            var session = SteamLobbySession.Instance;
            if (!session.IsInLobby)
                return;

            // Disconnect => remaining player wins
            int members = session.GetMemberCount();
            if (!RabbitVsMole.GameManager.IsGameEnded && members > 0 && members < 2)
            {
                RabbitVsMole.GameManager.ForceWinForLocalPlayer();
                return;
            }

            // Client: receive host-decided result
            if (!inspector.IsOnlineHost && !RabbitVsMole.GameManager.IsGameEnded)
            {
                if (session.TryGetGameEnd(out int nonce, out var winner) && nonce > _lastSeenGameEndNonce)
                {
                    _lastSeenGameEndNonce = nonce;
                    RabbitVsMole.GameManager.ApplyRemoteGameEnd(new RabbitVsMole.WinConditionEvaluator.WinResult(winner));
                }
            }
        }
    }
}


