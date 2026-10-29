using UnityEngine;
using UnityEngine.UI;

namespace CPS.KitchenCatch
{
    public sealed class ResultPanel : MonoBehaviour
    {
        [SerializeField] private Text identity;
        [SerializeField] private Text outcome;
        [SerializeField] private Text score;
        [SerializeField] private Text roundStats;
        [SerializeField] private Text leftStats;
        [SerializeField] private Text rightStats;
        [SerializeField] private Text balance;
        [SerializeField] private Text forceStatus;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;
        private GameSessionController controller;
        private SessionResult shown;

        private void OnEnable()
        {
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (menuButton != null) menuButton.onClick.AddListener(MainMenu);
        }

        private void OnDisable()
        {
            if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
            if (menuButton != null) menuButton.onClick.RemoveListener(MainMenu);
        }

        public void Bind(GameSessionController source) => controller = source;

        public void Show(SessionResult result)
        {
            if (result == null) return;
            shown = result.Copy();
            var reference = GetComponent<ResultReferenceView>();
            if (reference != null)
            {
                reference.Show(shown);
                if (restartButton != null) restartButton.interactable = true;
                if (menuButton != null) menuButton.interactable = true;
                gameObject.SetActive(true);
                if (restartButton != null && UnityEngine.EventSystems.EventSystem.current != null)
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
                return;
            }
            if (identity != null) { identity.text = shown.DisplayName; identity.supportRichText = false; }
            if (outcome != null) outcome.text = shown.EndReason == EndReason.HpDepleted ? "HP depleted" : "Triumph";
            if (score != null) score.text = "Score " + shown.Score;
            if (roundStats != null)
                roundStats.text = $"{shown.Difficulty} · {shown.PlayedSeconds:0.0} s · HP {shown.RemainingHp}\n" +
                    $"Food Caught {shown.FoodCaught} · Food Missed {shown.FoodMissed} · Trash Eaten {shown.TrashEaten}\n" +
                    $"Max Combo {shown.MaxCombo} · Highest Rank {shown.HighestRank}";
            var stats = shown.InputStatistics;
            if (leftStats != null) leftStats.text = HandText("LEFT", stats?.Left);
            if (rightStats != null) rightStats.text = HandText("RIGHT", stats?.Right);
            if (balance != null) balance.text = "Both " + (stats?.BothPressedDurationSeconds ?? 0).ToString("0.0") +
                " s · Left–Right Balance " + (stats?.HasBalance == true ? (stats.LeftBalance01 * 100).ToString("0") + "% / " +
                    ((1 - stats.LeftBalance01) * 100).ToString("0") + "%" : "—");
            bool keyboard = string.IsNullOrEmpty(shown.InputSource) || shown.InputSource == "Keyboard";
            if (forceStatus != null) forceStatus.text = keyboard ? "Simulated Input · " + (shown.InputSource ?? "Keyboard") +
                "\nForce (N): " + (stats?.Left?.HasForce == true && stats?.Right?.HasForce == true ?
                    $"L {stats.Left.MeanForceN:0.0}/{stats.Left.MaxForceN:0.0} · R {stats.Right.MeanForceN:0.0}/{stats.Right.MaxForceN:0.0}" :
                    "— / Sensor not connected") : shown.InputSource == "FSR" ? "FSR · Relative press level\nForce N/A in this mode" :
                    "Load Cell · Estimated Force (N)\nL " + ForceText(stats?.Left) + " · R " + ForceText(stats?.Right);
            foreach (var label in GetComponentsInChildren<Text>(true)) label.supportRichText = false;
            if (restartButton != null) restartButton.interactable = true;
            if (menuButton != null) menuButton.interactable = true;
            gameObject.SetActive(true);
            if (restartButton != null && UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
        }

        public void Hide()
        {
            shown = null;
            gameObject.SetActive(false);
        }

        public void Restart()
        {
            if (shown == null || controller == null || restartButton != null && !restartButton.interactable) return;
            if (restartButton != null) restartButton.interactable = false;
            if (controller.Restart()) Hide();
        }

        public void MainMenu()
        {
            if (shown == null || controller == null || menuButton != null && !menuButton.interactable) return;
            if (menuButton != null) menuButton.interactable = false;
            controller.ReturnToMenu();
        }

        private static string HandText(string title, HandStatistics hand)
        {
            if (hand?.HasForce == true && !hand.HasStrength)
                return title + $"\nMean {hand.MeanForceN:0.00} N · Max {hand.MaxForceN:0.00} N\nPressed {hand.PressedDurationSeconds:0.0} s";
            if (hand == null || !hand.HasStrength) return title + "\nMean — · Max —\nPressed 0.0 s";
            return title + $"\nMean {hand.MeanStrength01 * 100:0}% · Max {hand.MaxStrength01 * 100:0}%\nPressed {hand.PressedDurationSeconds:0.0} s";
        }
        private static string ForceText(HandStatistics hand) => hand?.HasForce == true ? $"{hand.MeanForceN:0.00}/{hand.MaxForceN:0.00}" : "N/A";
        private void Update() { if (shown != null && controller != null && restartButton != null) restartButton.interactable = controller.CanRestart; }
    }
}
