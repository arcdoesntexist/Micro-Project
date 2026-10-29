using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CPS.KitchenCatch.Editor
{
    public static class WebGLBuild
    {
        public const string ReleaseOutputFolder = "Builds/WebGL";
        public const string DevelopmentOutputFolder = "Builds/WebGL-Dev";
        public const BuildOptions DevelopmentOptions = BuildOptions.Development;
        private const string GameplayScenePath = "Assets/CPS/Scenes/Gameplay.unity";
        private static readonly string[] RequiredScenes =
        {
            "Assets/CPS/Scenes/Boot.unity",
            "Assets/CPS/Scenes/ModeSelect.unity",
            "Assets/CPS/Scenes/MainMenu.unity",
            "Assets/CPS/Scenes/VersusSetup.unity",
            "Assets/CPS/Scenes/VersusGameplay.unity",
            "Assets/CPS/Scenes/Gameplay.unity"
        };

        public static void Validate(EditorBuildSettingsScene[] scenes)
        {
            ValidateRequiredScenes(scenes);
            ValidateGameplayScene(GameplayScenePath);
        }

        public static void ValidateGameplayScene(string scenePath)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                throw new InvalidOperationException("Gameplay scene is missing: " + scenePath);

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                GameSessionController template = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var candidate in root.GetComponentsInChildren<GameSessionController>(true))
                    {
                        if (template != null)
                            throw new InvalidOperationException("Gameplay scene must contain exactly one GameSessionController.");
                        template = candidate;
                    }
                }
                if (template == null)
                    throw new InvalidOperationException("Gameplay scene GameSessionController is missing.");
                ValidateController(template);
            }
            finally
            {
                if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void Validate(EditorBuildSettingsScene[] scenes, GameSessionController template)
        {
            ValidateRequiredScenes(scenes);
            ValidateController(template);
        }

        private static void ValidateRequiredScenes(EditorBuildSettingsScene[] scenes)
        {
            if (scenes == null) throw new InvalidOperationException("Build scenes are missing.");
            foreach (string required in RequiredScenes)
            {
                bool enabled = false;
                foreach (var scene in scenes)
                    if (scene != null && scene.enabled && scene.path == required) enabled = true;
                if (!enabled) throw new InvalidOperationException(Path.GetFileNameWithoutExtension(required) + " scene must be enabled for WebGL build.");
            }
        }

        private static void ValidateController(GameSessionController template)
        {
            if (template == null) throw new InvalidOperationException("GameplayRig GameSessionController is missing.");
            var controller = new SerializedObject(template);
            var player = controller.FindProperty("player").objectReferenceValue as PlayerController;
            if (player == null)
                throw new InvalidOperationException("GameplayRig player reference is missing.");
            if (controller.FindProperty("keyboardInput").objectReferenceValue == null)
                throw new InvalidOperationException("GameplayRig keyboard input reference is missing.");
            var spawner = controller.FindProperty("spawner").objectReferenceValue as FallingObjectSpawner;
            if (spawner == null) throw new InvalidOperationException("GameplayRig spawner reference is missing.");
            if (spawner.Player != null && spawner.Player != player)
                throw new InvalidOperationException("GameSessionController player must match FallingObjectSpawner player.");

            var difficulties = controller.FindProperty("difficulties");
            foreach (DifficultyId id in new[] { DifficultyId.Easy, DifficultyId.Normal, DifficultyId.Hard })
            {
                DifficultyConfig config = null;
                for (int i = 0; i < difficulties.arraySize; i++)
                {
                    var candidate = difficulties.GetArrayElementAtIndex(i).objectReferenceValue as DifficultyConfig;
                    if (candidate != null && candidate.difficulty == id) config = candidate;
                }
                if (config == null) throw new InvalidOperationException(id + " difficulty config is missing.");
                string error = spawner.GetValidationError(config);
                if (error != null) throw new InvalidOperationException(id + ": " + error);
            }
        }

        public static void Build()
        {
            ValidateReleaseOptions(BuildOptions.None);
            BuildAt(ReleaseOutputFolder, BuildOptions.None);
        }

        public static void BuildDevelopment()
        {
            BuildAt(DevelopmentOutputFolder, DevelopmentOptions);
        }

        public static void ValidateReleaseOptions(BuildOptions options)
        {
            if ((options & BuildOptions.Development) != 0)
                throw new InvalidOperationException("Release WebGL build cannot include Development options.");
        }

        private static void BuildAt(string outputFolder, BuildOptions options)
        {
            Validate(EditorBuildSettings.scenes);
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputFolder));
            Directory.CreateDirectory(output);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = RequiredScenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = options
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("WebGL build failed: " + report.summary.result);
        }
    }
}
