// Networking controller for online play (Steam P2P transport).
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using GameSystems;
using System.IO;
using System.Linq;
using PlayerManagementSystem;
using RabbitVsMole.Events;
using RabbitVsMole.InteractableGameObject.Base;
using RabbitVsMole.InteractableGameObject.Enums;
using RabbitVsMole.InteractableGameObject.Field;
using RabbitVsMole.Online;
using PlayerManagementSystem.Backpack;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static RabbitVsMole.GameManager;
using RabbitVsMole.InteractableGameObject.Field.Base;

#if !DISABLESTEAMWORKS
using Steamworks;
using System.Collections;
#endif

namespace RabbitVsMole
{
    public class OnlineAgentController : HumanAgentControllerBase<PlayerType, PlayerAvatar>
    {
        [SerializeField] private FarmFieldTileHighlighter farmFieldTileHighlighter;
        [SerializeField] private Image _blackMask;
        
        private PlayerType _playerType;
        private bool _initialized;
        private bool _isHost;
        private PlayGameSettings.OnlineConfig _onlineConfig;
        private bool _isLocalView;
        private ulong _remoteSteamId;
        private ulong _lobbyId;
        private CinemachineCamera _cinemachineCamera;
        private IOnlineAgentTransport _transport;
        private float _lastPositionSyncTime;
        private const float PositionSyncInterval = 0.2f;

        private enum InteractionCommand
        {
            ActionFront,
            ActionDown,
            ActionSpecial
        }

        private readonly struct InteractionRequest
        {
            public readonly InteractionCommand Command;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;

            public InteractionRequest(InteractionCommand command, Vector3 position, Quaternion rotation)
            {
                Command = command;
                Position = position;
                Rotation = rotation;
            }
        }

        private readonly struct InventorySnapshot
        {
            public readonly int Seed;
            public readonly int Water;
            public readonly int Dirt;
            public readonly int Health;
            public readonly int Carrot;

            public InventorySnapshot(int seed, int water, int dirt, int health, int carrot)
            {
                Seed = seed;
                Water = water;
                Dirt = dirt;
                Health = health;
                Carrot = carrot;
            }
        }

        public static void CreateInstance(PlayGameSettings playGameSettings, PlayerType playerType)
        {
            var prefab = _agentPrefabs.GetPrefab(PlayerControlAgent.Online);
            if (prefab == null)
            {
                DebugHelper.LogError(null, "Failed to load OnlineAgentController prefab (Addressables returned null)");
                return;
            }

            var go = Instantiate(prefab);
            var instance = go.GetComponent<OnlineAgentController>();

            if (instance == null)
            {
                DebugHelper.LogError(null, "Failed to instantiate OnlineAgentController prefab (component missing on prefab)");
                Destroy(go);
                return;
            }

            instance._playerType = playerType;
            instance._onlineConfig = playGameSettings.onlineConfig;
            instance._isHost = instance._onlineConfig.IsHost;
            instance._remoteSteamId = instance._onlineConfig.RemoteSteamId;
            instance._lobbyId = instance._onlineConfig.LobbyId;
            instance._initialized = true;
            // Host has a HumanAgentController for the local player; OnlineAgentController represents the remote player on host.
            instance._isLocalView = !instance._isHost;

            if (instance._onlineConfig.IsOnline)
                OnlineAuthority.Configure(instance._isHost);
            else
                OnlineAuthority.Disable();

            instance.SubscribeEvents();
            instance.SetupInputControl();

            if (!instance.Initialize(playerType, playGameSettings.IsGamepadUsing(playerType)))
            {
                DebugHelper.LogError(instance, "Failed to initialize OnlineAgentController");
                Destroy(instance.gameObject);
                return;
            }

            if (!instance.InitializeCamera())
            {
                DebugHelper.LogError(instance, "Failed to initialize camera");
                Destroy(instance.gameObject);
                return;
            }

            instance.SetupTransport();
        }

        private void SetupTransport()
        {
            if (_onlineConfig.IsOnline)
            {
#if !DISABLESTEAMWORKS
                _transport = new SteamOnlineAgentTransport(_remoteSteamId);
#else
                Debug.LogWarning("Steam transport disabled, using null transport");
                _transport = new NullOnlineAgentTransport();
#endif
            }
            else
            {
                _transport = new NullOnlineAgentTransport();
            }

            _transport.OnMoveReceived += HandleRemoteMove;
            _transport.OnInteractionReceived += HandleRemoteInteraction;
            _transport.OnInteractionResult += HandleInteractionResult;
            _transport.OnAvatarPositionReceived += HandleRemotePosition;
            _transport.OnFieldStateChangeReceived += HandleRemoteFieldStateChange;
            _transport.OnInventorySyncReceived += HandleInventorySync;
            _transport.Start(_isHost);
            if (_isHost)
                OnlineAuthority.RegisterFieldStateSender(_transport.SendFieldStateChange);
        }

        private void SetupInputControl()
        {
            SetDefaultActionMapName("Player");
            SetGamepadSchemaName("Gamepad");
            SetupInputActions();

            if (GameManager.CurrentGameInspector.IsSplitScreen)
                SetKeyboardSchemaName($"KeyboardP{(_playerType == PlayerType.Rabbit ? "1" : "2")}");
            else
                SetKeyboardSchemaName("KeyboardP1");
        }

        private void SetupInputActions()
        {
            SetInputActionNames(new List<string>
            {
                "Move",
                "Action_front",
                "Action_down",
                "Action_special"
            });
        }

        private void Start()
        {
            if (!_initialized)
                return;

            if (_isLocalView)
            {
                Instantiate(farmFieldTileHighlighter).Setup(_playerType);
                ChangeView(_playerAvatar.IsOnSurface);
            }
        }

        private void Update()
        {
            _transport?.Tick();

            if (_isHost && Time.time - _lastPositionSyncTime >= PositionSyncInterval)
            {
                _lastPositionSyncTime = Time.time;

                // Host authoritative: send both player positions so client can render both avatars correctly.
                var rabbit = PlayerAvatar.RabbitStaticInstance;
                if (rabbit != null)
                    _transport?.SendAvatarPosition(PlayerType.Rabbit, rabbit.transform.position, rabbit.transform.rotation);

                var mole = PlayerAvatar.MoleStaticInstance;
                if (mole != null)
                    _transport?.SendAvatarPosition(PlayerType.Mole, mole.transform.position, mole.transform.rotation);
            }

            // Safety: ensure local camera keeps following the avatar (covers prefab/reference losses).
            if (_isLocalView && _cinemachineCamera != null && _playerAvatar != null)
            {
                if (_cinemachineCamera.Follow != _playerAvatar.transform)
                    _cinemachineCamera.Follow = _playerAvatar.transform;
            }
        }

        public void OnMove(InputValue value)
        {
            if (_isHost)
                return;

            var moveInput = value.Get<Vector2>();
            _playerAvatar?.SetMoveInput(moveInput);
            _transport?.SendMove(moveInput);
        }

        private void HandleRemoteMove(Vector2 moveInput)
        {
            _playerAvatar?.SetMoveInput(moveInput);
        }

        public void OnAction_front()
        {
            if (_isHost)
                return;

            SendInteraction(InteractionCommand.ActionFront);
        }

        public void OnAction_down()
        {
            if (_isHost)
                return;

            SendInteraction(InteractionCommand.ActionDown);
        }

        public void OnAction_special()
        {
            if (_isHost)
                return;

            SendInteraction(InteractionCommand.ActionSpecial);
        }

        private void SendInteraction(InteractionCommand command)
        {
            var pos = _playerAvatar != null ? _playerAvatar.transform.position : transform.position;
            var rot = _playerAvatar != null ? _playerAvatar.transform.rotation : transform.rotation;
            _transport?.SendInteractionRequest(new InteractionRequest(command, pos, rot));
        }

        private void HandleRemoteInteraction(InteractionRequest request)
        {
            if (!_isHost)
                return;

            if (_playerAvatar != null)
            {
                // Apply client intent pose before executing interaction to reduce mismatch.
                _playerAvatar.transform.SetPositionAndRotation(request.Position, request.Rotation);
            }

            var canPerform = ExecuteInteraction(request.Command);
            var performedActionType = _playerAvatar != null ? _playerAvatar.LastRequestedActionType : ActionType.None;
            _transport?.SendInteractionResult(request.Command, canPerform, performedActionType);

            if (canPerform)
            {
                SendInventorySnapshotFor(_playerType);
            }
        }

        private void HandleInteractionResult(InteractionCommand command, bool canPerform, ActionType actionType)
        {
            if (_isHost || !canPerform)
                return;

            // Client is NOT authoritative: do not execute interaction locally (prevents double field changes and visuals).
            // But it should still play animation + SFX to match the host.
            _playerAvatar?.FakeAction(actionType, _playerAvatar.transform.position);
        }

        private void SendInventorySnapshotFor(PlayerType playerType)
        {
            if (!_isHost || _transport == null)
                return;

            var avatar = playerType == PlayerType.Rabbit ? PlayerAvatar.RabbitStaticInstance : PlayerAvatar.MoleStaticInstance;
            var bp = avatar != null ? avatar.Backpack : null;
            if (bp == null)
                return;

            int seed = bp.Seed != null ? bp.Seed.Count : 0;
            int water = bp.Water != null ? bp.Water.Count : 0;
            int dirt = bp.Dirt != null ? bp.Dirt.Count : 0;
            int health = bp.Health != null ? bp.Health.Count : 0;
            int carrot = bp.Carrot != null ? bp.Carrot.Count : 0;

            _transport.SendInventorySnapshot(playerType, new InventorySnapshot(seed, water, dirt, health, carrot));
        }

        private void HandleInventorySync(PlayerType playerType, InventorySnapshot snapshot)
        {
            // Apply authoritative counts on client.
            if (_isHost)
                return;

            var avatar = playerType == PlayerType.Rabbit ? PlayerAvatar.RabbitStaticInstance : PlayerAvatar.MoleStaticInstance;
            var bp = avatar != null ? avatar.Backpack : null;
            if (bp == null)
                return;

            bp.Carrot?.SetCountFromNetwork(snapshot.Carrot);
            bp.Seed?.SetCountFromNetwork(snapshot.Seed);
            bp.Water?.SetCountFromNetwork(snapshot.Water);
            bp.Dirt?.SetCountFromNetwork(snapshot.Dirt);
            bp.Health?.SetCountFromNetwork(snapshot.Health);
        }

        private bool ExecuteInteraction(InteractionCommand command) =>
            command switch
            {
                InteractionCommand.ActionFront => _playerAvatar?.TryActionFront() ?? false,
                InteractionCommand.ActionDown => _playerAvatar?.TryActionDown() ?? false,
                InteractionCommand.ActionSpecial => _playerAvatar?.TryActionSpecial() ?? false,
                _ => false
            };

        private void HandleRemotePosition(PlayerType playerType, Vector3 position, Quaternion rotation)
        {
            var avatar = playerType == PlayerType.Rabbit ? PlayerAvatar.RabbitStaticInstance : PlayerAvatar.MoleStaticInstance;
            if (avatar == null)
                return;

            var delta = position - avatar.transform.position;

            // Local avatar: trust host correction directly.
            if (playerType == _playerType && _playerAvatar == avatar)
            {
                avatar.transform.SetPositionAndRotation(position, rotation);
            }
            else
            {
                // Remote avatar proxy: smooth movement for animation, hard-warp if too far.
                var planar = new Vector2(delta.x, delta.z);
                if (planar.magnitude > 2.0f)
                {
                    avatar.transform.position = position;
                    avatar.SetMoveInput(Vector2.zero);
                }
                else if (planar.magnitude > 0.02f)
                {
                    avatar.SetMoveInput(planar.normalized);
                }
                else
                {
                    avatar.SetMoveInput(Vector2.zero);
                }

                avatar.transform.rotation = Quaternion.Slerp(avatar.transform.rotation, rotation, 0.35f);
            }

            // Only adjust local camera follow/view when this position is for the locally controlled avatar.
            if (playerType == _playerType && _playerAvatar == avatar)
            {
                ChangeView(_playerAvatar.IsOnSurface);

                if (_cinemachineCamera != null)
                {
                    CinemachineCore.OnTargetObjectWarped(_playerAvatar.transform, delta);
                }
            }
        }

        private void HandleRemoteFieldStateChange(FieldStateMessage message)
        {
            if (!_onlineConfig.IsOnline || message == null)
                return;

            FieldBase field = null;
            if (message.IsUnderground.HasValue && message.GridX.HasValue && message.GridY.HasValue)
            {
                if (message.IsUnderground.Value)
                    field = UndergroundManager.GetUndergroundField(message.GridX.Value, message.GridY.Value);
                else
                    field = FarmManager.GetFarmField(message.GridX.Value, message.GridY.Value);
            }

            field ??= FindFieldByPosition(message.Position, desiredUnderground: message.IsUnderground);
            if (field == null)
            {
                Debug.LogWarning($"Online sync: field not found near position {message.Position}");
                return;
            }

            var newState = CreateFieldStateByName(field, message.StateName);
            if (newState == null)
            {
                Debug.LogWarning($"Online sync: unknown field state '{message.StateName}'");
                return;
            }

            // Avoid re-applying identical state (prevents re-running OnDestroy/OnStart spam like carrot/mound duplication).
            var currentStateName = field.State?.GetType().Name;
            var isSameState = string.Equals(currentStateName, message.StateName, StringComparison.Ordinal);
            if (!isSameState)
            {
                OnlineAuthority.AuthorizeRemoteFieldChange(field);
                field.SetNewState(newState);
            }

            if (field is FarmFieldBase farmField)
            {
                if (message.Water.HasValue)
                    farmField.SetWaterLevelFromNetwork(message.Water.Value);

                if (message.CarrotProgress.HasValue)
                    farmField.SetCarrotProgressFromNetwork(message.CarrotProgress.Value);
            }
        }

        private FieldBase FindFieldByPosition(Vector3 position, bool? desiredUnderground = null, float maxDistance = 3.5f)
        {
            var fields = FindObjectsByType<FieldBase>(FindObjectsSortMode.None);
            FieldBase closest = null;
            float bestSqr = maxDistance * maxDistance;

            foreach (var field in fields)
            {
                if (desiredUnderground.HasValue)
                {
                    bool isUnderground = field is UndergroundFieldBase;
                    if (isUnderground != desiredUnderground.Value)
                        continue;
                }

                float dist = (field.transform.position - position).sqrMagnitude;
                if (dist < bestSqr)
                {
                    bestSqr = dist;
                    closest = field;
                }
            }

            return closest;
        }

        private FieldState CreateFieldStateByName(FieldBase field, string stateName) =>
            stateName switch
            {
                nameof(FarmFieldClean) => field.CreateFarmCleanState(),
                nameof(FarmFieldPlanted) => field.CreateFarmPlantedState(),
                nameof(FarmFieldMounded) => field.CreateFarmMoundedState(),
                nameof(FarmFieldRooted) => field.CreateFarmRootedState(),
                nameof(FarmFieldWithCarrot) => field.CreateFarmWithCarrotState(),
                nameof(UndergroundFieldWall) => field.CreateUndergroundWallState(),
                nameof(UndergroundFieldMounded) => field.CreateUndergroundMoundedState(),
                nameof(UndergroundFieldCarrot) => field.CreateUndergroundCarrotState(),
                nameof(UndergroundFieldClean) => field.CreateUndergroundCleanState(),
                _ => null
            };

        private bool InitializeCamera()
        {
            if (!_isLocalView)
            {
                // Disable camera/UI objects on host-side remote controller (avoid camera stealing & duplicate UI).
                foreach (var vcam in GetComponentsInChildren<CinemachineCamera>(true))
                    vcam.enabled = false;

                foreach (var cam in GetComponentsInChildren<Camera>(true))
                    cam.enabled = false;

                foreach (var brain in GetComponentsInChildren<CinemachineBrain>(true))
                    brain.enabled = false;

                foreach (var canvas in GetComponentsInChildren<Canvas>(true))
                    canvas.enabled = false;

                // Remote view: ensure no audio listeners here to avoid mixing remote sounds.
                foreach (var listener in GetComponentsInChildren<AudioListener>(true))
                    listener.enabled = false;

                return true;
            }

            var cinemachineCamera = GetComponentInChildren<CinemachineCamera>();
            if (cinemachineCamera == null)
            {
                Debug.LogError("CinemachineCamera not found");
                return false;
            }
            _cinemachineCamera = cinemachineCamera;
            _cinemachineCamera.Follow = _playerAvatar.transform;
            _cinemachineCamera.OutputChannel = Getchannel(_playerType);

            var camera = GetComponentInChildren<Camera>();
            if (camera == null)
            {
                Debug.LogError("Camera not found");
                return false;
            }

            int layerIndex = LayerMask.NameToLayer($"FrameFor{_playerType}");
            if (layerIndex != -1)
            {
                camera.cullingMask |= (1 << layerIndex);
            }

            camera.rect = (GameManager.CurrentGameInspector.IsSplitScreen, _playerType == PlayerType.Rabbit) switch
            {
                (false, _) => new Rect(0.0f, 0.0f, 1f, 1f),
                (true, true) => new Rect(0.0f, 0.0f, 0.5f, 1f),
                (true, false) => new Rect(0.5f, 0.0f, 0.5f, 1f)
            };

            var cinemachineBrain = GetComponentInChildren<CinemachineBrain>();
            if (cinemachineBrain == null)
            {
                Debug.LogError("CinemachineBrain not found");
                return false;
            }
            cinemachineBrain.ChannelMask = Getchannel(_playerType);

            // Enforce single active listener on local view: keep the one on this camera, disable others under this controller.
            var listeners = GetComponentsInChildren<AudioListener>(true);
            AudioListener mainListener = null;
            foreach (var listener in listeners)
            {
                if (listener.gameObject == camera.gameObject && mainListener == null)
                {
                    mainListener = listener;
                    listener.enabled = true;
                }
                else
                {
                    listener.enabled = false;
                }
            }
            if (mainListener == null)
            {
                mainListener = camera.gameObject.AddComponent<AudioListener>();
            }

            return true;
        }

        private OutputChannels Getchannel(PlayerType playerType) =>
            playerType == PlayerType.Rabbit ? OutputChannels.Channel01 : OutputChannels.Channel02;

        private void ChangeView(bool isOnSurface)
        {
            if (_cinemachineCamera == null)
                return;
            if (!_cinemachineCamera.TryGetComponent<CinemachineFollow>(out var followComponent))
                return;

            if (isOnSurface)
                followComponent.FollowOffset = new Vector3(0, 5, -5);
            else
                followComponent.FollowOffset = new Vector3(0, 8, -3);
        }

        private void OnEnable()
        {
            if (_initialized)
                SubscribeEvents();
        }

        private void OnDisable()
        {
            if (_initialized)
                UnsubscribeEvents();
        }

        private void OnDestroy()
        {
            if (_isHost)
                OnlineAuthority.RegisterFieldStateSender(null);
            _transport?.Dispose();
        }

        private void SubscribeEvents()
        {
            if (_playerType == PlayerType.Mole)
                EventBus.Subscribe<TravelEvent>(MoleTravel);
        }

        private void UnsubscribeEvents()
        {
            if (_playerType == PlayerType.Mole)
                EventBus.Unsubscribe<TravelEvent>(MoleTravel);
        }

        private void MoleTravel(TravelEvent moleTravelEvent) =>
            StartCoroutine(MoleTravelInternal(moleTravelEvent));

        private IEnumerator MoleTravelInternal(TravelEvent moleTravelEvent)
        {
            _blackMask.gameObject.SetActive(true);

            yield return StartCoroutine(FadeMask(new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 1f), GameManager.CurrentGameStats.TimeActionEnterMound));

            yield return null;

            Vector3 delta = moleTravelEvent.NewLocation - _playerAvatar.transform.position;

            _playerAvatar.transform.position = moleTravelEvent.NewLocation;

            ChangeView(_playerAvatar.IsOnSurface);
            CinemachineCore.OnTargetObjectWarped(_playerAvatar.transform, delta);

            yield return null;
            _playerAvatar.PerformAction(moleTravelEvent.actionTypeAfterTravel);

            yield return StartCoroutine(FadeMask(new Color(0f, 0f, 0f, 1f), new Color(0f, 0f, 0f, 0f), GameManager.CurrentGameStats.TimeActionExitMound));

            _blackMask.gameObject.SetActive(false);
            _playerAvatar.SetupNewTerrain();
        }

        private IEnumerator FadeMask(Color fromColor, Color toColor, float duration)
        {
            float elapsedTime = 0;
            _blackMask.color = fromColor;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsedTime / duration);
                _blackMask.color = Color.Lerp(fromColor, toColor, progress);
                yield return null;
            }
            _blackMask.color = toColor;
        }

        #region Transport contracts

        private sealed class FieldStateMessage
        {
            public Vector3 Position { get; set; }
            public string StateName { get; set; }
            public bool? IsUnderground { get; set; }
            public int? GridX { get; set; }
            public int? GridY { get; set; }
            public float? Water { get; set; }
            public float? CarrotProgress { get; set; }
        }

        private interface IOnlineAgentTransport : IDisposable
        {
            event Action<Vector2> OnMoveReceived;
            event Action<InteractionRequest> OnInteractionReceived;
            event Action<InteractionCommand, bool, ActionType> OnInteractionResult;
            event Action<PlayerType, Vector3, Quaternion> OnAvatarPositionReceived;
            event Action<FieldStateMessage> OnFieldStateChangeReceived;
            event Action<PlayerType, InventorySnapshot> OnInventorySyncReceived;

            void Start(bool isHost);
            void Tick();
            void SendMove(Vector2 move);
            void SendInteractionRequest(InteractionRequest request);
            void SendInteractionResult(InteractionCommand command, bool canPerform, ActionType actionType);
            void SendAvatarPosition(PlayerType playerType, Vector3 position, Quaternion rotation);
            void SendFieldStateChange(FieldBase field, FieldState newState);
            void SendInventorySnapshot(PlayerType playerType, InventorySnapshot snapshot);
        }

        /// <summary>
        /// Placeholder transport until Steam networking is integrated.
        /// </summary>
        private sealed class NullOnlineAgentTransport : IOnlineAgentTransport
        {
#pragma warning disable CS0067 // Event is never used (Null transport never emits events; required by interface)
            public event Action<Vector2> OnMoveReceived;
            public event Action<InteractionRequest> OnInteractionReceived;
            public event Action<InteractionCommand, bool, ActionType> OnInteractionResult;
            public event Action<PlayerType, Vector3, Quaternion> OnAvatarPositionReceived;
            public event Action<FieldStateMessage> OnFieldStateChangeReceived;
            public event Action<PlayerType, InventorySnapshot> OnInventorySyncReceived;
#pragma warning restore CS0067

            public void Dispose()
            {
            }

            public void SendAvatarPosition(PlayerType playerType, Vector3 position, Quaternion rotation)
            {
            }

            public void SendFieldStateChange(FieldBase field, FieldState newState)
            {
            }

            public void SendInventorySnapshot(PlayerType playerType, InventorySnapshot snapshot)
            {
            }

            public void SendInteractionRequest(InteractionRequest request)
            {
            }

            public void SendInteractionResult(InteractionCommand command, bool canPerform, ActionType actionType)
            {
            }

            public void SendMove(Vector2 move)
            {
            }

            public void Start(bool isHost)
            {
            }

            public void Tick()
            {
            }
        }

#if !DISABLESTEAMWORKS
        private sealed class SteamOnlineAgentTransport : IOnlineAgentTransport
        {
            public event Action<Vector2> OnMoveReceived;
            public event Action<InteractionRequest> OnInteractionReceived;
            public event Action<InteractionCommand, bool, ActionType> OnInteractionResult;
            public event Action<PlayerType, Vector3, Quaternion> OnAvatarPositionReceived;
            public event Action<FieldStateMessage> OnFieldStateChangeReceived;
            public event Action<PlayerType, InventorySnapshot> OnInventorySyncReceived;

            private readonly CSteamID _peer;
            private const int Channel = 0;

            public SteamOnlineAgentTransport(ulong remoteSteamId)
            {
                _peer = remoteSteamId == 0 ? CSteamID.Nil : new CSteamID(remoteSteamId);
            }

            public void Start(bool isHost)
            {
                _ = isHost; // role decided by creator; no-op for transport
            }

            public void Tick()
            {
                if (!SteamManager.Initialized)
                    return;

                while (SteamNetworking.IsP2PPacketAvailable(out uint size, Channel))
                {
                    var buffer = new byte[size];
                    if (SteamNetworking.ReadP2PPacket(buffer, size, out uint bytesRead, out CSteamID remote, Channel))
                    {
                        if (bytesRead == 0)
                            continue;
                        ParseIncoming(remote, buffer, (int)bytesRead);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            public void SendMove(Vector2 move) =>
                SendMessage(MessageType.Move, writer =>
                {
                    writer.Write(move.x);
                    writer.Write(move.y);
                });

            public void SendInteractionRequest(InteractionRequest request) =>
                SendMessage(MessageType.InteractionRequest, writer =>
                {
                    writer.Write((byte)request.Command);
                    writer.Write(request.Position.x);
                    writer.Write(request.Position.y);
                    writer.Write(request.Position.z);
                    writer.Write(request.Rotation.x);
                    writer.Write(request.Rotation.y);
                    writer.Write(request.Rotation.z);
                    writer.Write(request.Rotation.w);
                });

            public void SendInteractionResult(InteractionCommand command, bool canPerform, ActionType actionType) =>
                SendMessage(MessageType.InteractionResult, writer =>
                {
                    writer.Write((byte)command);
                    writer.Write(canPerform);
                    writer.Write((byte)actionType);
                });

            public void SendAvatarPosition(PlayerType playerType, Vector3 position, Quaternion rotation) =>
                SendMessage(MessageType.AvatarPosition, writer =>
                {
                    writer.Write((byte)playerType);
                    writer.Write(position.x);
                    writer.Write(position.y);
                    writer.Write(position.z);
                    writer.Write(rotation.x);
                    writer.Write(rotation.y);
                    writer.Write(rotation.z);
                    writer.Write(rotation.w);
                });

            public void SendFieldStateChange(FieldBase field, FieldState newState)
            {
                if (field == null || newState == null)
                    return;

                var pos = field.transform.position;
                bool isUnderground = field is UndergroundFieldBase;

                int gx = -1, gy = -1;
                bool includeCarrotData = false;
                float waterLevel = 0f;
                float carrotProgress = 0f;

                if (isUnderground && field is UndergroundFieldBase uf)
                {
                    var xy = UndergroundManager.GetFieldXY(uf);
                    if (xy.HasValue) { gx = xy.Value.x; gy = xy.Value.y; }
                }
                else if (!isUnderground && field is FarmFieldBase ff)
                {
                    var xy = FarmManager.GetFieldXY(ff);
                    if (xy.HasValue) { gx = xy.Value.x; gy = xy.Value.y; }

                    includeCarrotData = newState is FarmFieldWithCarrot || ff.State is FarmFieldWithCarrot;
                    if (includeCarrotData)
                    {
                        waterLevel = ff.GetWaterLevel();
                        carrotProgress = ff.GetCarrotProgressNormalized();
                    }
                }
                SendMessage(MessageType.FieldStateChange, writer =>
                {
                    writer.Write(pos.x);
                    writer.Write(pos.y);
                    writer.Write(pos.z);
                    writer.Write(newState.GetType().Name ?? string.Empty);
                    writer.Write((byte)(isUnderground ? 1 : 0));
                    writer.Write(gx);
                    writer.Write(gy);
                    writer.Write((byte)(includeCarrotData ? 1 : 0));
                    if (includeCarrotData)
                    {
                        writer.Write(waterLevel);
                    }
                    writer.Write((byte)(includeCarrotData ? 1 : 0));
                    if (includeCarrotData)
                    {
                        writer.Write(carrotProgress);
                    }
                });
            }

            public void SendInventorySnapshot(PlayerType playerType, InventorySnapshot snapshot) =>
                SendMessage(MessageType.InventorySync, EP2PSend.k_EP2PSendReliable, writer =>
                {
                    writer.Write((byte)playerType);
                    writer.Write(snapshot.Seed);
                    writer.Write(snapshot.Water);
                    writer.Write(snapshot.Dirt);
                    writer.Write(snapshot.Health);
                    writer.Write(snapshot.Carrot);
                });

            public void Dispose()
            {
            }

            private enum MessageType : byte
            {
                Move = 1,
                InteractionRequest = 2,
                InteractionResult = 3,
                AvatarPosition = 4,
                FieldStateChange = 5,
                InventorySync = 6
            }

            private void SendMessage(MessageType type, Action<BinaryWriter> payloadWriter) =>
                SendMessage(type, EP2PSend.k_EP2PSendUnreliable, payloadWriter);

            private void SendMessage(MessageType type, EP2PSend sendType, Action<BinaryWriter> payloadWriter)
            {
                if (!SteamManager.Initialized || _peer == CSteamID.Nil)
                    return;

                using var ms = new MemoryStream();
                using (var writer = new BinaryWriter(ms))
                {
                    writer.Write((byte)type);
                    payloadWriter?.Invoke(writer);
                }
                var data = ms.ToArray();
                SteamNetworking.SendP2PPacket(_peer, data, (uint)data.Length, sendType, Channel);
            }

            private void ParseIncoming(CSteamID sender, byte[] buffer, int length)
            {
                if (_peer != CSteamID.Nil && sender != _peer)
                    return;

                using var ms = new MemoryStream(buffer, 0, length);
                using var reader = new BinaryReader(ms);

                var type = (MessageType)reader.ReadByte();
                switch (type)
                {
                    case MessageType.Move:
                        OnMoveReceived?.Invoke(new Vector2(reader.ReadSingle(), reader.ReadSingle()));
                        break;
                    case MessageType.InteractionRequest:
                        var cmd = (InteractionCommand)reader.ReadByte();
                        // payload: cmd + pos(3) + rot(4)
                        var p = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        var q = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        OnInteractionReceived?.Invoke(new InteractionRequest(cmd, p, q));
                        break;
                    case MessageType.InteractionResult:
                        var command = (InteractionCommand)reader.ReadByte();
                        var canPerform = reader.ReadBoolean();
                        ActionType actionType = ActionType.None;
                        if (ms.Length - ms.Position >= 1)
                        {
                            actionType = (ActionType)reader.ReadByte();
                        }
                        OnInteractionResult?.Invoke(command, canPerform, actionType);
                        break;
                    case MessageType.AvatarPosition:
                        // v3 payload: [playerType:byte][pos:3f][rot:4f]
                        var pType = (PlayerType)reader.ReadByte();
                        var pos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        var rot = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        OnAvatarPositionReceived?.Invoke(pType, pos, rot);
                        break;
                    case MessageType.FieldStateChange:
                        var fsPos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        var stateName = reader.ReadString();
                        bool? isUnderground = null;
                        if (ms.Length - ms.Position >= 1)
                        {
                            isUnderground = reader.ReadByte() != 0;
                        }
                        int? gx = null;
                        int? gy = null;
                        if (ms.Length - ms.Position >= 8)
                        {
                            gx = reader.ReadInt32();
                            gy = reader.ReadInt32();
                        }
                        float? water = null;
                        float? carrotProgress = null;
                        if (ms.Length - ms.Position >= 1)
                        {
                            var hasWater = reader.ReadByte() != 0;
                            if (hasWater && ms.Length - ms.Position >= 4)
                                water = reader.ReadSingle();
                        }
                        if (ms.Length - ms.Position >= 1)
                        {
                            var hasCarrot = reader.ReadByte() != 0;
                            if (hasCarrot && ms.Length - ms.Position >= 4)
                                carrotProgress = reader.ReadSingle();
                        }
                        OnFieldStateChangeReceived?.Invoke(new FieldStateMessage
                        {
                            Position = fsPos,
                            StateName = stateName,
                            IsUnderground = isUnderground,
                            GridX = gx,
                            GridY = gy,
                            Water = water,
                            CarrotProgress = carrotProgress
                        });
                        break;
                    case MessageType.InventorySync:
                        var invPlayer = (PlayerType)reader.ReadByte();
                        var snap = new InventorySnapshot(
                            seed: reader.ReadInt32(),
                            water: reader.ReadInt32(),
                            dirt: reader.ReadInt32(),
                            health: reader.ReadInt32(),
                            carrot: reader.ReadInt32());
                        OnInventorySyncReceived?.Invoke(invPlayer, snap);
                        break;
                }
            }
        }
#endif

        #endregion
    }
}

