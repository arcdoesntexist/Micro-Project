using UnityEngine;

namespace CPS.KitchenCatch.Tests
{
    public sealed class UsbFixtureFeed : MonoBehaviour
    {
        public SerialConnection Connection;
        public string Template;
        public bool Running;
        public uint TareId = 1;
        private uint sequence;
        public void Emit()
        {
            string frame = Template.Replace("\"sequence\":12", "\"sequence\":" + ++sequence)
                .Replace("\"uptimeMs\":700", "\"uptimeMs\":" + sequence * 50)
                .Replace("\"tareId\":1", "\"tareId\":" + TareId);
            Connection.Receive(frame + "\n", Connection.ConnectionGeneration, SerialConnection.NowSeconds);
        }
        private void Update() { if (Running) Emit(); }
    }
}
