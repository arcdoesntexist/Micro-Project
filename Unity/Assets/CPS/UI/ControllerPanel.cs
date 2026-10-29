using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CPS.KitchenCatch
{
    public sealed class ControllerPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button launcher;
        [SerializeField] private Text launcherCaption;
        [SerializeField] private Button[] modeButtons;
        [SerializeField] private Text stateText;
        [SerializeField] private Text leftValue;
        [SerializeField] private Text rightValue;
        [SerializeField] private Text guide;
        [SerializeField] private Dropdown ports;
        [SerializeField] private Button refresh;
        [SerializeField] private Button connect;
        [SerializeField] private Button disconnect;
        [SerializeField] private Button tare;
        [SerializeField] private Button close;
        [SerializeField] private bool compactLauncher;
        private GameSessionController round;
        private GameObject previousSelection;
        private readonly Vector3[] corners = new Vector3[4];
        public bool IsOpen => panelRoot != null && panelRoot.activeInHierarchy;
        public GameObject PanelRoot => panelRoot;
        private ControllerInput Input => ControllerInput.EnsureExists();
        private void OnEnable()
        {
            if (modeButtons != null && modeButtons.Length == 3)
            {
                modeButtons[0].onClick.AddListener(Keyboard); modeButtons[1].onClick.AddListener(Fsr); modeButtons[2].onClick.AddListener(Load);
            }
            refresh?.onClick.AddListener(RefreshPorts); connect?.onClick.AddListener(Connect);
            disconnect?.onClick.AddListener(Disconnect); tare?.onClick.AddListener(Tare); close?.onClick.AddListener(Hide);
        }
        private void OnDisable()
        {
            if (modeButtons != null && modeButtons.Length == 3)
            {
                modeButtons[0].onClick.RemoveListener(Keyboard); modeButtons[1].onClick.RemoveListener(Fsr); modeButtons[2].onClick.RemoveListener(Load);
            }
            refresh?.onClick.RemoveListener(RefreshPorts); connect?.onClick.RemoveListener(Connect);
            disconnect?.onClick.RemoveListener(Disconnect); tare?.onClick.RemoveListener(Tare); close?.onClick.RemoveListener(Hide);
            if (Application.isPlaying) Input.Connection.HideWebConnect();
        }
        public void Show(GameSessionController controller = null)
        {
            if (controller != null && !controller.IsPaused) return;
            round = controller; previousSelection = EventSystem.current?.currentSelectedGameObject;
            panelRoot.SetActive(true); panelRoot.transform.SetAsLastSibling();
            if (!SerialConnection.IsWeb) RefreshPorts();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(close.gameObject);
            Refresh();
        }
        public void Hide()
        {
            panelRoot.SetActive(false); Input.Connection.HideWebConnect();
            if (previousSelection != null && previousSelection.activeInHierarchy && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection);
            round = null;
        }
        private void Keyboard() => Input.SelectMode(ControllerMode.Keyboard);
        private void Fsr() => Input.SelectMode(ControllerMode.FSR);
        private void Load() => Input.SelectMode(ControllerMode.LoadCell);
        private void RefreshPorts()
        {
            string previous = ports.options.Count > 0 ? ports.options[ports.value].text : null;
            var names = Input.Connection.RefreshPorts();
            ports.ClearOptions(); ports.AddOptions(new System.Collections.Generic.List<string>(names.Length > 0 ? names : new[] { "No serial ports found" }));
            int chosen = System.Array.IndexOf(names, previous); ports.SetValueWithoutNotify(Mathf.Max(0, chosen));
            ports.interactable = names.Length > 0;
        }
        private void Connect() { if (!SerialConnection.IsWeb && ports.interactable) Input.Connection.ConnectNative(ports.options[ports.value].text); }
        private void Disconnect() => Input.Connection.Disconnect();
        private void Tare() => Input.Connection.RequestTare();
        private void Update()
        {
            if (launcherCaption != null) launcherCaption.text = compactLauncher ? "Controller" : "Controller · " + ModeName(Input.SelectedMode);
            if (IsOpen) Refresh();
        }
        private static string ModeName(ControllerMode mode) => mode == ControllerMode.LoadCell ? "Load Cell" : mode.ToString();
        private void Refresh()
        {
            var input = Input; var serial = input.Connection; var mode = input.SelectedMode;
            bool ready = input.CanPlay(out string reason);
            stateText.text = mode == ControllerMode.Keyboard ? "Keyboard ready\nA / Left Arrow    D / B / Right Arrow" :
                serial.LastError ?? (ready ? ModeName(mode) + " ready" : reason);
            if (modeButtons != null)
                for (int i = 0; i < modeButtons.Length; i++)
                {
                    modeButtons[i].interactable = !input.IsFrozen;
                    var colors = modeButtons[i].colors; colors.normalColor = i == (int)mode ? Color.white : new Color(.72f, .72f, .72f);
                    colors.selectedColor = colors.highlightedColor = Color.white; modeButtons[i].colors = colors;
                }
            bool fresh = serial.IsFresh(SerialConnection.NowSeconds);
            var packet = fresh ? serial.Packets.Latest : null;
            leftValue.font = rightValue.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (mode == ControllerMode.Keyboard)
            {
                var value = input.Read(); leftValue.text = "LEFT\n" + (value.Left.Pressed ? "100%" : "0%"); rightValue.text = "RIGHT\n" + (value.Right.Pressed ? "100%" : "0%");
            }
            else if (mode == ControllerMode.FSR)
            {
                leftValue.text = "LEFT\n" + (packet != null ? packet.FsrLeftPercent.ToString("0.0") + "%" : "N/A");
                rightValue.text = "RIGHT\n" + (packet != null ? packet.FsrRightPercent.ToString("0.0") + "%" : "N/A");
            }
            else
            {
                double elapsed = packet == null ? double.MaxValue : SerialConnection.NowSeconds - serial.Packets.ReceivedAtSeconds;
                bool leftFresh = packet?.LoadLeftValid == true && packet.LoadLeftAgeMs.Value / 1000d + elapsed < .75;
                bool rightFresh = packet?.LoadRightValid == true && packet.LoadRightAgeMs.Value / 1000d + elapsed < .75;
                leftValue.text = "LEFT · 1 kg\n" + (leftFresh ? packet.LoadLeftN.Value.ToString("0.000") + " N" : "N/A");
                rightValue.text = "RIGHT · 5 kg\n" + (rightFresh ? packet.LoadRightN.Value.ToString("0.000") + " N" : "N/A");
            }
            ports.gameObject.SetActive(!SerialConnection.IsWeb); refresh.gameObject.SetActive(!SerialConnection.IsWeb);
            connect.interactable = !serial.IsConnected && (SerialConnection.IsWeb || ports.interactable);
            disconnect.interactable = serial.IsConnected; tare.interactable = serial.IsConnected && packet != null;
            tare.gameObject.SetActive(mode == ControllerMode.LoadCell);
            guide.text = mode == ControllerMode.LoadCell ? "Estimated Force (N). Unload BOTH cells, then Tare.\nSpace / Esc pauses or resumes when ready." :
                mode == ControllerMode.FSR ? "Relative press level, not force in N.\nConnect using Sensor_Controller firmware." : "Choose FSR or Load Cell to use the USB controller.\nKeyboard mode needs no USB connection.";
            if (SerialConnection.IsWeb && !serial.IsConnected)
            {
                var rect = (RectTransform)connect.transform; rect.GetWorldCorners(corners);
                var canvas = connect.GetComponentInParent<Canvas>().rootCanvas;
                var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                Vector2 topRight = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
                serial.ShowWebConnect(new Rect(bottomLeft.x / Screen.width, 1 - topRight.y / Screen.height,
                    (topRight.x - bottomLeft.x) / Screen.width, (topRight.y - bottomLeft.y) / Screen.height));
            }
            else serial.HideWebConnect();
        }
    }
}
