using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CPS.KitchenCatch
{
    public sealed class VersusSensorChannel
    {
        private static readonly Regex Raw = new Regex(@"\AFSR raw L=([0-9]{1,4}) R=([0-9]{1,4})\z", RegexOptions.CultureInvariant);
        private readonly int player;
        private readonly VersusControllerSettings settings;
        private readonly SerialLineBuffer lines = new SerialLineBuffer();
        private readonly SensorPacketReader packets = new SensorPacketReader();
        private bool hasSample;
        private double receivedAt;
        private float left;
        private float right;
        private bool leftPressed;
        private bool rightPressed;
        public uint Revision => packets.BootRevision;
        public string Calibration => player == 1 ? packets.Latest?.CalibrationVersion : "stm32-raw-provisional";

        public VersusSensorChannel(int player, VersusControllerSettings settings)
        {
            if (player != 1 && player != 2) throw new ArgumentOutOfRangeException(nameof(player));
            this.player = player;
            this.settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
        }

        public void Receive(string chunk, double now)
        {
            if (!double.IsFinite(now) || now < 0 || hasSample && now < receivedAt || settings.ValidationError != null) return;
            lines.Push(chunk, line =>
            {
                float nextLeft;
                float nextRight;
                if (player == 1)
                {
                    uint previous = packets.BootRevision;
                    if (!packets.TryAccept(line, now, out _)) return;
                    if (previous != packets.BootRevision) leftPressed = rightPressed = false;
                    nextLeft = packets.Latest.FsrLeftPercent;
                    nextRight = packets.Latest.FsrRightPercent;
                }
                else
                {
                    var match = Raw.Match(line);
                    if (!match.Success) return;
                    int l = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    int r = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    if (l > 4095 || r > 4095) return;
                    nextLeft = Percent(l); nextRight = Percent(r);
                }
                if (!IsFresh(now)) leftPressed = rightPressed = false;
                left = nextLeft; right = nextRight; receivedAt = now; hasSample = true;
                leftPressed = EnabledLeft && Press(leftPressed, left);
                rightPressed = EnabledRight && Press(rightPressed, right);
            });
        }

        public bool IsFresh(double now) => settings.ValidationError == null && hasSample && double.IsFinite(now) &&
            now >= receivedAt && now - receivedAt < settings.staleSeconds;

        public InputSnapshot Read(double now)
        {
            if (!IsFresh(now))
            {
                leftPressed = rightPressed = false;
                return new InputSnapshot(default, default, "FSR P" + player);
            }
            return new InputSnapshot(new HandInput(leftPressed, left / 100, EnabledLeft, null),
                new HandInput(rightPressed, right / 100, EnabledRight, null), "FSR P" + player, Calibration);
        }

        public void Reset()
        {
            lines.Reset(); packets.Reset(); hasSample = leftPressed = rightPressed = false;
            left = right = 0; receivedAt = 0;
        }
        private bool EnabledLeft => player == 1 ? settings.esp32LeftEnabled : settings.stm32LeftEnabled;
        private bool EnabledRight => player == 1 ? settings.esp32RightEnabled : settings.stm32RightEnabled;
        private float Percent(int raw) => Mathf.Clamp01((settings.stm32ReleasedRaw - raw) /
            (float)(settings.stm32ReleasedRaw - settings.stm32ReferenceRaw)) * 100;
        private bool Press(bool previous, float percent) => previous ? percent > settings.releasePercent : percent >= settings.pressPercent;
    }
}
