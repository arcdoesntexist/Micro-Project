using System;
using UnityEngine;
using UnityEngine.UI;

namespace CPS.KitchenCatch
{
    public sealed class VersusControllerPanel : MonoBehaviour
    {
        private VersusControllerHub hub;
        private Font font;
        private Sprite frame;
        private Sprite orange;
        private Sprite cocoa;
        private GameObject overlay;
        private readonly Text[] ports = new Text[2];
        private readonly Text[] statuses = new Text[2];
        private readonly Text[] connectLabels = new Text[2];
        private readonly string[] selected = new string[2];
        private string[] available = Array.Empty<string>();
        private static readonly Color Ink = new Color(.357f, .165f, .094f);

        public static VersusControllerPanel Create(Canvas canvas, Font font, Sprite frame, Sprite orange, Sprite cocoa)
        {
            var component = new GameObject("Versus Controller Panel").AddComponent<VersusControllerPanel>();
            component.transform.SetParent(canvas.transform, false);
            component.hub = VersusControllerHub.EnsureExists();
            component.font = font; component.frame = frame; component.orange = orange; component.cocoa = cocoa;
            component.Build(canvas.transform);
            return component;
        }
        public Button AddButton(Transform parent, Vector2 position, Vector2 size) =>
            Button(parent, "CONTROLLERS", position, size, Show);
        public void Show()
        {
            RefreshPorts();
            overlay.SetActive(true); overlay.transform.SetAsLastSibling(); hub.SuppressInput = true;
        }
        public void Hide() { overlay.SetActive(false); hub.SuppressInput = false; }
        private void Build(Transform parent)
        {
            overlay = VersusGameplayUI.Rect(parent, "Controllers overlay", Vector2.zero, new Vector2(1920, 1080)).gameObject;
            overlay.AddComponent<Image>().color = new Color(.1f, .03f, .01f, .82f);
            var card = Picture(overlay.transform, "Controller card", Vector2.zero, new Vector2(1340, 880));
            Label(card, "CONTROLLERS", new Vector2(0, 355), new Vector2(1150, 70), 48);
            Label(card, "Keyboard + FSR · choose one USB port per player", new Vector2(0, 286), new Vector2(1190, 50), 29);
            for (int i = 0; i < 2; i++)
            {
                int index = i;
                var column = Picture(card, "P" + (i + 1) + " controller", new Vector2(i == 0 ? -312 : 312, 0), new Vector2(584, 470));
                Label(column, i == 0 ? "P1 · ESP32" : "P2 · STM32", new Vector2(0, 174), new Vector2(500, 55), 37);
                var choose = Button(column, "CHOOSE COM PORT", new Vector2(0, 85), new Vector2(480, 72), () => CyclePort(index));
                ports[i] = choose.GetComponentInChildren<Text>();
                var connect = Button(column, "CONNECT", new Vector2(0, -5), new Vector2(480, 72), () => Toggle(index));
                connectLabels[i] = connect.GetComponentInChildren<Text>();
                statuses[i] = Label(column, "Keyboard only", new Vector2(0, -100), new Vector2(490, 95), 26);
                Label(column, i == 0 ? "A / D or left / right FSR" : "Left / Right arrows or right FSR\nLeft FSR disabled", new Vector2(0, -180), new Vector2(500, 65), 24);
            }
            Label(card, VersusControllerHub.NativeSupported ? "STM32 strength is provisional. FSR values are relative, not N." :
                "Dual USB requires Windows Editor/player. Use keyboard in this browser.", new Vector2(0, -282), new Vector2(1190, 55), 25);
            Button(card, "REFRESH PORTS", new Vector2(-290, -363), new Vector2(470, 78), RefreshPorts);
            Button(card, "DONE", new Vector2(290, -363), new Vector2(470, 78), Hide);
            overlay.SetActive(false);
        }
        private void RefreshPorts()
        {
            available = hub.RefreshPorts();
            for (int i = 0; i < 2; i++)
                if (hub.Port(i + 1) != null) selected[i] = hub.Port(i + 1);
                else if (Array.IndexOf(available, selected[i]) < 0) selected[i] = null;
            Refresh();
        }
        private void CyclePort(int index)
        {
            if (available.Length == 0) { RefreshPorts(); return; }
            int next = (Array.IndexOf(available, selected[index]) + 1) % available.Length;
            selected[index] = available[next]; Refresh();
        }
        private void Toggle(int index)
        {
            if (hub.Port(index + 1) != null) hub.Disconnect(index + 1);
            else hub.Connect(index + 1, selected[index]);
            Refresh();
        }
        private void Update() { if (overlay != null && overlay.activeSelf) Refresh(); }
        private void Refresh()
        {
            for (int i = 0; i < 2; i++)
            {
                ports[i].text = selected[i] ?? (available.Length == 0 ? "NO COM PORTS · REFRESH" : "CHOOSE COM PORT >");
                connectLabels[i].text = hub.Port(i + 1) == null ? "CONNECT" : "DISCONNECT";
                statuses[i].text = hub.Settings.ValidationError ?? hub.Status(i + 1);
                if (hub.Port(i + 1) != null && !hub.IsFresh(i + 1, SerialConnection.NowSeconds))
                    statuses[i].text += "\nWaiting for fresh data...";
            }
        }
        private RectTransform Picture(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var image = VersusGameplayUI.Picture(parent, name, frame, position, size);
            image.type = Image.Type.Sliced;
            return image.rectTransform;
        }
        private Text Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize)
        {
            var label = VersusGameplayUI.Rect(parent, text, position, size).gameObject.AddComponent<Text>();
            label.font = font; label.fontSize = fontSize; label.text = text; label.color = Ink;
            label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false; label.supportRichText = false;
            return label;
        }
        private Button Button(Transform parent, string text, Vector2 position, Vector2 size, Action action)
        {
            var image = VersusGameplayUI.Picture(parent, text, text == "DONE" || text == "CONNECT" ? orange : cocoa, position, size);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.normalColor = colors.selectedColor = Color.white;
            colors.highlightedColor = new Color(1, .93f, .8f); button.colors = colors;
            button.onClick.AddListener(() => { LocalDemoAudio.NotifyUserInteraction(); action(); });
            var label = Label(image.transform, text, Vector2.zero, size - new Vector2(40, 14), 28);
            label.color = text == "DONE" || text == "CONNECT" ? Ink : new Color(1, .965f, .84f);
            return button;
        }
        private void OnDestroy() { if (hub != null) hub.SuppressInput = false; }
    }
}
