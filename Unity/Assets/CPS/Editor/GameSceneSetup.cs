using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Editor
{
    public static class GameSceneSetup
    {
        private const string RigPath = "Assets/CPS/Prefabs/GameplayRig.prefab";
        private const string PrototypePath = "Assets/CPS/Prototype/";
        private const string ResultEndpoint = "https://script.google.com/macros/s/AKfycbzBmXK-7_5x4EEBYNKbSOan61djhXDhBI_CD7mElk2EdohlfHCJwU34wO4pV1ckKdUXsQ/exec";
        private static readonly string[] SceneNames = { "Boot", "MainMenu", "Gameplay" };

        [MenuItem("CPS/Create Playable Scenes")]
        public static void CreateOrUpdate()
        {
            foreach (string name in SceneNames)
                if (File.Exists(ScenePath(name))) throw new InvalidOperationException("Authored scene already exists: " + ScenePath(name));
            CreateGameplayTemplate();
            EnsureFolder("Assets/CPS", "Scenes");
            CreateBoot(); CreateMenu(); CreateGameplay();
            ProductionUISetup.Author();
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (string name in SceneNames) scenes.Add(new EditorBuildSettingsScene(ScenePath(name), true));
            foreach (var existing in EditorBuildSettings.scenes)
                if (Array.IndexOf(SceneNames, Path.GetFileNameWithoutExtension(existing.path)) < 0) scenes.Add(existing);
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }

        public static void CreateGameplayTemplate()
        {
            GameObjectAssetSetup.CreateMissing();
            EnsureFolder("Assets/CPS", "Prototype");
            if (!File.Exists(RigPath))
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
                AssetDatabase.CreateAsset(material, PrototypePath + "Unlit.mat");
                var food = new FallingObjectDefinition[3]; var trash = new FallingObjectDefinition[2];
                string[] names = { "Apple", "Bread", "Cake", "Can", "BananaPeel" };
                Color[] colors = { new Color(.8f, .15f, .12f), new Color(.9f, .6f, .22f),
                    new Color(1, .8f, .45f), new Color(.4f, .5f, .6f), new Color(.9f, .8f, .1f) };
                for (int i = 0; i < names.Length; i++)
                {
                    var definition = Object.Instantiate(AssetDatabase.LoadAssetAtPath<FallingObjectDefinition>("Assets/CPS/Config/" + names[i] + ".asset"));
                    definition.sprite = CreateSprite(names[i], colors[i], i);
                    AssetDatabase.CreateAsset(definition, PrototypePath + names[i] + ".asset");
                    if (i < 3) food[i] = definition; else trash[i - 3] = definition;
                }
                var objectGo = new GameObject("PrototypeFallingObject");
                objectGo.AddComponent<FallingObject>(); objectGo.GetComponent<SpriteRenderer>().sharedMaterial = material;
                var prefab = PrefabUtility.SaveAsPrefabAsset(objectGo, PrototypePath + "FallingObject.prefab").GetComponent<FallingObject>();
                Object.DestroyImmediate(objectGo);
                var rig = new GameObject("GameplayRig");
                try
                {
                    var input = rig.AddComponent<KeyboardInput>(); var navigator = rig.AddComponent<SceneNavigator>();
                    var playerGo = new GameObject("PrototypePlayer"); playerGo.transform.SetParent(rig.transform);
                    playerGo.transform.localPosition = new Vector3(0, -3.5f, 0);
                    var player = playerGo.AddComponent<PlayerController>();
                    var renderer = playerGo.AddComponent<SpriteRenderer>(); renderer.sharedMaterial = material;
                    renderer.sprite = CreateSprite("Player", new Color(1, .9f, .7f), 5);
                    var spawner = rig.AddComponent<FallingObjectSpawner>();
                    Set(spawner, "player", player); Set(spawner, "prefab", prefab);
                    SetArray(spawner, "foodDefinitions", food); SetArray(spawner, "trashDefinitions", trash);
                    var controller = rig.AddComponent<GameSessionController>();
                    Set(controller, "player", player); Set(controller, "keyboardInput", input);
                    Set(controller, "spawner", spawner); Set(controller, "navigator", navigator);
                    SetArray(controller, "difficulties", new[] { LoadDifficulty("Easy"), LoadDifficulty("Normal"), LoadDifficulty("Hard") });
                    PrefabUtility.SaveAsPrefabAsset(rig, RigPath);
                }
                finally { Object.DestroyImmediate(rig); }
            }
            EnsureResultUploader();
            VerifyTemplate();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("CPS/Wire Result Uploader")]
        public static void WireResultUploader()
        {
            EnsureResultUploader();
            AssetDatabase.SaveAssets();
        }

        private static void EnsureResultUploader()
        {
            var root = PrefabUtility.LoadPrefabContents(RigPath);
            try
            {
                var controller = root.GetComponent<GameSessionController>();
                var uploaders = root.GetComponents<GoogleSheetsResultUploader>();
                var uploader = uploaders.Length == 0 ? root.AddComponent<GoogleSheetsResultUploader>() : uploaders[0];
                for (int i = 1; i < uploaders.Length; i++) Object.DestroyImmediate(uploaders[i]);
                var serialized = new SerializedObject(uploader);
                serialized.FindProperty("controller").objectReferenceValue = controller;
                serialized.FindProperty("endpoint").stringValue = ResultEndpoint;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, RigPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void VerifyTemplate()
        {
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath).GetComponent<GameSessionController>();
            var go = new GameObject("Authoring validation"); go.SetActive(false);
            try
            {
                var app = go.AddComponent<AppSession>();
                app.Initialize(new ProfileStore(key => null, (key, value) => { }, () => { }));
                var profile = app.Store.Create("Authoring validation");
                foreach (DifficultyId id in new[] { DifficultyId.Easy, DifficultyId.Normal, DifficultyId.Hard })
                {
                    app.Select(profile.PlayerId, id);
                    string error = template.GetValidationError(app);
                    if (error != null) throw new InvalidOperationException(error);
                }
                Debug.Log("Task5 shared prefab read-only validation passed for Easy/Normal/Hard without instantiation.");
            }
            finally { Object.DestroyImmediate(go); }
        }

        private static void CreateBoot()
        {
            var scene = NewScene();
            new GameObject("AppSession").AddComponent<AppSession>();
            var navigator = new GameObject("BootRoute").AddComponent<SceneNavigator>();
            Set(navigator.gameObject.AddComponent<BootRoute>(), "navigator", navigator);
            EditorSceneManager.SaveScene(scene, ScenePath("Boot"));
        }

        private static void CreateMenu()
        {
            var scene = NewScene(); CreateCanvas(CreateCamera());
            EditorSceneManager.SaveScene(scene, ScenePath("MainMenu"));
        }

        private static void CreateGameplay()
        {
            var scene = NewScene(); CreateCanvas(CreateCamera());
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RigPath));
            EditorSceneManager.SaveScene(scene, ScenePath("Gameplay"));
        }

        private static Scene NewScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        private static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera"); go.tag = "MainCamera";
            go.transform.position = new Vector3(0, 0, -10);
            var camera = go.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 4.5f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.22f, .16f, .12f);
            go.AddComponent<AudioListener>();
            go.AddComponent<GameplayViewport>(); return camera;
        }

        private static Canvas CreateCanvas(Camera camera)
        {
            var go = new GameObject("SceneCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera; canvas.planeDistance = 1;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            Set(camera.GetComponent<GameplayViewport>(), "canvas", canvas);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return canvas;
        }

        private static Sprite CreateSprite(string name, Color color, int shape)
        {
            string path = PrototypePath + name + ".png";
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float dx = x - 31.5f, dy = y - 31.5f;
                bool fill = shape == 0 || shape == 5 ? dx * dx + dy * dy < 28 * 28 :
                    shape == 1 ? Mathf.Abs(dx) < 29 && Mathf.Abs(dy) < 15 :
                    shape == 2 ? y > 6 && y < 58 && Mathf.Abs(dx) < (58 - y) * .55f :
                    shape == 3 ? Mathf.Abs(dx) + Mathf.Abs(dy) < 29 :
                    (Mathf.Abs(dx) < 9 || Mathf.Abs(dy) < 9) && Mathf.Abs(dx) < 28 && Mathf.Abs(dy) < 28;
                texture.SetPixel(x, y, fill ? color : Color.clear);
            }
            texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 64;
            importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void Set(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target); serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetArray(Object target, string property, Object[] values)
        {
            var serialized = new SerializedObject(target); var array = serialized.FindProperty(property); array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static DifficultyConfig LoadDifficulty(string name) => AssetDatabase.LoadAssetAtPath<DifficultyConfig>("Assets/CPS/Config/" + name + ".asset");
        private static string ScenePath(string name) => "Assets/CPS/Scenes/" + name + ".unity";
        private static void EnsureFolder(string parent, string name)
        { if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name); }
    }
}
