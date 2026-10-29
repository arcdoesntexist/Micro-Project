using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Linq;

namespace CPS.KitchenCatch.Tests
{
    public sealed class HomeReferenceTests
    {
        [Test]
        public void HomeUsesSeparateCreationDialogAndOpenPracticeArea()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/MainMenuCanvas.prefab");
            var menu = new SerializedObject(root.GetComponent<MainMenuUI>());
            var dialogProperty = menu.FindProperty("createProfilePanel");
            Assert.That(dialogProperty, Is.Not.Null, "Home needs a separate creation dialog");
            var dialog = (GameObject)dialogProperty.objectReferenceValue;
            Assert.That(dialog, Is.Not.Null);
            Assert.That(dialog.activeSelf, Is.False);
            var input = (InputField)menu.FindProperty("nicknameField").objectReferenceValue;
            Assert.That(input.transform.IsChildOf(dialog.transform), Is.True);
            Assert.That(menu.FindProperty("openCreateButton").objectReferenceValue, Is.Not.Null);
            Assert.That(menu.FindProperty("openCreateButton").objectReferenceValue,
                Is.Not.SameAs(menu.FindProperty("createButton").objectReferenceValue), "Opening and confirming are separate actions");
            Assert.That(root.transform.Find("Approved practice").GetComponent<Image>().enabled, Is.False);
            var board = root.transform.Find("Profile and difficulty").GetComponent<RectTransform>();
            Assert.That(board.sizeDelta.x, Is.EqualTo(792).Within(.01f));
            Assert.That(board.sizeDelta.y, Is.EqualTo(732).Within(.01f));
            Assert.That(board.GetComponent<Image>().sprite.name, Is.EqualTo("CreamPanel"));
        }

        [Test]
        public void CreationKeyboardNavigationStaysInsideDialog()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/MainMenuCanvas.prefab");
            var menu = new SerializedObject(root.GetComponent<MainMenuUI>());
            var overlay = (GameObject)menu.FindProperty("createProfilePanel").objectReferenceValue;
            foreach (var control in overlay.GetComponentsInChildren<Selectable>(true))
            {
                var navigation = control.navigation;
                Assert.That(navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
                foreach (var next in new[] { navigation.selectOnUp, navigation.selectOnDown, navigation.selectOnLeft, navigation.selectOnRight })
                    if (next != null) Assert.That(next.transform.IsChildOf(overlay.transform), Is.True, control.name + " escapes to " + next.name);
            }
        }

        [Test]
        public void ChosenDifficultyStillShowsKeyboardFocus()
        {
            var go = new GameObject("Focus test", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                var button = go.GetComponent<Button>();
                typeof(MainMenuUI).GetMethod("SetDifficultyTint", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { button, true });
                Assert.That(button.colors.selectedColor, Is.Not.EqualTo(button.colors.normalColor));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void ReapplyingHomeKeepsOneLabelPerActionAndTitleAbovePlaque()
        {
            CPS.KitchenCatch.Editor.ReferenceHomeSetup.Apply();
            var before = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/MainMenuCanvas.prefab");
            int count = before.GetComponentsInChildren<Transform>(true).Length;
            CPS.KitchenCatch.Editor.ReferenceHomeSetup.Apply();
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/MainMenuCanvas.prefab");
            Assert.That(root.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(count));
            Assert.That(root.transform.Find("KITCHEN CATCH").GetSiblingIndex(), Is.GreaterThan(root.transform.Find("Home plaque").GetSiblingIndex()));
            Assert.That(root.transform.Find("KITCHEN CATCH").gameObject.activeSelf, Is.True);
            foreach (var button in root.GetComponentsInChildren<Button>(true))
                Assert.That(button.GetComponentsInChildren<Text>(true).Length, Is.LessThanOrEqualTo(1), button.name);
            Assert.That(root.transform.Cast<Transform>().Count(t => t.name == "SETTINGS"), Is.EqualTo(1));
        }
    }
}
