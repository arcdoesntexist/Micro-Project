using System;
using UnityEngine;

namespace CPS.KitchenCatch
{
    public sealed class MascotBodyPalette : ScriptableObject
    {
        [Serializable]
        public struct Variant
        {
            public string ColorHex;
            public Sprite[] Frames;
        }

        [SerializeField] private Variant[] variants;
        private static MascotBodyPalette instance;

        public static Sprite ForFrame(Sprite frame, string colorHex)
        {
            if (frame == null) return null;
            if (instance == null) instance = Resources.Load<MascotBodyPalette>("MascotBodyPalette");
            if (instance == null) return frame;
            foreach (var variant in instance.variants)
            {
                if (!string.Equals(variant.ColorHex, colorHex, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var candidate in variant.Frames)
                    if (candidate != null && candidate.name == frame.name) return candidate;
            }
            return frame;
        }
    }
}
