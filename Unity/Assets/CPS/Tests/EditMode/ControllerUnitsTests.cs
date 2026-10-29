using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CPS.KitchenCatch.Tests
{
    public sealed class ControllerUnitsTests
    {
        [Test] public void LoadCellResultShowsNewtonsAndDoesNotHideOneValidHand()
        {
            var root = new GameObject("Result units");
            try
            {
                var panel = root.AddComponent<ResultPanel>();
                Text left = Label(root.transform, "Left"); Text right = Label(root.transform, "Right"); Text force = Label(root.transform, "Force");
                Set(panel, "leftStats", left); Set(panel, "rightStats", right); Set(panel, "forceStatus", force);
                panel.Show(new SessionResult { InputSource = "LoadCell", InputStatistics = new InputStatistics {
                    Left = new HandStatistics { HasForce = true, MeanForceN = .5f, MaxForceN = 2, PressedDurationSeconds = 1 }, Right = new HandStatistics() } });
                Assert.That(left.text, Does.Contain("0.50 N").And.Contain("2.00 N").And.Not.Contain("%"));
                Assert.That(right.text, Does.Not.Contain("0.00 N"));
                Assert.That(force.text, Does.Contain("Estimated Force").And.Contain("0.50").And.Not.Contain("Simulated"));
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private static Text Label(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.transform.SetParent(parent); return go.GetComponent<Text>();
        }
    }
}
