using System;
using System.Text;
namespace CPS.KitchenCatch
{
    public sealed class SerialLineBuffer
    {
        private readonly StringBuilder pending = new StringBuilder(1024);
        private bool discarding;
        public void Push(string chunk, Action<string> onLine)
        {
            if (onLine == null) throw new ArgumentNullException(nameof(onLine));
            if (chunk == null) return;
            foreach (char c in chunk)
            {
                if (c == '\n')
                {
                    if (!discarding && pending.Length > 0) onLine(pending.ToString().TrimEnd('\r'));
                    Reset();
                }
                else if (!discarding)
                {
                    if (pending.Length == 1024) { pending.Clear(); discarding = true; }
                    else pending.Append(c);
                }
            }
        }
        public void Reset() { pending.Clear(); discarding = false; }
    }
}
