using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CPS.KitchenCatch
{
    public sealed class SensorPacket
    {
        public uint BootId { get; }
        public uint Sequence { get; }
        public uint UptimeMs { get; }
        public uint TareId { get; }
        public string CalibrationVersion { get; }
        public float FsrLeftPercent { get; }
        public float FsrRightPercent { get; }
        public float? LoadLeftN { get; }
        public float? LoadRightN { get; }
        public bool LoadLeftTared { get; }
        public bool LoadRightTared { get; }
        public bool LoadLeftValid { get; }
        public bool LoadRightValid { get; }
        public uint? LoadLeftAgeMs { get; }
        public uint? LoadRightAgeMs { get; }

        internal SensorPacket(JObject data)
        {
            BootId = data.Value<uint>("bootId");
            Sequence = data.Value<uint>("sequence");
            UptimeMs = data.Value<uint>("uptimeMs");
            TareId = data.Value<uint>("tareId");
            CalibrationVersion = data.Value<string>("calibrationVersion");
            FsrLeftPercent = data.Value<float>("fsrLeftPercent");
            FsrRightPercent = data.Value<float>("fsrRightPercent");
            LoadLeftN = data["loadLeftN"].Type == JTokenType.Null ? (float?)null : float.Parse(data["loadLeftN"].ToString(Formatting.None), CultureInfo.InvariantCulture);
            LoadRightN = data["loadRightN"].Type == JTokenType.Null ? (float?)null : float.Parse(data["loadRightN"].ToString(Formatting.None), CultureInfo.InvariantCulture);
            LoadLeftTared = data.Value<bool>("loadLeftTared");
            LoadRightTared = data.Value<bool>("loadRightTared");
            LoadLeftValid = data.Value<bool>("loadLeftValid");
            LoadRightValid = data.Value<bool>("loadRightValid");
            LoadLeftAgeMs = data.Value<uint?>("loadLeftAgeMs");
            LoadRightAgeMs = data.Value<uint?>("loadRightAgeMs");
        }
    }
}
