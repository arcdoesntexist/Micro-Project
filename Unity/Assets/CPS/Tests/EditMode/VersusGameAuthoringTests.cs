using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CPS.KitchenCatch.Tests
{
    public sealed class VersusGameAuthoringTests
    {
        [Test]
        public void SerializedGameplaySceneResolvesEveryRequiredAsset()
        {
            var scene = EditorSceneManager.OpenScene("Assets/CPS/Scenes/VersusGameplay.unity", OpenSceneMode.Additive);
            try
            {
                var controller = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<VersusMatchController>()).Single();
                Assert.That(controller.Kitchen, Is.Not.Null, "Kitchen Sprite reference");
                Assert.That(controller.Frame, Is.Not.Null, "Frame Sprite reference");
                Assert.That(controller.Frame.border.sqrMagnitude, Is.GreaterThan(0));
                Assert.That(controller.OrangeButton, Is.Not.Null);
                Assert.That(controller.CocoaButton, Is.Not.Null);
                Assert.That(controller.Font, Is.Not.Null);
                Assert.That(Resources.Load<MascotBodyPalette>("MascotBodyPalette"), Is.Not.Null);
                Assert.That(controller.BodyFrames, Has.Length.EqualTo(10));
                Assert.That(controller.BodyFrames, Has.All.Not.Null);
                Assert.That(controller.Definitions, Has.Length.EqualTo(5));
                foreach (var definition in controller.Definitions)
                { Assert.That(definition, Is.Not.Null); Assert.That(definition.sprite, Is.Not.Null); }
                var data = new SerializedObject(controller);
                Assert.That(data.FindProperty("settingsPrefab").objectReferenceValue, Is.Not.Null);
                var variants = data.FindProperty("hats");
                Assert.That(variants.arraySize, Is.EqualTo(6));
                for (int i = 0; i < variants.arraySize; i++)
                {
                    Assert.That(variants.GetArrayElementAtIndex(i).FindPropertyRelative("Right").objectReferenceValue, Is.Not.Null);
                    Assert.That(variants.GetArrayElementAtIndex(i).FindPropertyRelative("Left").objectReferenceValue, Is.Not.Null);
                }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
