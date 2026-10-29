using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
namespace CPS.KitchenCatch
{
    public sealed class PauseHotkeys : MonoBehaviour
    {
        private GameSessionController controller;
        private GameObject[] modalBlockers;
        private bool hasFocus = true;
        public void Bind(GameSessionController source, GameObject[] blockers)
        {
            controller = source; modalBlockers = blockers;
            var module = EventSystem.current?.GetComponent<InputSystemUIInputModule>();
            var submit = module?.submit?.action;
            if (submit != null)
                for (int i = 0; i < submit.bindings.Count; i++)
                {
                    string path = submit.bindings[i].effectivePath?.ToLowerInvariant();
                    if (path == "*/{submit}")
                    {
                        submit.ApplyBindingOverride(i, "<Gamepad>/{Submit}");
                        submit.AddBinding("<Keyboard>/enter");
                        submit.AddBinding("<Keyboard>/numpadEnter");
                    }
                    else if (path == "<keyboard>/space")
                        submit.ApplyBindingOverride(i, "");
                }
        }
        private void Update()
        {
            if (!hasFocus || controller == null ||
                (controller.State != SessionState.Ready && controller.State != SessionState.Playing)) return;
            var keyboard = Keyboard.current;
            if (keyboard == null || (!keyboard.spaceKey.wasPressedThisFrame && !keyboard.escapeKey.wasPressedThisFrame)) return;
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected != null && selected.GetComponentInParent<InputField>() != null) return;
            if (modalBlockers != null)
                foreach (var blocker in modalBlockers) if (blocker != null && blocker.activeInHierarchy) return;
            if (controller.IsPaused) controller.Resume(); else controller.Pause();
        }
        private void OnApplicationFocus(bool focused) => hasFocus = focused;
    }
}
