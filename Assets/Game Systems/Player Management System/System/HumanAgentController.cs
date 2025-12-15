using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerManagementSystem
{
    /// <summary>
    /// Provides a base class for agent controllers that manage human player input and avatar interaction using
    /// customizable control schemes.
    /// </summary>
    /// <remarks><para> <b>HumanAgentController</b> coordinates input handling, control scheme switching
    /// (keyboard or gamepad), and avatar management for human-controlled agents. It is intended to be subclassed to
    /// implement specific behaviors for different player types and avatars. </para> <para> This class supports dynamic
    /// switching between input devices and manages the initialization and enabling of input actions. It also provides
    /// hooks for handling device loss and regain events, ensuring a seamless player experience when input devices are
    /// connected or disconnected. </para> <para> Thread safety is not guaranteed. All interactions should occur on the
    /// main Unity thread. </para></remarks>
    /// <typeparam name="TypeOfPlayer">The enumeration type representing the player identity or role.</typeparam>
    /// <typeparam name="TypeOfHumanAvatar">The type of the player avatar associated with the human agent. Must inherit from <see cref="PlayerAvatar"/>.</typeparam>
    public abstract class HumanAgentController<TypeOfPlayer, TypeOfHumanAvatar> : AgentController<TypeOfPlayer> 
        where TypeOfPlayer : Enum 
        where TypeOfHumanAvatar : PlayerAvatar
    {
        protected TypeOfHumanAvatar _playerAvatar;
        protected PlayerInput _playerInput;
        protected InputActionMap _playerActionMap;
        protected bool _isUsingGamepad;
        private Gamepad _myGamepad;

        private string _keyboardSchemaName = "Keyboard";
        private string _gamepadSchemaName = "Gamepad";
        private string _defaultDefaultActionMap = "Player";
        
        protected void SetKeyboardSchemaName(string name) =>
            _keyboardSchemaName = name;
        
        protected void SetGamepadSchemaName(string name) =>
            _gamepadSchemaName = name;
        
        protected void SetDefaultActionMapName(string name) =>
            _defaultDefaultActionMap = name;

        private Dictionary<string, InputAction> _inputActions = new();
        private List<string> _inputActionNames = new();
        
        /// <summary>
        /// Sets the list of input action names to be initialized from the action map.
        /// Actions will be found and assigned automatically during InitializeInputActions().
        /// </summary>
        protected void SetInputActionNames(List<string> actionNames) => 
            _inputActionNames = actionNames ?? new List<string>();

        // Input Values
        public Vector2 MoveInput { get; private set; }

        // Initialization
        private bool InitializePlayerInput()
        {
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                DebugHelper.LogError(this, "PlayerInput component not found");
                return false;
            }
            return true;
        }

        protected bool Initialize(TypeOfPlayer playerType, bool isGamepadUsing)
        {
            _isUsingGamepad = isGamepadUsing;
            _playerAvatar = CreateAvatar<TypeOfHumanAvatar>(playerType);

            if (_playerAvatar == null)
            {
                DebugHelper.LogError(this, "PlayerAvatar component not found on avatar");
                return false;
            }

            _playerAvatar.AddComponent<AudioLisnerController>();

            if (!InitializePlayerInput())
            {
                DebugHelper.LogError(this, "Failed to initialize PlayerInput");
                return false;
            }

            if (!InitializeControls(_isUsingGamepad))
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

        private bool SetKeyboardScheme()
        {
            var device = InputDeviceManager.GetKeyboardDevice();
            if (device == null)
            {
                DebugHelper.LogError(this, "No valid input device found");
                return false;
            }

            _playerInput.defaultControlScheme = _keyboardSchemaName;
            _playerInput.SwitchCurrentControlScheme(_playerInput.defaultControlScheme, device);
            return true;
        }

        private bool SetGamepadScheme()
        {
            if (_myGamepad == null)
            {
                if (GameInspector.InputDeviceManager == null || 
                    !GameInspector.InputDeviceManager.TryToGetGamepadDevice(out Gamepad gamepadDevice))
                {
                    DebugHelper.LogWarning(this, "Not enough gamepads available, switching to keyboard");
                    return SetKeyboardScheme();
                }
                _playerInput.defaultControlScheme = _gamepadSchemaName;
                _myGamepad = gamepadDevice;
            }

            var keyboardDevice = InputDeviceManager.GetKeyboardDevice();
            if (keyboardDevice != null)
            {
                _playerInput.SwitchCurrentControlScheme(_playerInput.defaultControlScheme, _myGamepad, keyboardDevice);
            }
            else
            {
                _playerInput.SwitchCurrentControlScheme(_playerInput.defaultControlScheme, _myGamepad);
            }

            return true;
        }

        private bool InitializeControls(bool useGamepad) =>
            useGamepad ? SetGamepadScheme() : SetKeyboardScheme();

        private bool InitializeInputActions()
        {
            if (_playerInput == null || _playerInput.actions == null)
            {
                DebugHelper.LogError(this, "PlayerInput or actions is null");
                return false;
            }

            if (string.IsNullOrEmpty(_playerInput.defaultActionMap))
            {
                _playerInput.defaultActionMap = _defaultDefaultActionMap;
            }

            _playerActionMap?.Disable();

            _playerActionMap = _playerInput.actions.FindActionMap(_playerInput.defaultActionMap);
            if (_playerActionMap == null)
            {
                DebugHelper.LogError(this, $"Action map '{_playerInput.defaultActionMap}' not found");
                return false;
            }

            _playerActionMap.Enable();

            // Find and assign input actions from the action map
            if (_inputActionNames.Count > 0)
            {
                try
                {
                    _inputActions.Clear();
                    foreach (var actionName in _inputActionNames)
                    {
                        var foundAction = _playerActionMap.FindAction(actionName, true);
                        if (foundAction != null)
                        {
                            _inputActions[actionName] = foundAction;
                        }
                        else
                        {
                            DebugHelper.LogWarning(this, $"Input action '{actionName}' not found in action map '{_playerInput.defaultActionMap}'");
                        }
                    }
                }
                catch (ArgumentException e)
                {
                    DebugHelper.LogError(this, $"Error finding input actions: {e.Message}");
                    return false;
                }
            }
            else
            {
                // If no specific actions are registered, use all actions from the action map
                _inputActions.Clear();
                foreach (var action in _playerActionMap.actions)
                {
                    _inputActions[action.name] = action;
                }
            }

            string currentControlScheme = _playerInput.currentControlScheme;
            if (string.IsNullOrEmpty(currentControlScheme))
            {
                DebugHelper.LogError(this, "Current control scheme is null or empty");
                return false;
            }

            string bindingMaskGroups = currentControlScheme;
            if (currentControlScheme == _gamepadSchemaName)
            {
                bindingMaskGroups = $"{_gamepadSchemaName};{_keyboardSchemaName}";
            }

            foreach (var action in _playerActionMap.actions)
            {
                action.bindingMask = InputBinding.MaskByGroup(bindingMaskGroups);
            }

            // Enable all input actions
            foreach (var kvp in _inputActions)
            {
                kvp.Value?.Enable();
            }

            return true;
        }

        /// <summary>
        /// Enables or disables a specific input action by name.
        /// Logs warnings if attempting to change state to the same state (helps catch logic errors).
        /// </summary>
        /// <param name="actionName">The name of the input action to enable/disable.</param>
        /// <param name="enable">True to enable the action, false to disable it.</param>
        /// <returns>True if the action was found and the operation succeeded, false otherwise.</returns>
        public bool SetInputAction(string actionName, bool enable)
        {
            if (string.IsNullOrEmpty(actionName))
            {
                DebugHelper.LogError(this, "Action name cannot be null or empty");
                return false;
            }

            if (!_inputActions.TryGetValue(actionName, out var inputAction))
            {
                DebugHelper.LogError(this, $"Input action '{actionName}' not found in registered actions");
                return false;
            }

            if (inputAction == null)
            {
                DebugHelper.LogError(this, $"Input action '{actionName}' is null (may not have been initialized yet)");
                return false;
            }

            // Check current state and warn if trying to set the same state
            bool isCurrentlyEnabled = inputAction.enabled;
            
            if (enable)
            {
                if (isCurrentlyEnabled)
                {
                    DebugHelper.LogWarning(this, $"Attempted to enable input action '{actionName}', but it is already enabled");
                    return true;
                }
                inputAction.Enable();
            }
            else
            {
                if (!isCurrentlyEnabled)
                {
                    DebugHelper.LogWarning(this, $"Attempted to disable input action '{actionName}', but it is already disabled");
                    return true;
                }
                inputAction.Disable();
            }

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

        private void OnDestroy()
        {
            // Release the gamepad when the controller is destroyed
            if (_myGamepad != null && GameInspector.InputDeviceManager != null)
            {
                GameInspector.InputDeviceManager.ReleaseGamepad(_myGamepad);
            }
        }

        private void DisableInputActions()
        {
            _playerActionMap?.Disable();
            foreach (var kvp in _inputActions)
            {
                kvp.Value?.Disable();
            }
        }

        public void OnDeviceLost()
        {
            if (!_isUsingGamepad)
            {
                return;
            }

            DebugHelper.Log(this, "Device Lost :( switching to keyboard");
            
            // Release the gamepad so it can be used by other players if reconnected
            if (_myGamepad != null && GameInspector.InputDeviceManager != null)
            {
                GameInspector.InputDeviceManager.ReleaseGamepad(_myGamepad);
            }
            
            _isUsingGamepad = false;
            _myGamepad = null;

            if (!InitializeControls(false))
            {
                DebugHelper.LogError(this, "Failed to initialize controls after device loss");
                GameManager.GoToMainMenu();
                return;
            }

            if (!InitializeInputActions())
            {
                DebugHelper.LogError(this, "Failed to initialize input actions after device loss");
                GameManager.GoToMainMenu();
            }
        }
    }
}