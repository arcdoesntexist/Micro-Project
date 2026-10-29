using UnityEngine;
using UnityEngine.InputSystem;

namespace CPS.KitchenCatch
{
    public sealed class KeyboardInput : MonoBehaviour, IPlayerInput
    {
        private bool hasFocus = true;

        public InputSnapshot Read()
        {
            var keyboard = Keyboard.current;
            if (!hasFocus || keyboard == null)
                return new InputSnapshot(default, default, "Keyboard");

            bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
            bool right = keyboard.dKey.isPressed || keyboard.bKey.isPressed || keyboard.rightArrowKey.isPressed;
            return new InputSnapshot(
                new HandInput(left, left ? 1 : 0, true, null),
                new HandInput(right, right ? 1 : 0, true, null), "Keyboard");
        }

        private void OnApplicationFocus(bool focused) => hasFocus = focused;
    }
}
