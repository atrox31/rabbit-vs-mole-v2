using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace PlayerManagementSystem
{
    /// <summary>
    /// Manages input devices (gamepads and keyboards) for player controllers.
    /// Tracks gamepad usage to prevent multiple players from using the same device.
    /// </summary>
    public class InputDeviceManager
    {
        private class GamepadCollection
        {
            public Gamepad Gamepad { get; set; }
            public bool IsInUse { get; private set; }

            public Gamepad Get()
            {
                IsInUse = true;
                return Gamepad;
            }

            public void Release()
            {
                IsInUse = false;
            }

            public GamepadCollection(Gamepad gamepad, bool isInUse = false)
            {
                Gamepad = gamepad;
                IsInUse = isInUse;
            }
        }

        public static int GamepadCount => Gamepad.all.Count;

        private readonly List<GamepadCollection> _gamepads = new();

        public InputDeviceManager()
        {
            RefreshGamepadList();
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
        public Gamepad GetGamepadDevice()
        {
            RefreshGamepadList();
            
            foreach (var gamepadCollection in _gamepads)
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
        public bool TryToGetGamepadDevice(out Gamepad device)
        {
            device = GetGamepadDevice();
            return device != null;
        }

        /// <summary>
        /// Releases a gamepad device, making it available for other players.
        /// </summary>
        /// <param name="gamepad">The gamepad device to release.</param>
        public void ReleaseGamepad(Gamepad gamepad)
        {
            if (gamepad == null)
            {
                return;
            }

            var gamepadCollection = _gamepads.FirstOrDefault(g => g.Gamepad == gamepad);
            gamepadCollection?.Release();
        }

        /// <summary>
        /// Returns the first keyboard device, if any.
        /// </summary>
        public static InputDevice GetKeyboardDevice() =>
            InputSystem.devices.FirstOrDefault(x => x is Keyboard);
    }
}