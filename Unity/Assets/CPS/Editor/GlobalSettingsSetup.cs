using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Editor
{
    public static class GlobalSettingsSetup
    {
        private const string MixerPath = "Assets/CPS/Resources/Audio/KitchenAudioMixer.mixer";
        private const string FontPath = "Assets/CPS/Fonts/FFSpokenTrial-Regular.ttf";
        private const string SettingsPath = "Assets/CPS/Prefabs/AudioSettingsPanel.prefab";
        private static readonly Color Cream = new Color(.96f, .84f, .65f);
        private static readonly Color Cocoa = new Color(.22f, .10f, .06f);
        private static readonly Color Orange = new Color(.96f, .39f, .10f);

        [MenuItem("CPS/Apply Global Settings UI")]
        public static void Apply()
        {
            EnsureMixer();
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null) throw new InvalidOperationException("FF Spoken Trial font is missing: " + FontPath);
            var settings = BuildSettingsPrefab(font);
            UpdateMenu(font, settings);
            UpdateGameplay(font, settings);
            UpdateFont("Assets/CPS/Prefabs/ResultPanel.prefab", font);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureMixer()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (existing != null && existing.FindMatchingGroups("Music").Length == 1 &&
                existing.FindMatchingGroups("SFX").Length == 1) return;
            if (existing != null) AssetDatabase.DeleteAsset(MixerPath);
            EnsureFolder("Assets/CPS/Resources", "Audio");
            var assembly = typeof(AudioMixer).Assembly;
            var editorAssembly = typeof(AssetDatabase).Assembly;
            var controllerType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerController");
            var controller = controllerType.GetMethod("CreateMixerControllerAtPath", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(null, new object[] { MixerPath });
            var master = controllerType.GetProperty("masterGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(controller);
            var groupType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerGroupController");
            var create = controllerType.GetMethod("CreateNewGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var add = controllerType.GetMethod("AddChildToParent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (string name in new[] { "Music", "SFX" })
            {
                var group = create.Invoke(controller, new object[] { name, true });
                add.Invoke(controller, new[] { group, master });
            }
            var volume = groupType.GetMethod("GetGUIDForVolume", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(master, null);
            var exposedType = editorAssembly.GetType("UnityEditor.Audio.ExposedAudioParameter");
            var exposed = Activator.CreateInstance(exposedType);
            exposedType.GetField("guid").SetValue(exposed, volume);
            exposedType.GetField("name").SetValue(exposed, "MasterVolume");
            var array = Array.CreateInstance(exposedType, 1);
            array.SetValue(exposed, 0);
            controllerType.GetProperty("exposedParameters", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SetValue(controller, array);
            AssetDatabase.SaveAssets();
        }

        private static GameObject BuildSettingsPrefab(Font font)
        {
            var root = Rect(null, "Settings panel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.AddComponent<Image>().color = new Color(.08f, .04f, .03f, .88f);
            var panel = root.AddComponent<AudioSettingsPanel>();
            try
            {
                var card = Rect(root.transform, "Settings card", new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(720, 560));
                card.AddComponent<Image>().color = Cream;
                Label(card.transform, "SETTINGS", new Vector2(0, 190), new Vector2(620, 75), 54, font);
                Label(card.transform, "MASTER VOLUME", new Vector2(0, 95), new Vector2(520, 45), 30, font);
                var slider = Slider(card.transform, new Vector2(-45, 35), new Vector2(470, 48));
                var value = Label(card.transform, "100", new Vector2(255, 35), new Vector2(100, 48), 30, font);
                var toggle = Toggle(card.transform, "BACKGROUND MUSIC", new Vector2(0, -60), new Vector2(520, 58), 28, font);
                var close = Button(card.transform, "CLOSE", new Vector2(0, -175), new Vector2(360, 72), 32, font);
                Set(panel, "volumeSlider", slider);
                Set(panel, "backgroundMusicToggle", toggle);
                Set(panel, "volumeValue", value);
                Set(panel, "closeButton", close);
                Chain(slider, toggle, close);
                return PrefabUtility.SaveAsPrefabAsset(root, SettingsPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void UpdateMenu(Font font, GameObject settingsPrefab)
        {
            const string path = "Assets/CPS/Prefabs/MainMenuCanvas.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Remove(root.transform, "SETTINGS");
                Remove(root.transform, "Settings panel");
                Remove(root.transform, "AudioSettingsPanel");
                var button = AnchoredButton(root.transform, "SETTINGS", new Vector2(1, 1), new Vector2(1, 1), Vector2.one,
                    new Vector2(-28, -24), new Vector2(220, 64), 27, font);
                var panel = (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab, root.transform);
                panel.name = "Settings panel";
                panel.SetActive(false);
                var menu = root.GetComponent<MainMenuUI>();
                Set(menu, "settingsButton", button);
                Set(menu, "settingsPanel", panel.GetComponent<AudioSettingsPanel>());
                var menuData = new SerializedObject(menu);
                var name = (InputField)menuData.FindProperty("nicknameField").objectReferenceValue;
                var start = (Button)menuData.FindProperty("startButton").objectReferenceValue;
                Link(start, start.navigation.selectOnUp, button);
                Link(button, start, name);
                Link(name, button, name.navigation.selectOnDown);
                ApplyFont(root, menu, font);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void UpdateGameplay(Font font, GameObject settingsPrefab)
        {
            const string path = "Assets/CPS/Prefabs/GameplayCanvas.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Remove(root.transform, "Settings panel");
                Remove(root.transform, "AudioSettingsPanel");
                var card = root.transform.Find("Pause overlay/Pause card").GetComponent<RectTransform>();
                card.sizeDelta = new Vector2(730, 570);
                var resume = root.transform.Find("Pause overlay/RESUME").GetComponent<Button>();
                resume.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 70);
                Remove(root.transform, "Pause overlay/SETTINGS");
                Remove(root.transform, "Pause overlay/MAIN MENU");
                var settingsButton = Button(card.parent, "SETTINGS", new Vector2(0, -55), new Vector2(410, 84), 34, font);
                var menuButton = Button(card.parent, "MAIN MENU", new Vector2(0, -180), new Vector2(410, 84), 32, font);
                var panel = (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab, root.transform);
                panel.name = "Settings panel";
                panel.SetActive(false);
                var ui = root.GetComponent<GameplayUI>();
                Set(ui, "settingsButton", settingsButton);
                Set(ui, "backToMenuButton", menuButton);
                Set(ui, "settingsPanel", panel.GetComponent<AudioSettingsPanel>());
                Chain(resume, settingsButton, menuButton);
                ApplyFont(root, ui, font);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void UpdateFont(string path, Font font)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var label in root.GetComponentsInChildren<Text>(true)) label.font = font;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ApplyFont(GameObject root, Object owner, Font font)
        {
            foreach (var label in root.GetComponentsInChildren<Text>(true)) label.font = font;
            var serialized = new SerializedObject(owner);
            var property = serialized.FindProperty("licensedFont");
            if (property != null) property.objectReferenceValue = font;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Rect(Transform parent, string name, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max;
            rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size;
            return go;
        }

        private static Text Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize, Font font)
        {
            var go = Rect(parent, text, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, size);
            var label = go.AddComponent<Text>(); label.text = text; label.font = font; label.fontSize = fontSize;
            label.color = Cocoa; label.alignment = TextAnchor.MiddleCenter; label.supportRichText = false; label.raycastTarget = false;
            return label;
        }

        private static Button Button(Transform parent, string text, Vector2 position, Vector2 size, int fontSize, Font font)
        {
            var go = Rect(parent, text, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, size);
            var image = go.AddComponent<Image>(); image.color = Orange;
            var button = go.AddComponent<Button>(); button.targetGraphic = image;
            Label(go.transform, text, Vector2.zero, size - new Vector2(10, 8), fontSize, font);
            return button;
        }

        private static Button AnchoredButton(Transform parent, string text, Vector2 min, Vector2 max, Vector2 pivot,
            Vector2 position, Vector2 size, int fontSize, Font font)
        {
            var button = Button(parent, text, position, size, fontSize, font);
            var rect = button.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot;
            return button;
        }

        private static Slider Slider(Transform parent, Vector2 position, Vector2 size)
        {
            var go = Rect(parent, "Volume", new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, size);
            var background = go.AddComponent<Image>(); background.color = new Color(.35f, .22f, .16f);
            var slider = go.AddComponent<Slider>(); slider.targetGraphic = background;
            var handle = Rect(go.transform, "Handle", new Vector2(0, .5f), new Vector2(0, .5f), Vector2.zero, new Vector2(34, 64));
            handle.AddComponent<Image>().color = Orange;
            slider.handleRect = handle.GetComponent<RectTransform>(); slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            return slider;
        }

        private static Toggle Toggle(Transform parent, string text, Vector2 position, Vector2 size, int fontSize, Font font)
        {
            var go = Rect(parent, text, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, size);
            var box = Rect(go.transform, "Box", new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(25, 0), new Vector2(48, 48));
            var background = box.AddComponent<Image>(); background.color = Color.white;
            var check = Rect(box.transform, "Checkmark", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-12, -12));
            var mark = check.AddComponent<Image>(); mark.color = Orange;
            var toggle = go.AddComponent<Toggle>(); toggle.targetGraphic = background; toggle.graphic = mark;
            var label = Label(go.transform, text, new Vector2(70, 0), new Vector2(size.x - 100, size.y), fontSize, font);
            label.alignment = TextAnchor.MiddleLeft;
            return toggle;
        }

        private static void Remove(Transform root, string path)
        {
            Transform child;
            while ((child = root.Find(path)) != null) Object.DestroyImmediate(child.gameObject);
        }

        private static void Chain(params Selectable[] controls)
        {
            for (int i = 0; i < controls.Length; i++)
                Link(controls[i], controls[(i - 1 + controls.Length) % controls.Length], controls[(i + 1) % controls.Length]);
        }

        private static void Link(Selectable control, Selectable up, Selectable down)
        {
            var navigation = control.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = up;
            navigation.selectOnDown = down;
            control.navigation = navigation;
        }

        private static void Set(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
