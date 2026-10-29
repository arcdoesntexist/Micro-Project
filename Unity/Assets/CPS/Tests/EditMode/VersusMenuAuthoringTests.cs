using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace CPS.KitchenCatch.Tests
{
    public sealed class VersusMenuAuthoringTests
    {
        [Test]
        public void AuthoredScenesContainRequiredControllersAndBuildEntries()
        {
            AssertScene<ModeSelectionUI>("Assets/CPS/Scenes/ModeSelect.unity", 1);
            AssertScene<VersusSetupUI>("Assets/CPS/Scenes/VersusSetup.unity", 1);
            AssertScene<VersusPracticePlayer>("Assets/CPS/Scenes/VersusSetup.unity", 2);

            var enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            CollectionAssert.Contains(enabled, "Assets/CPS/Scenes/ModeSelect.unity");
            CollectionAssert.Contains(enabled, "Assets/CPS/Scenes/VersusSetup.unity");
        }

        private static void AssertScene<T>(string path, int count) where T : UnityEngine.Component
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Not.Null, path);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                int found = scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<T>(true).Length);
                Assert.That(found, Is.EqualTo(count), path);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
