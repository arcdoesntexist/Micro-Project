using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CPS.KitchenCatch.Tests
{
    public sealed class ControllerAuthoringTests
    {
        [Test] public void ScopedAuthoringAddsOnePanelPerCanvasWithoutChangingSceneDecorOrOldLayout()
        {
            string menuScene = File.ReadAllText("Assets/CPS/Scenes/MainMenu.unity");
            string gameplayScene = File.ReadAllText("Assets/CPS/Scenes/Gameplay.unity");
            string menuPath = "Assets/CPS/Prefabs/MainMenuCanvas.prefab";
            var original = AssetDatabase.LoadAssetAtPath<GameObject>(menuPath);
            var oldPositions = original.GetComponentsInChildren<RectTransform>(true)
                .ToDictionary(item => AnimationUtility.CalculateTransformPath(item, original.transform), item => item.anchoredPosition);
            Editor.ControllerUISetup.Apply(); Editor.ControllerUISetup.Apply();
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>(menuPath);
            Assert.That(menu.GetComponents<ControllerPanel>().Length, Is.EqualTo(1));
            Assert.That(menu.transform.Find("Controller launcher"), Is.Not.Null);
            Assert.That(menu.transform.Find("Controller panel/Controller card/FSR"), Is.Not.Null);
            Assert.That(menu.transform.Find("Controller panel/Controller card/Load Cell"), Is.Not.Null);
            var positions = menu.GetComponentsInChildren<RectTransform>(true)
                .ToDictionary(item => AnimationUtility.CalculateTransformPath(item, menu.transform), item => item.anchoredPosition);
            foreach (var item in oldPositions)
            {
                Assert.That(positions.TryGetValue(item.Key, out var position), Is.True, item.Key);
                Assert.That(position, Is.EqualTo(item.Value), item.Key);
            }
            var gameplay = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/GameplayCanvas.prefab");
            Assert.That(gameplay.GetComponents<ControllerPanel>().Length, Is.EqualTo(1));
            Assert.That(gameplay.GetComponents<PauseHotkeys>().Length, Is.EqualTo(1));
            Assert.That(AssetDatabase.LoadAssetAtPath<ControllerSettings>("Assets/CPS/Resources/ControllerSettings.asset"), Is.Not.Null);
            Assert.That(File.ReadAllText("Assets/CPS/Scenes/MainMenu.unity"), Is.EqualTo(menuScene));
            Assert.That(File.ReadAllText("Assets/CPS/Scenes/Gameplay.unity"), Is.EqualTo(gameplayScene));
        }
    }
}
