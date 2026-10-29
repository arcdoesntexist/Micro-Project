using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Editor
{
    public static class ProductionUISetup
    {
        private const string Prefabs = "Assets/CPS/Prefabs/";
        private static readonly Color Cocoa = new Color(.22f, .10f, .07f);
        private static readonly Color Brown = new Color(.38f, .20f, .12f);
        private static readonly Color Cream = new Color(.99f, .91f, .73f);
        private static readonly Color Light = new Color(1f, .97f, .85f);
        private static readonly Color Orange = new Color(.98f, .49f, .19f);

        [MenuItem("CPS/Author Production UI")]
        public static void Author()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "MainMenuCanvas.prefab") != null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "GameplayCanvas.prefab") != null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "ResultPanel.prefab") != null)
                throw new InvalidOperationException("Production UI prefabs already exist; authoring will not overwrite them.");
            BuildResultPrefab();
            BuildMenuPrefab();
            BuildGameplayPrefab();
            GlobalSettingsSetup.Apply();
            ReplaceMenuScene();
            ReplaceGameplayScene();
            AssetDatabase.SaveAssets();
        }

        private static void BuildMenuPrefab()
        {
            var root = CanvasRoot("MainMenuCanvas");
            try
            {
                var menu = root.AddComponent<MainMenuUI>();
                Set(menu, "navigator", root.AddComponent<SceneNavigator>());
                Set(menu, "gameplayTemplate", AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "GameplayRig.prefab").GetComponent<GameSessionController>());
                Panel(root.transform, "Kitchen backdrop", Vector2.zero, new Vector2(1920, 1080), new Color(.27f, .15f, .10f));
                Label(root.transform, "KITCHEN CATCH", new Vector2(0, 422), new Vector2(1200, 110), 78, Cream);
                Label(root.transform, "Catch food. Dodge rubbish. One minute to cook up a score.", new Vector2(0, 342), new Vector2(1400, 55), 30, Light);
                Panel(root.transform, "Profile and difficulty", new Vector2(-340, -90), new Vector2(890, 690), Cream);
                Panel(root.transform, "How to play", new Vector2(585, -90), new Vector2(560, 690), Cream);
                Label(root.transform, "CHOOSE YOUR PLAYER", new Vector2(-340, 205), new Vector2(760, 60), 38, Cocoa);
                Label(root.transform, "Nickname · 1–24 characters", new Vector2(-340, 148), new Vector2(720, 45), 26, Cocoa);
                var name = Input(root.transform, new Vector2(-340, 92), new Vector2(700, 72));
                Set(menu, "nicknameField", name);
                var create = Button(root.transform, "CREATE PROFILE", new Vector2(-340, 0), new Vector2(700, 68), 29);
                Set(menu, "createButton", create);
                Label(root.transform, "Or choose a local profile", new Vector2(-340, -67), new Vector2(700, 42), 26, Cocoa);
                var dropdown = Dropdown(root.transform, new Vector2(-340, -122), new Vector2(700, 68));
                Set(menu, "profileDropdown", dropdown);
                Label(root.transform, "DIFFICULTY", new Vector2(-340, -190), new Vector2(700, 50), 30, Cocoa);
                var easy = Button(root.transform, "EASY", new Vector2(-573, -250), new Vector2(215, 66), 27);
                var normal = Button(root.transform, "NORMAL", new Vector2(-340, -250), new Vector2(215, 66), 27);
                var hard = Button(root.transform, "HARD", new Vector2(-107, -250), new Vector2(215, 66), 27);
                Set(menu, "easyButton", easy); Set(menu, "normalButton", normal); Set(menu, "hardButton", hard);
                var start = Button(root.transform, "START COOKING", new Vector2(-340, -344), new Vector2(700, 80), 36);
                Set(menu, "startButton", start);
                Set(menu, "status", Label(root.transform, "Select or create a profile.", new Vector2(-340, -405), new Vector2(780, 50), 24, Cocoa));
                Label(root.transform, "MOVE", new Vector2(585, 202), new Vector2(470, 55), 36, Cocoa);
                Label(root.transform, "A / ←     D / →", new Vector2(585, 129), new Vector2(470, 70), 38, Cocoa);
                Label(root.transform, "Use left and right to catch Apple, Bread and Cake. Avoid the can and banana peel.",
                    new Vector2(585, -24), new Vector2(440, 200), 28, Cocoa);
                Label(root.transform, "3 HP  ·  60 SECONDS", new Vector2(585, -180), new Vector2(470, 60), 32, Cocoa);
                Label(root.transform, "Keyboard controls are simulated input. Physical force (N) is unavailable until a sensor is connected.",
                    new Vector2(585, -316), new Vector2(440, 165), 25, Cocoa);
                Chain(name, create, dropdown, easy, normal, hard, start);
                PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "MainMenuCanvas.prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void BuildGameplayPrefab()
        {
            var root = CanvasRoot("GameplayCanvas");
            try
            {
                var ui = root.AddComponent<GameplayUI>();
                var playerPanel = AnchoredPanel(root.transform, "Player", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -22), new Vector2(490, 130), Cream);
                Set(ui, "profile", Label(playerPanel.transform, "PLAYER", new Vector2(0, 28), new Vector2(450, 52), 37, Cocoa));
                Set(ui, "hp", Label(playerPanel.transform, "HP 3 / 3", new Vector2(0, -30), new Vector2(450, 45), 31, Cocoa));
                var timer = AnchoredPanel(root.transform, "Timer", new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -22), new Vector2(330, 105), Brown);
                Set(ui, "time", Label(timer.transform, "TIME 60", Vector2.zero, new Vector2(320, 95), 53, Light));
                var scorePanel = AnchoredPanel(root.transform, "Score", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -22), new Vector2(490, 130), Cream);
                Set(ui, "score", Label(scorePanel.transform, "SCORE 0", new Vector2(0, 28), new Vector2(460, 60), 40, Cocoa));
                Set(ui, "difficulty", Label(scorePanel.transform, "Easy", new Vector2(0, -30), new Vector2(460, 42), 29, Cocoa));
                var rankPanel = AnchoredPanel(root.transform, "Rank badge", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -170), new Vector2(195, 70), Orange);
                Set(ui, "rank", Label(rankPanel.transform, "RANK D", Vector2.zero, new Vector2(190, 65), 33, Cocoa));
                Set(ui, "pauseButton", AnchoredButton(root.transform, "PAUSE", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -258), new Vector2(195, 64), 27));
                var left = AnchoredPanel(root.transform, "Left input", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(28, 26), new Vector2(400, 95), Brown);
                Set(ui, "leftHand", Label(left.transform, "LEFT  0%", Vector2.zero, new Vector2(395, 90), 40, Light));
                var right = AnchoredPanel(root.transform, "Right input", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-28, 26), new Vector2(400, 95), Brown);
                Set(ui, "rightHand", Label(right.transform, "RIGHT  0%", Vector2.zero, new Vector2(395, 90), 40, Light));
                Set(ui, "inputSource", Label(root.transform, "Simulated Input · Keyboard", new Vector2(0, -505), new Vector2(620, 55), 27, Light));
                Set(ui, "status", Label(root.transform, "3", new Vector2(0, 120), new Vector2(500, 110), 72, Light));
                var callout = AnchoredPanel(root.transform, "Rank callout", new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-28, 150), new Vector2(370, 90), Orange);
                Set(ui, "rankCallout", Label(callout.transform, "", Vector2.zero, new Vector2(350, 82), 28, Cocoa));
                var pauseOverlay = Panel(root.transform, "Pause overlay", Vector2.zero, new Vector2(1920, 1080), new Color(.13f, .07f, .05f, .83f));
                Panel(pauseOverlay.transform, "Pause card", Vector2.zero, new Vector2(730, 360), Cream);
                Label(pauseOverlay.transform, "PAUSED", new Vector2(0, 65), new Vector2(650, 90), 62, Cocoa);
                Set(ui, "resumeButton", Button(pauseOverlay.transform, "RESUME", new Vector2(0, -95), new Vector2(410, 84), 38));
                Set(ui, "pauseOverlay", pauseOverlay);
                pauseOverlay.SetActive(false);
                var result = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "ResultPanel.prefab"), root.transform);
                result.name = "ResultPanel";
                Set(ui, "resultPanel", result.GetComponent<ResultPanel>());
                result.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "GameplayCanvas.prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void BuildResultPrefab()
        {
            var root = new GameObject("ResultPanel", typeof(RectTransform), typeof(Image), typeof(ResultPanel));
            try
            {
                var rect = root.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
                root.GetComponent<Image>().color = new Color(.12f, .06f, .04f, .84f);
                var panel = Panel(root.transform, "Results card", Vector2.zero, new Vector2(1280, 1000), Cream);
                var result = root.GetComponent<ResultPanel>();
                Set(result, "outcome", Label(panel.transform, "Triumph", new Vector2(0, 360), new Vector2(1100, 70), 56, Cocoa));
                Set(result, "identity", Label(panel.transform, "PLAYER", new Vector2(0, 292), new Vector2(1100, 54), 37, Cocoa));
                Set(result, "score", Label(panel.transform, "Score 0", new Vector2(0, 208), new Vector2(1100, 80), 55, Cocoa));
                Set(result, "roundStats", Label(panel.transform, "", new Vector2(0, 82), new Vector2(1110, 155), 27, Cocoa));
                var left = Panel(panel.transform, "Left statistics", new Vector2(-302, -165), new Vector2(530, 190), Light);
                var right = Panel(panel.transform, "Right statistics", new Vector2(302, -165), new Vector2(530, 190), Light);
                Set(result, "leftStats", Label(left.transform, "", Vector2.zero, new Vector2(500, 170), 28, Cocoa));
                Set(result, "rightStats", Label(right.transform, "", Vector2.zero, new Vector2(500, 170), 28, Cocoa));
                Set(result, "balance", Label(panel.transform, "", new Vector2(0, -290), new Vector2(1130, 55), 26, Cocoa));
                Set(result, "forceStatus", Label(panel.transform, "", new Vector2(0, -348), new Vector2(1130, 65), 25, Cocoa));
                Set(result, "restartButton", Button(panel.transform, "RESTART", new Vector2(-250, -450), new Vector2(390, 72), 32));
                Set(result, "menuButton", Button(panel.transform, "MAIN MENU", new Vector2(250, -450), new Vector2(390, 72), 32));
                PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "ResultPanel.prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void ReplaceMenuScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/CPS/Scenes/MainMenu.unity", OpenSceneMode.Single);
            if (Find<MainMenuUI>(scene) != null) throw new InvalidOperationException("MainMenu already has production UI.");
            var camera = Find<GameplayViewport>(scene).GetComponent<Camera>();
            var oldCanvas = Find<Canvas>(scene);
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas.gameObject);
            foreach (var old in scene.GetRootGameObjects())
                if (old.name == "PrototypeMenu") { Object.DestroyImmediate(old); break; }
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "MainMenuCanvas.prefab"), scene);
            var canvas = root.GetComponent<Canvas>(); canvas.worldCamera = camera;
            Set(camera.GetComponent<GameplayViewport>(), "canvas", canvas);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ReplaceGameplayScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/CPS/Scenes/Gameplay.unity", OpenSceneMode.Single);
            if (Find<GameplayUI>(scene) != null) throw new InvalidOperationException("Gameplay already has production UI.");
            var camera = Find<GameplayViewport>(scene).GetComponent<Camera>();
            var controller = Find<GameSessionController>(scene);
            var oldCanvas = Find<Canvas>(scene);
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas.gameObject);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "GameplayCanvas.prefab"), scene);
            var canvas = root.GetComponent<Canvas>(); canvas.worldCamera = camera;
            Set(camera.GetComponent<GameplayViewport>(), "canvas", canvas);
            Set(root.GetComponent<GameplayUI>(), "controller", controller);
            EditorSceneManager.SaveScene(scene);
        }

        private static GameObject CanvasRoot(string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.planeDistance = 1;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            return root;
        }

        private static GameObject Panel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = Rect(parent, name, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, size);
            go.AddComponent<Image>().color = color;
            return go;
        }

        private static GameObject AnchoredPanel(Transform parent, string name, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size, Color color)
        {
            var go = Rect(parent, name, min, max, pivot, position, size);
            go.AddComponent<Image>().color = color;
            return go;
        }

        private static GameObject Rect(Transform parent, string name, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max;
            rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
            return go;
        }

        private static Text Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var go = Rect(parent, text, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, size);
            var label = go.AddComponent<Text>(); label.text = text; label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize; label.color = color; label.alignment = TextAnchor.MiddleCenter;
            label.supportRichText = false; label.raycastTarget = false;
            return label;
        }

        private static Button Button(Transform parent, string caption, Vector2 position, Vector2 size, int fontSize)
        {
            var go = Panel(parent, caption, position, size, Orange);
            var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>();
            button.colors = new ColorBlock { normalColor = Color.white, highlightedColor = new Color(1f, .86f, .64f),
                pressedColor = new Color(.84f, .57f, .35f), selectedColor = new Color(1f, .83f, .48f),
                disabledColor = new Color(.50f, .46f, .43f), colorMultiplier = 1, fadeDuration = .08f };
            Label(go.transform, caption, Vector2.zero, size - new Vector2(12, 8), fontSize, Cocoa);
            return button;
        }

        private static Button AnchoredButton(Transform parent, string caption, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size, int fontSize)
        {
            var go = AnchoredPanel(parent, caption, min, max, pivot, position, size, Orange);
            var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>();
            Label(go.transform, caption, Vector2.zero, size, fontSize, Cocoa);
            return button;
        }

        private static InputField Input(Transform parent, Vector2 position, Vector2 size)
        {
            var go = Panel(parent, "Nickname input", position, size, Light);
            var input = go.AddComponent<InputField>(); input.targetGraphic = go.GetComponent<Image>();
            input.textComponent = Label(go.transform, "", Vector2.zero, size - new Vector2(36, 8), 31, Cocoa);
            input.placeholder = Label(go.transform, "Enter nickname", Vector2.zero, size - new Vector2(36, 8), 30, Brown);
            input.characterLimit = 0;
            return input;
        }

        private static Dropdown Dropdown(Transform parent, Vector2 position, Vector2 size)
        {
            var go = Panel(parent, "Profile dropdown", position, size, Light);
            var dropdown = go.AddComponent<Dropdown>(); dropdown.targetGraphic = go.GetComponent<Image>();
            dropdown.captionText = Label(go.transform, "Choose profile", Vector2.zero, size - new Vector2(30, 8), 29, Cocoa);
            var template = Rect(go.transform, "Template", new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 1), new Vector2(0, -size.y), new Vector2(0, 330));
            template.AddComponent<Image>().color = Cream;
            var scroll = template.AddComponent<ScrollRect>(); scroll.horizontal = false;
            var viewport = Rect(template.transform, "Viewport", Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            viewport.AddComponent<Image>().color = Light; viewport.AddComponent<Mask>().showMaskGraphic = false;
            var content = Rect(viewport.transform, "Content", new Vector2(0, 1), Vector2.one, new Vector2(.5f, 1), Vector2.zero, Vector2.zero);
            var layout = content.AddComponent<VerticalLayoutGroup>(); layout.childControlHeight = false;
            layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.spacing = 2;
            var fitter = content.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var item = Rect(content.transform, "Item", new Vector2(0, 1), Vector2.one, new Vector2(.5f, 1), Vector2.zero, new Vector2(0, 60));
            item.AddComponent<Image>().color = Light;
            var toggle = item.AddComponent<Toggle>(); toggle.targetGraphic = item.GetComponent<Image>();
            var itemLabel = Label(item.transform, "Profile", Vector2.zero, new Vector2(620, 56), 27, Cocoa);
            dropdown.itemText = itemLabel; dropdown.template = template.GetComponent<RectTransform>();
            scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.content = content.GetComponent<RectTransform>();
            template.SetActive(false);
            return dropdown;
        }

        private static void Chain(params Selectable[] controls)
        {
            for (int i = 0; i < controls.Length; i++)
            {
                var nav = controls[i].navigation; nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = controls[(i - 1 + controls.Length) % controls.Length];
                nav.selectOnDown = controls[(i + 1) % controls.Length];
                controls[i].navigation = nav;
            }
        }

        private static T Find<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }

        private static void Set(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target); serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
