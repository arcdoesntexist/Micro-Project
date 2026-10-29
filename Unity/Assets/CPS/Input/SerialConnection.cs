using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using UnityEngine;

namespace CPS.KitchenCatch
{
    public sealed class SerialConnection : MonoBehaviour
    {
        public SensorPacketReader Packets { get; } = new SensorPacketReader();
        public int ConnectionGeneration { get; private set; }
        public bool IsConnected { get; private set; }
        public bool LoadTareReady => authorized && IsConnected && Packets.Latest != null &&
            Packets.Latest.LoadLeftTared && Packets.Latest.LoadRightTared && Packets.Latest.LoadLeftValid && Packets.Latest.LoadRightValid;
        public string Status { get; private set; } = "Disconnected";
        public string LastError { get; private set; }
        public bool ControlsLocked { get; set; }
        public event Action<string> SendCommand;
        private readonly SerialLineBuffer lines = new SerialLineBuffer();
        private readonly ConcurrentQueue<Action> pending = new ConcurrentQueue<Action>();
        private volatile bool overflow;
        private bool authorized;
        private bool awaitingTare;
        private uint expectedTare;
        private uint tareBoot;
        private uint authorizedTare;
        private bool webUnsupported;
        private static SerialConnection instance;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private NativeSerialTransport native;
#endif
        public static double NowSeconds => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        public static bool IsWeb => Application.platform == RuntimePlatform.WebGLPlayer;
        public static SerialConnection EnsureExists()
        {
            if (instance == null) instance = new GameObject("USB Serial Connection").AddComponent<SerialConnection>();
            return instance;
        }
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }
        public int BeginSession()
        {
            if (ControlsLocked) throw new InvalidOperationException("Pause the game before changing the USB connection.");
            ResetConnection();
            Status = "Ready to connect";
            LastError = null;
            return ConnectionGeneration;
        }
        private void ResetConnection()
        {
            ConnectionGeneration++;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            native?.Dispose(); native = null;
#endif
            if (IsWeb) WebSerialTransport.Close();
            SendCommand = null;
            while (pending.TryDequeue(out _)) { }
            overflow = false;
            IsConnected = authorized = awaitingTare = false;
            lines.Reset(); Packets.Reset();
            Status = "Disconnected";
        }
        public void TransportOpened(int generation)
        {
            if (generation != ConnectionGeneration) return;
            IsConnected = true;
            Status = "USB connected; waiting for telemetry";
        }
        public void Receive(string chunk, int generation, double receivedAtSeconds)
        {
            if (generation != ConnectionGeneration || !IsConnected) return;
            lines.Push(chunk, line =>
            {
                var previous = Packets.Latest;
                if (!Packets.TryAccept(line, receivedAtSeconds, out _)) return;
                var packet = Packets.Latest;
                if (previous == null || previous.BootId != packet.BootId)
                    authorized = awaitingTare = false;
                if (authorized && packet.TareId != authorizedTare) authorized = false;
                if (awaitingTare && packet.BootId == tareBoot &&
                    unchecked(packet.TareId - expectedTare) < 0x80000000u &&
                    packet.LoadLeftTared && packet.LoadRightTared && packet.LoadLeftValid && packet.LoadRightValid)
                {
                    authorized = true; awaitingTare = false; authorizedTare = packet.TareId;
                }
                Status = "USB telemetry connected";
                LastError = null;
            });
        }
        public bool RequestTare()
        {
            if (ControlsLocked || !IsConnected || Packets.Latest == null || SendCommand == null) return false;
            authorized = false; awaitingTare = true;
            expectedTare = unchecked(Packets.Latest.TareId + 1); tareBoot = Packets.Latest.BootId;
            try { SendCommand("t\n"); Status = "Taring both unloaded load cells..."; return true; }
            catch (Exception error) { awaitingTare = false; LastError = error.Message; return false; }
        }
        public void Disconnect() { if (!ControlsLocked) ResetConnection(); }
        public bool IsFresh(double nowSeconds) => IsConnected && Packets.Latest != null &&
            nowSeconds >= Packets.ReceivedAtSeconds && nowSeconds - Packets.ReceivedAtSeconds < .75;
        public string[] RefreshPorts()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            try { return NativeSerialTransport.Ports(); }
            catch (Exception error) { LastError = (error.InnerException ?? error).Message; }
#endif
            return Array.Empty<string>();
        }
        public void ConnectNative(string portName)
        {
            if (string.IsNullOrWhiteSpace(portName)) { LastError = "Choose a serial port first."; return; }
            int generation = BeginSession();
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            native = new NativeSerialTransport((kind, text, receivedAt) =>
            {
                if (pending.Count >= 64) { overflow = true; return; }
                pending.Enqueue(() =>
                {
                    if (generation != ConnectionGeneration) return;
                    if (kind == "connected") TransportOpened(generation);
                    else if (kind == "chunk") Receive(text, generation, receivedAt);
                    else Fault(text);
                });
            });
            SendCommand += native.Send;
            Status = "Connecting...";
            native.Open(portName);
#else
            Fault("Use the browser Connect USB button on Web, or Windows Unity Editor.");
#endif
        }
        public void ShowWebConnect(Rect normalizedRect)
        {
            if (!IsWeb || ControlsLocked || IsConnected || webUnsupported) { WebSerialTransport.Hide(); return; }
            if (ConnectionGeneration == 0) BeginSession();
            SendCommand -= WebSerialTransport.Write;
            SendCommand += WebSerialTransport.Write;
            WebSerialTransport.Show(gameObject.name, ConnectionGeneration, normalizedRect);
        }
        public void HideWebConnect() => WebSerialTransport.Hide();
        public void OnWebSerialChunk(string message)
        {
            if (Envelope(message, out int generation, out string text)) Receive(text, generation, NowSeconds);
        }
        public void OnWebSerialStatus(string message)
        {
            if (!Envelope(message, out int generation, out string text) || generation != ConnectionGeneration) return;
            if (text == "connected") TransportOpened(generation);
            else if (text == "connecting")
            {
                IsConnected = authorized = awaitingTare = false;
                Packets.Reset(); lines.Reset(); Status = "Connecting...";
            }
            else if (text == "cancelled") { Status = "Connection cancelled"; IsConnected = false; }
            else if (text == "disconnected") { Status = "Disconnected"; IsConnected = false; authorized = false; Packets.Reset(); }
            else if (text.StartsWith("error:") || text.StartsWith("unsupported:"))
            {
                if (text.StartsWith("unsupported:")) webUnsupported = true;
                Fault(text);
            }
        }
        private static bool Envelope(string message, out int generation, out string text)
        {
            generation = 0; text = null;
            if (string.IsNullOrEmpty(message) || message.Length > 4096) return false;
            int colon = message.IndexOf(':');
            if (colon <= 0 || !int.TryParse(message.Substring(0, colon), out generation)) return false;
            text = message.Substring(colon + 1); return true;
        }
        private void Fault(string error)
        {
            ResetConnection();
            LastError = string.IsNullOrEmpty(error) ? "USB connection failed." : error.Substring(0, Math.Min(error.Length, 200));
            Status = "USB connection unavailable";
        }
        private void Update()
        {
            if (overflow) { Fault("USB input overflow. Reconnect the controller."); return; }
            for (int i = 0; i < 64 && pending.TryDequeue(out var action); i++) action();
        }
        private void OnDestroy() { ResetConnection(); if (instance == this) instance = null; }
    }
}
