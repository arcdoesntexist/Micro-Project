using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Editor
{
    public static class ReferenceHomeSetup
    {
        private const string Atlas = "Assets/CPS/Art/UI/home-ui-atlas-v1.png";
        private const string Prefab = "Assets/CPS/Prefabs/MainMenuCanvas.prefab";
        private static readonly Color Cocoa = new Color(.25f, .12f, .07f);
        private static readonly Color Cream = new Color(1f, .96f, .85f);
        private static Font Font => AssetDatabase.LoadAssetAtPath<Font>("Assets/CPS/Fonts/FFSpokenTrial-Regular.ttf");
        private static Sprite Art(string name) => AssetDatabase.LoadAllAssetsAtPath(Atlas).OfType<Sprite>().First(s => s.name == name);

        [MenuItem("CPS/Apply Reference Home")]
        public static void Apply()
        {
            ImportAtlas();
            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var menu = new SerializedObject(root.GetComponent<MainMenuUI>());
                Layout(root.transform, menu);
                menu.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }

        private static void ImportAtlas()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Atlas);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.spritePixelsPerUnit = 100;
            importer.SaveAndReimport();
            var slices = new[] {
                Slice("Plaque", 28, 146, 409, 228, 0),
                Slice("CreamPanel", 444, 86, 365, 346, 55),
                Slice("OrangeButton", 823, 207, 407, 145, 58),
                Slice("CocoaButton", 34, 579, 400, 142, 52),
                Slice("Input", 445, 575, 414, 148, 48),
                Slice("Ivy", 917, 524, 246, 251, 0),
                Slice("Star", 87, 868, 294, 273, 0),
                Slice("Gear", 473, 854, 311, 310, 0),
                Slice("Pencil", 906, 849, 274, 308, 0)
            };
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existing = provider.GetSpriteRects().ToDictionary(sprite => sprite.name);
            foreach (var slice in slices)
                slice.spriteID = existing.TryGetValue(slice.name, out var old) ? old.spriteID : GUID.Generate();
            provider.SetSpriteRects(slices);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(
                slices.Select(slice => new SpriteNameFileIdPair(slice.name, slice.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static SpriteRect Slice(string name, int x, int top, int w, int h, int border)
        {
            return new SpriteRect { name = name, rect = new Rect(x, 1254 - top - h, w, h),
                alignment = SpriteAlignment.Center, pivot = new Vector2(.5f, .5f),
                border = new Vector4(border, border, border, border) };
        }

        private static T Ref<T>(SerializedObject target, string field) where T : Object =>
            (T)target.FindProperty(field).objectReferenceValue;
        private static void Bind(SerializedObject target, string field, Object value) =>
            target.FindProperty(field).objectReferenceValue = value;

        private static void Layout(Transform root, SerializedObject menu)
        {
            var board = root.Find("Profile and difficulty");
            Place(board, 110, 210, 660, 610); Skin(board.GetComponent<Image>(), "CreamPanel");
            foreach (var label in root.GetComponentsInChildren<Text>(true))
                if (label.transform.parent == root && (label.text.StartsWith("Or choose") || label.text.StartsWith("NEW PLAYER") ||
                    label.text.StartsWith("Nickname") || label.text.StartsWith("Keyboard controls") || label.text == "MOVE"))
                    label.gameObject.SetActive(false);
            var title = root.Find("KITCHEN CATCH").GetComponent<Text>();
            title.text = "KITCHEN\nCATCH"; title.fontSize = 60; title.color = Cocoa;
            title.lineSpacing = .85f; title.verticalOverflow = VerticalWrapMode.Overflow;
            Place(title.transform, 210, 52, 460, 130);
            foreach (var label in root.Cast<Transform>().Select(t => t.GetComponent<Text>()).Where(t => t != null))
                label.gameObject.SetActive(label == title || label.name == "CHOOSE YOUR PLAYER" || label.name == "DIFFICULTY" || label == Ref<Text>(menu, "status"));
            var plaque = Ensure(root, "Home plaque");
            Place(plaque, 185, 38, 510, 154); Skin(Image(plaque), "Plaque", false);
            plaque.SetSiblingIndex(title.transform.GetSiblingIndex());
            title.transform.SetSiblingIndex(plaque.GetSiblingIndex() + 1);
            Decor(root, "Logo ivy", 165, 42, 78, 85);
            Decor(root, "Board ivy", 714, 206, 75, 83);
            var heading = root.Find("CHOOSE YOUR PLAYER").GetComponent<Text>();
            Place(heading.transform, 160, 255, 550, 48); Style(heading, 38);
            var dropdown = Ref<Dropdown>(menu, "profileDropdown");
            Place(dropdown.transform, 160, 320, 466, 70); Skin(dropdown.GetComponent<Image>(), "Input");
            dropdown.GetComponent<Image>().raycastTarget = true;
            Style(dropdown.captionText, 34); Style(dropdown.itemText, 30);
            ProfileDropdownSetup.Configure(dropdown);
            var edit = Ref<Button>(menu, "manageProfilesButton");
            Place(edit.transform, 638, 320, 72, 70); SkinButton(edit, "Input", "", 28);
            var pencil = Ensure(edit.transform, "Pencil icon"); Local(pencil, 0, 0, 43, 48);
            Skin(Image(pencil), "Pencil", false);
            Ref<Button>(menu, "createButton").name = "Confirm profile";
            var open = EnsureButton(root, "CREATE PROFILE", "CREATE PROFILE");
            Place(open.transform, 160, 411, 550, 56); SkinButton(open, "Input", "CREATE PROFILE", 32);
            Bind(menu, "openCreateButton", open);
            var difficulty = root.Find("DIFFICULTY").GetComponent<Text>();
            Place(difficulty.transform, 160, 492, 550, 44); Style(difficulty, 34);
            string[] fields = { "easyButton", "normalButton", "hardButton" };
            string[] names = { "EASY", "NORMAL", "HARD" };
            Color[] colors = { new Color(.55f,.78f,.36f), new Color(1f,.83f,.35f), new Color(1f,.54f,.34f) };
            for (int i = 0; i < 3; i++)
            {
                var button = Ref<Button>(menu, fields[i]);
                Place(button.transform, 160 + i * 188, 552, 174, 96);
                SkinButton(button, "CocoaButton", names[i], 28, true);
                var label = button.GetComponentInChildren<Text>(true);
                Local(label.transform, 0, -24, 190, 38);
                for (int j = 0; j <= i; j++)
                {
                    var star = Ensure(button.transform, "Star " + j);
                    Local(star, (j - i * .5f) * 39, 24, 38, 38);
                    Skin(Image(star), "Star", false); star.GetComponent<Image>().color = colors[i];
                }
                var selected = Ensure(button.transform, "Selected");
                Local(selected, 0, -63, 150, 5); Image(selected).color = new Color(.34f,.51f,.25f);
            }
            var start = Ref<Button>(menu, "startButton");
            Place(start.transform, 160, 688, 550, 83); SkinButton(start, "OrangeButton", "START COOKING", 43);
            var status = Ref<Text>(menu, "status");
            Place(status.transform, 160, 654, 550, 28); Style(status, 23);
            var settings = Ref<Button>(menu, "settingsButton");
            Place(settings.transform, 1480, 42, 76, 76); SkinButton(settings, "Gear", "", 24);
            settings.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Simple;
            settings.GetComponent<Image>().preserveAspect = true;
            BuildCreation(root, menu);
            foreach (var control in new Selectable[] { dropdown, edit, open, start, settings,
                Ref<Button>(menu, "easyButton"), Ref<Button>(menu, "normalButton"), Ref<Button>(menu, "hardButton") })
                control.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            ArrangePractice(root, menu);
            var manager = Ref<GameObject>(menu, "profileManagerPanel");
            Place(manager.transform, 850, 200, 600, 625); Skin(manager.GetComponent<Image>(), "CreamPanel");
            var rules = root.Find("Game rules overlay");
            Skin(rules.Find("Rules card").GetComponent<Image>(), "CreamPanel");
            foreach (var button in rules.GetComponentsInChildren<Button>(true))
                SkinButton(button, "OrangeButton", button.GetComponentInChildren<Text>().text, 28);
            foreach (var button in manager.GetComponentsInChildren<Button>(true))
                SkinButton(button, "Input", button.GetComponentInChildren<Text>(true).text, 24);
            Skin(Ref<InputField>(menu, "renameField").GetComponent<Image>(), "Input");
            Ref<InputField>(menu, "renameField").GetComponent<Image>().raycastTarget = true;
            var panel = Ref<AudioSettingsPanel>(menu, "settingsPanel");
            var card = panel.transform.Find("Settings card");
            if (card != null) Skin(card.GetComponent<Image>(), "CreamPanel");
            foreach (var button in panel.GetComponentsInChildren<Button>(true))
                SkinButton(button, "OrangeButton", button.GetComponentInChildren<Text>(true).text, 30);
            var volume = panel.GetComponentInChildren<Slider>(true);
            if (volume != null)
            {
                Skin(volume.GetComponent<Image>(), "CocoaButton");
                volume.GetComponent<Image>().raycastTarget = true;
                Skin(volume.handleRect.GetComponent<Image>(), "OrangeButton");
                volume.handleRect.GetComponent<Image>().raycastTarget = true;
            }
            rules.SetAsLastSibling();
            Ref<GameObject>(menu, "createProfilePanel").transform.SetAsLastSibling();
            panel.transform.SetAsLastSibling();
        }

        private static void ArrangePractice(Transform root, SerializedObject menu)
        {
            var practice = root.Find("Approved practice");
            Local(practice, 0, 0, 1920, 1080); practice.GetComponent<Image>().enabled = false;
            var heading = practice.GetComponentsInChildren<Text>(true).First(t => t.text == "A LITTLE PRACTICE?");
            Place(heading.transform, 880, 207, 340, 46); Style(heading, 31);
            var rules = practice.Find("GAME RULES").GetComponent<Button>();
            Place(rules.transform, 1240, 208, 190, 48); SkinButton(rules, "Input", "GAME RULES", 26);
            var tutorial = Ensure(practice, "Practice header"); Place(tutorial, 855, 190, 600, 170);
            Skin(Image(tutorial), "CreamPanel"); tutorial.SetAsFirstSibling();
            var left = practice.Cast<Transform>().First(t => t.name == "A / ←");
            var right = practice.Cast<Transform>().First(t => t.name == "D / →");
            Place(left, 885, 277, 135, 56); Skin(left.GetComponent<Image>(), "Input");
            Place(right, 1035, 277, 135, 56); Skin(right.GetComponent<Image>(), "Input");
            foreach (var key in new[] {left, right})
            {
                var label = key.GetComponentInChildren<Text>(); Local(label.transform, 0, 0, 155, 58); Style(label, 28);
            }
            var move = practice.GetComponentsInChildren<Text>(true).First(t => t.text == "Move left and right");
            Place(move.transform, 1190, 277, 235, 56); Style(move, 26);
            var hint = practice.Find("Catch hint"); Place(hint, 870, 390, 580, 100);
            Skin(hint.GetComponent<Image>(), "Input");
            var outline = hint.GetComponent<Outline>(); if (outline != null) Object.DestroyImmediate(outline);
            var hintText = hint.GetComponentInChildren<Text>(); Local(hintText.transform, 0, 0, 650, 100); Style(hintText, 30);
            var track = practice.Find("Practice track"); Place(track, 805, 550, 600, 230);
            var mascot = track.Find("Practice mascot"); Local(mascot, 0, -12, 216, 216);
            Local(mascot.Find("Body"), 0, 0, 216, 216);
            Local(mascot.Find("Hat"), -17, 65, 156, 120);
            var behavior = new SerializedObject(root.GetComponent<MenuPracticeUI>());
            behavior.FindProperty("leftLimit").floatValue = -250;
            behavior.FindProperty("rightLimit").floatValue = 250;
            var blockers = behavior.FindProperty("inputBlockers"); blockers.arraySize = 3;
            blockers.GetArrayElementAtIndex(0).objectReferenceValue = Ref<GameObject>(menu, "profileManagerPanel");
            blockers.GetArrayElementAtIndex(1).objectReferenceValue = Ref<GameObject>(menu, "createProfilePanel");
            blockers.GetArrayElementAtIndex(2).objectReferenceValue = Ref<AudioSettingsPanel>(menu, "settingsPanel").gameObject;
            behavior.ApplyModifiedPropertiesWithoutUndo();
            Bind(menu, "howToPlayPanel", practice.gameObject);
        }

        private static void BuildCreation(Transform root, SerializedObject menu)
        {
            var overlay = Ensure(root, "Create profile overlay"); Local(overlay, 0, 0, 1920, 1080);
            var shade = Image(overlay); shade.color = new Color(.10f,.05f,.03f,.78f); shade.raycastTarget = true;
            var card = Ensure(overlay, "Create card"); Local(card, 0, 0, 780, 450); Skin(Image(card), "CreamPanel");
            var title = EnsureLabel(card, "Create title", "CREATE PROFILE", 42); Local(title.transform, 0, 140, 660, 65);
            var input = Ref<InputField>(menu, "nicknameField"); input.transform.SetParent(card, false);
            Local(input.transform, 0, 35, 640, 78); Skin(input.GetComponent<Image>(), "Input"); Style(input.textComponent, 34);
            input.GetComponent<Image>().raycastTarget = true;
            if (input.placeholder is Text placeholder) { placeholder.text = "Enter nickname..."; Style(placeholder, 30); }
            var validation = EnsureLabel(card, "Creation feedback", "Nickname · 1–24 characters", 25);
            Local(validation.transform, 0, -33, 640, 55);
            var confirm = Ref<Button>(menu, "createButton"); confirm.transform.SetParent(card, false);
            Local(confirm.transform, 165, -125, 290, 72); SkinButton(confirm, "OrangeButton", "CREATE", 32);
            var cancel = EnsureButton(card, "Cancel creation", "CANCEL"); Local(cancel.transform, -165, -125, 290, 72);
            SkinButton(cancel, "Input", "CANCEL", 32);
            Bind(menu, "createProfilePanel", overlay.gameObject); Bind(menu, "cancelCreateButton", cancel); Bind(menu, "createStatus", validation);
            input.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = cancel, selectOnDown = confirm };
            confirm.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = input,
                selectOnDown = cancel, selectOnLeft = cancel, selectOnRight = cancel };
            cancel.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = input,
                selectOnDown = input, selectOnLeft = confirm, selectOnRight = confirm };
            overlay.gameObject.SetActive(false);
        }

        private static void Decor(Transform parent, string name, float x, float y, float w, float h)
        { var leaf = Ensure(parent, name); Place(leaf, x, y, w, h); Skin(Image(leaf), "Ivy", false); }
        private static RectTransform Ensure(Transform parent, string name)
        {
            var found = parent.Find(name) as RectTransform;
            if (found != null) return found;
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return (RectTransform)go.transform;
        }
        private static Image Image(Transform target) => target.GetComponent<Image>() ?? target.gameObject.AddComponent<Image>();
        private static void Skin(Image image, string sprite, bool sliced = true)
        {
            image.sprite = Art(sprite); image.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.color = Color.white; image.pixelsPerUnitMultiplier = 2; image.raycastTarget = false;
        }
        private static Text EnsureLabel(Transform parent, string name, string caption, int size)
        {
            var rect = Ensure(parent, name); var text = rect.GetComponent<Text>() ?? rect.gameObject.AddComponent<Text>();
            text.text = caption; Style(text, size); return text;
        }
        private static void Style(Text text, int size)
        { text.font = Font; text.fontSize = size; text.color = Cocoa; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false; text.supportRichText = false; }
        private static Button EnsureButton(Transform parent, string name, string caption)
        {
            var rect = Ensure(parent, name); var image = Image(rect);
            var button = rect.GetComponent<Button>() ?? rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            EnsureLabel(rect, "Label", caption, 30); return button;
        }
        private static void SkinButton(Button button, string sprite, string caption, int size, bool light = false)
        {
            Skin(button.GetComponent<Image>(), sprite); button.GetComponent<Image>().raycastTarget = true;
            var label = button.GetComponentInChildren<Text>(true); label.text = caption; Style(label, size);
            foreach (var extra in button.GetComponentsInChildren<Text>(true).Where(t => t != label))
                Object.DestroyImmediate(extra.gameObject);
            if (light) label.color = Cream;
            var rect = label.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f,.5f); rect.offsetMin = new Vector2(12,4); rect.offsetMax = new Vector2(-12,-4);
            var colors = ColorBlock.defaultColorBlock; colors.highlightedColor = new Color(1f,.93f,.75f);
            colors.selectedColor = new Color(.75f,.9f,1f); colors.pressedColor = new Color(.77f,.65f,.50f);
            colors.disabledColor = new Color(.6f,.6f,.6f,.65f); button.colors = colors;
        }
        private static void Place(Transform t, float x, float y, float w, float h) =>
            Local(t, (x + w / 2 - 800) * 1.2f, (450 - y - h / 2) * 1.2f, w * 1.2f, h * 1.2f);
        private static void Local(Transform target, float x, float y, float w, float h)
        {
            var rect = (RectTransform)target; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
            rect.anchoredPosition = new Vector2(x,y); rect.sizeDelta = new Vector2(w,h); rect.localScale = Vector3.one;
        }
    }
}
