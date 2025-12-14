using Extensions;
using System.Linq;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;

public class HumanAgentController : AgentController
{
    private PlayerType _playerType;

    private PlayerAvatar _playerAvatar;
    private PlayerInput _playerInput;
    private CinemachineCamera _cinemachineCamera;

    // Input Actions
    private InputAction _moveAction;
    private InputAction _actionFrontAction;
    private InputAction _actionDownAction;
    private InputAction _actionSpecialAction;
    private InputActionMap _playerActionMap;

    // Input Values
    public Vector2 MoveInput { get; private set; }
    public bool ActionFrontPressed { get; private set; }
    public bool ActionDownPressed { get; private set; }
    public bool ActionSpecialPressed { get; private set; }

    void Awake()
    {
        
    }

    private void Start()
    {
    }

    // inicialization
    private bool InitializePlayerInput()
    {
        //InputSystem.actions.Disable();
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("PlayerInput not found");
            return false;
        }
        return true;
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
            (false, _) =>       new Rect(0.0f, 0.0f,  1f,  1f), // Not split-screen -> Full screen
            (true, true) =>     new Rect(0.0f, 0.0f, 0.5f, 1f), // Split-screen & Rabbit -> Left side
            (true, false) =>    new Rect(0.5f, 0.0f, 0.5f, 1f)  // Split-screen & Other -> Right side
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


    public static HumanAgentController CreateInstance(PlayerType playerType, bool isGamepadUsing)
    {
        var instance = Instantiate(_agentPrefabs.GetPrefab(PlayerControlAgent.Human)).GetComponent<HumanAgentController>();
        if(instance == null) 
            return null;

        if (instance.Initialize(playerType, isGamepadUsing))
        {
            return instance;
        }
        else
        {
            Destroy(instance.gameObject);
            return null;
        }
    }

    private bool Initialize(PlayerType playerType, bool isGamepadUsing)
    {
        _playerType = playerType;
        _playerAvatar = CreateAvatar(_playerType);
        _playerAvatar.AddComponent<AudioLisnerController>();

        if (_playerAvatar == null)
        {
            DebugHelper.LogError(this, "PlayerAvatar component not found on avatar");
            return false;
        }

        if (!InitializeCamera())
        {
            DebugHelper.LogError(this, "Failed to initialize camera");
            return false;
        }

        if (!InitializePlayerInput())
        {
            DebugHelper.LogError(this, "Failed to initialize PlayerInput");
            return false;
        }

        if (!InitializeControls(isGamepadUsing))
        {
            DebugHelper.LogError(this, "Failed to initialize controls");
            return false;
        }

        if (!InitializeInputActions())
        {
            DebugHelper.LogError(this, "Failed to initialize input actions");
            return false;
        }


        return true;
    }

    private string GetKeyboardScheme(out InputDevice device)
    {
        device = InputSystem.devices.FirstOrDefault(x => x is Keyboard);
        if (GameInspector.IsSplitScreen)
        {
            return _playerType == PlayerType.Rabbit ? "KeyboardP1" : "KeyboardP2";
        }
        else
        {
            return "KeyboardP1";
        }
    }

    private bool InitializeControls(bool useGamepad)
    {
        InputDevice device;
        if (useGamepad)
        {
            _playerInput.defaultControlScheme = "Gamepad";
            var availableGamepad = GameManager.GetGamepadDevice((int)_playerType);
            if (availableGamepad == null)
            {
                DebugHelper.LogWarning(this, "Not enough gamepads available, switching to keyboard");
                _playerInput.defaultControlScheme = GetKeyboardScheme(out device);
            }
            else
            {
                device = availableGamepad;
            }
        }
        else
        {
            _playerInput.defaultControlScheme = GetKeyboardScheme(out device);
        }

        if(device == null)
        {
            DebugHelper.LogError(this, "No valid input device found");
            return false;
        }

        _playerInput.SwitchCurrentControlScheme(_playerInput.defaultControlScheme, device);
        return true;
    }

    private bool InitializeInputActions()
    {
        if (_playerInput == null || _playerInput.actions == null)
            return false;

        if (string.IsNullOrEmpty(_playerInput.defaultActionMap))
        {
            _playerInput.defaultActionMap = "Player";
        }

        _playerActionMap = _playerInput.actions.FindActionMap("Player");
        if (_playerActionMap == null)
            return false;

        _playerActionMap.Enable();

        _moveAction = _playerActionMap.FindAction("Move");
        _actionFrontAction = _playerActionMap.FindAction("Action_front");
        _actionDownAction = _playerActionMap.FindAction("Action_down");
        _actionSpecialAction = _playerActionMap.FindAction("Action_special");

        string currentControlScheme = _playerInput.currentControlScheme;
        if (!string.IsNullOrEmpty(currentControlScheme))
        {
            foreach (var action in _playerActionMap.actions)
            {
                action.bindingMask = InputBinding.MaskByGroup(currentControlScheme);
            }
        }
        else
        {
            return false;
        }

        _moveAction?.Enable();
        _actionFrontAction?.Enable();
        _actionDownAction?.Enable();
        _actionSpecialAction?.Enable();
        return true;
    }

    private void OnEnable()
    {
        if (_playerInput != null)
        {
            InitializeInputActions();
        }
    }
    private void OnDisable()
    {
        DisableInputActions();
    }

    // input actionsprivate
    void Update()
    {
        ReadInputValues();
    }

    private void DisableInputActions()
    {
        _playerActionMap?.Disable();
        MoveInput = Vector2.zero;
        ActionFrontPressed = false;
        ActionDownPressed = false;
        ActionSpecialPressed = false;
    }

    private void ReadInputValues()
    {
        MoveInput = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        ActionFrontPressed = _actionFrontAction?.IsPressed() ?? false;
        ActionDownPressed = _actionDownAction?.IsPressed() ?? false;
        ActionSpecialPressed = _actionSpecialAction?.IsPressed() ?? false;
        if (ActionFrontPressed)
        {
            DebugHelper.Log(this, "Action Front Pressed");
        }
    }

    public void OnDeviceLost(InputAction.CallbackContext context)
    {
        GameManager.Pause();
    }

    public void OnDeviceRegained(InputAction.CallbackContext context)
    {
        GameManager.Unpause();
    }

    public void OnMove(InputValue value)
    {
        _playerAvatar.SetMoveInput(value.Get<Vector2>());
    }

    public void OnAction_front()
    {
        DebugHelper.Log(this, "OnAction_front");
        _playerAvatar.TryActionFront();
    }

    public void OnAction_down()
    {
        DebugHelper.Log(this, "OnAction_down");
        _playerAvatar.TryActionDown();
    }

    public void OnAction_special()
    {
        DebugHelper.Log(this, "OnAction_special");
        _playerAvatar.TryActionSpecial();
    }

}