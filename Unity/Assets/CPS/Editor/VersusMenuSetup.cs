using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Editor
{
    public static class VersusMenuSetup
    {
        private const string Scenes = "Assets/CPS/Scenes/";
        private const string UiAtlas = "Assets/CPS/Art/UI/home-ui-atlas-v1.png";
        private const string BodyAtlas = "Assets/CPS/Art/Mascot/mascot-body-atlas-v1.png";
        private const string HatRight = "Assets/CPS/Art/Mascot/engineer-hat-v1.png";
        private const string HatLeft = "Assets/CPS/Art/Mascot/engineer-hat-left-v1.png";
        private const string Kitchen = "Assets/CPS/Art/Kitchen/kitchen-background-v1.png";
        private const string FontPath = "Assets/CPS/Fonts/FFSpokenTrial-Regular.ttf";
        private const string FramePath = "Assets/CPS/Art/UI/versus-frame-v2.png";
        private static Vector2 mascotBodySize;
        private static Vector2 mascotHatSize;
        private static Vector2 mascotHatOffset;
        private static readonly string[] HatNames = { "Orange", "Blue", "Green", "Purple", "Yellow", "Pink" };
        private static readonly string[] HatColors = { "#F57C32", "#3C78D8", "#68A832", "#9B59B6", "#E6B422", "#D94A64" };
        private static readonly Color Cocoa = Hex("#5B2A18");
        private static Font font;

        [MenuItem("CPS/Author Versus Menus")]
        public static void Author()
        {
            font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            MeasureGameplayMascot();
            CreateFrame();
            ImportHatVariants();
            CreateModeSelect();
            CreateVersusSetup();
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
        }

        private static void CreateModeSelect()
        {
            var scene = NewScene(out var canvas);
            AddBackground(canvas.transform);
            var navigator = new GameObject("ModeSelection").AddComponent<SceneNavigator>();
            var ui = navigator.gameObject.AddComponent<ModeSelectionUI>();
            Panel(canvas.transform, "Mode Panel", Pos(245, 205, 1110, 560), new Vector2(1332, 672));
            var plaque = Image(canvas.transform, "Kitchen Catch Plaque", "Plaque", Pos(545, 34, 510, 180), new Vector2(612, 216));
            Text(plaque.transform, "Title", "KITCHEN\nCATCH", 62, Vector2.zero, new Vector2(530, 170));
            Text(canvas.transform, "Prompt", "CHOOSE YOUR MODE", 47, Pos(480, 230, 640, 55), new Vector2(768, 66));
            Text(canvas.transform, "Hint", "Cook solo or challenge a friend", 27, Pos(480, 286, 640, 34), new Vector2(768, 41));
            var single = Button(canvas.transform, "Single Player", "", "OrangeButton", Pos(350, 350, 400, 185), new Vector2(480, 222));
            var versus = Button(canvas.transform, "Versus", "", "CocoaButton", Pos(850, 350, 400, 185), new Vector2(480, 222), true);
            ChoiceLabels(single.transform, "1P", "SINGLE PLAYER", "Classic 60-second kitchen", false);
            ChoiceLabels(versus.transform, "VS", "VERSUS", "Endless local survival", true);
            Mascot(canvas.transform, "Solo Mascot", Pos(460, 565, 180, 155), false);
            Mascot(canvas.transform, "Versus Mascot 1", Pos(830, 565, 155, 155), false);
            Mascot(canvas.transform, "Versus Mascot 2", Pos(1115, 565, 155, 155), true);
            var vs = Text(canvas.transform, "VS", "VS", 38, Pos(1007, 607, 85, 70), new Vector2(102, 84));
            vs.color = Hex("#9F4527");
            var footer = Text(canvas.transform, "Footer", "Both modes use the same profiles and global settings.", 25,
                Pos(480, 792, 640, 34), new Vector2(768, 41));
            footer.color = Hex("#FFF7D9");
            var settings = Button(canvas.transform, "Settings", "", "Gear", Pos(1472, 42, 76, 76), new Vector2(91, 91));
            settings.image.preserveAspect = true;
            var settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/AudioSettingsPanel.prefab");
            if (settingsPrefab != null)
            {
                var settingsPanel = (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab, canvas.transform);
                settingsPanel.SetActive(false);
                UnityEditor.Events.UnityEventTools.AddPersistentListener(settings.onClick, settingsPanel.GetComponent<AudioSettingsPanel>().Show);
            }
            Bind(ui, "navigator", navigator);
            Bind(ui, "singlePlayerButton", single);
            Bind(ui, "versusButton", versus);
            EditorSceneManager.SaveScene(scene, Scenes + "ModeSelect.unity");
        }

        private static void CreateVersusSetup()
        {
            var scene = NewScene(out var canvas);
            AddBackground(canvas.transform);
            var navigator = new GameObject("VersusSetup").AddComponent<SceneNavigator>();
            var ui = navigator.gameObject.AddComponent<VersusSetupUI>();
            var backplate = Panel(canvas.transform, "Title Backplate", Pos(390, 34, 820, 86), new Vector2(984, 103));
            backplate.GetComponent<Image>().pixelsPerUnitMultiplier = 3.2f;
            Text(canvas.transform, "Title", "VERSUS KITCHEN", 44, Pos(390, 42, 820, 41), new Vector2(984, 49));
            Text(canvas.transform, "Subtitle", "Choose two different profiles · test your controls · ready up", 27,
                Pos(390, 83, 820, 32), new Vector2(984, 38));
            var first = PlayerPanel(canvas.transform, 1, Pos(142, 150, 620, 626), Key.A, Key.D, "A / D", "#F57C32");
            var second = PlayerPanel(canvas.transform, 2, Pos(838, 150, 620, 626), Key.LeftArrow, Key.RightArrow, "Left / Right", "#3C78D8");
            var status = Text(canvas.transform, "Status", "", 23, Pos(400, 778, 800, 26), new Vector2(960, 31));
            var back = Button(canvas.transform, "Back", "BACK", "CocoaButton", Pos(500, 806, 270, 68), new Vector2(324, 82), true);
            var start = Button(canvas.transform, "Start", "START BATTLE", "OrangeButton", Pos(830, 806, 330, 68), new Vector2(396, 82));

            Bind(ui, "navigator", navigator);
            BindPlayer(ui, first, 1);
            BindPlayer(ui, second, 2);
            Bind(ui, "statusText", status);
            Bind(ui, "backButton", back);
            Bind(ui, "startButton", start);
            EditorSceneManager.SaveScene(scene, Scenes + "VersusSetup.unity");
        }

        private static PlayerWidgets PlayerPanel(Transform parent, int player, Vector2 position, Key left, Key right, string controls, string accent)
        {
            var panel = Panel(parent, "Player " + player + " Panel", position, new Vector2(744, 751.2f));
            var header = Text(panel.transform, "Header", "PLAYER " + player, 40, new Vector2(-135, 301), new Vector2(360, 67));
            header.alignment = TextAnchor.MiddleLeft;
            var keys = Text(panel.transform, "Control Keys", controls, 25, new Vector2(234, 301), new Vector2(160, 54));
            var dropdown = Dropdown(panel.transform, "Profile", new Vector2(-42, 206), new Vector2(544, 74));
            var edit = Button(panel.transform, "Edit Profile", "", "Input", new Vector2(280, 206), new Vector2(68, 68));
            var pencil = Image(edit.transform, "Pencil", "Pencil", Vector2.zero, new Vector2(43, 43));
            pencil.preserveAspect = true; pencil.raycastTarget = false;
            var input = Input(panel.transform, "Nickname", "New nickname...", new Vector2(-112, 116), new Vector2(405, 65));
            var create = Button(panel.transform, "Create", "CREATE", "OrangeButton", new Vector2(211, 116), new Vector2(207, 65));
            var colorsLabel = Text(panel.transform, "Colors Label", "Custom colors", 24, new Vector2(0, 45), new Vector2(600, 34));
            colorsLabel.alignment = TextAnchor.MiddleLeft;
            var hat = Button(panel.transform, "Hat Color", "Hat", null, new Vector2(-156, -6), new Vector2(288, 50));
            var shoes = Button(panel.transform, "Shoe Color", "Shoes", null, new Vector2(156, -6), new Vector2(288, 50));
            hat.image.color = shoes.image.color = Hex("#F3E3B9");
            var hatSwatch = Swatch(hat.transform, "Swatch", new Vector2(111, 0), accent);
            var shoeSwatch = Swatch(shoes.transform, "Swatch", new Vector2(111, 0), accent);
            var stage = Rect(panel.transform, "Practice Stage", new Vector2(0, -172), new Vector2(629, 211));
            Text(stage.transform, "Practice Label", "Practice  " + controls, 23, new Vector2(0, 126), new Vector2(550, 28));
            var track = Image(stage.transform, "Practice Track", (Sprite)null, new Vector2(0, -83), new Vector2(590, 3));
            track.color = Hex("#CDA16B"); track.raycastTarget = false;
            var inputSource = panel.gameObject.AddComponent<VersusKeyboardInput>();
            inputSource.Configure(left, right, "Keyboard P" + player);
            var practice = panel.gameObject.AddComponent<VersusPracticePlayer>();
            var mascot = Rect(stage.transform, "Mascot", new Vector2(0, -9), mascotBodySize);
            var body = Image(mascot.transform, "Body", Sprite(BodyAtlas, "IdleA"), Vector2.zero, mascotBodySize);
            body.preserveAspect = true;
            var helmet = Image(mascot.transform, "Hat", HatSprite(player == 1 ? 0 : 1, false), mascotHatOffset, mascotHatSize);
            helmet.preserveAspect = true;
            var readyPlate = Image(panel.transform, "Ready Plate", (Sprite)null, new Vector2(0, -323), new Vector2(504, 53));
            readyPlate.color = Hex("#F3E3B9"); readyPlate.raycastTarget = false;
            var ready = Text(readyPlate.transform, "Ready State", "Press both controls", 24, Vector2.zero, new Vector2(480, 48));
            Bind(practice, "input", inputSource); Bind(practice, "mascot", mascot.GetComponent<RectTransform>());
            Bind(practice, "body", body); Bind(practice, "hat", helmet); Bind(practice, "shoeSwatch", shoeSwatch);
            Bind(practice, "idle", Sprite(BodyAtlas, "IdleA"));
            BindArray(practice, "runFrames", new[] { Sprite(BodyAtlas, "RunA"), Sprite(BodyAtlas, "RunB"), Sprite(BodyAtlas, "RunC"), Sprite(BodyAtlas, "RunD") });
            Bind(practice, "rightHat", Sprite(HatRight, "EngineerHat")); Bind(practice, "leftHat", Sprite(HatLeft, "EngineerHatLeft"));
            var style = new SerializedObject(practice);
            var variants = style.FindProperty("hatVariants");
            variants.arraySize = HatNames.Length;
            for (int i = 0; i < HatNames.Length; i++)
            {
                var variant = variants.GetArrayElementAtIndex(i);
                variant.FindPropertyRelative("Color").colorValue = Hex(HatColors[i]);
                variant.FindPropertyRelative("Right").objectReferenceValue = HatSprite(i, false);
                variant.FindPropertyRelative("Left").objectReferenceValue = HatSprite(i, true);
            }
            style.ApplyModifiedPropertiesWithoutUndo();
            practice.Initialize(inputSource, mascot.GetComponent<RectTransform>(), body, -198, 198, 372);
            practice.SetColors(Hex(accent), Hex(accent));
            return new PlayerWidgets(dropdown, input, create, edit, hat, shoes, hatSwatch, shoeSwatch, practice, ready);
        }

        private static void BindPlayer(VersusSetupUI ui, PlayerWidgets widgets, int player)
        {
            Bind(ui, "player" + player + "Profiles", widgets.Profiles);
            Bind(ui, "player" + player + "Name", widgets.Name);
            Bind(ui, "player" + player + "CreateButton", widgets.Create);
            Bind(ui, "player" + player + "EditButton", widgets.Edit);
            Bind(ui, "player" + player + "HatButton", widgets.Hat);
            Bind(ui, "player" + player + "ShoeButton", widgets.Shoes);
            Bind(ui, "player" + player + "HatSwatch", widgets.HatSwatch);
            Bind(ui, "player" + player + "ShoeSwatch", widgets.ShoeSwatch);
            Bind(ui, "player" + player + "Practice", widgets.Practice);
            Bind(ui, "player" + player + "ReadyText", widgets.Ready);
        }

        private static Scene NewScene(out Canvas canvas)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Cocoa; cameraObject.AddComponent<AudioListener>();
            var root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var viewport = cameraObject.AddComponent<GameplayViewport>();
            Bind(viewport, "canvas", canvas);
            viewport.Apply(1920, 1080);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return scene;
        }

        private static void AddBackground(Transform parent)
        {
            var background = Image(parent, "Kitchen Background", AssetDatabase.LoadAssetAtPath<Sprite>(Kitchen), Vector2.zero, new Vector2(1920, 1080));
            background.color = Color.white;
            background.transform.SetAsFirstSibling();
            var shade = Image(parent, "Readability Shade", (Sprite)null, Vector2.zero, new Vector2(1920, 1080));
            shade.color = new Color(.20f, .08f, .025f, .08f);
            shade.raycastTarget = false;
        }

        private static GameObject Panel(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var panel = Image(parent, name, AssetDatabase.LoadAssetAtPath<Sprite>(FramePath), position, size);
            panel.pixelsPerUnitMultiplier = 1f / 1.2f;
            panel.raycastTarget = false;
            return panel.gameObject;
        }

        private static Vector2 Pos(float x, float y, float width, float height) =>
            new Vector2((x + width / 2 - 800) * 1.2f, (450 - y - height / 2) * 1.2f);

        private static void ChoiceLabels(Transform parent, string mark, string title, string hint, bool light)
        {
            var color = light ? Hex("#FFF7D9") : Cocoa;
            Text(parent, "Mode Mark", mark, 35, new Vector2(0, 52), new Vector2(200, 48)).color = color;
            Text(parent, "Mode Name", title, 39, new Vector2(0, -8), new Vector2(430, 55)).color = color;
            Text(parent, "Mode Hint", hint, 22, new Vector2(0, -58), new Vector2(430, 36)).color = color;
        }

        private static void Mascot(Transform parent, string name, Vector2 position, bool blue)
        {
            position.y += (mascotBodySize.y - 186f) / 2;
            var mascot = Rect(parent, name, position, mascotBodySize);
            var body = Image(mascot.transform, "Body", Sprite(BodyAtlas, "IdleA"), Vector2.zero, mascotBodySize);
            body.preserveAspect = true; body.raycastTarget = false;
            if (blue) body.rectTransform.localScale = new Vector3(-1, 1, 1);
            var offset = mascotHatOffset;
            if (blue) offset.x *= -1;
            var hat = Image(mascot.transform, "Hat", HatSprite(blue ? 1 : 0, blue),
                offset, mascotHatSize);
            hat.preserveAspect = true; hat.raycastTarget = false;
            hat.color = Color.white;
        }

        private static void MeasureGameplayMascot()
        {
            string path = Scenes + "Gameplay.unity";
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var camera = roots.SelectMany(root => root.GetComponentsInChildren<Camera>()).First(item => item.CompareTag("MainCamera"));
                var mascot = roots.SelectMany(root => root.GetComponentsInChildren<MascotPresentation>()).First();
                var body = mascot.transform.Find("Body").GetComponent<SpriteRenderer>();
                var hat = mascot.transform.Find("Hat").GetComponent<SpriteRenderer>();
                if (!camera.orthographic) throw new InvalidOperationException("Gameplay mascot measurement requires its orthographic camera.");
                float pixelsPerUnit = 1080f / (camera.orthographicSize * 2);
                mascotBodySize = (Vector2)body.bounds.size * pixelsPerUnit;
                mascotHatSize = (Vector2)hat.bounds.size * pixelsPerUnit;
                mascotHatOffset = (Vector2)(hat.bounds.center - body.bounds.center) * pixelsPerUnit;
                Debug.Log("GAMEPLAY_MASCOT_REFERENCE body=" + mascotBodySize.ToString("F3") + " hat=" + mascotHatSize.ToString("F3") + " offset=" + mascotHatOffset.ToString("F3"));
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        private static string HatPath(int index) => "Assets/CPS/Art/Mascot/Hats/engineer-hat-" + HatNames[index].ToLowerInvariant() + "-pair-v2.png";

        private static Sprite HatSprite(int index, bool left) => index == 0
            ? Sprite(left ? HatLeft : HatRight, left ? "EngineerHatLeft" : "EngineerHat")
            : Sprite(HatPath(index), "EngineerHat" + HatNames[index] + (left ? "Left" : "Right"));

        private static void ImportHatVariants()
        {
            for (int i = 1; i < HatNames.Length; i++)
            {
                string path = HatPath(i);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null) throw new FileNotFoundException("Generated hat sheet is missing.", path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 100;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
                var source = new Texture2D(2, 2);
                source.LoadImage(File.ReadAllBytes(path));
                if (source.GetPixel(0, 0).a > .01f) throw new InvalidDataException("Hat sheet must have transparent corners.");
                var slices = new SpriteRect[2];
                for (int view = 0; view < 2; view++)
                {
                    int start = view * source.width / 2, end = (view + 1) * source.width / 2;
                    int minX = end, minY = source.height, maxX = start, maxY = 0;
                    for (int y = 0; y < source.height; y++)
                        for (int x = start; x < end; x++)
                            if (source.GetPixel(x, y).a > .5f)
                            { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
                    if (minX >= maxX || minY >= maxY) throw new InvalidDataException("Empty helmet cell.");
                    slices[view] = new SpriteRect { name = "EngineerHat" + HatNames[i] + (view == 0 ? "Right" : "Left"),
                        rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1), pivot = new Vector2(.5f, .5f), alignment = SpriteAlignment.Center };
                }
                Object.DestroyImmediate(source);
                var factories = new SpriteDataProviderFactories(); factories.Init();
                var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
                var existing = provider.GetSpriteRects().ToDictionary(item => item.name);
                foreach (var slice in slices) slice.spriteID = existing.TryGetValue(slice.name, out var old) ? old.spriteID : GUID.Generate();
                provider.SetSpriteRects(slices);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(slices.Select(slice => new SpriteNameFileIdPair(slice.name, slice.spriteID)));
                provider.Apply(); importer.SaveAndReimport();
            }
        }

        private static void CreateFrame()
        {
            var texture = new Texture2D(200, 200, TextureFormat.RGBA32, false);
            var layers = new[] { 3, 7, 11, 16, 23 };
            var radii = new[] { 28, 24, 21, 18, 12 };
            var colors = new[] { Hex("#61331F"), Hex("#CF8D4F"), Hex("#F3CA8E"), Hex("#E9B875"), Hex("#FFF7D9") };
            for (int y = 0; y < 200; y++)
                for (int x = 0; x < 200; x++)
                {
                    Color color = Color.clear;
                    for (int i = 0; i < layers.Length; i++)
                    {
                        float inset = layers[i], radius = radii[i];
                        float dx = Mathf.Max(Mathf.Abs(x - 99.5f) - (100 - inset - radius), 0);
                        float dy = Mathf.Max(Mathf.Abs(y - 99.5f) - (100 - inset - radius), 0);
                        if (dx * dx + dy * dy <= radius * radius) color = colors[i];
                    }
                    texture.SetPixel(x, y, color);
                }
            texture.Apply();
            File.WriteAllBytes(FramePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(FramePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(FramePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(32, 32, 32, 32);
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        public static void AuthorAndVerify()
        {
            Author();
            string output = Path.GetFullPath("../verification/gameplay-mascot-scale");
            Directory.CreateDirectory(output);
            foreach (string name in new[] { "ModeSelect", "VersusSetup" })
            {
                var scene = EditorSceneManager.OpenScene(Scenes + name + ".unity");
                var camera = Object.FindFirstObjectByType<Camera>();
                var viewport = camera.GetComponent<GameplayViewport>();
                if (viewport == null) throw new InvalidOperationException("Fixed viewport is missing.");
                var canvas = Object.FindFirstObjectByType<Canvas>();
                foreach (var panel in canvas.GetComponentsInChildren<Image>().Where(image => image.name.EndsWith("Panel")))
                    if (panel.sprite == null || panel.sprite.border.x != 32 || panel.type != UnityEngine.UI.Image.Type.Sliced)
                        throw new InvalidOperationException("Nine-slice frame is missing: " + panel.name);
                if (name == "ModeSelect" && canvas.transform.Find("Solo Mascot") == null)
                    throw new InvalidOperationException("Solo mascot is missing.");
                if (name == "VersusSetup" && Object.FindObjectsByType<VersusPracticePlayer>(FindObjectsSortMode.None).Length != 2)
                    throw new InvalidOperationException("Practice controls are missing.");
                foreach (var practice in Object.FindObjectsByType<VersusPracticePlayer>(FindObjectsSortMode.None))
                {
                    var serialized = new SerializedObject(practice);
                    var variants = serialized.FindProperty("hatVariants");
                    if (variants.arraySize != 6) throw new InvalidOperationException("Six hat colors are required.");
                    for (int i = 0; i < variants.arraySize; i++)
                    {
                        var variant = variants.GetArrayElementAtIndex(i);
                        if (variant.FindPropertyRelative("Right").objectReferenceValue == null || variant.FindPropertyRelative("Left").objectReferenceValue == null)
                            throw new InvalidOperationException("Both hat directions are required.");
                    }
                    var hat = (Image)serialized.FindProperty("hat").objectReferenceValue;
                    var mascot = (RectTransform)serialized.FindProperty("mascot").objectReferenceValue;
                    var body = (Image)serialized.FindProperty("body").objectReferenceValue;
                    if (hat.color != Color.white || mascot.sizeDelta != mascotBodySize || body.type != UnityEngine.UI.Image.Type.Simple)
                        throw new InvalidOperationException("Hat tint or mascot proportions do not match the reference.");
                }
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
                {
                    var target = new RenderTexture(size.x, size.y, 24);
                    camera.targetTexture = target;
                    viewport.Apply(size.x, size.y);
                    Canvas.ForceUpdateCanvases();
                    foreach (var label in canvas.GetComponentsInChildren<Text>(true)) label.SetAllDirty();
                    Canvas.ForceUpdateCanvases();
                    foreach (var label in canvas.GetComponentsInChildren<Text>(true)) label.SetAllDirty();
                    Canvas.ForceUpdateCanvases();
                    camera.Render();
                    var old = RenderTexture.active;
                    RenderTexture.active = target;
                    var capture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    capture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                    capture.Apply();
                    File.WriteAllBytes(Path.Combine(output, name + "-" + size.x + ".png"), capture.EncodeToPNG());
                    RenderTexture.active = old;
                    camera.targetTexture = null;
                    Object.DestroyImmediate(capture);
                    Object.DestroyImmediate(target);
                }
            }
            Debug.Log("GAMEPLAY_MASCOT_SCALE_AUTHORING_AND_CAPTURES_PASS");
        }

        private static Image Swatch(Transform parent, string name, Vector2 position, string color)
        {
            var swatch = Image(parent, name, (Sprite)null, position, new Vector2(38, 38));
            swatch.color = Hex(color); swatch.raycastTarget = false; return swatch;
        }

        private static Button Button(Transform parent, string name, string caption, string sprite, Vector2 position, Vector2 size, bool lightText = false)
        {
            var image = Image(parent, name, sprite, position, size);
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1, .88f, .68f); colors.pressedColor = new Color(.72f, .54f, .38f); button.colors = colors;
            colors = button.colors; colors.selectedColor = Color.white; button.colors = colors;
            var label = Text(image.transform, "Label", caption, 28, Vector2.zero, size - new Vector2(20, 12));
            label.color = lightText ? new Color(1, .94f, .81f) : Cocoa; label.raycastTarget = false;
            return button;
        }

        private static Dropdown Dropdown(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = DefaultControls.CreateDropdown(new DefaultControls.Resources()); go.name = name; go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), position, size);
            var dropdown = go.GetComponent<Dropdown>(); Skin(go.GetComponent<Image>(), "Input");
            foreach (var text in go.GetComponentsInChildren<Text>(true)) Style(text, 25);
            dropdown.ClearOptions();
            dropdown.captionText.text = "Choose profile";
            var arrow = go.transform.Find("Arrow");
            if (arrow != null) arrow.gameObject.SetActive(false);
            Text(go.transform, "Dropdown Arrow", "v", 22, new Vector2(size.x / 2 - 24, 0), new Vector2(30, 30));
            ProfileDropdownSetup.Configure(dropdown);
            return dropdown;
        }

        private static InputField Input(Transform parent, string name, string placeholder, Vector2 position, Vector2 size)
        {
            var go = DefaultControls.CreateInputField(new DefaultControls.Resources()); go.name = name; go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), position, size); Skin(go.GetComponent<Image>(), "Input");
            var field = go.GetComponent<InputField>(); field.placeholder.GetComponent<Text>().text = placeholder;
            foreach (var text in go.GetComponentsInChildren<Text>(true)) Style(text, 23);
            return field;
        }

        private static Image Image(Transform parent, string name, string sprite, Vector2 position, Vector2 size) =>
            Image(parent, name, sprite == null ? null : Sprite(UiAtlas, sprite), position, size);

        private static Image Image(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            var go = Rect(parent, name, position, size); var image = go.AddComponent<Image>(); image.sprite = sprite;
            image.type = sprite != null && sprite.border.sqrMagnitude > 0 ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple; return image;
        }

        private static Text Text(Transform parent, string name, string value, int size, Vector2 position, Vector2 bounds)
        {
            var go = Rect(parent, name, position, bounds); var text = go.AddComponent<Text>(); text.text = value; Style(text, size); return text;
        }

        private static void Style(Text text, int size)
        {
            text.font = font; text.fontSize = size; text.color = Cocoa; text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false; text.raycastTarget = false;
        }

        private static GameObject Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), position, size); return go;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; rect.localScale = Vector3.one;
        }

        private static void Skin(Image image, string sprite)
        {
            image.sprite = Sprite(UiAtlas, sprite); image.type = UnityEngine.UI.Image.Type.Sliced;
        }

        private static Sprite Sprite(string path, string name) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First(item => item.name == name);

        private static void Bind(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindArray(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target); var array = serialized.FindProperty(field); array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void UpdateBuildSettings()
        {
            string[] preferred = { "Boot", "ModeSelect", "MainMenu", "VersusSetup", "Gameplay" };
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string name in preferred) scenes.Add(new EditorBuildSettingsScene(Scenes + name + ".unity", true));
            foreach (var existing in EditorBuildSettings.scenes)
                if (!preferred.Contains(Path.GetFileNameWithoutExtension(existing.path))) scenes.Add(existing);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out var color); return color;
        }

        private readonly struct PlayerWidgets
        {
            public readonly Dropdown Profiles; public readonly InputField Name; public readonly Button Create; public readonly Button Edit;
            public readonly Button Hat; public readonly Button Shoes; public readonly Image HatSwatch; public readonly Image ShoeSwatch;
            public readonly VersusPracticePlayer Practice; public readonly Text Ready;
            public PlayerWidgets(Dropdown profiles, InputField name, Button create, Button edit, Button hat, Button shoes,
                Image hatSwatch, Image shoeSwatch, VersusPracticePlayer practice, Text ready)
            { Profiles = profiles; Name = name; Create = create; Edit = edit; Hat = hat; Shoes = shoes; HatSwatch = hatSwatch; ShoeSwatch = shoeSwatch; Practice = practice; Ready = ready; }
        }
    }
}
