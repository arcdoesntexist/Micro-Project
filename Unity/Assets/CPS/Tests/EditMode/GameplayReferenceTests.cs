using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CPS.KitchenCatch.Tests
{
    public sealed class GameplayReferenceTests
    {
        [Test]
        public void FocusKeepsOrangeArtworkAndClearsOutlineOnDeselectAndDisable()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/GameplayCanvas.prefab");
            var root=Object.Instantiate(prefab);
            try
            {
                var ui=new SerializedObject(root.GetComponent<GameplayUI>());
                foreach(var field in new[]{"pauseButton","resumeButton"})
                {
                    var button=(Button)ui.FindProperty(field).objectReferenceValue;
                    for(var parent=button.transform;parent!=null;parent=parent.parent)
                        parent.gameObject.SetActive(true);
                    Assert.That(button.colors.selectedColor,Is.EqualTo(Color.white),"Selection must preserve orange artwork");
                    var outline=button.GetComponent<Outline>();
                    Assert.That(outline,Is.Not.Null);
                    var focus=button.GetComponent<ButtonFocusOutline>();
                    Assert.That(focus,Is.Not.Null);
                    focus.OnSelect(null);
                    Assert.That(outline.enabled,Is.True);
                    focus.OnDeselect(null);
                    Assert.That(outline.enabled,Is.False);
                    focus.OnSelect(null);
                    typeof(ButtonFocusOutline).GetMethod("OnDisable",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        .Invoke(focus,null);
                    Assert.That(outline.enabled,Is.False);
                }
            }
            finally {Object.DestroyImmediate(root);}
        }

        [Test]
        public void HeartSlotsHaveRenderersAndScoreHasFrameInset()
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/GameplayCanvas.prefab");
            foreach(var heart in root.GetComponentsInChildren<HeartIcon>(true))
                Assert.That(heart.GetComponent<CanvasRenderer>(),Is.Not.Null,"Heart needs a renderer");
            var ui=new SerializedObject(root.GetComponent<GameplayUI>());
            var score=(Text)ui.FindProperty("score").objectReferenceValue;
            var panel=(RectTransform)score.transform.parent;
            Assert.That(score.rectTransform.anchoredPosition.x+score.rectTransform.sizeDelta.x/2,
                Is.LessThanOrEqualTo(panel.rect.width/2-30));
        }

        [Test]
        public void ApplyingReferenceTwiceDoesNotDuplicateHudOrPauseControls()
        {
            var method=System.Type.GetType("CPS.KitchenCatch.Editor.ReferenceGameplaySetup, CPS.Editor")?.GetMethod("Apply");
            Assert.That(method,Is.Not.Null);
            method.Invoke(null,null);
            var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/GameplayCanvas.prefab");
            int count=root.GetComponentsInChildren<Transform>(true).Length;
            method.Invoke(null,null);
            root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/GameplayCanvas.prefab");
            Assert.That(root.GetComponentsInChildren<Transform>(true).Length,Is.EqualTo(count));
        }

        [Test]
        public void GameplayPrefabHasLiveReferenceBindingsAndReadablePauseActions()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/GameplayCanvas.prefab");
            var ui = new SerializedObject(root.GetComponent<GameplayUI>());
            var mode = ui.FindProperty("referenceHud");
            Assert.That(mode, Is.Not.Null, "Missing approved HUD presentation");
            Assert.That(mode.boolValue, Is.True);
            foreach (var field in new[] { "streakLabel", "leftFill", "rightFill", "streakFill" })
                Assert.That(ui.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            Assert.That(ui.FindProperty("heartImages").arraySize, Is.EqualTo(3));
            foreach (var name in new[] { "Player", "Timer", "Score", "Left input", "Right input" })
                Assert.That(root.transform.Find(name).GetComponent<Image>().sprite.name, Is.EqualTo("Input"));
            var input = (Text)ui.FindProperty("inputSource").objectReferenceValue;
            Assert.That(input.gameObject.activeSelf, Is.False);
            var overlay = (GameObject)ui.FindProperty("pauseOverlay").objectReferenceValue;
            Assert.That(overlay.activeSelf, Is.False);
            var resume = (Button)ui.FindProperty("resumeButton").objectReferenceValue;
            Assert.That(resume.GetComponentInChildren<Text>().text, Is.EqualTo("Keep cooking"));
            Assert.That(resume.GetComponentInChildren<Text>().color.r, Is.LessThan(.4f));
            foreach (var button in overlay.GetComponentsInChildren<Button>(true))
            {
                Assert.That(button.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
                var next = button.navigation.selectOnDown;
                Assert.That(next == null || next.transform.IsChildOf(overlay.transform), Is.True);
            }
        }

        [Test]
        public void ReferenceHudRefreshesClockHeartsAndStreakWithoutChangingScore()
        {
            var root = new GameObject("Reference HUD test");
            try
            {
                var controller = root.AddComponent<GameSessionController>();
                controller.Model.Start();
                var ui = root.AddComponent<GameplayUI>();
                var serialized = new SerializedObject(ui);
                var mode = serialized.FindProperty("referenceHud");
                Assert.That(mode, Is.Not.Null, "Missing reference formatting mode");
                mode.boolValue = true;
                foreach (var field in new[] { "time", "score", "streakLabel" })
                {
                    var label = new GameObject(field, typeof(RectTransform), typeof(Text));
                    label.transform.SetParent(root.transform);
                    serialized.FindProperty(field).objectReferenceValue = label.GetComponent<Text>();
                }
                var hearts = serialized.FindProperty("heartImages"); hearts.arraySize = 3;
                for (int i = 0; i < 3; i++)
                {
                    var heart = new GameObject("Heart " + i, typeof(RectTransform), typeof(Image));
                    heart.transform.SetParent(root.transform);
                    hearts.GetArrayElementAtIndex(i).objectReferenceValue = heart.GetComponent<Image>();
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                ui.Bind(controller);
                Assert.That(root.transform.Find("time").GetComponent<Text>().text, Is.EqualTo("01:00"));
                controller.Model.Resolve(1, ObjectKind.Food, ObjectOutcome.Caught, 10);
                ui.Refresh();
                Assert.That(root.transform.Find("streakLabel").GetComponent<Text>().text, Is.EqualTo("1 IN A ROW"));
                controller.Model.Resolve(2, ObjectKind.Food, ObjectOutcome.Missed, 10);
                ui.Refresh();
                Assert.That(root.transform.Find("score").GetComponent<Text>().text, Is.EqualTo("10"));
                Assert.That(root.transform.Find("streakLabel").GetComponent<Text>().text, Is.EqualTo("0 IN A ROW"));
                Assert.That(root.transform.Find("Heart 2").GetComponent<Image>().color,
                    Is.Not.EqualTo(root.transform.Find("Heart 0").GetComponent<Image>().color));
                controller.Model.Resolve(3, ObjectKind.Food, ObjectOutcome.Missed, 10);
                controller.Model.Resolve(4, ObjectKind.Food, ObjectOutcome.Missed, 10);
                controller.Model.CompleteStep(60);
                ui.Refresh();
                Assert.That(root.transform.Find("time").GetComponent<Text>().text, Is.EqualTo("00:00"));
                Assert.That(root.transform.Find("Heart 0").GetComponent<Image>().color,
                    Is.EqualTo(root.transform.Find("Heart 2").GetComponent<Image>().color));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
