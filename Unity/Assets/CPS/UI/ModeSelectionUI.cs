using UnityEngine;
using UnityEngine.UI;

namespace CPS.KitchenCatch
{
    public sealed class ModeSelectionUI : MonoBehaviour
    {
        public const string SceneName = "ModeSelect";
        public const string SinglePlayerScene = "MainMenu";
        public const string VersusScene = "VersusSetup";
        [SerializeField] private SceneNavigator navigator;
        [SerializeField] private Button singlePlayerButton;
        [SerializeField] private Button versusButton;

        private void Start()
        {
            foreach (var image in FindObjectsByType<Image>(FindObjectsSortMode.None))
                if (image.name == "Body" && image.transform.parent.name == "Versus Mascot 2")
                    image.sprite = MascotBodyPalette.ForFrame(image.sprite, "#3C78D8");
        }

        private void OnEnable()
        {
            if (singlePlayerButton != null) singlePlayerButton.onClick.AddListener(OpenSinglePlayer);
            if (versusButton != null) versusButton.onClick.AddListener(OpenVersus);
        }

        private void OnDisable()
        {
            if (singlePlayerButton != null) singlePlayerButton.onClick.RemoveListener(OpenSinglePlayer);
            if (versusButton != null) versusButton.onClick.RemoveListener(OpenVersus);
        }

        public void OpenSinglePlayer()
        {
            if (navigator != null) navigator.GoTo(SinglePlayerScene);
        }

        public void OpenVersus()
        {
            if (navigator != null) navigator.GoTo(VersusScene);
        }
    }
}
