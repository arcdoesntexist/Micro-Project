using System.Runtime.InteropServices;
using UnityEngine;

namespace CPS.KitchenCatch
{
    internal static class WebSerialTransport
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void CpsSerial_Show(string target, int generation, float x, float y, float width, float height);
        [DllImport("__Internal")] private static extern void CpsSerial_Hide();
        [DllImport("__Internal")] private static extern void CpsSerial_Close();
        [DllImport("__Internal")] private static extern void CpsSerial_Write(string text);
#endif
        public static void Show(string target, int generation, Rect normalizedRect)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CpsSerial_Show(target, generation, normalizedRect.x, normalizedRect.y, normalizedRect.width, normalizedRect.height);
#endif
        }
        public static void Hide()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CpsSerial_Hide();
#endif
        }
        public static void Close()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CpsSerial_Close();
#endif
        }
        public static void Write(string text)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CpsSerial_Write(text);
#endif
        }
    }
}
