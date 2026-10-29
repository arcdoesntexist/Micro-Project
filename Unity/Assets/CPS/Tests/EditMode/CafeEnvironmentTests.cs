using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CPS.KitchenCatch.Tests
{
    public sealed class CafeEnvironmentTests
    {
        [Test]
        public void AuthoringKeepsOneSelectorAndSixPropsWithControllerBinding()
        {
            CPS.KitchenCatch.Editor.CafeEnvironmentSetup.Apply();
            CPS.KitchenCatch.Editor.CafeEnvironmentSetup.Apply();
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var roots=scene.GetRootGameObjects();
            var environment=GameObject.Find("LevelEnvironment").GetComponent<LevelEnvironment>();
            Assert.That(Object.FindObjectsByType<LevelEnvironment>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            var serialized=new SerializedObject(environment);
            var cafe=(GameObject)serialized.FindProperty("cafeAmbient").objectReferenceValue;
            Assert.That(cafe.transform.childCount,Is.EqualTo(6));
            var flow=Object.FindFirstObjectByType<GameSessionController>();
            Assert.That(serialized.FindProperty("controller").objectReferenceValue,Is.SameAs(flow));
            Assert.That(cafe.activeSelf,Is.False);
        }
        [Test]
        public void BackgroundsMatchKitchenWorldBoundsAndPropsHaveCustomPivots()
        {
            var kitchen=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CPS/Art/Kitchen/kitchen-background-v1.png");
            foreach(string name in new[]{"day","night"})
            {
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CPS/Art/Cafe/cafe-"+name+"-background-v1.png");
                Assert.That(sprite,Is.Not.Null);Assert.That(sprite.bounds.size,Is.EqualTo(kitchen.bounds.size));
            }
            var assets=AssetDatabase.LoadAllAssetsAtPath("Assets/CPS/Art/Cafe/cafe-props-atlas-v1.png");int count=0;
            foreach(var asset in assets)if(asset is Sprite sprite)
            {
                bool hanging=sprite.name=="Curtain"||sprite.name=="PendantLamp"||sprite.name=="HangingIvy";
                Assert.That(sprite.pivot.y,Is.EqualTo(hanging?sprite.rect.height:0).Within(.01f));count++;
            }
            Assert.That(count,Is.EqualTo(6));
        }
        [Test]
        public void CafeSwitchingRestoresKitchenAndRejectsInvalidAnimationSteps()
        {
            var root=new GameObject("Environment test");root.SetActive(false);
            try
            {
                var environment=root.AddComponent<LevelEnvironment>();
                var renderer=root.AddComponent<SpriteRenderer>();
                var kitchen=new GameObject("Kitchen");kitchen.transform.SetParent(root.transform);
                var cafe=new GameObject("Cafe");cafe.transform.SetParent(root.transform);
                var prop=new GameObject("Curtain");prop.transform.SetParent(cafe.transform);
                var data=new SerializedObject(environment);
                data.FindProperty("background").objectReferenceValue=renderer;
                data.FindProperty("kitchenAmbient").objectReferenceValue=kitchen;
                data.FindProperty("cafeAmbient").objectReferenceValue=cafe;
                data.FindProperty("kitchenSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CPS/Art/Kitchen/kitchen-background-v1.png");
                data.FindProperty("daySprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CPS/Art/Cafe/cafe-day-background-v1.png");
                data.FindProperty("nightSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CPS/Art/Cafe/cafe-night-background-v1.png");
                data.FindProperty("swayingProps").arraySize=1;data.FindProperty("swayingProps").GetArrayElementAtIndex(0).objectReferenceValue=prop.transform;data.ApplyModifiedPropertiesWithoutUndo();
                environment.Apply(DifficultyId.Normal);Assert.That(renderer.sprite.texture.name,Is.EqualTo("cafe-day-background-v1"));
                Assert.That(cafe.activeSelf,Is.True);Assert.That(kitchen.activeSelf,Is.False);
                environment.StepAmbient(1);Assert.That(prop.transform.localRotation,Is.Not.EqualTo(Quaternion.identity));
                var rotation=prop.transform.localRotation;environment.StepAmbient(float.NaN);environment.StepAmbient(-1);Assert.That(prop.transform.localRotation,Is.EqualTo(rotation));
                environment.Apply(DifficultyId.Hard);Assert.That(renderer.sprite.texture.name,Is.EqualTo("cafe-night-background-v1"));
                environment.Apply(DifficultyId.Easy);Assert.That(kitchen.activeSelf,Is.True);Assert.That(cafe.activeSelf,Is.False);
                Assert.That(renderer.sprite.texture.name,Is.EqualTo("kitchen-background-v1"));
                Assert.That(prop.transform.localRotation,Is.EqualTo(Quaternion.identity));
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
