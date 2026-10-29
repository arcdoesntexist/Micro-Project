using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CPS.KitchenCatch
{
    public sealed class MenuPracticeUI : MonoBehaviour
    {
        [SerializeField] private RectTransform mascot;
        [SerializeField] private Image body;
        [SerializeField] private Image hat;
        [SerializeField] private Sprite idle;
        [SerializeField] private Sprite[] runFrames;
        [SerializeField] private Sprite rightHat;
        [SerializeField] private Sprite leftHat;
        [SerializeField] private GameObject rulesOverlay;
        [SerializeField] private Button rulesButton;
        [SerializeField] private Button closeRulesButton;
        [SerializeField] private GameObject[] inputBlockers;
        [SerializeField] private float speed = 340f;
        [SerializeField] private float leftLimit = -145f;
        [SerializeField] private float rightLimit = 145f;
        private float animationTime;

        private void OnEnable()
        {
            if (rulesButton != null) rulesButton.onClick.AddListener(ShowRules);
            if (closeRulesButton != null) closeRulesButton.onClick.AddListener(HideRules);
            if (rulesOverlay != null) rulesOverlay.SetActive(false);
        }

        private void OnDisable()
        {
            if (rulesButton != null) rulesButton.onClick.RemoveListener(ShowRules);
            if (closeRulesButton != null) closeRulesButton.onClick.RemoveListener(HideRules);
        }

        private void Update()
        {
            if (mascot == null || body == null || !mascot.gameObject.activeInHierarchy ||
                !Application.isFocused || rulesOverlay == null || rulesOverlay.activeSelf) return;
            if (inputBlockers != null)
                foreach (var blocker in inputBlockers)
                    if (blocker != null && blocker.activeInHierarchy) return;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
                EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null) return;
            var input = ControllerInput.EnsureExists().Read();
            StepPractice(input, Time.unscaledDeltaTime);
        }
        public void StepPractice(InputSnapshot input, float seconds)
        {
            if (mascot == null || body == null || !float.IsFinite(seconds) || seconds <= 0) return;
            float direction = 0f;
            if (input.Left.Valid && input.Left.Pressed) direction -= 1f;
            if (input.Right.Valid && input.Right.Pressed) direction += 1f;
            var position = mascot.anchoredPosition;
            position.x = Mathf.Clamp(position.x + direction * speed * seconds, leftLimit, rightLimit);
            mascot.anchoredPosition = position;
            if (Mathf.Approximately(direction, 0f))
            {
                animationTime = 0f;
                body.sprite = idle;
                return;
            }
            bool right = direction > 0f;
            body.rectTransform.localScale = new Vector3(right ? 1f : -1f, 1f, 1f);
            if (hat != null) hat.sprite = right ? rightHat : leftHat;
            animationTime += seconds;
            if (runFrames != null && runFrames.Length > 0)
                body.sprite = runFrames[Mathf.FloorToInt(animationTime * 10f) % runFrames.Length];
        }

        public void ShowRules()
        {
            if (rulesOverlay != null) rulesOverlay.SetActive(true);
            Select(closeRulesButton);
        }

        public void HideRules()
        {
            if (rulesOverlay != null) rulesOverlay.SetActive(false);
            Select(rulesButton);
        }

        private static void Select(Selectable control)
        {
            if (control != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(control.gameObject);
        }
    }
}
