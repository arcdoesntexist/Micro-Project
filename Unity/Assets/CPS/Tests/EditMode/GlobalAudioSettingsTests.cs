using NUnit.Framework;
using UnityEngine;

namespace CPS.KitchenCatch.Tests
{
    public sealed class GlobalAudioSettingsTests
    {
        private bool hadVolume;
        private bool hadBackgroundMusic;
        private float savedVolume;
        private int savedBackgroundMusic;

        [SetUp]
        public void SetUp()
        {
            hadVolume = PlayerPrefs.HasKey(GlobalAudioSettings.VolumeKey);
            hadBackgroundMusic = PlayerPrefs.HasKey(GlobalAudioSettings.BackgroundMusicKey);
            savedVolume = PlayerPrefs.GetFloat(GlobalAudioSettings.VolumeKey);
            savedBackgroundMusic = PlayerPrefs.GetInt(GlobalAudioSettings.BackgroundMusicKey);
            PlayerPrefs.DeleteKey(GlobalAudioSettings.VolumeKey);
            PlayerPrefs.DeleteKey(GlobalAudioSettings.BackgroundMusicKey);
        }

        [TearDown]
        public void TearDown()
        {
            if (hadVolume) PlayerPrefs.SetFloat(GlobalAudioSettings.VolumeKey, savedVolume);
            else PlayerPrefs.DeleteKey(GlobalAudioSettings.VolumeKey);
            if (hadBackgroundMusic) PlayerPrefs.SetInt(GlobalAudioSettings.BackgroundMusicKey, savedBackgroundMusic);
            else PlayerPrefs.DeleteKey(GlobalAudioSettings.BackgroundMusicKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void DefaultsAreLoudAndBackgroundMusicIsEnabled()
        {
            Assert.That(GlobalAudioSettings.MasterVolume, Is.EqualTo(1f));
            Assert.That(GlobalAudioSettings.BackgroundMusicEnabled, Is.True);
            Assert.That(GlobalAudioSettings.VolumeToDecibels(1f), Is.EqualTo(12f).Within(.001f));
            Assert.That(GlobalAudioSettings.VolumeToDecibels(0f), Is.EqualTo(-80f).Within(.001f));
        }

        [Test]
        public void ValuesPersistGloballyWithoutAProfile()
        {
            GlobalAudioSettings.SetMasterVolume(.25f);
            GlobalAudioSettings.SetBackgroundMusicEnabled(false);

            Assert.That(PlayerPrefs.GetFloat(GlobalAudioSettings.VolumeKey), Is.EqualTo(.25f).Within(.001f));
            Assert.That(PlayerPrefs.GetInt(GlobalAudioSettings.BackgroundMusicKey), Is.Zero);
            Assert.That(GlobalAudioSettings.MasterVolume, Is.EqualTo(.25f).Within(.001f));
            Assert.That(GlobalAudioSettings.BackgroundMusicEnabled, Is.False);
            Assert.That(GlobalAudioSettings.VolumeToDecibels(.25f), Is.EqualTo(-.0412f).Within(.001f));
        }
    }
}
