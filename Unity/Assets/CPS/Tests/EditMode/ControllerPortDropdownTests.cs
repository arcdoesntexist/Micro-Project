using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CPS.KitchenCatch.Tests
{
    public sealed class ControllerPortDropdownTests
    {
        [TestCase("MainMenuCanvas")]
        [TestCase("GameplayCanvas")]
        public void PortListHasReadableRowsAndAScrollingViewport(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/" + name + ".prefab");
            var panel = new SerializedObject(prefab.GetComponent<ControllerPanel>());
            var dropdown = (Dropdown)panel.FindProperty("ports").objectReferenceValue;
            var scroll = dropdown.template.GetComponent<ScrollRect>();
            Assert.That(scroll, Is.Not.Null, "Port list needs the same readable scrolling template as profile lists.");
            Assert.That(scroll.viewport.GetComponent<RectMask2D>(), Is.Not.Null);
            var row = dropdown.template.GetComponentInChildren<Toggle>(true).GetComponent<RectTransform>();
            Assert.That(row.rect.height, Is.GreaterThanOrEqualTo(52));
            Assert.That(scroll.content.rect.height, Is.GreaterThanOrEqualTo(row.rect.height));
            Assert.That(dropdown.itemText.font, Is.SameAs(dropdown.captionText.font));
        }
    }
}
