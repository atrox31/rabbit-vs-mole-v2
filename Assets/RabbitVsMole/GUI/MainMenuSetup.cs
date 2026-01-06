using Extensions;
using GameSystems.Steam.Scripts;
using Interface;
using PlayerManagementSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using RabbitVsMole.GameData.Mutator;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RabbitVsMole
{
    using static GameManager;

    public class MainMenuSetup : MonoBehaviour
    {
        private MainMenuManager _menuManager;

        private GUIPanel _mainMenu;
        private GUIPanel _playPanel;
        private GUIPanel _playPanelStory;
        private GUIPanel _playPanelStoryRabbit;
        private GUIPanel _playPanelStoryMole;
        private GUIPanel _playPanelChalleange;
        private GUIPanel _playPanelDuel;
        private GUIPanel _playPanelDuelLocalSolo;
        private GUIPanel _playPanelDuelLocalSplit;
        private GUIPanel _playPanelDuelOnline;
        private GUIPanel _playPanelDuelOnlineHostSetup;
        private GUIPanel _playPanelDuelOnlineHostLobby;
        private GUIPanel _playPanelDuelOnlineJoinLobby;
        private GUIPanel _panelOptions;
        private GUIPanel _panelOptionsGeneral;
        private GUIPanel _panelOptionsGraphic;
        private GUIPanel _panelOptionsAudio;
        private GUIPanel _panelOptionsControls;
        private GUIPanel _panelOptionsControlsKeyboardPrimary;
        private GUIPanel _panelOptionsControlsKeyboardSecondary;
        private GUIPanel _panelOptionsControlsGamepad;
        private GUIPanel _creditsPanel;
        private GUIPanel _playMutatorSelector;

        private GUIPanel _mainInputPrompt;
        private Action<PlayGameSettings> _fallbackAfterSelect;
        private PlayGameSettings _playGameSettings;
        private GUICustomElement_MutatorSelector _mutatorSelectorElement;
        private GUICustomElement_GameModeSelector _mutatorSourceSelector;
        private readonly List<GUICustomElement_GameModeSelector> _mutatorAwareSelectors = new();
        private readonly Dictionary<GameModeData, List<MutatorSO>> _defaultMutatorsByMode = new();

        private List<Interface.Element.GUILabel> _creditLabels = new List<Interface.Element.GUILabel>();

        [SerializeField] private TMPro.TextMeshProUGUI _versionLabel;

        [Header("Localization Settings")]
        [SerializeField] private string _localizationTableName = "Interface";
        LocalizedString GetLocalizedString(string key) => new LocalizedString(_localizationTableName, key);

        [Header("Key Bindings")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string playerActionMap = "Player";
        [SerializeField] private string playerPrimaryBindingGroup = "KeyboardP1";
        [SerializeField] private string playerSecondaryBindingGroup = "KeyboardP2";
        [SerializeField] private string playerGamepadBindingGroup = "Gamepad";

        [Header("Custom elements")]
        [SerializeField] private GameObject daySelectorPrefab;
        [SerializeField] private GameObject widePanelPrefab;
        [SerializeField] private GameObject gameModeSelectorPrefab;
        [SerializeField] private GameObject onlineSessionsTablePrefab;
        [SerializeField] private GameObject mutatorSelectorPrefab;

        [Header("Mutators")]
        [SerializeField] private List<MutatorSO> _availableMutators = new();

        [Header("GameModes")]
        [SerializeField] private List<GameModeData> rabbitStoryGameModes;
        [SerializeField] private List<GameModeData> moleStoryGameModes;
        [SerializeField] private List<GameModeData> challengeGameModes;
        [SerializeField] private List<GameModeData> duelGameModes;

        private bool IsStoryComplite => GameManager.GetStoryProgress(System.DayOfWeek.Sunday, PlayerType.Rabbit);
        void Awake()
        {
            _menuManager = GetComponent<MainMenuManager>();
            if (_menuManager == null)
            {
                Debug.LogError("MainMenuManager not found!");
                return;
            }

            if (inputActions != null)
            {
                InputBindingManager.LoadInputBindings(inputActions);
            }
            else
            {
                DebugHelper.LogWarning(this, "Input Action Asset is not assigned on RabbitVsMoleMenuSetup.");
            }

            if (_versionLabel != null)
            {
                _versionLabel.text = "V " + Application.version;
            }

            // Initialize graphics settings (FPS, etc.)
            MainMenuDefaultLogic.InitializeTargetFPS();

            CacheDefaultMutators();
            SetupMenus();
            LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;

#if UNITY_EDITOR
            // In editor, runtime changes to ScriptableObject assets can "stick" after exiting Play Mode.
            // Restore defaults ONLY when leaving Play Mode (not on scene changes when starting gameplay).
            EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        private void OnEditorPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                RestoreDefaultMutators(rabbitStoryGameModes);
                RestoreDefaultMutators(moleStoryGameModes);
                RestoreDefaultMutators(challengeGameModes);
                RestoreDefaultMutators(duelGameModes);
            }
        }
#endif

        private void CacheDefaultMutators()
        {
            _defaultMutatorsByMode.Clear();

            void Cache(IEnumerable<GameModeData> list)
            {
                if (list == null) return;
                foreach (var gm in list)
                {
                    if (gm == null) continue;
                    if (_defaultMutatorsByMode.ContainsKey(gm)) continue;

                    var copy = gm.mutators != null
                        ? gm.mutators.Where(m => m != null).ToList()
                        : null;
                    _defaultMutatorsByMode[gm] = copy;
                }
            }

            Cache(rabbitStoryGameModes);
            Cache(moleStoryGameModes);
            Cache(challengeGameModes);
            Cache(duelGameModes);
        }

        private void RestoreDefaultMutators(IEnumerable<GameModeData> list)
        {
            if (list == null) return;
            foreach (var gm in list)
            {
                if (gm == null) continue;
                if (_defaultMutatorsByMode.TryGetValue(gm, out var original))
                {
                    gm.mutators = original != null ? new List<MutatorSO>(original) : null;
                }
                else
                {
                    // Not cached (shouldn't happen), fallback to clearing.
                    gm.mutators = null;
                }
            }
        }

        public void ShowMenu()
        {
            _menuManager.ChangePanel(_mainMenu);
            _menuManager.SteamAvatar?.Show();
        }

        public void ShowOnlineDuelList()
        {
            _menuManager.ChangePanel(_playPanelDuelOnline);
            _menuManager.SteamAvatar?.Show();
            // Fake a reasonable back history: MainMenu -> Play -> Duel -> Online list
            // (Do not include current panel in history; top should be previous panel.)
            _menuManager.SeedHistory( _mainMenu, _playPanel, _playPanelDuel);
        }

        public void WebPageRedirect()
        {
            Application.OpenURL("https://gamejolt.com/@atrox_studio");
        }

        public void ShowInputPrompt(Action<PlayGameSettings> fallbackAfterSelect, PlayGameSettings playGameSettings)
        {
            if (fallbackAfterSelect == null)
                throw new ArgumentNullException(nameof(fallbackAfterSelect));

            _fallbackAfterSelect = fallbackAfterSelect;
            _playGameSettings = playGameSettings;
            _menuManager.ChangePanel(_mainInputPrompt);
        }

        private List<MutatorSO> GetAvailableMutators()
        {
            if (_availableMutators != null && _availableMutators.Count > 0)
                return _availableMutators;

            var unique = new HashSet<MutatorSO>();
            void Collect(IEnumerable<GameModeData> list)
            {
                if (list == null) return;
                foreach (var gm in list)
                {
                    if (gm?.mutators == null) continue;
                    foreach (var mut in gm.mutators)
                    {
                        if (mut != null)
                            unique.Add(mut);
                    }
                }
            }

            Collect(rabbitStoryGameModes);
            Collect(moleStoryGameModes);
            Collect(challengeGameModes);
            Collect(duelGameModes);

            return unique.ToList();
        }

        private void BindMutatorButton(GUIPanel panel, string selectorId)
        {
            if (panel == null || string.IsNullOrWhiteSpace(selectorId))
                return;

            if (panel.GetElementByID(selectorId) is GUICustomElement_GameModeSelector selector)
            {
                if (_mutatorAwareSelectors.Contains(selector))
                    return;

                selector.MutatorConfigRequested += OnMutatorConfigRequested;
                _mutatorAwareSelectors.Add(selector);
            }
        }

        private void OnMutatorConfigRequested(GUICustomElement_GameModeSelector selector, GameModeData gameMode, List<MutatorSO> preselected)
        {
            if (_mutatorSelectorElement == null || _playMutatorSelector == null || _menuManager == null || gameMode == null)
                return;

            _mutatorSourceSelector = selector;

            var available = GetAvailableMutators();
            var selected = preselected ?? gameMode.mutators ?? new List<MutatorSO>();
            // NOTE: gameMode.mutators are defaults for the mode, but user should be able to remove them.
            // Use locked mutators only for truly mandatory ones (none for now).
            List<MutatorSO> locked = null;

            _mutatorSelectorElement.InitializeWithArgument(
                new GUICustomElement_MutatorSelector.InitArgs(
                    available,
                    new List<MutatorSO>((selected ?? new List<MutatorSO>()).Where(m => m != null)),
                    new List<MutatorSO>((locked ?? Enumerable.Empty<MutatorSO>()).Where(m => m != null))));

            _menuManager.ChangePanel(_playMutatorSelector);
        }

        private void OnMutatorAcceptClicked(List<MutatorSO> mutators)
        {
            var safe = mutators ?? new List<MutatorSO>();
            _mutatorSourceSelector?.ApplyMutatorsToSelectedMode(safe);

            // Online lobby: update lobby browser flag dynamically when host changes mutators after lobby creation.
            if (SteamLobbySession.Instance.IsInLobby && SteamLobbySession.Instance.IsHost)
            {
                SteamLobbySession.Instance.SetHasMutators(safe.Any(m => m != null));
            }

            _mutatorSourceSelector = null;
            _menuManager.GoBack();
        }

        private void OnMutatorBackClicked()
        {
            _mutatorSourceSelector = null;
            _menuManager.GoBack();
        }

        private int GetMaxStoryProgress(PlayerType playerType)
        {

            if (GameManager.GetStoryProgress(System.DayOfWeek.Sunday, playerType))
            {
                return 7;
            }
            else
            {
                for (var i = 6; i >= 0; i--)
                {
                    if (GameManager.GetStoryProgress((System.DayOfWeek)i, playerType))
                    {
                        return i;
                    }
                }
                return 1;
            }
        }

        DayOfWeek GetDayFromSelectedIndex(int index)
        {
            if (index < 0 || index > 6)
            {
                DebugHelper.LogWarning(this, "GetDayFromSelectedIndex: Index out of range, returning Monday as default.");
                return DayOfWeek.Monday;
            }

            int dayValue = (index + 1) % 7;
            return (DayOfWeek)dayValue;
        }

        void PlayStoryRabbit()
        {
            var gamemode = (_playPanelStoryRabbit.GetElementByID("GameModeStoryRabbit") as GUICustomElement_GameModeSelector).GetSelectedGameMode();
            var dayOfWeek = GetDayFromSelectedIndex((_playPanelStoryRabbit.GetElementByID("GameModeStoryRabbit") as GUICustomElement_GameModeSelector).SelectedIndex);

            GameManager.PlayGame(
                gameMode: gamemode,
                map: GameSceneManager.SceneType.Gameplay_RabbitSolo,
                day: dayOfWeek,
                playerTypeForStory: PlayerType.Rabbit,
                rabbitControlAgent: PlayerControlAgent.Human,
                moleControlAgent: PlayerControlAgent.None);
        }
        void PlayStoryMole()
        {
            var gamemode = (_playPanelStoryMole.GetElementByID("GameModeStoryMole") as GUICustomElement_GameModeSelector).GetSelectedGameMode();
            var dayOfWeek = GetDayFromSelectedIndex((_playPanelStoryRabbit.GetElementByID("GameModeStoryMole") as GUICustomElement_GameModeSelector).SelectedIndex);

            GameManager.PlayGame(
                gameMode: gamemode,
                map: GameSceneManager.SceneType.GamePlay_MoleSolo,
                day: dayOfWeek,
                playerTypeForStory: PlayerType.Mole,
                rabbitControlAgent: PlayerControlAgent.None,
                moleControlAgent: PlayerControlAgent.Human);
        }

        void PlayDuelSplitScreen()
        {
            var gamemode = (_playPanelDuelLocalSplit.GetElementByID("GameModelDuelLocalSplit") as GUICustomElement_GameModeSelector).GetSelectedGameMode();
            GameManager.PlayGame(
                gameMode: gamemode,
                map: GameSceneManager.SceneType.GamePlay_Duel,
                day: System.DayOfWeek.Monday.SelectRandom(),
                playerTypeForStory: PlayerType.Rabbit,
                rabbitControlAgent: PlayerControlAgent.Human,
                moleControlAgent: PlayerControlAgent.Human);
        }

        void PlayDuelSolo(PlayerType playerType)
        {
            var selector = _playPanelDuelLocalSolo.GetElementByID("GameModeDuelLocalSolo") as GUICustomElement_GameModeSelector;
            var gamemode = selector.GetSelectedGameMode();
            int aiIntelligence = selector != null ? selector.GetSelectedAiIntelligence() : 90;
            GameManager.PlayGame(
                gameMode: gamemode,
                map: GameSceneManager.SceneType.GamePlay_Duel,
                day: System.DayOfWeek.Monday.SelectRandom(),
                playerTypeForStory: playerType,
                rabbitControlAgent: playerType == PlayerType.Rabbit ? PlayerControlAgent.Human : PlayerControlAgent.Bot,
                moleControlAgent: playerType == PlayerType.Mole ? PlayerControlAgent.Human : PlayerControlAgent.Bot,
                aiIntelligence: aiIntelligence);
        }

        private bool TryBuildHostOnlinePlaySettings(out PlayGameSettings settings, out PlayerType onlineAgentPlayerType)
        {
            settings = default;
            onlineAgentPlayerType = PlayerType.Rabbit;

            var session = SteamLobbySession.Instance;
            if (!session.IsInLobby || !session.IsHost)
                return false;

            string assetName = session.GetGameModeAssetName();
            if (string.IsNullOrWhiteSpace(assetName))
                return false;

            var gameMode = duelGameModes.Find(gm => gm != null && gm.name == assetName);
            if (gameMode == null)
                return false;

            if (!session.TryGetGuestSteamId(out var guestSteamId))
                return false;

            var hostRole = session.GetHostRole();
            var guestRole = session.GetGuestRole();

            PlayerType hostPlayerType = hostRole == SteamLobbySession.Role.Rabbit ? PlayerType.Rabbit : PlayerType.Mole;
            PlayerType guestPlayerType = guestRole == SteamLobbySession.Role.Rabbit ? PlayerType.Rabbit : PlayerType.Mole;

            // Host side: local player is Human on host role, remote is Online (driven by guest via transport).
            var rabbitAgent = hostPlayerType == PlayerType.Rabbit ? PlayerControlAgent.Human : PlayerControlAgent.Online;
            var moleAgent = hostPlayerType == PlayerType.Mole ? PlayerControlAgent.Human : PlayerControlAgent.Online;

            var onlineConfig = PlayGameSettings.OnlineConfig.CreateHost(session.CurrentLobbyId, guestSteamId);
            settings = new PlayGameSettings(
                gameMode: gameMode,
                map: GameSceneManager.SceneType.GamePlay_Duel,
                day: System.DayOfWeek.Monday.SelectRandom(),
                playerTypeForStory: hostPlayerType,
                rabbitControlAgent: rabbitAgent,
                moleControlAgent: moleAgent,
                aiIntelligence: 90,
                onlineConfig: onlineConfig);

            // We only create OnlineAgentController for the remote-controlled player on host.
            onlineAgentPlayerType = guestPlayerType;
            return true;
        }

        private bool TryBuildClientOnlinePlaySettings(out PlayGameSettings settings)
        {
            settings = default;

            var session = SteamLobbySession.Instance;
            if (!session.IsInLobby || session.IsHost)
                return false;

            if (!session.TryGetStartNonce(out var startNonce) || startNonce <= 0)
                return false;

            if (!session.TryGetStartMapAndDay(out int mapScene, out int dayOfWeek))
                return false;

            if (!session.TryGetHostSteamId(out var hostSteamId))
                return false;

            string assetName = session.GetGameModeAssetName();
            if (string.IsNullOrWhiteSpace(assetName))
                return false;

            var gameMode = duelGameModes.Find(gm => gm != null && gm.name == assetName);
            if (gameMode == null)
                return false;

            // Client wants to control the guest role.
            var guestRole = session.GetGuestRole();
            PlayerType guestPlayerType = guestRole == SteamLobbySession.Role.Rabbit ? PlayerType.Rabbit : PlayerType.Mole;

            // Current OnlineAgentController implementation assumes a single Online agent per peer.
            // We create Online controller only for the local (guest) player on client.
            var rabbitAgent = guestPlayerType == PlayerType.Rabbit ? PlayerControlAgent.Online : PlayerControlAgent.None;
            var moleAgent = guestPlayerType == PlayerType.Mole ? PlayerControlAgent.Online : PlayerControlAgent.None;

            var onlineConfig = PlayGameSettings.OnlineConfig.CreateClient(session.CurrentLobbyId, hostSteamId);
            settings = new PlayGameSettings(
                gameMode: gameMode,
                map: (GameSceneManager.SceneType)mapScene,
                day: (System.DayOfWeek)dayOfWeek,
                playerTypeForStory: guestPlayerType,
                rabbitControlAgent: rabbitAgent,
                moleControlAgent: moleAgent,
                aiIntelligence: 90,
                onlineConfig: onlineConfig);

            // Stash nonce so coordinator will only begin when the correct session begins.
            GameSystems.Steam.Scripts.SteamOnlineStartCoordinator.Instance.SetLocalStartNonce(startNonce);
            return true;
        }

        private void SetupMenus()
        {
            _mainInputPrompt = _menuManager.CreatePanel(GetLocalizedString("menu_input_selector"))    
              .AddLabel(GetLocalizedString("text_input_selector_header")) 
              .AddSpacer()
              .AddButton(GetLocalizedString("button_play_duel_as_rabbit"), () =>
              {
                  _fallbackAfterSelect?.Invoke(_playGameSettings.SetGamepadForPlayer(PlayerType.Rabbit));
              })
              .AddButton(GetLocalizedString("button_play_duel_as_mole"), () =>
              {
                  _fallbackAfterSelect?.Invoke(_playGameSettings.SetGamepadForPlayer(PlayerType.Mole));
              })
              .Build();

            _playMutatorSelector = _menuManager.CreatePanel(GetLocalizedString("menu_mutator_selector"), widePanelPrefab)
                .AddCustomElement(mutatorSelectorPrefab)
                .SetId("MutatorSelectorElement")
                .AddSpacer(10f)
                .AddButton(GetLocalizedString("button_add"), () => _mutatorSelectorElement?.AddSelectedMutator(), true)
                .AddButton(GetLocalizedString("button_remove"), () => _mutatorSelectorElement?.DeleteSelectedMutator(), true)
                .AddButton(GetLocalizedString("button_accept"), () => OnMutatorAcceptClicked(_mutatorSelectorElement?.GetSelectedMutators()), true)
                .AddBackButton()
                .Build();
            _mutatorSelectorElement = _playMutatorSelector?.GetElementByID("MutatorSelectorElement") as GUICustomElement_MutatorSelector;
            if (_mutatorSelectorElement != null)
            {
                _mutatorSelectorElement.AcceptClicked += OnMutatorAcceptClicked;
                _mutatorSelectorElement.BackClicked += OnMutatorBackClicked;
            }

            _playPanelStoryMole = _menuManager.CreatePanel(GetLocalizedString("menu_play_story_mole"))
                .AddCustomElement(gameModeSelectorPrefab,
                    new GUICustomElement_GameModeSelector.InitArgs(
                        rabbitStoryGameModes.GetRange(0, GetMaxStoryProgress(PlayerType.Mole)),
                        showAiSlider: false,
                        isStoryMode: true))
                    .SetId("PlayPanelStoryMole")
                .AddButton(GetLocalizedString("button_play_story"), () => { }, true)
                .AddButton(_menuManager.GetBackButtonLocalized(), () =>
                {
                    RestoreDefaultMutators(rabbitStoryGameModes);
                    _menuManager.GoBack();
                }, true)
                .Build();
            BindMutatorButton(_playPanelStoryMole, "PlayPanelStoryMole");

            _playPanelStoryRabbit = _menuManager.CreatePanel(GetLocalizedString("menu_play_story_rabbit"), widePanelPrefab)
                .AddCustomElement(gameModeSelectorPrefab,
                    new GUICustomElement_GameModeSelector.InitArgs(
                        rabbitStoryGameModes.GetRange(0, GetMaxStoryProgress(PlayerType.Rabbit)),
                        showAiSlider: false,
                        isStoryMode: true))
                    .SetId("GameModeStoryRabbit")
                .AddButton(GetLocalizedString("button_play_story"), PlayStoryRabbit, true)
                .AddButton(_menuManager.GetBackButtonLocalized(), () =>
                {
                    RestoreDefaultMutators(rabbitStoryGameModes);
                    _menuManager.GoBack();
                }, true)
                .Build();
            BindMutatorButton(_playPanelStoryRabbit, "GameModeStoryRabbit");

            _playPanelStory = _menuManager.CreatePanel(GetLocalizedString("menu_play_story"), widePanelPrefab)
                .AddButton(GetLocalizedString("button_play_story_rabbit"), _playPanelStoryRabbit)
                .AddButton(GetLocalizedString("button_play_story_mole"), _playPanelStoryMole)
                .AddBackButton()
                .Build();

            _playPanelDuelLocalSolo = _menuManager.CreatePanel(GetLocalizedString("menu_play_duel_local_solo"), widePanelPrefab)
               .AddCustomElement(gameModeSelectorPrefab,
                    new GUICustomElement_GameModeSelector.InitArgs(
                        duelGameModes,
                        showAiSlider: true))
                    .SetId("GameModeDuelLocalSolo")
               .AddButton(GetLocalizedString("button_play_duel_as_rabbit"), () => PlayDuelSolo(PlayerType.Rabbit), true)
               .AddButton(GetLocalizedString("button_play_duel_as_mole"), () => PlayDuelSolo(PlayerType.Mole), true)
               .AddButton(_menuManager.GetBackButtonLocalized(), () =>
               {
                   RestoreDefaultMutators(duelGameModes);
                   _menuManager.GoBack();
               }, true)
               .Build();
            BindMutatorButton(_playPanelDuelLocalSolo, "GameModeDuelLocalSolo");

            _playPanelDuelLocalSplit = _menuManager.CreatePanel(GetLocalizedString("menu_play_duel_local_split"), widePanelPrefab)
                .AddCustomElement(gameModeSelectorPrefab,
                    new GUICustomElement_GameModeSelector.InitArgs(
                        duelGameModes,
                        showAiSlider: false))
                    .SetId("GameModelDuelLocalSplit")
                .AddButton(GetLocalizedString("button_play_duel"), PlayDuelSplitScreen, true)
                .AddButton(_menuManager.GetBackButtonLocalized(), () =>
                {
                    RestoreDefaultMutators(duelGameModes);
                    _menuManager.GoBack();
                }, true)
                .Build();
            BindMutatorButton(_playPanelDuelLocalSplit, "GameModelDuelLocalSplit");

            _playPanelDuelOnlineHostSetup = _menuManager.CreatePanel(GetLocalizedString("interface_online_host_setup_title"), widePanelPrefab)
                .AddCustomElement(gameModeSelectorPrefab,
                    new GUICustomElement_GameModeSelector.InitArgs(
                        duelGameModes,
                        showAiSlider: false))
                    .SetId("HostGameModeSelector")
                .AddButton(GetLocalizedString("interface_table_host"), () =>
                {
                    var selector = _playPanelDuelOnlineHostSetup.GetElementByID("HostGameModeSelector") as GUICustomElement_GameModeSelector;
                    var selected = selector != null ? selector.GetSelectedGameMode() : null;
                    if (selected == null) return;

                    bool hasMutators = selected.mutators != null && selected.mutators.Count > 0;
                    SteamLobbySession.Instance.CreateLobby(selected.name, hasMutators, maxMembers: 2);
                    _menuManager.ChangePanel(_playPanelDuelOnlineHostLobby);
                }, true)
                .AddButton(_menuManager.GetBackButtonLocalized(), () =>
                {
                    RestoreDefaultMutators(duelGameModes);
                    _menuManager.GoBack();
                }, true)
                .Build();
            BindMutatorButton(_playPanelDuelOnlineHostSetup, "HostGameModeSelector");

            _playPanelDuelOnlineHostLobby = _menuManager.CreatePanel(GetLocalizedString("interface_online_host_lobby_title"), widePanelPrefab)
                .AddCustomElement(gameModeSelectorPrefab).SetId("HostLobbySelector")
                .AddButton(GetLocalizedString("interface_lobby_swap_players"), () =>
                {
                    SteamLobbySession.Instance.SwapPlayers();
                }, true).SetId("HostSwapPlayersButton")
                .AddButton(GetLocalizedString("interface_lobby_start"), () => { }, true).SetId("HostStartButton")
                .AddButton(GetLocalizedString("interface_lobby_exit"), () =>
                {
                    SteamLobbySession.Instance.LeaveLobby();
                    // We are closing the lobby flow; Back from server list should go to Duel menu (not back into lobby).
                    _menuManager.PopHistoryUntil(_playPanelDuel);
                    _menuManager.ChangePanel(_playPanelDuelOnline, pushToHistory: false);
                }, true).SetId("HostExitLobbyButton")
                .Build();
            BindMutatorButton(_playPanelDuelOnlineHostLobby, "HostLobbySelector");

            _playPanelDuelOnlineJoinLobby = _menuManager.CreatePanel(GetLocalizedString("interface_online_join_lobby_title"), widePanelPrefab)
                .AddCustomElement(gameModeSelectorPrefab).SetId("ClientLobbySelector")
                .AddButton(GetLocalizedString("interface_lobby_ready"), () =>
                {
                    SteamLobbySession.Instance.SetReady(true);
                    // Keep the panel open; host will see Start enabled.
                }, true).SetId("ClientReadyButton")
                .AddButton(GetLocalizedString("interface_lobby_exit"), () =>
                {
                    SteamLobbySession.Instance.LeaveLobby();
                    _menuManager.PopHistoryUntil(_playPanelDuel);
                    _menuManager.ChangePanel(_playPanelDuelOnline, pushToHistory: false);
                }, true).SetId("ClientExitLobbyButton")
                .Build();
            BindMutatorButton(_playPanelDuelOnlineJoinLobby, "ClientLobbySelector");

            _playPanelDuelOnline = _menuManager.CreatePanel(GetLocalizedString("menu_play_duel_online"), widePanelPrefab)
                // Extra top margin so the panel header/banner doesn't overlap the table header.
                .AddSpacer(50f)
                .AddCustomElement(onlineSessionsTablePrefab,
                    new Interface.Element.GUITable.InitArgs(
                        rows: new List<Interface.Element.GUITable.RowData>()))
                    .SetId("OnlineSessionsTable")
                .AddButton(GetLocalizedString("interface_table_join"), () =>
                {
                    var table = _playPanelDuelOnline.GetElementByID("OnlineSessionsTable") as Interface.Element.GUITable;
                    if (table == null || !table.HasSelection) return;

                    if (ulong.TryParse(table.SelectedIp, out var lobbyId))
                    {
                        SteamLobbySession.Instance.JoinLobby(lobbyId);
                        _menuManager.ChangePanel(_playPanelDuelOnlineJoinLobby);
                    }
                }, true).SetId("OnlineJoinButton")
                .AddButton(GetLocalizedString("interface_table_host"), () =>
                {
                    _menuManager.ChangePanel(_playPanelDuelOnlineHostSetup);
                }, true).SetId("OnlineHostButton")
                .AddBackButton()
                .Build();

            // Wire selection => enable Join button (debug step - action will be added later)
            if (_playPanelDuelOnline != null)
            {
                var table = _playPanelDuelOnline.GetElementByID("OnlineSessionsTable") as Interface.Element.GUITable;
                var joinButton = _playPanelDuelOnline.GetElementByID("OnlineJoinButton") as Interface.Element.GUIButton;
                var hostButton = _playPanelDuelOnline.GetElementByID("OnlineHostButton") as Interface.Element.GUIButton;

                if (joinButton != null)
                {
                    joinButton.SetDisabled(true);
                }

                if (table != null && joinButton != null)
                {
                    // Start background lobby refresh while this panel is visible (component stops when table is disabled).
                    var lobbyBrowser = table.GetComponent<SteamLobbyBrowser>() ?? table.gameObject.AddComponent<SteamLobbyBrowser>();
                    lobbyBrowser.Bind(table);
                    table.OnRefreshRequested += lobbyBrowser.RefreshNow;

                    table.OnRowSelected += _ =>
                    {
                        if (joinButton != null)
                        {
                            joinButton.SetDisabled(false);
                        }
                    };
                }
            }

            // Lobby UI binders and button states
            {
                var hostSelector = _playPanelDuelOnlineHostLobby.GetElementByID("HostLobbySelector") as GUICustomElement_GameModeSelector;
                if (hostSelector != null)
                {
                    var binder = hostSelector.GetComponent<SteamLobbyRoomBinder>() ?? hostSelector.gameObject.AddComponent<SteamLobbyRoomBinder>();
                    binder.Configure(hostSelector, isHostView: true, availableGameModes: duelGameModes);
                }

                var clientSelector = _playPanelDuelOnlineJoinLobby.GetElementByID("ClientLobbySelector") as GUICustomElement_GameModeSelector;
                if (clientSelector != null)
                {
                    var binder = clientSelector.GetComponent<SteamLobbyRoomBinder>() ?? clientSelector.gameObject.AddComponent<SteamLobbyRoomBinder>();
                    binder.Configure(clientSelector, isHostView: false, availableGameModes: duelGameModes);
                }

                var startBtn = _playPanelDuelOnlineHostLobby.GetElementByID("HostStartButton") as Interface.Element.GUIButton;
                if (startBtn != null)
                {
                    startBtn.SetDisabled(true);
                    void UpdateStart()
                    {
                        bool canStart = SteamLobbySession.Instance.CanHostStart();
                        startBtn.SetDisabled(!canStart);
                    }
                    SteamLobbySession.Instance.OnMembersChanged += UpdateStart;
                    SteamLobbySession.Instance.OnStateChanged += UpdateStart;
                    SteamLobbySession.Instance.OnLobbyDataChanged += UpdateStart;
                    UpdateStart();

                    // IMPORTANT: Start must go through GameManager.PlayGame (scene load + GameInspector), then
                    // OnlineAgentController(s) will be created in GameManager.CreateAgentController.
                    startBtn.SetOnClick(() =>
                    {
                        if (!TryBuildHostOnlinePlaySettings(out var pgs, out var onlinePlayerType))
                            return;

                        // Send start request to client via lobby data so it can also load the same session.
                        var day = System.DayOfWeek.Monday.SelectRandom();
                        int startNonce = SteamLobbySession.Instance.HostRequestStart(
                            mapScene: (int)GameSceneManager.SceneType.GamePlay_Duel,
                            dayOfWeek: (int)day);

                        GameSystems.Steam.Scripts.SteamOnlineStartCoordinator.Instance.ResetForNewSession();
                        GameSystems.Steam.Scripts.SteamOnlineStartCoordinator.Instance.SetLocalStartNonce(startNonce);

                        // Ensure host uses the same map/day stored in lobby data.
                        pgs.day = day;
                        pgs.map = GameSceneManager.SceneType.GamePlay_Duel;

                        GameManager.PlayGame(
                            gameMode: pgs.gameMode,
                            map: pgs.map,
                            day: pgs.day,
                            playerTypeForStory: pgs.playerTypeForStory,
                            rabbitControlAgent: pgs.GetPlayerControlAgent(PlayerType.Rabbit),
                            moleControlAgent: pgs.GetPlayerControlAgent(PlayerType.Mole),
                            aiIntelligence: pgs.aiIntelligence,
                            onlineConfig: pgs.onlineConfig);
                    });
                }

                var readyBtn = _playPanelDuelOnlineJoinLobby.GetElementByID("ClientReadyButton") as Interface.Element.GUIButton;
                if (readyBtn != null)
                {
                    void UpdateClientReadyButton()
                    {
                        var session = SteamLobbySession.Instance;
                        if (!session.IsInLobby || session.IsHost)
                        {
                            readyBtn.SetDisabled(true);
                            return;
                        }

                        // If roles changed since last Ready, Ready must be enabled again.
                        bool rolesKnown = session.TryGetRolesNonce(out int rolesNonce) && rolesNonce > 0;
#if !DISABLESTEAMWORKS
                        // Read local member data directly (only local can set it).
                        try
                        {
                            var lobby = new Steamworks.CSteamID(session.CurrentLobbyId);
                            var local = Steamworks.SteamUser.GetSteamID();
                            var v = Steamworks.SteamMatchmaking.GetLobbyMemberData(lobby, local, SteamLobbySession.MemberData_ReadyRolesNonce) ?? "0";
                            int.TryParse(v, out int readyNonce);
                            bool isReadyForRoles = rolesKnown && readyNonce == rolesNonce;
                            readyBtn.SetDisabled(isReadyForRoles);
                        }
                        catch
                        {
                            readyBtn.SetDisabled(false);
                        }
#else
                        readyBtn.SetDisabled(false);
#endif
                    }

                    // Disable after clicking once (no unready yet). Ready notifies host and stays in lobby panel.
                    readyBtn.SetOnClick(() =>
                    {
                        SteamLobbySession.Instance.SetReady(true);
                        UpdateClientReadyButton();
                    });

                    SteamLobbySession.Instance.OnLobbyDataChanged += UpdateClientReadyButton; // role swap increments roles nonce
                    SteamLobbySession.Instance.OnStateChanged += UpdateClientReadyButton;
                    SteamLobbySession.Instance.OnMembersChanged += UpdateClientReadyButton;
                    UpdateClientReadyButton();
                }
            }

            // Client-side: when host publishes start_nonce, auto-start loading the gameplay scene.
            // (Client is in lobby but currently looking at server list after pressing Ready.)
            {
                int lastSeenStartNonce = 0;
                void TryClientStartFromLobby()
                {
                    var session = SteamLobbySession.Instance;
                    if (!session.IsInLobby || session.IsHost)
                        return;

                    if (!session.TryGetStartNonce(out int startNonce) || startNonce <= 0)
                        return;

                    if (startNonce == lastSeenStartNonce)
                        return;

                    // Only react to host request, not to begin_nonce updates.
                    if (session.TryGetBeginNonce(out int beginNonce) && beginNonce == startNonce)
                        return;

                    lastSeenStartNonce = startNonce;

                    if (!TryBuildClientOnlinePlaySettings(out var clientPgs))
                        return;

                    GameSystems.Steam.Scripts.SteamOnlineStartCoordinator.Instance.ResetForNewSession();
                    GameSystems.Steam.Scripts.SteamOnlineStartCoordinator.Instance.SetLocalStartNonce(startNonce);

                    GameManager.PlayGame(
                        gameMode: clientPgs.gameMode,
                        map: clientPgs.map,
                        day: clientPgs.day,
                        playerTypeForStory: clientPgs.playerTypeForStory,
                        rabbitControlAgent: clientPgs.GetPlayerControlAgent(PlayerType.Rabbit),
                        moleControlAgent: clientPgs.GetPlayerControlAgent(PlayerType.Mole),
                        aiIntelligence: clientPgs.aiIntelligence,
                        onlineConfig: clientPgs.onlineConfig);
                }

                SteamLobbySession.Instance.OnLobbyDataChanged += TryClientStartFromLobby;
                SteamLobbySession.Instance.OnStateChanged += TryClientStartFromLobby;
            }

            _playPanelDuel = _menuManager.CreatePanel(GetLocalizedString("menu_play_duel"))
                .AddButton(GetLocalizedString("button_play_duel_local_solo"), _playPanelDuelLocalSolo)
                .AddButton(GetLocalizedString("button_play_duel_local_split"), _playPanelDuelLocalSplit)
                .AddButton(GetLocalizedString("button_play_duel_online"), _playPanelDuelOnline)
                .AddBackButton()
                .Build();

            _playPanelChalleange = _menuManager.CreatePanel(GetLocalizedString("menu_play_challenge"))
                //TODO: Add challenge mode levels selection here
                .AddBackButton()
                .Build();

            _playPanel = _menuManager.CreatePanel(GetLocalizedString("menu_play"))
                .AddButton(GetLocalizedString("button_play_story"), moleStoryGameModes.Count == 0 ? _playPanelStoryRabbit : _playPanelStory)
                .AddButton(GetLocalizedString("button_play_challenge"), _playPanelChalleange, !IsStoryComplite,
                    !IsStoryComplite
                    ? GetLocalizedString("tooltip_challenge_disabled")
                    : new LocalizedString())
                .AddButton(GetLocalizedString("button_play_duel"), _playPanelDuel)
                .AddBackButton()
                .Build();

            _creditsPanel = _menuManager.CreatePanel(GetLocalizedString("menu_credits"))
                .AddBackButton()
                .AddAutoScroll(.1f, 2f)
                .Build();
            UpdateCredits();

            _panelOptionsGraphic = _menuManager.CreatePanel(GetLocalizedString("menu_options_graphics"))
                .AddDropDown(GetLocalizedString("option_resolution"), MainMenuDefaultLogic.HandleResolutionChange, MainMenuDefaultLogic.GetAvailableResolutions(), MainMenuDefaultLogic.GetCurrentResolutionIndex)
                .AddToggle(GetLocalizedString("option_fullscreen"), MainMenuDefaultLogic.HandleFullScreen, MainMenuDefaultLogic.GetFullScreenCurrentMode)
                .AddToggle(GetLocalizedString("option_vsync"), MainMenuDefaultLogic.HandleVSync, MainMenuDefaultLogic.GetVSync)
                .AddSlider(GetLocalizedString("option_target_fps"), MainMenuDefaultLogic.HandleTargetFPSChange, MainMenuDefaultLogic.GetTargetFPS, MainMenuDefaultLogic.FormatTargetFPS)
                .AddDropDown(GetLocalizedString("option_quality"), MainMenuDefaultLogic.HandleQualityChange, MainMenuDefaultLogic.GetAvailableQualitySettings(), MainMenuDefaultLogic.GetCurrentQualityIndex)
                .AddBackButton()
                .Build();

            _panelOptionsAudio = _menuManager.CreatePanel(GetLocalizedString("menu_options_audio"))
                .AddSlider(GetLocalizedString("option_master_volume"), MainMenuDefaultLogic.HandleMasterVolumeChange, MainMenuDefaultLogic.GetMasterVolume)
                .AddSlider(GetLocalizedString("option_sfx_volume"), MainMenuDefaultLogic.HandleSFXVolume, MainMenuDefaultLogic.GetSFXVolume)
                .AddSlider(GetLocalizedString("option_music_volume"), MainMenuDefaultLogic.HandleMusicVolumeChange, MainMenuDefaultLogic.GetMusicVolume)
                .AddSlider(GetLocalizedString("option_dialogue_volume"), MainMenuDefaultLogic.HandleDialogueVolumeChange, MainMenuDefaultLogic.GetDialogueVolume)
                .AddSlider(GetLocalizedString("option_ambient_volume"), MainMenuDefaultLogic.HandleAmbientVolumeChange, MainMenuDefaultLogic.GetAmbientVolume)
                .AddBackButton()
                .Build();

            _panelOptionsControlsKeyboardPrimary = _menuManager.CreatePanel(GetLocalizedString("menu_options_controls_keyboard_primary"))
                .AddKeyBindControls(inputActions, playerActionMap, playerPrimaryBindingGroup)
                .AddBackButton()
                .Build();

            _panelOptionsControlsKeyboardSecondary = _menuManager.CreatePanel(GetLocalizedString("menu_options_controls_keyboard_secondary"))
                .AddKeyBindControls(inputActions, playerActionMap, playerSecondaryBindingGroup)
                .AddBackButton()
                .Build();

            _panelOptionsControlsGamepad = _menuManager.CreatePanel(GetLocalizedString("menu_options_controls_gamepad"))
                .AddKeyBindControls(inputActions, playerActionMap, playerGamepadBindingGroup)
                .AddBackButton()
                .Build();

            _panelOptionsControls = _menuManager.CreatePanel(GetLocalizedString("menu_options_controls"))
                .AddButton(GetLocalizedString("button_controls_keyboard_primary"), _panelOptionsControlsKeyboardPrimary)
                .AddButton(GetLocalizedString("button_controls_keyboard_secondary"), _panelOptionsControlsKeyboardSecondary)
                .AddButton(GetLocalizedString("button_controls_gamepad"), _panelOptionsControlsGamepad)
                .AddBackButton()
                .Build();

            _panelOptionsGeneral = _menuManager.CreatePanel(GetLocalizedString("menu_general_options"))
                .AddDropDown(GetLocalizedString("option_language"), MainMenuDefaultLogic.HandleLanguageChange, MainMenuDefaultLogic.GetAvailableLanguages(), MainMenuDefaultLogic.GetCurrentLanguageIndex)
                .AddBackButton()
                .Build();

            _panelOptions = _menuManager.CreatePanel(GetLocalizedString("menu_options"))
                .AddButton(GetLocalizedString("button_general"), _panelOptionsGeneral)
                .AddButton(GetLocalizedString("button_graphics"), _panelOptionsGraphic)
                .AddButton(GetLocalizedString("button_audio"), _panelOptionsAudio)
                .AddButton(GetLocalizedString("button_controls"), _panelOptionsControls)
                .AddBackButton()
                .Build();

            _mainMenu = _menuManager.CreatePanel(GetLocalizedString("menu_main"))
                .AddButton(GetLocalizedString("button_play"), _playPanel)
                .AddButton(GetLocalizedString("button_options"), _panelOptions)
                .AddButton(GetLocalizedString("button_credits"), _creditsPanel)
                .AddExitButton()
                .Build();

        }

        private void UpdateCredits()
        {
            // Remove old labels
            foreach (var label in _creditLabels)
            {
                if (label != null)
                {
                    _creditsPanel.RemoveElement(label);
                    Destroy(label.gameObject);
                }
            }
            _creditLabels.Clear();

            // Add new labels
            foreach (var line in PrepareCredits())
            {
                var label = _menuManager.CreateLabel(line);
                _creditsPanel.AddElement(label);
                _creditLabels.Add(label as Interface.Element.GUILabel);
            }
        }

        private void OnLanguageChanged(Locale locale)
        {
            UpdateCredits();
        }

        private void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
#endif

            if (_mutatorSelectorElement != null)
            {
                _mutatorSelectorElement.AcceptClicked -= OnMutatorAcceptClicked;
                _mutatorSelectorElement.BackClicked -= OnMutatorBackClicked;
            }

            foreach (var selector in _mutatorAwareSelectors)
            {
                if (selector != null)
                {
                    selector.MutatorConfigRequested -= OnMutatorConfigRequested;
                }
            }
            _mutatorAwareSelectors.Clear();
        }

        private List<string> PrepareCredits()
        {
            var localizedString = GetLocalizedString("credits_content");
            var op = localizedString.GetLocalizedStringAsync();

            if (op.IsDone && op.Result != null)
            {
                return new List<string>(op.Result.Split('\n'));
            }
            else
            {
                // If not done, wait for completion
                string result = op.WaitForCompletion();

                if (result != null)
                {
                    return new List<string>(result.Split('\n'));
                }

                // Fallback - return empty list and update when ready
                op.Completed += (operation) =>
                {
                    if (operation.IsDone && operation.Result != null && _creditsPanel != null)
                    {
                        UpdateCredits();
                    }
                };

                return new List<string>();
            }
        }
    }
}