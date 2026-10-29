using NUnit.Framework;
using UnityEngine;

namespace CPS.KitchenCatch.Tests
{
    public sealed class PresentationTests
    {
        [Test]
        public void LeftMovementUsesLeftFacingReadableHatAndMirrorsItsOffset()
        {
            var root = new GameObject("Mascot");
            var rightTexture = new Texture2D(1, 1);
            var leftTexture = new Texture2D(1, 1);
            try
            {
                var body = new GameObject("Body").AddComponent<SpriteRenderer>();
                body.transform.SetParent(root.transform);
                var hat = new GameObject("Hat").AddComponent<SpriteRenderer>();
                hat.transform.SetParent(root.transform);
                hat.transform.localPosition = new Vector3(-.14f, 1.25f, 0);
                var rightHat = Sprite.Create(rightTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f);
                var leftHat = Sprite.Create(leftTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f);
                hat.sprite = rightHat;
                var presentation = root.AddComponent<MascotPresentation>();
                Assign(presentation, "body", body);
                Assign(presentation, "hat", hat);
                Assign(presentation, "rightFacingHat", rightHat);
                Assign(presentation, "leftFacingHat", leftHat);

                presentation.SetMovement(-1);

                Assert.That(body.flipX, Is.True);
                Assert.That(hat.flipX, Is.False);
                Assert.That(hat.sprite, Is.SameAs(leftHat));
                Assert.That(hat.transform.localPosition.x, Is.EqualTo(.14f).Within(.001f));

                presentation.SetMovement(1);

                Assert.That(body.flipX, Is.False);
                Assert.That(hat.sprite, Is.SameAs(rightHat));
                Assert.That(hat.transform.localPosition.x, Is.EqualTo(-.14f).Within(.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(rightTexture);
                Object.DestroyImmediate(leftTexture);
            }
        }

        [Test]
        public void DeathBurstContainsExactlyFourDecorativeColliderFreeObjects()
        {
            var root = new GameObject("Mascot");
            var prototype = new GameObject("FoodBurst").AddComponent<FoodBurst>();
            try
            {
                var presentation = root.AddComponent<MascotPresentation>();
                Assign(presentation, "burstPrefab", prototype);
                presentation.PlayTerminal(EndReason.HpDepleted);
                presentation.StepPresentation(.53f);

                var bursts = root.GetComponentsInChildren<FoodBurst>(true);
                Assert.That(bursts.Length, Is.EqualTo(4));
                foreach (var burst in bursts)
                {
                    Assert.That(burst.gameObject.activeSelf, Is.True);
                    Assert.That(burst.GetComponent<Collider2D>(), Is.Null);
                    Assert.That(burst.GetComponent<Collider>(), Is.Null);
                }
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(prototype.gameObject); }
        }

        [Test]
        public void ResetRemovesDeathTintAndDissolveAndReturnsToIdle()
        {
            var root = new GameObject("Mascot");
            try
            {
                var body = new GameObject("Body").AddComponent<SpriteRenderer>();
                body.transform.SetParent(root.transform);
                var hat = new GameObject("Hat").AddComponent<SpriteRenderer>();
                hat.transform.SetParent(root.transform);
                var presentation = root.AddComponent<MascotPresentation>();
                Assign(presentation, "body", body);
                Assign(presentation, "hat", hat);
                presentation.PlayTerminal(EndReason.HpDepleted);
                presentation.StepPresentation(1.2f);

                presentation.ResetPresentation();

                Assert.That(body.color, Is.EqualTo(Color.white));
                Assert.That(hat.color, Is.EqualTo(Color.white));
                Assert.That(body.enabled, Is.True);
                Assert.That(hat.enabled, Is.True);
                Assert.That(body.flipX, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void Assign(object target, string name, Object value)
        {
            var field = target.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
