using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CPS.KitchenCatch
{
    public sealed class VersusSetupUI : MonoBehaviour
    {
        private static readonly string[] Palette = { "#F57C32", "#3C78D8", "#68A832", "#9B59B6", "#E6B422", "#D94A64" };
        [SerializeField] private SceneNavigator navigator;
        [SerializeField] private Dropdown player1Profiles;
        [SerializeField] private Dropdown player2Profiles;
        [SerializeField] private InputField player1Name;
        [SerializeField] private InputField player2Name;
        [SerializeField] private Button player1CreateButton;
        [SerializeField] private Button player2CreateButton;
        [SerializeField] private Button player1EditButton;
        [SerializeField] private Button player2EditButton;
        [SerializeField] private Button player1HatButton;
        [SerializeField] private Button player1ShoeButton;
        [SerializeField] private Button player2HatButton;
        [SerializeField] private Button player2ShoeButton;
        [SerializeField] private Image player1HatSwatch;
        [SerializeField] private Image player1ShoeSwatch;
        [SerializeField] private Image player2HatSwatch;
        [SerializeField] private Image player2ShoeSwatch;
        [SerializeField] private VersusPracticePlayer player1Practice;
        [SerializeField] private VersusPracticePlayer player2Practice;
        [SerializeField] private Text player1ReadyText;
        [SerializeField] private Text player2ReadyText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;
        private AppSession session;
        private string player1ProfileId;
        private string player2ProfileId;
        private string player1Hat = Palette[0];
        private string player1Shoe = Palette[0];
        private string player2Hat = Palette[1];
        private string player2Shoe = Palette[1];
        private bool player1Ready;
        private bool player2Ready;
        private string player1EditingId;
        private string player2EditingId;
        private VersusControllerHub controllers;
        private VersusControllerPanel controllerPanel;
        private string controllerStatus;

        public string Player1ProfileId => player1ProfileId;
        public string Player2ProfileId => player2ProfileId;
        public bool CanStart => session != null && player1Ready && player2Ready &&
            !string.IsNullOrWhiteSpace(player1ProfileId) && !string.IsNullOrWhiteSpace(player2ProfileId) &&
            !string.Equals(player1ProfileId, player2ProfileId, StringComparison.Ordinal) &&
            (controllers == null || controllers.CanPlay(SerialConnection.NowSeconds, out _));

        private void Start()
        {
            if (session == null && AppSession.Instance != null) Initialize(AppSession.Instance, navigator);
        }

        private void OnEnable()
        {
            if (player1CreateButton != null) player1CreateButton.onClick.AddListener(CreatePlayer1);
            if (player2CreateButton != null) player2CreateButton.onClick.AddListener(CreatePlayer2);
            if (player1EditButton != null) player1EditButton.onClick.AddListener(EditPlayer1);
            if (player2EditButton != null) player2EditButton.onClick.AddListener(EditPlayer2);
            if (player1HatButton != null) player1HatButton.onClick.AddListener(CyclePlayer1Hat);
            if (player1ShoeButton != null) player1ShoeButton.onClick.AddListener(CyclePlayer1Shoes);
            if (player2HatButton != null) player2HatButton.onClick.AddListener(CyclePlayer2Hat);
            if (player2ShoeButton != null) player2ShoeButton.onClick.AddListener(CyclePlayer2Shoes);
            if (player1Profiles != null) player1Profiles.onValueChanged.AddListener(SelectPlayer1);
            if (player2Profiles != null) player2Profiles.onValueChanged.AddListener(SelectPlayer2);
            if (player1Practice != null) player1Practice.ReadyChanged += OnPlayer1Ready;
            if (player2Practice != null) player2Practice.ReadyChanged += OnPlayer2Ready;
            if (startButton != null) startButton.onClick.AddListener(StartMatch);
            if (backButton != null) backButton.onClick.AddListener(Back);
        }

        private void OnDisable()
        {
            if (player1CreateButton != null) player1CreateButton.onClick.RemoveListener(CreatePlayer1);
            if (player2CreateButton != null) player2CreateButton.onClick.RemoveListener(CreatePlayer2);
            if (player1EditButton != null) player1EditButton.onClick.RemoveListener(EditPlayer1);
            if (player2EditButton != null) player2EditButton.onClick.RemoveListener(EditPlayer2);
            if (player1HatButton != null) player1HatButton.onClick.RemoveListener(CyclePlayer1Hat);
            if (player1ShoeButton != null) player1ShoeButton.onClick.RemoveListener(CyclePlayer1Shoes);
            if (player2HatButton != null) player2HatButton.onClick.RemoveListener(CyclePlayer2Hat);
            if (player2ShoeButton != null) player2ShoeButton.onClick.RemoveListener(CyclePlayer2Shoes);
            if (player1Profiles != null) player1Profiles.onValueChanged.RemoveListener(SelectPlayer1);
            if (player2Profiles != null) player2Profiles.onValueChanged.RemoveListener(SelectPlayer2);
            if (player1Practice != null) player1Practice.ReadyChanged -= OnPlayer1Ready;
            if (player2Practice != null) player2Practice.ReadyChanged -= OnPlayer2Ready;
            if (startButton != null) startButton.onClick.RemoveListener(StartMatch);
            if (backButton != null) backButton.onClick.RemoveListener(Back);
        }

        public void Initialize(AppSession appSession, SceneNavigator sceneNavigator)
        {
            session = appSession ?? throw new ArgumentNullException(nameof(appSession));
            navigator = sceneNavigator;
            controllers = VersusControllerHub.EnsureExists();
            controllers.StopWatching();
            if (controllerPanel == null && startButton != null && backButton != null && player1Practice != null)
            {
                var canvas = startButton.GetComponentInParent<Canvas>();
                var card = player1Practice.GetComponentInParent<Image>();
                controllerPanel = VersusControllerPanel.Create(canvas, startButton.GetComponentInChildren<Text>().font,
                    card == null ? player1Profiles.image.sprite : card.sprite, startButton.image.sprite, backButton.image.sprite);
                controllerPanel.AddButton(canvas.transform, new Vector2(760, 464), new Vector2(300, 72));
            }
            LinkColorControls(player1HatButton, player1ShoeButton, player1HatSwatch);
            LinkColorControls(player2HatButton, player2ShoeButton, player2HatSwatch);
            RefreshProfiles();
            RefreshVisuals();
        }

        public PlayerProfile CreateProfile(int player, string displayName)
        {
            RequirePlayer(player);
            if (session == null) throw new InvalidOperationException("Versus setup is not initialized.");
            var created = session.Store.Create(displayName);
            if (player == 1) player1ProfileId = created.PlayerId;
            else player2ProfileId = created.PlayerId;
            RefreshProfiles();
            RefreshVisuals();
            return created;
        }

        public void SelectProfile(int player, string playerId)
        {
            RequirePlayer(player);
            if (session == null) throw new InvalidOperationException("Versus setup is not initialized.");
            bool found = false;
            foreach (var profile in session.Store.Profiles)
                if (profile.PlayerId == playerId) found = true;
            if (!found) throw new ArgumentException("Unknown player ID.", nameof(playerId));
            if (player == 1) player1ProfileId = playerId;
            else player2ProfileId = playerId;
            RefreshVisuals();
        }

        public void SetColors(int player, string hatHex, string shoeHex)
        {
            RequirePlayer(player);
            hatHex = VersusMatchSetup.NormalizeColor(hatHex, nameof(hatHex));
            shoeHex = hatHex;
            if (player == 1) { player1Hat = hatHex; player1Shoe = shoeHex; }
            else { player2Hat = hatHex; player2Shoe = shoeHex; }
            RefreshVisuals();
        }

        public void SetReady(int player, bool ready)
        {
            RequirePlayer(player);
            if (player == 1) player1Ready = ready;
            else player2Ready = ready;
            RefreshVisuals();
        }

        public bool TryPrepareMatch()
        {
            if (!CanStart) return false;
            session.ConfigureVersus(player1ProfileId, player2ProfileId,
                player1Hat, player1Shoe, player2Hat, player2Shoe);
            return true;
        }

        public void StartMatch()
        {
            if (TryPrepareMatch() && navigator != null) navigator.GoTo("VersusGameplay");
        }

        public void Back()
        {
            if (navigator != null) navigator.GoTo(ModeSelectionUI.SceneName);
        }

        public void CreatePlayer1() => TryCreate(1, player1Name == null ? null : player1Name.text);
        public void CreatePlayer2() => TryCreate(2, player2Name == null ? null : player2Name.text);
        public void SelectPlayer1(int index) => SelectDropdown(1, index);
        public void SelectPlayer2(int index) => SelectDropdown(2, index);
        public void CyclePlayer1Hat() => CycleColor(1, true);
        public void CyclePlayer1Shoes() => CycleColor(1, false);
        public void CyclePlayer2Hat() => CycleColor(2, true);
        public void CyclePlayer2Shoes() => CycleColor(2, false);
        public void OnPlayer1Ready(bool ready) => SetReady(1, ready);
        public void OnPlayer2Ready(bool ready) => SetReady(2, ready);

        private void TryCreate(int player, string name)
        {
            try
            {
                string editingId = player == 1 ? player1EditingId : player2EditingId;
                if (editingId == null) CreateProfile(player, name);
                else
                {
                    if (!session.Store.Rename(editingId, name)) { SetStatus(session.Store.Warning); return; }
                    if (player == 1) player1EditingId = null; else player2EditingId = null;
                    var button = player == 1 ? player1CreateButton : player2CreateButton;
                    if (button != null) button.GetComponentInChildren<Text>().text = "CREATE";
                    RefreshProfiles();
                }
                if (player == 1 && player1Name != null) player1Name.text = "";
                if (player == 2 && player2Name != null) player2Name.text = "";
                SetStatus("");
            }
            catch (ArgumentException exception) { SetStatus(exception.Message); }
        }

        public void EditPlayer1() => EditProfile(1);
        public void EditPlayer2() => EditProfile(2);

        private void EditProfile(int player)
        {
            if (session == null) return;
            string id = player == 1 ? player1ProfileId : player2ProfileId;
            foreach (var profile in session.Store.Profiles)
            {
                if (profile.PlayerId != id) continue;
                if (player == 1) player1EditingId = id; else player2EditingId = id;
                var input = player == 1 ? player1Name : player2Name;
                var button = player == 1 ? player1CreateButton : player2CreateButton;
                if (input != null) { input.text = profile.DisplayName; input.ActivateInputField(); }
                if (button != null) button.GetComponentInChildren<Text>().text = "SAVE";
                return;
            }
        }

        private void RefreshProfiles()
        {
            if (session == null) return;
            if (string.IsNullOrEmpty(player1ProfileId) && session.Store.Profiles.Count > 0)
                player1ProfileId = session.Store.Profiles[0].PlayerId;
            if (string.IsNullOrEmpty(player2ProfileId) && session.Store.Profiles.Count > 1)
                player2ProfileId = session.Store.Profiles[1].PlayerId;
            else if (string.IsNullOrEmpty(player2ProfileId) && session.Store.Profiles.Count > 0)
                player2ProfileId = session.Store.Profiles[0].PlayerId;
            Populate(player1Profiles, player1ProfileId);
            Populate(player2Profiles, player2ProfileId);
        }

        private void Populate(Dropdown dropdown, string selectedId)
        {
            if (dropdown == null) return;
            dropdown.ClearOptions();
            var labels = new List<string>();
            int selected = 0;
            for (int i = 0; i < session.Store.Profiles.Count; i++)
            {
                var profile = session.Store.Profiles[i];
                labels.Add(profile.DisplayName + " · " + profile.PlayerId.Substring(0, Math.Min(6, profile.PlayerId.Length)));
                if (session.Store.Profiles[i].PlayerId == selectedId) selected = i;
            }
            dropdown.AddOptions(labels);
            dropdown.SetValueWithoutNotify(selected);
        }

        private void SelectDropdown(int player, int index)
        {
            if (session == null || index < 0 || index >= session.Store.Profiles.Count) return;
            SelectProfile(player, session.Store.Profiles[index].PlayerId);
        }

        private void CycleColor(int player, bool hat)
        {
            string current = player == 1 ? player1Hat : player2Hat;
            int index = Array.IndexOf(Palette, current);
            string next = Palette[(index + 1 + Palette.Length) % Palette.Length];
            SetColors(player, next, next);
        }

        private static void LinkColorControls(Button colorButton, Button shoesButton, Image swatch)
        {
            if (shoesButton != null) shoesButton.gameObject.SetActive(false);
            if (colorButton == null) return;
            var rect = colorButton.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0, rect.anchoredPosition.y);
            rect.sizeDelta = new Vector2(600, rect.sizeDelta.y);
            var label = colorButton.GetComponentInChildren<Text>();
            if (label != null) label.text = "CHARACTER COLOR";
            if (swatch != null) swatch.rectTransform.anchoredPosition = new Vector2(270, 0);
        }

        private void RefreshVisuals()
        {
            ColorUtility.TryParseHtmlString(player1Hat, out var p1HatColor);
            ColorUtility.TryParseHtmlString(player1Shoe, out var p1ShoeColor);
            ColorUtility.TryParseHtmlString(player2Hat, out var p2HatColor);
            ColorUtility.TryParseHtmlString(player2Shoe, out var p2ShoeColor);
            SetSwatch(player1HatSwatch, p1HatColor); SetSwatch(player1ShoeSwatch, p1ShoeColor);
            SetSwatch(player2HatSwatch, p2HatColor); SetSwatch(player2ShoeSwatch, p2ShoeColor);
            if (player1Practice != null) player1Practice.SetColors(p1HatColor, p1ShoeColor);
            if (player2Practice != null) player2Practice.SetColors(p2HatColor, p2ShoeColor);
            if (player1ReadyText != null) player1ReadyText.text = player1Ready ? "READY!" : "Press both controls · keys or FSR";
            if (player2ReadyText != null) player2ReadyText.text = player2Ready ? "READY!" : "Press both controls · keys or FSR";
            if (startButton != null) startButton.interactable = CanStart;
            if (statusText != null)
                statusText.text = player1ProfileId == player2ProfileId && !string.IsNullOrEmpty(player1ProfileId)
                    ? "Choose two different profiles." : "";
        }

        private static void SetSwatch(Image image, Color color)
        {
            if (image != null) image.color = color;
        }

        private void SetStatus(string value)
        {
            if (statusText != null) statusText.text = value;
        }

        private void Update()
        {
            if (startButton != null) startButton.interactable = CanStart;
            if (controllers != null && statusText != null)
            {
                if (!controllers.CanPlay(SerialConnection.NowSeconds, out var reason))
                { controllerStatus = reason; statusText.text = reason; }
                else if (controllerStatus != null && statusText.text == controllerStatus)
                { statusText.text = ""; controllerStatus = null; }
            }
        }

        private static void RequirePlayer(int player)
        {
            if (player != 1 && player != 2) throw new ArgumentOutOfRangeException(nameof(player));
        }
    }
}
