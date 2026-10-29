using UnityEngine;

namespace CPS.KitchenCatch
{
    [CreateAssetMenu(menuName = "CPS/Falling Object")]
    public sealed class FallingObjectDefinition : ScriptableObject
    {
        public ObjectKind kind;
        public int points;
        public Sprite sprite;
        public int itemIndex = -1;
    }
}
