using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CPS.KitchenCatch
{
    public sealed class VersusGameplayUI : MonoBehaviour
    {
        private static readonly Color Cocoa = new Color(.357f, .165f, .094f);
        private static readonly Color Cream = new Color(1, .965f, .84f);
        private VersusMatchController owner;
        private Canvas canvas;
        private Text clock;
        private Text tier;
        private Text countdownText;
        private GameObject countdown;
        private GameObject pause;
        private GameObject results;
        private Text resultTitle;
        private Text resultTime;
        private Text result1;
        private Text result2;
        private Text upload;
        private AudioSettingsPanel settings;
        private VersusControllerPanel controllerPanel;
        private Text controllerWarning;
        public bool SettingsOpen => settings != null && settings.gameObject.activeInHierarchy;
        private readonly Text[] names = new Text[2];
        private readonly Text[] hp = new Text[2];
        private readonly Text[] scores = new Text[2];
        private readonly Text[] ranks = new Text[2];
        private readonly Text[] streaks = new Text[2];

        public void Build(VersusMatchController controller, VersusMatchSetup setup,
            out VersusFieldController first, out VersusFieldController second)
        {
            owner = controller;
            var cameraRoot = new GameObject("Versus Camera", typeof(Camera), typeof(AudioListener), typeof(GameplayViewport));
            cameraRoot.transform.SetParent(transform, false);
            var camera = cameraRoot.GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Cocoa;
            camera.transform.position = new Vector3(0, 0, -10);
            canvas = new GameObject("Versus Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10;
            canvas.pixelPerfect = true;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (EventSystem.current == null)
            {
                var events = new GameObject("Versus EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
            }
            var backdrop = Picture(canvas.transform, "Kitchen backdrop", owner.Kitchen, Vector2.zero, new Vector2(1920, 1080));
            backdrop.color = new Color(.45f, .35f, .3f);
            Plate(canvas.transform, "Versus title", new Vector2(-646, 442), new Vector2(570, 132));
            Label(canvas.transform, "Title", "VERSUS KITCHEN", new Vector2(-646, 460), new Vector2(520, 60), 43);
            Label(canvas.transform, "Title hint", "Same drops. Last chef standing.", new Vector2(-646, 412), new Vector2(520, 36), 25);
            Plate(canvas.transform, "Shared clock", new Vector2(20, 442), new Vector2(620, 132));
            clock = Label(canvas.transform, "Elapsed time", "00:00", new Vector2(-90, 450), new Vector2(270, 68), 52);
            tier = Label(canvas.transform, "Challenge tier", "TIER 1", new Vector2(170, 450), new Vector2(240, 60), 34);
            Label(canvas.transform, "Endless", "ENDLESS SURVIVAL", new Vector2(20, 407), new Vector2(520, 32), 22);
            ActionButton(canvas.transform, "PAUSE", new Vector2(775, 448), new Vector2(260, 90), owner.Pause);
            first = PlayerField(0, setup.Player1);
            second = PlayerField(1, setup.Player2);
            var hint = Label(canvas.transform, "Rules hint", "Catch food for score. Avoid rubbish to survive. Missed food costs no HP.",
                new Vector2(0, -485), new Vector2(1800, 50), 28);
            hint.color = Cream;
            BuildPause();
            BuildResults(setup);
            countdown = Rect(canvas.transform, "Shared countdown", Vector2.zero, new Vector2(1920, 1080)).gameObject;
            var shade = countdown.AddComponent<Image>(); shade.color = new Color(.1f, .04f, .02f, .5f);
            Plate(countdown.transform, "Countdown card", Vector2.zero, new Vector2(420, 310));
            Label(countdown.transform, "Get ready", "GET READY", new Vector2(0, 87), new Vector2(360, 55), 36);
            countdownText = Label(countdown.transform, "Countdown", "3", new Vector2(0, -24), new Vector2(360, 160), 112);
        }

        private VersusFieldController PlayerField(int index, VersusPlayerSetup setup)
        {
            float center = index == 0 ? -470 : 470;
            var header = Plate(canvas.transform, "P" + (index + 1) + " HUD", new Vector2(center, 301), new Vector2(910, 114));
            var accent = Picture(header, "Player accent", null, new Vector2(-425, 0), new Vector2(10, 65));
            ColorUtility.TryParseHtmlString(setup.HatColorHex, out var color); accent.color = color;
            names[index] = Label(header, "Profile", "P" + (index + 1) + " · " + setup.DisplayName, new Vector2(-210, 21), new Vector2(380, 44), 31);
            names[index].alignment = TextAnchor.MiddleLeft;
            names[index].resizeTextForBestFit = true;
            names[index].resizeTextMinSize = 20; names[index].resizeTextMaxSize = 31;
            hp[index] = Label(header, "Health", "HP  3 / 3", new Vector2(-210, -25), new Vector2(380, 40), 27);
            hp[index].alignment = TextAnchor.MiddleLeft;
            scores[index] = Label(header, "Score", "SCORE  0", new Vector2(230, 0), new Vector2(360, 70), 38);
            Plate(canvas.transform, "P" + (index + 1) + " field frame", new Vector2(center, -36), new Vector2(924, 530.25f));
            var field = Rect(canvas.transform, "P" + (index + 1) + " playfield", new Vector2(center, -36), new Vector2(900, 506.25f));
            field.gameObject.AddComponent<RectMask2D>();
            Picture(field, "Kitchen", owner.Kitchen, Vector2.zero, field.sizeDelta);
            var component = field.gameObject.AddComponent<VersusFieldController>();
            for (int i = 0; i < owner.Decorations.Length; i++)
            {
                var decoration = Picture(field, "Hanging decoration " + i, owner.Decorations[i],
                    new Vector2(-300 + i * 56, 160), new Vector2(40, 126));
                decoration.preserveAspect = true;
                component.AddDecoration(decoration.rectTransform);
            }
            const float unit = 56.25f;
            var mascot = Rect(field, "Mascot", new Vector2(0, -2.5f * unit), new Vector2(1.638889f, 1.244444f) * unit);
            var body = Picture(mascot, "Body", owner.BodyFrames[0], Vector2.zero, mascot.sizeDelta);
            body.preserveAspect = true;
            owner.GetHat(setup.HatColorHex, out var right, out _);
            var hat = Picture(mascot, "Hat", right, new Vector2(-.14f, .627778f) * unit, new Vector2(1.109f, .769f) * unit);
            hat.preserveAspect = true;
            component.Initialize(owner, field, mascot, body, hat, setup);
            var footer = Plate(canvas.transform, "P" + (index + 1) + " combo plate", new Vector2(center, -372), new Vector2(910, 118));
            ranks[index] = Label(footer, "Combo rank", "D · Dull Dish", new Vector2(-185, 21), new Vector2(470, 44), 31);
            streaks[index] = Label(footer, "Streak", "0 catches in a row", new Vector2(-185, -25), new Vector2(470, 40), 25);
            Label(footer, "Controls", index == 0 ? "A   /   D" : "LEFT   /   RIGHT", new Vector2(265, 0), new Vector2(320, 66), 28);
            return component;
        }

        private void BuildPause()
        {
            pause = Modal("Pause", new Vector2(980, 930), out var card);
            Label(card, "Pause title", "TAKE A BREATHER", new Vector2(0, 362), new Vector2(850, 90), 51);
            controllerWarning = Label(card, "Pause hint", "Both kitchens are paused.", new Vector2(0, 265), new Vector2(850, 88), 27);
            ActionButton(card, "KEEP COOKING", new Vector2(0, 158), new Vector2(650, 82), owner.Resume);
            controllerPanel = VersusControllerPanel.Create(canvas, owner.Font, owner.Frame, owner.OrangeButton, owner.CocoaButton);
            ActionButton(card, "CONTROLLERS", new Vector2(0, 58), new Vector2(650, 82), controllerPanel.Show, true);
            ActionButton(card, "CONTINUE WITH KEYBOARD", new Vector2(0, -42), new Vector2(650, 82), owner.ContinueWithKeyboard, true);
            ActionButton(card, "SETTINGS", new Vector2(0, -142), new Vector2(650, 82), () => settings?.Show(), true);
            ActionButton(card, "BACK TO MAIN MENU", new Vector2(0, -242), new Vector2(650, 82), owner.ReturnToMenu, true);
            Label(card, "Abandon hint", "Leaving an unfinished match saves no result.", new Vector2(0, -352), new Vector2(850, 50), 24);
            pause.SetActive(false);
        }

        public void AttachSettings(GameObject prefab)
        {
            if (prefab == null) return;
            var instance = Instantiate(prefab, canvas.transform);
            settings = instance.GetComponent<AudioSettingsPanel>();
            instance.SetActive(false);
        }

        private void BuildResults(VersusMatchSetup setup)
        {
            results = Modal("Match results", new Vector2(1760, 920), out var card);
            resultTitle = Label(card, "Winner", "MATCH COMPLETE", new Vector2(0, 370), new Vector2(1550, 85), 58);
            resultTime = Label(card, "Survival time", "", new Vector2(0, 301), new Vector2(1500, 55), 29);
            result1 = ResultColumn(card, setup.Player1, -420, 1);
            result2 = ResultColumn(card, setup.Player2, 420, 2);
            ActionButton(card, "REMATCH", new Vector2(-540, -330), new Vector2(440, 90), owner.Restart);
            ActionButton(card, "PLAYER SETUP", new Vector2(0, -330), new Vector2(440, 90), owner.BackToSetup, true);
            ActionButton(card, "MAIN MENU", new Vector2(540, -330), new Vector2(440, 90), owner.ReturnToMenu, true);
            upload = Label(card, "Upload status", "", new Vector2(0, -411), new Vector2(1500, 40), 23);
            results.SetActive(false);
        }

        private Text ResultColumn(RectTransform parent, VersusPlayerSetup setup, float x, int player)
        {
            var plate = Plate(parent, "P" + player + " summary", new Vector2(x, 0), new Vector2(780, 520));
            var name = Label(plate, "Profile", "P" + player + " · " + setup.DisplayName, new Vector2(0, 204), new Vector2(680, 55), 36);
            name.resizeTextForBestFit = true; name.resizeTextMinSize = 22; name.resizeTextMaxSize = 36;
            var text = Label(plate, "Statistics", "", new Vector2(0, 62), new Vector2(690, 225), 29);
            for (int i = 0; i < owner.Definitions.Length; i++)
            {
                float column = i < 3 ? -240 + i * 240 : -130 + (i - 3) * 260;
                float y = i < 3 ? -108 : -187;
                var image = Picture(plate, "Item " + i, owner.Definitions[i].sprite, new Vector2(column - 40, y), new Vector2(53, 53));
                image.preserveAspect = true;
                Label(plate, "Count " + i, "× 0", new Vector2(column + 35, y), new Vector2(100, 45), 28);
            }
            return text;
        }

        private GameObject Modal(string name, Vector2 size, out RectTransform card)
        {
            var root = Rect(canvas.transform, name, Vector2.zero, new Vector2(1920, 1080));
            root.gameObject.AddComponent<Image>().color = new Color(.1f, .03f, .01f, .8f);
            card = Plate(root, name + " card", Vector2.zero, size);
            return root.gameObject;
        }

        public void Refresh(VersusMatchModel model)
        {
            int elapsed = Mathf.FloorToInt(model.PlayedSeconds);
            clock.text = (elapsed / 60).ToString("00") + ":" + (elapsed % 60).ToString("00");
            tier.text = "TIER " + (model.CurrentTier + 1);
            RefreshPlayer(0, model.Player1); RefreshPlayer(1, model.Player2);
        }

        private void RefreshPlayer(int index, VersusPlayerModel player)
        {
            hp[index].text = "HP  " + player.Health.CurrentHp + " / 3";
            hp[index].color = player.Health.CurrentHp <= 1 ? new Color(.75f, .15f, .09f) : Cocoa;
            scores[index].text = "SCORE  " + player.Score.Score;
            ranks[index].text = player.Score.CurrentRank + " · " + RankName(player.Score.CurrentRank);
            streaks[index].text = player.Score.Streak + " catches in a row";
        }

        public void SetCountdown(string value)
        {
            countdown.SetActive(value != null);
            if (value != null) countdownText.text = value;
        }

        public void ShowPause(bool visible)
        {
            if (controllerWarning != null) controllerWarning.text = owner.ControllerWarning ?? "Both kitchens are paused.";
            if (!visible && controllerPanel != null) controllerPanel.Hide();
            if (!visible && settings != null) settings.gameObject.SetActive(false);
            pause.SetActive(visible);
        }
        public void HideOverlays() { controllerPanel.Hide(); pause.SetActive(false); results.SetActive(false); SetUploadStatus(""); }
        public void SetUploadStatus(string value) { if (upload != null) upload.text = value; }

        public void ShowResults(VersusMatchSetup setup, VersusMatchResult result)
        {
            results.SetActive(true);
            resultTitle.text = result.Winner == VersusWinner.Draw ? "A DELICIOUS DRAW!" :
                (result.Winner == VersusWinner.P1 ? setup.Player1.DisplayName : setup.Player2.DisplayName) + " WINS!";
            resultTitle.resizeTextForBestFit = true; resultTitle.resizeTextMinSize = 28; resultTitle.resizeTextMaxSize = 58;
            resultTime.text = "Survived " + result.PlayedSeconds.ToString("0.0") + " seconds · Final tier " + (owner.Model.CurrentTier + 1);
            FillResult(result1, result.Player1); FillResult(result2, result.Player2);
        }

        private static void FillResult(Text label, VersusPlayerResult player)
        {
            label.text = "SCORE  " + player.Score + "       HP  " + player.RemainingHp + " / 3\n" +
                "Highest rank  " + player.HighestRank + " · " + RankName(player.HighestRank) + "\n" +
                "Best streak  " + player.MaxCombo + "       Food missed  " + player.FoodMissed + "\n" +
                "Food caught  " + player.FoodCaught + "       Rubbish eaten  " + player.TrashEaten;
            for (int i = 0; i < player.CaughtItems.Length; i++)
                label.transform.parent.Find("Count " + i).GetComponent<Text>().text = "× " + player.CaughtItems[i];
        }

        private static string RankName(ComboRank rank) => rank switch
        {
            ComboRank.C => "Cookin'", ComboRank.B => "Bold Bite", ComboRank.A => "Appetizing!",
            ComboRank.S => "Sizzling!", ComboRank.SS => "Super Savory!", ComboRank.SSS => "Supreme Chef!", _ => "Dull Dish"
        };

        private RectTransform Plate(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var image = Picture(parent, name, owner.Frame, position, size);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.2f;
            return image.rectTransform;
        }

        private Button ActionButton(Transform parent, string title, Vector2 position, Vector2 size, Action action, bool secondary = false)
        {
            var image = Picture(parent, title, secondary ? owner.CocoaButton : owner.OrangeButton, position, size);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = colors.selectedColor = Color.white;
            colors.highlightedColor = new Color(1, .93f, .8f);
            colors.pressedColor = new Color(.84f, .75f, .65f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            button.onClick.AddListener(() => { LocalDemoAudio.NotifyUserInteraction(); action(); });
            var label = Label(image.transform, "Label", title, Vector2.zero, size - new Vector2(40, 14), 32);
            label.color = secondary ? Cream : Cocoa;
            return button;
        }

        private Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize)
        {
            var label = Rect(parent, name, position, size).gameObject.AddComponent<Text>();
            label.font = owner.Font; label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Cocoa; label.text = value; label.supportRichText = false; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            return label;
        }

        public static Image Picture(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            var image = Rect(parent, name, position, size).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border.sqrMagnitude > 0 ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }
    }
}
