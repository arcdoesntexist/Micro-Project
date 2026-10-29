using UnityEngine;
using UnityEngine.UI;

namespace CPS.KitchenCatch
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HeartIcon : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            mesh.AddVert(rect.center, color, Vector2.zero);
            const int segments = 48;
            for (int i = 0; i <= segments; i++)
            {
                float t = i * Mathf.PI * 2 / segments;
                float x = 16 * Mathf.Pow(Mathf.Sin(t), 3);
                float y = 13 * Mathf.Cos(t) - 5 * Mathf.Cos(2*t) - 2 * Mathf.Cos(3*t) - Mathf.Cos(4*t);
                mesh.AddVert(new Vector2(rect.xMin + (x + 16) / 32 * rect.width,
                    rect.yMin + (y + 17) / 29 * rect.height), color, Vector2.zero);
                if (i > 0) mesh.AddTriangle(0, i, i + 1);
            }
        }
    }
}
