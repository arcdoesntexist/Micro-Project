using System;
using System.IO;
using CPS.KitchenCatch.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Tests
{
    public sealed class BuildValidationTests
    {
        private const string RigPath = "Assets/CPS/Prefabs/GameplayRig.prefab";

        [Test]
        public void ProductionBuildInputsValidate()
        {
            WebGLBuild.Validate(EditorBuildSettings.scenes);
        }

        [Test]
        public void DisabledRequiredSceneBlocksValidation()
        {
            var scenes = (EditorBuildSettingsScene[])EditorBuildSettings.scenes.Clone();
            int menuIndex = Array.FindIndex(scenes, scene => scene.path == "Assets/CPS/Scenes/MainMenu.unity");
            Assert.That(menuIndex, Is.GreaterThanOrEqualTo(0));
            scenes[menuIndex] = new EditorBuildSettingsScene(scenes[menuIndex].path, false);

            var error = Assert.Throws<InvalidOperationException>(() => WebGLBuild.Validate(scenes));

            Assert.That(error.Message, Does.Contain("MainMenu"));
        }

        [Test]
        public void MissingDifficultyReferenceBlocksValidation()
        {
            var instance = Object.Instantiate(LoadTemplate().gameObject);
            instance.SetActive(false);
            try
            {
                var controller = instance.GetComponent<GameSessionController>();
                var difficulties = new SerializedObject(controller).FindProperty("difficulties");
                for (int i = 0; i < difficulties.arraySize; i++)
                {
                    var config = difficulties.GetArrayElementAtIndex(i).objectReferenceValue as DifficultyConfig;
                    if (config != null && config.difficulty == DifficultyId.Normal)
                        difficulties.GetArrayElementAtIndex(i).objectReferenceValue = null;
                }
                difficulties.serializedObject.ApplyModifiedPropertiesWithoutUndo();

                var error = Assert.Throws<InvalidOperationException>(() => WebGLBuild.Validate(EditorBuildSettings.scenes, controller));

                Assert.That(error.Message, Does.Contain("Normal"));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GameplaySceneEffectiveMissingPlayerOverrideBlocksValidation()
        {
            string suffix = Guid.NewGuid().ToString("N");
            string scenePath = $"Assets/CPS/Tests/TempBuildValidationScene-{suffix}.unity";
            Scene originalActiveScene = SceneManager.GetActiveScene();
            int originalSceneCount = SceneManager.sceneCount;
            bool originalSceneDirty = originalActiveScene.IsValid() && originalActiveScene.isDirty;
            Scene scene = default;
            try
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath), Is.Null);
                Assert.That(AssetDatabase.CopyAsset("Assets/CPS/Scenes/Gameplay.unity", scenePath), Is.True);
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                GameSessionController controller = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    controller = root.GetComponentInChildren<GameSessionController>(true);
                    if (controller != null) break;
                }
                Assert.That(controller, Is.Not.Null);
                var serialized = new SerializedObject(controller);
                serialized.FindProperty("player").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(EditorSceneManager.SaveScene(scene, scenePath), Is.True);
                Assert.That(EditorSceneManager.CloseScene(scene, true), Is.True);
                Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(originalActiveScene.handle));
                Assert.That(SceneManager.sceneCount, Is.EqualTo(originalSceneCount));

                var error = Assert.Throws<InvalidOperationException>(() => WebGLBuild.ValidateGameplayScene(scenePath));

                Assert.That(error.Message, Does.Contain("player"));
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
                    AssetDatabase.DeleteAsset(scenePath);
            }

            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(originalActiveScene.handle));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(originalSceneCount));
            if (originalActiveScene.IsValid()) Assert.That(originalActiveScene.isDirty, Is.EqualTo(originalSceneDirty));
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath), Is.Null);
        }

        [Test]
        public void MismatchedControllerAndSpawnerPlayerBlocksValidation()
        {
            var instance = Object.Instantiate(LoadTemplate().gameObject);
            var differentPlayer = new GameObject("DifferentPlayer").AddComponent<PlayerController>();
            try
            {
                var spawner = instance.GetComponent<FallingObjectSpawner>();
                var serialized = new SerializedObject(spawner);
                serialized.FindProperty("player").objectReferenceValue = differentPlayer;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var error = Assert.Throws<InvalidOperationException>(() => WebGLBuild.Validate(EditorBuildSettings.scenes, instance.GetComponent<GameSessionController>()));

                Assert.That(error.Message, Does.Contain("must match"));
            }
            finally
            {
                Object.DestroyImmediate(differentPlayer.gameObject);
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void MissingFoodDefinitionBlocksValidation()
        {
            var instance = Object.Instantiate(LoadTemplate().gameObject);
            instance.SetActive(false);
            try
            {
                var controller = instance.GetComponent<GameSessionController>();
                var spawner = instance.GetComponent<FallingObjectSpawner>();
                var foods = new SerializedObject(spawner).FindProperty("foodDefinitions");
                foods.GetArrayElementAtIndex(0).objectReferenceValue = null;
                foods.serializedObject.ApplyModifiedPropertiesWithoutUndo();

                var error = Assert.Throws<InvalidOperationException>(() => WebGLBuild.Validate(EditorBuildSettings.scenes, controller));

                Assert.That(error.Message, Does.Contain("food"));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void ReleaseBuildRejectsDevelopmentOptions()
        {
            var error = Assert.Throws<InvalidOperationException>(() => WebGLBuild.ValidateReleaseOptions(BuildOptions.Development));

            Assert.That(error.Message, Does.Contain("Release"));
        }

        [Test]
        public void DevelopmentBuildUsesSeparateOutputAndDevelopmentFlag()
        {
            Assert.That(WebGLBuild.DevelopmentOutputFolder, Is.Not.EqualTo(WebGLBuild.ReleaseOutputFolder));
            Assert.That(WebGLBuild.DevelopmentOptions.HasFlag(BuildOptions.Development), Is.True);
        }

        [Test]
        public void BrowserHarnessIsExcludedFromReleasePlayers()
        {
            string source = File.ReadAllText("Assets/CPS/Development/DevelopmentBrowserHarness.cs");

            Assert.That(source, Does.StartWith("#if DEVELOPMENT_BUILD || UNITY_EDITOR"));
            Assert.That(source, Does.Contain("#if DEVELOPMENT_BUILD\n        [RuntimeInitializeOnLoadMethod"));
            Assert.That(source, Does.Contain("Keyboard.current"));
            Assert.That(source, Does.Not.Contain("Input.GetKeyDown"));
        }

        [Test]
        public void BrowserHarnessSustainedModeKeepsNaturalRoundTimer()
        {
            string source = File.ReadAllText("Assets/CPS/Development/DevelopmentBrowserHarness.cs");

            Assert.That(source, Does.Contain("keyboard.f10Key.wasPressedThisFrame"));
            Assert.That(source, Does.Contain("controller.Model.Health.Reset(100000)"));
            Assert.That(source, Does.Contain("mode={roundMode}"));
            Assert.That(source, Does.Not.Contain("sustainMode) controller.Model.CompleteStep"));
        }

        private static GameSessionController LoadTemplate()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            Assert.That(prefab, Is.Not.Null);
            var controller = prefab.GetComponent<GameSessionController>();
            Assert.That(controller, Is.Not.Null);
            return controller;
        }
    }
}
