#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Threading;

namespace CPS.KitchenCatch
{
    internal sealed class NativeSerialTransport : IDisposable
    {
        private static Type SerialType => Type.GetType("System.IO.Ports.SerialPort, System") ??
            throw new PlatformNotSupportedException("SerialPort is unavailable in this Unity runtime.");
        private readonly Action<string, string, double> receive;
        private readonly ConcurrentQueue<string> writes = new ConcurrentQueue<string>();
        private volatile bool stopped;
        private Thread worker;

        public NativeSerialTransport(Action<string, string, double> receive) { this.receive = receive; }
        public static string[] Ports() => (string[])SerialType.GetMethod("GetPortNames", Type.EmptyTypes).Invoke(null, null);
        public void Open(string name)
        {
            worker = new Thread(() => Run(name)) { IsBackground = true, Name = "KitchenCatch USB" };
            worker.Start();
        }
        public void Send(string command)
        {
            if (stopped || writes.Count >= 8) throw new InvalidOperationException("USB command queue is unavailable.");
            writes.Enqueue(command);
        }
        private void Run(string name)
        {
            object port = null;
            try
            {
                Type type = SerialType;
                port = Activator.CreateInstance(type, name, 115200);
                type.GetProperty("ReadTimeout").SetValue(port, 100);
                type.GetProperty("WriteTimeout").SetValue(port, 100);
                type.GetProperty("DtrEnable").SetValue(port, false);
                type.GetProperty("RtsEnable").SetValue(port, false);
                MethodInfo open = type.GetMethod("Open", Type.EmptyTypes);
                MethodInfo read = type.GetMethod("Read", new[] { typeof(byte[]), typeof(int), typeof(int) });
                MethodInfo write = type.GetMethod("Write", new[] { typeof(string) });
                PropertyInfo available = type.GetProperty("BytesToRead");
                open.Invoke(port, null);
                if (stopped) return;
                receive("connected", null, SerialConnection.NowSeconds);
                byte[] buffer = new byte[256];
                while (!stopped)
                {
                    while (writes.TryDequeue(out string command)) write.Invoke(port, new object[] { command });
                    int count = Math.Min((int)available.GetValue(port), buffer.Length);
                    if (count > 0)
                    {
                        int length = (int)read.Invoke(port, new object[] { buffer, 0, count });
                        receive("chunk", Encoding.ASCII.GetString(buffer, 0, length), SerialConnection.NowSeconds);
                    }
                    else Thread.Sleep(10);
                }
            }
            catch (Exception error)
            {
                if (!stopped) receive("error", (error.InnerException ?? error).Message, SerialConnection.NowSeconds);
            }
            finally
            {
                try { (port as IDisposable)?.Dispose(); }
                catch (Exception) { }
            }
        }
        public void Dispose()
        {
            stopped = true;
            if (worker != null && worker != Thread.CurrentThread) worker.Join(300);
        }
    }
}
#endif
