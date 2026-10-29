using System;
using UnityEngine;

namespace CPS.KitchenCatch
{
    public static class GlobalAudioSettings
    {
        public const string VolumeKey = "CPS.Global.MasterVolume";
        public const string BackgroundMusicKey = "CPS.Global.BackgroundMusic";

        public static float MasterVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        public static bool BackgroundMusicEnabled => PlayerPrefs.GetInt(BackgroundMusicKey, 1) != 0;
        public static event Action Changed;

        public static void SetMasterVolume(float value)
        {
            PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void SetBackgroundMusicEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(BackgroundMusicKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static float VolumeToDecibels(float value)
        {
            value = Mathf.Clamp01(value);
            return value <= 0f ? -80f : 20f * Mathf.Log10(value) + 12f;
        }
    }
}
