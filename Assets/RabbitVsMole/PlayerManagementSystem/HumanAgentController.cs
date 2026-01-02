using GameSystems;
using PlayerManagementSystem;
using RabbitVsMole.Events;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Splines;
using UnityEngine.UI;
using static RabbitVsMole.GameManager;
using static UnityEngine.LowLevelPhysics2D.PhysicsLayers;

namespace RabbitVsMole
{
    public class HumanAgentController : HumanAgentControllerBase<PlayerType, PlayerAvatar>
    {
        [SerializeField] private FarmFieldTileHighlighter farmFieldTileHighlighter;
        private PlayerType _playerType;
        private bool _initialized;
        private CinemachineCamera _cinemachineCamera;
        [SerializeField] Image _blackMask;

        public static void CreateInstance(PlayGameSettings playGameSettings, PlayerType playerType)
        {
            var prefab = _agentPrefabs.GetPrefab(PlayerControlAgent.Human);
            var instance = Instantiate(prefab).GetComponent<HumanAgentController>();
            
            if (instance == null)
            {
                DebugHelper.LogError(null, "Failed to instantiate RabbitVsMoleHumanAgentController prefab");
                return;
            }

            instance._playerType = playerType;
            instance._initialized = true;
            instance.SubscribeEvents();
            instance.SetupInputContol();

            if (!instance.Initialize(playerType, playGameSettings.IsGamepadUsing(playerType)))
            {
                DebugHelper.LogError(instance, "Failed to initialize HumanAgentController");
                Destroy(instance.gameObject);

                return;
            }

            if (!instance.InitializeCamera())
            {
                DebugHelper.LogError(instance, "Failed to initialize camera");
                return;
            }
        }

        private void Start()
        {
            Instantiate(farmFieldTileHighlighter).Setup(_playerType);
            ChangeView(_playerAvatar.IsOnSurface);
        }

        private void SetupInputContol()
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
            // Input actions will be found and assigned automatically from Input System
            // We just need to register the action names here
            SetInputActionNames(new List<string>
        {
            "Move",
            "Action_front",
            "Action_down",
            "Action_special"
        });
        }

        private OutputChannels Getchannel(PlayerType playerType) =>
            playerType == PlayerType.Rabbit ? OutputChannels.Channel01 : OutputChannels.Channel02;

        private bool InitializeCamera()
        {
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

            // Set camera viewport rect based on split-screen mode and player type
            camera.rect = (GameManager.CurrentGameInspector.IsSplitScreen, _playerType == PlayerType.Rabbit) switch
            {
                (false, _) => new Rect(0.0f, 0.0f, 1f, 1f), // Not split-screen -> Full screen
                (true, true) => new Rect(0.0f, 0.0f, 0.5f, 1f), // Split-screen & Rabbit -> Left side
                (true, false) => new Rect(0.5f, 0.0f, 0.5f, 1f)  // Split-screen & Other -> Right side
            };

            var cinemachineBrain = GetComponentInChildren<CinemachineBrain>();
            if (cinemachineBrain == null)
            {
                Debug.LogError("CinemachineBrain not found");
                return false;
            }
            cinemachineBrain.ChannelMask = Getchannel(_playerType);
            
            return true;
        }

        void ChangeView(bool isOnSurface)
        {
            if (!_cinemachineCamera.TryGetComponent<CinemachineFollow>(out var followComponent))
                return;

            if (isOnSurface)
                followComponent.FollowOffset = new Vector3(0, 5, -5);
            else
                followComponent.FollowOffset = new Vector3(0, 8, -3);
        }

        public void OnMove(InputValue value)
        {
            _playerAvatar?.SetMoveInput(value.Get<Vector2>());
        }

        public void OnAction_front()
        {
            DebugHelper.Log(this, "OnAction_front");
            _playerAvatar?.TryActionFront();
        }


        public void OnAction_down()
        {
            DebugHelper.Log(this, "OnAction_down");
            _playerAvatar?.TryActionDown();
        }

        public void OnAction_special()
        {
            DebugHelper.Log(this, "OnAction_special");
            _playerAvatar?.TryActionSpecial();
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

            yield return StartCoroutine(FadeMask( new Color(0f,0f,0f,0f), new Color(0f, 0f, 0f, 1f), GameManager.CurrentGameStats.TimeActionEnterMound));

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
                Debug.Log(progress);
                _blackMask.color = Color.Lerp(fromColor, toColor, progress);
                yield return null;
            }
            _blackMask.color = toColor;
        }

    }
}