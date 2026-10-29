using UnityEngine;

namespace CPS.KitchenCatch
{
    [CreateAssetMenu(menuName = "CPS/Versus Controller Settings")]
    public sealed class VersusControllerSettings : ScriptableObject
    {
        public bool esp32LeftEnabled = true;
        public bool esp32RightEnabled = true;
        public bool stm32LeftEnabled;
        public bool stm32RightEnabled = true;
        [Range(0, 4095)] public int stm32ReleasedRaw = 4095;
        [Range(0, 4095)] public int stm32ReferenceRaw = 100;
        public float pressPercent = 20;
        public float releasePercent = 15;
        public float staleSeconds = .75f;
        public string ValidationError => stm32ReleasedRaw > 4095 || stm32ReferenceRaw < 0 ||
            stm32ReleasedRaw <= stm32ReferenceRaw ? "STM32 reference must be below released raw (0..4095)." :
            !float.IsFinite(pressPercent) || !float.IsFinite(releasePercent) || releasePercent < 0 ||
            pressPercent <= releasePercent || pressPercent > 100 ? "Release must be below press (0..100%)." :
            !float.IsFinite(staleSeconds) || staleSeconds <= 0 || staleSeconds > .75f ? "USB timeout must be within 0..0.75 seconds." : null;
    }
}
