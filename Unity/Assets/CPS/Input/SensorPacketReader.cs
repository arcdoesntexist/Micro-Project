using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CPS.KitchenCatch
{
    public sealed class SensorPacketReader
    {
        public SensorPacket Latest { get; private set; }
        public double ReceivedAtSeconds { get; private set; }
        public uint BootRevision { get; private set; }
        private readonly Queue<uint> retiredBoots = new Queue<uint>();
        public bool TryAccept(string line, double receivedAtSeconds, out string error)
        {
            error = "Invalid sensor packet.";
            if (string.IsNullOrWhiteSpace(line) || line.Length > 1024 ||
                double.IsNaN(receivedAtSeconds) || double.IsInfinity(receivedAtSeconds) ||
                receivedAtSeconds < 0 || (Latest != null && receivedAtSeconds < ReceivedAtSeconds)) return false;
            try
            {
                JObject data = JObject.Parse(line, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (!Unsigned(data["version"]) || data.Value<uint>("version") != 1 ||
                    data["calibrationVersion"]?.Type != JTokenType.String ||
                    data.Value<string>("calibrationVersion") != "cps-2026-10-26-v1") return false;
                foreach (string name in new[] { "bootId", "sequence", "uptimeMs", "tareId" })
                    if (!Unsigned(data[name])) return false;
                foreach (string name in new[] { "fsrLeftPercent", "fsrRightPercent" })
                    if (!Number(data[name]) || data.Value<double>(name) < 0 || data.Value<double>(name) > 100) return false;
                foreach (string side in new[] { "Left", "Right" })
                {
                    string prefix = "load" + side;
                    if (data[prefix + "Tared"]?.Type != JTokenType.Boolean || data[prefix + "Valid"]?.Type != JTokenType.Boolean) return false;
                    if (data.Value<bool>(prefix + "Valid"))
                    {
                        if (!data.Value<bool>(prefix + "Tared") || !Number(data[prefix + "N"]) || !Unsigned(data[prefix + "AgeMs"])) return false;
                    }
                    else if (data[prefix + "N"]?.Type != JTokenType.Null || data[prefix + "AgeMs"]?.Type != JTokenType.Null) return false;
                }
                var packet = new SensorPacket(data);
                if (Latest != null)
                {
                    if (packet.BootId == Latest.BootId)
                    {
                        uint delta = unchecked(packet.Sequence - Latest.Sequence);
                        if (delta == 0 || delta >= 0x80000000u || unchecked(packet.UptimeMs - Latest.UptimeMs) >= 0x80000000u) return false;
                    }
                    else
                    {
                        if (retiredBoots.Contains(packet.BootId)) return false;
                        retiredBoots.Enqueue(Latest.BootId);
                        if (retiredBoots.Count > 16) retiredBoots.Dequeue();
                        BootRevision++;
                    }
                }
                else BootRevision++;
                Latest = packet;
                ReceivedAtSeconds = receivedAtSeconds;
                error = null;
                return true;
            }
            catch (Exception exception) when (exception is JsonException || exception is OverflowException || exception is FormatException || exception is ArgumentException)
            {
                return false;
            }
        }
        private static bool Unsigned(JToken token) => token?.Type == JTokenType.Integer &&
            uint.TryParse(token.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out _);
        private static bool Number(JToken token)
        {
            if (token == null || (token.Type != JTokenType.Integer && token.Type != JTokenType.Float)) return false;
            return double.TryParse(token.ToString(Formatting.None), NumberStyles.Float, CultureInfo.InvariantCulture, out double number) &&
                !double.IsNaN(number) && !double.IsInfinity(number) && number >= -float.MaxValue && number <= float.MaxValue;
        }
        public void Reset() { Latest = null; ReceivedAtSeconds = 0; retiredBoots.Clear(); BootRevision++; }
    }
}
