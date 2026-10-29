using System;
using UnityEngine;
namespace CPS.KitchenCatch
{
    public sealed class ControllerInput : MonoBehaviour, IPlayerInput
    {
        [SerializeField] private SerialConnection serial;
        [SerializeField] private KeyboardInput keyboard;
        [SerializeField] private ControllerSettings settings;
        public const string PreferenceKey = "CPS.ControllerMode.v1";
        private static ControllerInput instance;
        private ControllerMode mode;
        private ControllerMode roundMode;
        private ControllerSettings roundSettings;
        private bool initialized;
        private bool frozen;
        private bool hasFocus = true;
        private bool leftPressed;
        private bool rightPressed;
        private int generation;
        private uint bootRevision;
        private uint tareId;
        public ControllerMode SelectedMode { get { EnsureDependencies(); return frozen ? roundMode : mode; } }
        public bool IsFrozen => frozen;
        public SerialConnection Connection { get { EnsureDependencies(); return serial; } }
        private ControllerSettings CurrentSettings => roundSettings != null ? roundSettings : settings;
        public static ControllerInput EnsureExists()
        {
            if (instance == null) instance = new GameObject("Player Controllers").AddComponent<ControllerInput>();
            instance.EnsureDependencies(); return instance;
        }
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            EnsureDependencies();
        }
        private void EnsureDependencies()
        {
            if (!initialized)
            {
                int saved = PlayerPrefs.GetInt(PreferenceKey, 0);
                mode = Enum.IsDefined(typeof(ControllerMode), saved) ? (ControllerMode)saved : ControllerMode.Keyboard;
                initialized = true;
            }
            if (serial == null) serial = SerialConnection.EnsureExists();
            if (keyboard == null) keyboard = GetComponent<KeyboardInput>() ?? gameObject.AddComponent<KeyboardInput>();
            if (settings == null) settings = Resources.Load<ControllerSettings>("ControllerSettings");
            if (settings == null) { settings = ScriptableObject.CreateInstance<ControllerSettings>(); settings.hideFlags = HideFlags.HideAndDontSave; }
        }
        public void SelectMode(ControllerMode selected)
        {
            EnsureDependencies();
            if (!Enum.IsDefined(typeof(ControllerMode), selected)) throw new ArgumentOutOfRangeException(nameof(selected));
            if (frozen) return;
            mode = selected; ResetPress();
            PlayerPrefs.SetInt(PreferenceKey, (int)mode); PlayerPrefs.Save();
        }
        public void FreezeForRound()
        {
            EnsureDependencies(); ReleaseRound();
            roundMode = mode; roundSettings = Instantiate(settings); roundSettings.hideFlags = HideFlags.HideAndDontSave;
            frozen = true; ResetPress(); serial.ControlsLocked = true;
        }
        public void ReleaseRound()
        {
            frozen = false; ResetPress();
            if (serial != null) serial.ControlsLocked = false;
            if (roundSettings != null)
            {
                if (Application.isPlaying) Destroy(roundSettings); else DestroyImmediate(roundSettings);
                roundSettings = null;
            }
        }
        public bool CanPlay(out string reason) => CanPlayAt(SerialConnection.NowSeconds, out reason);
        public bool CanPlayAt(double now, out string reason)
        {
            EnsureDependencies(); reason = null;
            if (SelectedMode == ControllerMode.Keyboard) return true;
            reason = CurrentSettings.ValidationError;
            if (reason != null) return false;
            if (!serial.IsFresh(now) || now - serial.Packets.ReceivedAtSeconds >= CurrentSettings.staleSeconds)
            { reason = "Connect USB and wait for fresh controller data."; return false; }
            if (SelectedMode == ControllerMode.LoadCell)
            {
                var packet = serial.Packets.Latest;
                if (!serial.LoadTareReady) { reason = "Unload both load cells, then press Tare."; return false; }
                double age = now - serial.Packets.ReceivedAtSeconds;
                if (!packet.LoadLeftAgeMs.HasValue || !packet.LoadRightAgeMs.HasValue ||
                    packet.LoadLeftAgeMs.Value / 1000d + age >= CurrentSettings.staleSeconds ||
                    packet.LoadRightAgeMs.Value / 1000d + age >= CurrentSettings.staleSeconds)
                { reason = "Load Cell sample is stale. Check both sensors."; return false; }
            }
            return true;
        }
        public InputSnapshot Read() => ReadAt(SerialConnection.NowSeconds);
        public InputSnapshot ReadAt(double now)
        {
            EnsureDependencies(); ControllerMode selected = SelectedMode;
            if (!hasFocus) { ResetPress(); return new InputSnapshot(default, default, selected.ToString()); }
            if (selected == ControllerMode.Keyboard) return keyboard.Read();
            if (!CanPlayAt(now, out _)) { ResetPress(); return new InputSnapshot(default, default, selected.ToString()); }
            var packet = serial.Packets.Latest;
            if (generation != serial.ConnectionGeneration || bootRevision != serial.Packets.BootRevision ||
                (selected == ControllerMode.LoadCell && tareId != packet.TareId)) ResetPress();
            generation = serial.ConnectionGeneration; bootRevision = serial.Packets.BootRevision; tareId = packet.TareId;
            var config = CurrentSettings;
            float left = selected == ControllerMode.FSR ? packet.FsrLeftPercent : Math.Max(0, packet.LoadLeftN.Value);
            float right = selected == ControllerMode.FSR ? packet.FsrRightPercent : Math.Max(0, packet.LoadRightN.Value);
            leftPressed = Press(leftPressed, left, selected == ControllerMode.FSR ? config.fsrLeftPress : config.loadLeftPressN,
                selected == ControllerMode.FSR ? config.fsrLeftRelease : config.loadLeftReleaseN);
            rightPressed = Press(rightPressed, right, selected == ControllerMode.FSR ? config.fsrRightPress : config.loadRightPressN,
                selected == ControllerMode.FSR ? config.fsrRightRelease : config.loadRightReleaseN);
            bool percent = selected == ControllerMode.FSR;
            return new InputSnapshot(new HandInput(leftPressed, percent ? left / 100 : 0, true, percent ? (float?)null : left, percent),
                new HandInput(rightPressed, percent ? right / 100 : 0, true, percent ? (float?)null : right, percent),
                selected.ToString(), packet.CalibrationVersion);
        }
        private static bool Press(bool wasPressed, float value, float press, float release) => wasPressed ? value > release : value >= press;
        private void ResetPress() { leftPressed = rightPressed = false; }
        private void OnApplicationFocus(bool focused) { hasFocus = focused; if (!focused) ResetPress(); }
        private void OnDestroy()
        {
            ReleaseRound();
            if (settings != null && (settings.hideFlags & HideFlags.HideAndDontSave) != 0)
            { if (Application.isPlaying) Destroy(settings); else DestroyImmediate(settings); }
            if (instance == this) instance = null;
        }
    }
}
