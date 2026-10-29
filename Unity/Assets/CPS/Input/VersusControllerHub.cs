using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CPS.KitchenCatch
{
    public sealed class VersusControllerHub : MonoBehaviour
    {
        public static VersusControllerHub Instance { get; private set; }
        public static bool NativeSupported => Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer;
        public VersusControllerSettings Settings { get; private set; }
        public bool SuppressInput { get; set; }
        public bool ControlsLocked { get; set; }
        private sealed class Slot
        {
            public Inbox Inbox = new Inbox();
            public VersusSensorChannel Channel;
            public string Port;
            public string Status = "Keyboard only";
            public int Generation;
            public bool Connected;
            public bool Required;
            public uint Boot;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            public NativeSerialTransport Transport;
#endif
        }
        private sealed class Inbox
        {
            public readonly ConcurrentQueue<Action> Pending = new ConcurrentQueue<Action>();
            public int Overflow;
        }
        private readonly Slot[] slots = { new Slot(), new Slot() };
        public static VersusControllerHub EnsureExists()
        {
            if (Instance == null) new GameObject("Versus USB Controllers").AddComponent<VersusControllerHub>();
            return Instance;
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Settings = Resources.Load<VersusControllerSettings>("VersusControllerSettings");
            if (Settings == null) { Settings = ScriptableObject.CreateInstance<VersusControllerSettings>(); ownsSettings = true; }
            for (int i = 0; i < slots.Length; i++) slots[i].Channel = new VersusSensorChannel(i + 1, Settings);
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            SceneManager.activeSceneChanged += SceneChanged;
        }
        private bool ownsSettings;
        private Slot Player(int player)
        {
            if (player < 1 || player > 2) throw new ArgumentOutOfRangeException(nameof(player));
            return slots[player - 1];
        }
        public string Port(int player) => Player(player).Port;
        public int Generation(int player) => Player(player).Generation;
        public string Status(int player) => Player(player).Status;
        public bool IsFresh(int player, double now) => Player(player).Connected && Player(player).Channel.IsFresh(now);
        public InputSnapshot Read(int player) => ReadAt(player, SerialConnection.NowSeconds);
        public InputSnapshot ReadAt(int player, double now) => Player(player).Connected && !SuppressInput
            ? Player(player).Channel.Read(now) : new InputSnapshot(default, default, "FSR P" + player);

        public string[] RefreshPorts()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            try { var ports = NativeSerialTransport.Ports(); Array.Sort(ports, StringComparer.OrdinalIgnoreCase); return ports; }
            catch (Exception) { }
#endif
            return Array.Empty<string>();
        }
        public bool BeginSession(int player, string port)
        {
            var slot = Player(player);
            if (ControlsLocked) { slot.Status = "Pause before changing controllers."; return false; }
            if (string.IsNullOrWhiteSpace(port)) { slot.Status = "Select a COM port."; return false; }
            var other = slots[2 - player];
            if (string.Equals(port, other.Port, StringComparison.OrdinalIgnoreCase))
            { slot.Status = "This port is assigned to the other player."; return false; }
            Disconnect(player);
            slot.Port = port; slot.Status = "Connecting " + port + "...";
            return true;
        }
        public bool Connect(int player, string port)
        {
            var slot = Player(player);
            if (!NativeSupported) { slot.Status = "Dual USB requires Windows Editor/player. Keyboard remains available."; return false; }
            if (!BeginSession(player, port)) return false;
            var single = FindFirstObjectByType<SerialConnection>();
            if (single != null)
            {
                if (single.ControlsLocked) { Disconnect(player); slot.Status = "Finish the Single Player round before using Versus USB."; return false; }
                single.Disconnect();
            }
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            int generation = slot.Generation;
            var inbox = slot.Inbox;
            slot.Transport = new NativeSerialTransport((kind, text, now) =>
            {
                if (generation != slot.Generation) return;
                if (inbox.Pending.Count >= 64) { Interlocked.Exchange(ref inbox.Overflow, 1); return; }
                inbox.Pending.Enqueue(() => TransportEvent(player, generation, kind, text, now));
            });
            slot.Transport.Open(port);
#endif
            return true;
        }
        public void TransportEvent(int player, int generation, string kind, string text, double now)
        {
            var slot = Player(player);
            if (generation != slot.Generation || slot.Port == null) return;
            if (kind == "connected") { slot.Connected = true; slot.Status = "Connected; waiting for FSR data."; }
            else if (kind == "chunk" && slot.Connected)
            {
                slot.Channel.Receive(text, now);
                if (slot.Channel.IsFresh(now)) slot.Status = "FSR connected · " + slot.Port;
            }
            else if (kind == "error")
            {
                slot.Connected = false; slot.Channel.Reset();
                slot.Status = "USB error: " + (text ?? "Disconnected").Substring(0, Math.Min(text?.Length ?? 12, 150));
            }
        }
        public void Disconnect(int player)
        {
            var slot = Player(player);
            if (ControlsLocked) return;
            slot.Generation++;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            slot.Transport?.Dispose(); slot.Transport = null;
#endif
            slot.Connected = false; slot.Port = null; slot.Channel.Reset(); slot.Status = "Keyboard only";
            slot.Inbox = new Inbox();
        }
        public bool CanPlay(double now, out string reason)
        {
            reason = null;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].Port != null && !IsFresh(i + 1, now))
                { reason = "P" + (i + 1) + ": reconnect FSR or disconnect it to use keyboard."; return false; }
            return true;
        }
        public void WatchMatch()
        {
            foreach (var slot in slots)
            { slot.Required = slot.Port != null; slot.Boot = slot.Channel.Revision; }
        }
        public bool HasStreamLoss(double now)
        {
            foreach (var slot in slots)
                if (slot.Required && (!slot.Connected || !slot.Channel.IsFresh(now) || slot.Boot != slot.Channel.Revision)) return true;
            return false;
        }
        public void ContinueWithKeyboard()
        {
            if (ControlsLocked) return;
            for (int i = 0; i < slots.Length; i++) { Disconnect(i + 1); slots[i].Required = false; }
        }
        public void StopWatching() { foreach (var slot in slots) slot.Required = false; ControlsLocked = false; }
        private void Update()
        {
            foreach (var slot in slots)
            {
                var inbox = slot.Inbox;
                if (Interlocked.Exchange(ref inbox.Overflow, 0) != 0)
                {
                    slot.Generation++;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                    slot.Transport?.Dispose(); slot.Transport = null;
#endif
                    slot.Connected = false; slot.Channel.Reset(); slot.Inbox = new Inbox();
                    slot.Status = "USB overflow; reconnect controller.";
                    continue;
                }
                for (int i = 0; i < 64 && inbox.Pending.TryDequeue(out var action); i++) action();
            }
        }
        private void SceneChanged(Scene previous, Scene next)
        {
            SuppressInput = false;
            if (next.name != "VersusSetup" && next.name != "VersusGameplay") Destroy(gameObject);
            else if (next.name == "VersusSetup") StopWatching();
        }
        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.activeSceneChanged -= SceneChanged;
            ControlsLocked = false;
            for (int i = 1; i <= 2; i++) Disconnect(i);
            if (ownsSettings && Settings != null) Destroy(Settings);
            if (Instance == this) Instance = null;
        }
    }
}
