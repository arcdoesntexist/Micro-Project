using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CPS.KitchenCatch.Tests
{
    public sealed class MascotBodyPaletteTests
    {
        [Test]
        public void EveryColorResolvesAllOriginalAnimationFramesWithMatchingGeometry()
        {
            var original = AssetDatabase.LoadAllAssetsAtPath("Assets/CPS/Art/Mascot/mascot-body-atlas-v1.png").OfType<Sprite>().ToArray();
            Assert.That(original, Has.Length.EqualTo(12));
            Assert.That(Resources.Load<MascotBodyPalette>("MascotBodyPalette"), Is.Not.Null);
            foreach (string color in new[] { "#F57C32", "#3C78D8", "#68A832", "#9B59B6", "#E6B422", "#D94A64" })
                foreach (var frame in original)
                {
                    var variant = MascotBodyPalette.ForFrame(frame, color);
                    Assert.That(variant, Is.Not.Null);
                    Assert.That(variant.name, Is.EqualTo(frame.name));
                    Assert.That(variant.rect, Is.EqualTo(frame.rect));
                    Assert.That(variant.pivot, Is.EqualTo(frame.pivot));
                    if (color == "#F57C32") Assert.That(variant, Is.SameAs(frame));
                    else Assert.That(variant.texture, Is.Not.SameAs(frame.texture), color + " " + frame.name);
                }
        }
    }
}
