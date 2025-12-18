using UnityEngine.InputSystem;

namespace InputManager
{
    class GamepadCollection
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
}