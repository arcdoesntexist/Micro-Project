using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CPS.KitchenCatch
{
    public sealed class AudioSettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle backgroundMusicToggle;
        [SerializeField] private Text volumeValue;
        [SerializeField] private Button closeButton;
        private Button returnButton;
        private GameObject previousSelection;

        public void Show()
        {
            previousSelection = EventSystem.current?.currentSelectedGameObject;
            gameObject.SetActive(true);
        }

        private void OnEnable()
        {
            AddModeReturnButton();
            if (volumeSlider != null)
            {
                volumeSlider.minValue = 0;
                volumeSlider.maxValue = 1;
                volumeSlider.SetValueWithoutNotify(GlobalAudioSettings.MasterVolume);
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }
            if (backgroundMusicToggle != null)
            {
                backgroundMusicToggle.SetIsOnWithoutNotify(GlobalAudioSettings.BackgroundMusicEnabled);
                backgroundMusicToggle.onValueChanged.AddListener(GlobalAudioSettings.SetBackgroundMusicEnabled);
            }
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (returnButton != null) returnButton.onClick.AddListener(ReturnToModeSelect);
            RefreshValue();
            var focus = volumeSlider != null ? volumeSlider.gameObject :
                backgroundMusicToggle != null ? backgroundMusicToggle.gameObject : closeButton?.gameObject;
            if (focus != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(focus);
        }

        private void OnDisable()
        {
            if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(SetVolume);
            if (backgroundMusicToggle != null)
                backgroundMusicToggle.onValueChanged.RemoveListener(GlobalAudioSettings.SetBackgroundMusicEnabled);
            if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
            if (returnButton != null) returnButton.onClick.RemoveListener(ReturnToModeSelect);
        }

        private void AddModeReturnButton()
        {
            if (returnButton != null || closeButton == null || GetComponentInParent<MainMenuUI>() == null) return;
            var closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchoredPosition = new Vector2(-170, closeRect.anchoredPosition.y);
            closeRect.sizeDelta = new Vector2(300, closeRect.sizeDelta.y);
            closeButton.GetComponentInChildren<Text>().rectTransform.sizeDelta = closeRect.sizeDelta - new Vector2(10, 8);
            returnButton = Instantiate(closeButton, closeButton.transform.parent);
            returnButton.name = "Return to mode select";
            returnButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(170, closeRect.anchoredPosition.y);
            returnButton.GetComponentInChildren<Text>().text = "RETURN";
            var navigation = closeButton.navigation;
            navigation.selectOnRight = returnButton;
            closeButton.navigation = navigation;
            navigation.selectOnLeft = closeButton;
            navigation.selectOnRight = closeButton;
            returnButton.navigation = navigation;
        }

        private void ReturnToModeSelect()
        {
            GetComponentInParent<MainMenuUI>().GetComponent<SceneNavigator>().GoTo("ModeSelect");
        }

        private void SetVolume(float value)
        {
            GlobalAudioSettings.SetMasterVolume(value);
            RefreshValue();
        }

        private void RefreshValue()
        {
            if (volumeValue != null) volumeValue.text = Mathf.RoundToInt(GlobalAudioSettings.MasterVolume * 100).ToString();
        }

        private void Hide()
        {
            var restore = previousSelection;
            gameObject.SetActive(false);
            if (restore != null && restore.activeInHierarchy && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(restore);
        }
    }
}
