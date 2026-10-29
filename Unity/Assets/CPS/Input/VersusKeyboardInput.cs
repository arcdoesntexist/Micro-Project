using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CPS.KitchenCatch
{
    public sealed class VersusKeyboardInput : MonoBehaviour, IPlayerInput
    {
        [SerializeField] private Key leftKey = Key.A;
        [SerializeField] private Key rightKey = Key.D;
        [SerializeField] private string source = "Keyboard P1";
        private bool hasFocus = true;

        public void Configure(Key left, Key right, string inputSource)
        {
            if (left == Key.None || right == Key.None || left == right)
                throw new ArgumentException("Two distinct keyboard keys are required.");
            if (string.IsNullOrWhiteSpace(inputSource))
                throw new ArgumentException("Input source is required.", nameof(inputSource));
            leftKey = left;
            rightKey = right;
            source = inputSource;
        }

        public InputSnapshot Read()
        {
            var keyboard = Keyboard.current;
            var hub = VersusControllerHub.Instance;
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (!hasFocus || hub != null && hub.SuppressInput || selected != null && selected.GetComponent<InputField>() != null)
                return new InputSnapshot(default, default, source);

            bool left = keyboard != null && keyboard[leftKey].isPressed;
            bool right = keyboard != null && keyboard[rightKey].isPressed;
            var keys = new InputSnapshot(
                new HandInput(left, left ? 1 : 0, keyboard != null, null),
                new HandInput(right, right ? 1 : 0, keyboard != null, null), source);
            if (hub == null) return keyboard == null ? new InputSnapshot(default, default, source) : keys;
            int player = leftKey == Key.LeftArrow ? 2 : 1;
            return Combine(keys, hub.Read(player), player);
        }

        public static InputSnapshot Combine(InputSnapshot keys, InputSnapshot sensors, int player)
        {
            bool fsr = sensors.Left.Valid || sensors.Right.Valid;
            return new InputSnapshot(Merge(keys.Left, sensors.Left), Merge(keys.Right, sensors.Right),
                fsr ? "Keyboard + FSR P" + player : keys.Source, fsr ? sensors.CalibrationVersion : null);
        }

        private static HandInput Merge(HandInput keys, HandInput sensor)
        {
            bool key = keys.Valid && keys.Pressed;
            bool pressed = key || sensor.Valid && sensor.Pressed;
            bool measured = sensor.Valid && (!key || sensor.Pressed);
            return new HandInput(pressed, measured ? sensor.Strength01 : key ? 1 : 0,
                keys.Valid || sensor.Valid, null, measured && sensor.HasStrength);
        }

        private void OnApplicationFocus(bool focused) => hasFocus = focused;
    }
}
