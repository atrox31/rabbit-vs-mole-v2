using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InputManager
{
    /// <summary>
    /// Manages input devices (gamepads and keyboards) for player controllers.
    /// Tracks gamepad usage to prevent multiple players from using the same device.
    /// </summary>
    public class InputDeviceManager
    {
        public static int GamepadCount => Gamepad.all.Count;

        private readonly List<GamepadCollection> _gamepads = new();

        private static InputDeviceManager _instance;

        private static InputDeviceManager Instance()
        {
            _instance ??= new InputDeviceManager();
            return _instance;
        }


        /// <summary>
        /// Refreshes the internal gamepad list to include newly connected devices.
        /// </summary>
        private void RefreshGamepadList()
        {
            var currentGamepads = Gamepad.all.ToHashSet();
            
            // Remove gamepads that are no longer connected
            _gamepads.RemoveAll(g => !currentGamepads.Contains(g.Gamepad));
            
            // Add newly connected gamepads
            foreach (var gamepad in currentGamepads)
            {
                if (!_gamepads.Any(g => g.Gamepad == gamepad))
                {
                    _gamepads.Add(new GamepadCollection(gamepad));
                }
            }
        }

        /// <summary>
        /// Returns the first available gamepad (or null if none are free).
        /// </summary>
        public static Gamepad GetGamepadDevice()
        {
            Instance().RefreshGamepadList();
            
            foreach (var gamepadCollection in Instance()._gamepads)
            {
                if (!gamepadCollection.IsInUse)
                {
                    return gamepadCollection.Get();
                }
            }
            return null;
        }

        /// <summary>
        /// Attempts to get a free gamepad device.
        /// </summary>
        /// <param name="device">The gamepad device if available, null otherwise.</param>
        /// <returns>True if a gamepad was found and assigned, false otherwise.</returns>
        public static bool TryToGetGamepadDevice(out Gamepad device)
        {
            device = GetGamepadDevice();
            return device != null;
        }

        /// <summary>
        /// Releases a gamepad device, making it available for other players.
        /// </summary>
        /// <param name="gamepad">The gamepad device to release.</param>
        public static void ReleaseGamepad(Gamepad gamepad)
        {
            if (gamepad == null)
            {
                return;
            }

            var gamepadCollection = Instance()._gamepads.FirstOrDefault(g => g.Gamepad == gamepad);
            gamepadCollection?.Release();
        }

        /// <summary>
        /// Returns the first keyboard device, if any.
        /// </summary>
        public static InputDevice GetKeyboardDevice() =>
            InputSystem.devices.FirstOrDefault(x => x is Keyboard);
    }
}