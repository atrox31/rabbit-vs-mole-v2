using GameSystems;
using PlayerManagementSystem;
using RabbitVsMole.Events;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static RabbitVsMole.GameManager;

namespace RabbitVsMole
{
    public class HumanAgentController : HumanAgentControllerBase<PlayerType, PlayerAvatar>
    {
        private PlayerType _playerType;
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

        private void SetupInputContol()
        {
            SetDefaultActionMapName("Player");
            SetGamepadSchemaName("Gamepad");
            SetupInputActions();

            if (GameInspector.IsSplitScreen)
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

            // Set camera viewport rect based on split-screen mode and player type
            camera.rect = (GameInspector.IsSplitScreen, _playerType == PlayerType.Rabbit) switch
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
            EventBus.Subscribe<MoleTravelEvent>(MoleTravel);
        }

        private void OnDisable() 
        {
            EventBus.Unsubscribe<MoleTravelEvent>(MoleTravel);
        }

        private void MoleTravel(MoleTravelEvent moleTravelEvent) =>
            StartCoroutine(MoleTravelInternal(moleTravelEvent));

        private IEnumerator MoleTravelInternal(MoleTravelEvent moleTravelEvent)
        {
            float moveInElapsedTime = 0f; 
            Color maskColor = _blackMask.color;

            maskColor.a = 0f;
            while (moveInElapsedTime < moleTravelEvent.EnterTime)
            {
                moveInElapsedTime += Time.deltaTime;
                var progress = Mathf.Clamp01(moveInElapsedTime / moleTravelEvent.EnterTime);
                maskColor.a = progress;
                yield return null;
            }
            maskColor.a = 1f;
            _playerAvatar.transform.position = moleTravelEvent.NewLocation + new Vector3(0f, .5f, 0f);

            float moveOutElapsedTime = 0f;
            while (moveOutElapsedTime < moleTravelEvent.ExitTime)
            {
                moveOutElapsedTime += Time.deltaTime;
                var progress = Mathf.Clamp01(moveOutElapsedTime / moleTravelEvent.ExitTime);
                maskColor.a = progress;
                yield return null;
            }
            maskColor.a = 0f;
        }

    }
}