using UnityEngine;
using UnityEngine.UI;

namespace CPS.KitchenCatch
{
    [RequireComponent(typeof(Camera))]
    public sealed class GameplayViewport : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        private int lastWidth;
        private int lastHeight;
        public void Apply(int width, int height)
        {
            if (width <= 0 || height <= 0) return;
            float ratio = (float)width / height;
            const float target = 16f / 9;
            var rect = new Rect(0, 0, 1, 1);
            if (ratio > target) { rect.width = target / ratio; rect.x = (1 - rect.width) / 2; }
            else { rect.height = ratio / target; rect.y = (1 - rect.height) / 2; }
            var camera = GetComponent<Camera>();
            camera.rect = rect;
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.pixelPerfect = true;
                var scaler = canvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }
            lastWidth = width; lastHeight = height;
        }
        private void OnEnable() => Apply(Screen.width, Screen.height);
        private void Update()
        {
            if (lastWidth != Screen.width || lastHeight != Screen.height) Apply(Screen.width, Screen.height);
        }
    }
}
