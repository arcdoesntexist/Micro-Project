using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CPS.KitchenCatch.Tests
{
    public sealed class GameSceneAuthoringTests
    {
        [Test]
        public void MenuAndGameplayUseOneAuthoredRigWithoutConfigurationOverrides()
        {
            var original = SceneManager.GetActiveScene();
            var menuScene = EditorSceneManager.OpenPreviewScene("Assets/CPS/Scenes/MainMenu.unity");
            var gameScene = EditorSceneManager.OpenPreviewScene("Assets/CPS/Scenes/Gameplay.unity");
            try
            {
                var menu = Find<MainMenuUI>(menuScene);
                var template = (GameSessionController)new SerializedObject(menu).FindProperty("gameplayTemplate").objectReferenceValue;
                var gameplay = Find<GameSessionController>(gameScene);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/MainMenuCanvas.prefab"), Is.Not.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/GameplayCanvas.prefab"), Is.Not.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/ResultPanel.prefab"), Is.Not.Null);
                Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(menu.gameObject), Is.Not.Null);
                Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(Find<GameplayUI>(gameScene).gameObject), Is.Not.Null);
                foreach (var scene in new[] { menuScene, gameScene })
                {
                    var viewport = Find<GameplayViewport>(scene);
                    var canvas = Find<Canvas>(scene);
                    Assert.That(viewport.GetComponent<AudioListener>(), Is.Not.Null,
                        scene.name + " main camera must listen for game audio.");
                    Assert.That(new SerializedObject(viewport).FindProperty("canvas").objectReferenceValue, Is.SameAs(canvas));
                    Assert.That(canvas.worldCamera, Is.SameAs(viewport.GetComponent<Camera>()));
                    Assert.That(canvas.GetComponent<UnityEngine.UI.CanvasScaler>().screenMatchMode,
                        Is.EqualTo(UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand));
                }
                Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(gameplay), Is.SameAs(template));
                var overrides = PrefabUtility.GetPropertyModifications(gameplay.gameObject);
                if (overrides != null)
                    foreach (var change in overrides)
                        Assert.That(change.target is GameSessionController || change.target is PlayerController ||
                            change.target is FallingObjectSpawner, Is.False, "Shared gameplay configuration must not be overridden in the scene.");
                var configs = new SerializedObject(template).FindProperty("difficulties");
                Assert.That(configs.arraySize, Is.EqualTo(3));
                string[] names = { "Easy", "Normal", "Hard" };
                for (int i = 0; i < names.Length; i++)
                    Assert.That(configs.GetArrayElementAtIndex(i).objectReferenceValue,
                        Is.SameAs(AssetDatabase.LoadAssetAtPath<DifficultyConfig>("Assets/CPS/Config/" + names[i] + ".asset")));
                var rig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/GameplayRig.prefab");
                var uploaders = rig.GetComponents<GoogleSheetsResultUploader>();
                Assert.That(uploaders.Length, Is.EqualTo(1));
                var uploader = new SerializedObject(uploaders[0]);
                Assert.That(uploader.FindProperty("controller").objectReferenceValue, Is.SameAs(template));
                string endpoint = uploader.FindProperty("endpoint").stringValue;
                Assert.That(endpoint, Does.StartWith("https://"));
                Assert.That(endpoint, Does.EndWith("/exec"));
                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(original));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(gameScene); EditorSceneManager.ClosePreviewScene(menuScene);
            }
        }

        private static T Find<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            Assert.Fail("Authored scene component missing: " + typeof(T).Name);
            return null;
        }
    }
}
