using UnityEngine;
using UnityEngine.UI;

namespace CPS.KitchenCatch
{
    public sealed class GameplayUI : MonoBehaviour
    {
        [SerializeField] private GameSessionController controller;
        [SerializeField] private Font licensedFont;
        [SerializeField] private Text profile;
        [SerializeField] private Text hp;
        [SerializeField] private Text time;
        [SerializeField] private Text score;
        [SerializeField] private Text difficulty;
        [SerializeField] private Text rank;
        [SerializeField] private Text rankLetter;
        [SerializeField] private Image rankPlate;
        [SerializeField] private Image rankCalloutPlate;
        [SerializeField] private Image[] rankAccents;
        [SerializeField] private Text leftHand;
        [SerializeField] private Text rightHand;
        [SerializeField] private Text inputSource;
        [SerializeField] private Text status;
        [SerializeField] private Text rankCallout;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private AudioSettingsPanel settingsPanel;
        [SerializeField] private ControllerPanel controllerPanel;
        [SerializeField] private Button controllerButton;
        [SerializeField] private PauseHotkeys pauseHotkeys;
        [SerializeField] private Text controllerNotice;
        [SerializeField] private GameObject pauseOverlay;
        [SerializeField] private ResultPanel resultPanel;
        [SerializeField] private bool referenceHud;
        [SerializeField] private Graphic[] heartImages;
        [SerializeField] private Text streakLabel;
        [SerializeField] private Image leftFill;
        [SerializeField] private Image rightFill;
        [SerializeField] private Image streakFill;
        private bool listening;
        private ComboRank lastRank = ComboRank.D;
        private float calloutSeconds;
        private SessionResult frozenResult;
        private bool wasPaused;

        private void OnEnable()
        {
            foreach (var label in GetComponentsInChildren<Text>(true))
            {
                label.supportRichText = false;
                if (licensedFont != null) label.font = licensedFont;
            }
            if (pauseButton != null) pauseButton.onClick.AddListener(Pause);
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (controllerButton != null) controllerButton.onClick.AddListener(OpenController);
            if (backToMenuButton != null) backToMenuButton.onClick.AddListener(BackToMenu);
            Bind(controller);
        }

        private void OnDisable()
        {
            if (pauseButton != null) pauseButton.onClick.RemoveListener(Pause);
            if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
            if (controllerButton != null) controllerButton.onClick.RemoveListener(OpenController);
            if (backToMenuButton != null) backToMenuButton.onClick.RemoveListener(BackToMenu);
            Unsubscribe();
        }

        public void Bind(GameSessionController source)
        {
            Unsubscribe();
            controller = source;
            if (controller != null)
            {
                controller.StateChanged += OnStateChanged;
                controller.Finished += OnFinished;
                listening = true;
            }
            if (resultPanel != null) resultPanel.Bind(source);
            if (pauseHotkeys != null) pauseHotkeys.Bind(source, new[] { settingsPanel != null ? settingsPanel.gameObject : null, controllerPanel != null ? controllerPanel.PanelRoot : null });
            Refresh();
        }

        public void Refresh()
        {
            if (controller == null) return;
            var model = controller.Model;
            var current = model.Score.CurrentRank;
            if (profile != null) profile.text = AppSession.Instance?.SelectedProfile?.DisplayName ?? "PLAYER";
            if (profile != null) profile.supportRichText = false;
            if (hp != null) hp.text = "HP " + model.Health.CurrentHp + " / 3";
            if (time != null) time.text = "TIME " + Mathf.CeilToInt(model.RemainingSeconds).ToString("00");
            if (score != null) score.text = "SCORE " + model.Score.Score.ToString("N0");
            if (difficulty != null) difficulty.text = AppSession.Instance?.SelectedDifficulty.ToString() ?? "Easy";
            if (rank != null) rank.text = RankName(current);
            ApplyRankPresentation(current);
            ControllerMode mode = controller.Router?.SelectedMode ?? ControllerMode.Keyboard;
            if (leftHand != null) leftHand.text = HandLabel("LEFT", controller.LastInput.Left, mode);
            if (rightHand != null) rightHand.text = HandLabel("RIGHT", controller.LastInput.Right, mode);
            if (inputSource != null) inputSource.text = mode == ControllerMode.Keyboard ? "Simulated Input · Keyboard" : mode == ControllerMode.FSR ? "FSR · Relative press level" : "Load Cell · Estimated Force (N)";
            bool showInputs = controller.State != SessionState.Results;
            if (leftHand != null) leftHand.transform.parent.gameObject.SetActive(showInputs);
            if (rightHand != null) rightHand.transform.parent.gameObject.SetActive(showInputs);
            if (inputSource != null) inputSource.gameObject.SetActive(showInputs && !referenceHud);
            if (referenceHud)
            {
                int seconds = Mathf.Max(0, Mathf.CeilToInt(model.RemainingSeconds));
                if (time != null)
                {
                    time.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
                    time.color = seconds <= 10 ? new Color(.69f, .13f, .10f) : new Color(.32f, .17f, .10f);
                }
                if (score != null) score.text = model.Score.Score.ToString("N0");
                if (leftHand != null) leftHand.text = HandLabel("LEFT", controller.LastInput.Left, mode);
                if (rightHand != null) rightHand.text = HandLabel("RIGHT", controller.LastInput.Right, mode);
                if (leftFill != null) leftFill.fillAmount = Mathf.Clamp01(controller.LastInput.Left.Strength01);
                if (rightFill != null) rightFill.fillAmount = Mathf.Clamp01(controller.LastInput.Right.Strength01);
                if (leftFill != null) leftFill.gameObject.SetActive(mode != ControllerMode.LoadCell);
                if (rightFill != null) rightFill.gameObject.SetActive(mode != ControllerMode.LoadCell);
                if (streakFill != null) streakFill.fillAmount = Mathf.Clamp01(model.Score.Streak / 12f);
                if (streakLabel != null) streakLabel.text = model.Score.Streak + " IN A ROW";
                if (heartImages != null)
                    for (int i = 0; i < heartImages.Length; i++)
                        if (heartImages[i] != null) heartImages[i].color = i < model.Health.CurrentHp
                            ? new Color(.83f, .29f, .21f) : new Color(.73f, .59f, .47f);
            }
            if (status != null)
                status.text = controller.ValidationError ?? (controller.IsPaused ? (referenceHud ? "" : "PAUSED · Choose RESUME") :
                    controller.CountdownSeconds > 0 ? Mathf.CeilToInt(controller.CountdownSeconds).ToString() :
                    controller.State == SessionState.Triumph ? "Triumph!" :
                    controller.State == SessionState.Dying ? "HP depleted" : "");
            if (pauseButton != null) pauseButton.gameObject.SetActive(controller.State == SessionState.Playing && !controller.IsPaused);
            if (resumeButton != null) resumeButton.gameObject.SetActive(controller.IsPaused);
            if (resumeButton != null) resumeButton.interactable = controller.CanResume;
            if (controllerNotice != null) controllerNotice.text = controller.InputIssue ?? "Space / Esc\nPause or resume";
            if (pauseOverlay != null) pauseOverlay.SetActive(controller.IsPaused);
            if (controller.IsPaused != wasPaused)
            {
                wasPaused = controller.IsPaused;
                var focus = wasPaused ? resumeButton : pauseButton;
                if (focus != null && focus.gameObject.activeInHierarchy && UnityEngine.EventSystems.EventSystem.current != null)
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(focus.gameObject);
            }

            if (current > lastRank)
            {
                lastRank = current;
                calloutSeconds = 1.1f;
                if (rankCallout != null) rankCallout.text = Praise(model.Score.Streak) + " · " + RankName(current);
            }
            else if (current == ComboRank.D) lastRank = current;
            if (rankCallout != null) rankCallout.transform.parent.gameObject.SetActive(calloutSeconds > 0 && controller.State == SessionState.Playing);
        }

        private void Update()
        {
            calloutSeconds = Mathf.Max(0, calloutSeconds - Time.unscaledDeltaTime);
            Refresh();
        }

        private void OnFinished(SessionResult result) => frozenResult = result.Copy();

        private void OnStateChanged(SessionState state)
        {
            if (state == SessionState.Ready)
            {
                frozenResult = null;
                lastRank = ComboRank.D;
                calloutSeconds = 0;
                if (resultPanel != null) resultPanel.Hide();
            }
            else if (state == SessionState.Results && resultPanel != null)
                resultPanel.Show(frozenResult ?? controller.LastResult);
            Refresh();
        }

        private void Unsubscribe()
        {
            if (!listening || controller == null) return;
            controller.StateChanged -= OnStateChanged;
            controller.Finished -= OnFinished;
            listening = false;
        }

        private void Pause() => controller?.Pause();
        private void Resume() => controller?.Resume();
        private void OpenSettings() => settingsPanel?.Show();
        private void OpenController() => controllerPanel?.Show(controller);
        private static string HandLabel(string side, HandInput hand, ControllerMode mode) => side + "  " +
            (!hand.Valid && mode != ControllerMode.Keyboard ? "N/A" : mode == ControllerMode.LoadCell ? (hand.ForceN.HasValue ? hand.ForceN.Value.ToString("0.00") + " N" : "N/A") :
                Mathf.RoundToInt(hand.Strength01 * 100) + "%");
        private void BackToMenu() => controller?.ReturnToMenu();

        private static string Praise(int streak) => streak == 2 ? "OK" : streak == 3 ? "Good" : streak == 4 ? "Great" : "Perfect";
        private static string RankName(ComboRank value) => value switch
        {
            ComboRank.C => "Cookin'", ComboRank.B => "Bold Bite", ComboRank.A => "Appetizing!",
            ComboRank.S => "Sizzling!", ComboRank.SS => "Super Savory!", ComboRank.SSS => "Supreme Chef!",
            _ => "Dull Dish"
        };

        private void ApplyRankPresentation(ComboRank value)
        {
            if (rankLetter != null)
            {
                rankLetter.text = value.ToString();
                rankLetter.fontSize = referenceHud ? (value == ComboRank.SSS ? 59 : value == ComboRank.SS ? 70 : 88) :
                    value == ComboRank.SSS ? 32 : value == ComboRank.SS ? 40 : 44;
            }
            var color = RankColor(value);
            if (rankPlate != null) rankPlate.color = color;
            if (rankCalloutPlate != null) rankCalloutPlate.color = color;
            int visibleAccents = value switch
            {
                ComboRank.C or ComboRank.B => 1,
                ComboRank.A or ComboRank.S => 2,
                ComboRank.SS or ComboRank.SSS => 3,
                _ => 0
            };
            if (rankAccents == null) return;
            for (int i = 0; i < rankAccents.Length; i++)
                if (rankAccents[i] != null) rankAccents[i].gameObject.SetActive(i < visibleAccents);
        }

        private static Color RankColor(ComboRank value) => value switch
        {
            ComboRank.C => new Color(.94f, .45f, .14f),
            ComboRank.B => new Color(.88f, .24f, .14f),
            ComboRank.A => new Color(.87f, .18f, .40f),
            ComboRank.S => new Color(.96f, .61f, .05f),
            ComboRank.SS => new Color(.72f, .10f, .43f),
            ComboRank.SSS => new Color(.02f, .58f, .48f),
            _ => new Color(.45f, .36f, .29f)
        };
    }
}
