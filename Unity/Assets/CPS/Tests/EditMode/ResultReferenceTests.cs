using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CPS.KitchenCatch.Tests
{
    public sealed class ResultReferenceTests
    {
        [Test]
        public void ItemCountsIgnoreDuplicateMissedAndTerminalResolutionsAndCopyIndependently()
        {
            var model=new GameSessionModel();model.Start();
            model.Resolve(1,ObjectKind.Food,ObjectOutcome.Caught,10,0);
            model.Resolve(1,ObjectKind.Food,ObjectOutcome.Caught,10,0);
            model.Resolve(2,ObjectKind.Food,ObjectOutcome.Missed,15,1);
            model.Resolve(3,ObjectKind.Trash,ObjectOutcome.Caught,0,3);
            Assert.That(model.CaughtItems,Is.EqualTo(new[]{1,0,0,1,0}));
            model.CompleteStep(60);model.Resolve(4,ObjectKind.Food,ObjectOutcome.Caught,20,2);
            Assert.That(model.CaughtItems[2],Is.Zero);
            var result=new SessionResult{CaughtItems=(int[])model.CaughtItems.Clone()};var copy=result.Copy();
            copy.CaughtItems[0]=99;Assert.That(result.CaughtItems[0],Is.EqualTo(1));
            Assert.That(new SessionResult().Copy().CaughtItems,Is.Null);
        }
        [Test]
        public void ResultViewUsesSnapshotCountsHidesEasyTrashAndResetsDetails()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/GameplayCanvas.prefab");
            var root=Object.Instantiate(prefab);
            try
            {
                var view=root.GetComponentInChildren<ResultReferenceView>(true);Assert.That(view,Is.Not.Null);
                var result=new SessionResult{DisplayName="<b>arc</b>",Difficulty=DifficultyId.Normal,Score=215,FoodCaught=16,CaughtItems=new[]{8,5,3,0,0},HighestRank=ComboRank.S};
                view.Show(result);
                Assert.That(view.details.gameObject.activeSelf,Is.False);
                Assert.That(view.summary.Find("Count 0").GetComponent<Text>().text,Is.EqualTo("x 8"));
                Assert.That(view.summary.Find("Identity").GetComponent<Text>().supportRichText,Is.False);
                view.Toggle();Assert.That(view.details.gameObject.activeSelf,Is.True);
                Assert.That(view.summary.anchoredPosition,Is.EqualTo(view.openPosition));
                result.Difficulty=DifficultyId.Easy;view.Show(result);
                Assert.That(view.summary.Find("Item 3").gameObject.activeSelf,Is.False);
                Assert.That(view.details.gameObject.activeSelf,Is.False);
                Assert.That(view.details.Find("Round stats").GetComponent<Text>().text,Does.Not.Contain("Trash"));
                Assert.That(view.details.Find("Force").GetComponent<Text>().text,Does.Contain("Sensor not connected"));
                result.CaughtItems=null;view.Show(result);
                Assert.That(view.summary.Find("Count 0").GetComponent<Text>().text,Is.EqualTo("N/A"));
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
