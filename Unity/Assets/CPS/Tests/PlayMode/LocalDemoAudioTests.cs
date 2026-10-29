using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Tests
{
    public sealed class LocalDemoAudioTests : InputTestFixture
    {
        private readonly List<AudioClip> clips = new List<AudioClip>();
        private readonly List<Scene> scenes = new List<Scene>();
        private Scene originalScene;
        private bool hadVolume;
        private bool hadBackgroundMusic;
        private float savedVolume;
        private int savedBackgroundMusic;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            DestroyDirectors();
            SetResolver(name => null);
            SetUnlocked(false);
            hadVolume = PlayerPrefs.HasKey(GlobalAudioSettings.VolumeKey);
            hadBackgroundMusic = PlayerPrefs.HasKey(GlobalAudioSettings.BackgroundMusicKey);
            savedVolume = PlayerPrefs.GetFloat(GlobalAudioSettings.VolumeKey);
            savedBackgroundMusic = PlayerPrefs.GetInt(GlobalAudioSettings.BackgroundMusicKey);
            PlayerPrefs.DeleteKey(GlobalAudioSettings.VolumeKey);
            PlayerPrefs.DeleteKey(GlobalAudioSettings.BackgroundMusicKey);
            originalScene = SceneManager.GetActiveScene();
        }

        [TearDown]
        public override void TearDown()
        {
            try
            {
                DestroyDirectors();
                SetResolver(null);
                if (hadVolume) PlayerPrefs.SetFloat(GlobalAudioSettings.VolumeKey, savedVolume);
                else PlayerPrefs.DeleteKey(GlobalAudioSettings.VolumeKey);
                if (hadBackgroundMusic) PlayerPrefs.SetInt(GlobalAudioSettings.BackgroundMusicKey, savedBackgroundMusic);
                else PlayerPrefs.DeleteKey(GlobalAudioSettings.BackgroundMusicKey);
                PlayerPrefs.Save();
                foreach (var clip in clips) Object.DestroyImmediate(clip);
                clips.Clear();
            }
            finally
            {
                SetUnlocked(false);
                base.TearDown();
            }
        }

        [UnityTearDown]
        public IEnumerator CleanupScenes()
        {
            if (originalScene.IsValid() && originalScene.isLoaded) SceneManager.SetActiveScene(originalScene);
            foreach (var scene in scenes)
                if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            scenes.Clear();
        }

        [UnityTest]
        public IEnumerator MissingLocalAssetsRemainSilentAcrossSupportedScenes()
        {
            var director = LocalDemoAudio.EnsureExists();

            Assert.DoesNotThrow(() =>
            {
                director.RefreshForScene("MainMenu");
                LocalDemoAudio.NotifyUserInteraction();
                director.RefreshForScene("Gameplay");
                director.SetRunning(true);
                director.SetRunning(false);
            });
            yield return new WaitForSecondsRealtime(.05f);
            var sources = director.GetComponents<AudioSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                Assert.That(source.clip, Is.Null);
                Assert.That(source.isPlaying, Is.False, $"source={i}");
            }
        }

        [UnityTest]
        public IEnumerator InteractionUnlocksSceneMusicWithLoopAndApprovedVolumes()
        {
            var lobby = Clip("Lobby", 44100 * 10);
            var gameplay = Clip("Gameplay", 44100 * 10);
            SetResolver(name => name == "LobbyMusic_BG" ? lobby : name == "GameplayMusic_BG" ? gameplay : null);
            var director = LocalDemoAudio.EnsureExists();
            var music = director.GetComponents<AudioSource>()[0];

            director.RefreshForScene("MainMenu");
            Assert.That(music.clip, Is.SameAs(lobby));
            Assert.That(music.isPlaying, Is.False);

            LocalDemoAudio.NotifyUserInteraction();
            yield return new WaitForSecondsRealtime(.1f);

            Assert.That(music.isPlaying, Is.True);
            Assert.That(music.timeSamples, Is.GreaterThan(0));
            Assert.That(music.loop, Is.True);
            Assert.That(music.volume, Is.EqualTo(.99f).Within(.001f));

            director.RefreshForScene("Gameplay");
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(music.clip, Is.SameAs(gameplay));
            Assert.That(music.isPlaying, Is.True);
            Assert.That(music.timeSamples, Is.GreaterThan(0));
            Assert.That(music.volume, Is.EqualTo(.99f).Within(.001f));

            director.RefreshForScene("Other");
            Assert.That(music.isPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator BackgroundToggleStopsOnlyMusicAndRestoresIt()
        {
            var gameplay = Clip("Gameplay", 44100 * 10);
            var running = Clip("Running", 44100 * 10);
            SetResolver(name => name == "GameplayMusic_BG" ? gameplay : name == "Running" ? running : null);
            var director = LocalDemoAudio.EnsureExists();
            director.RefreshForScene("Gameplay");
            LocalDemoAudio.NotifyUserInteraction();
            director.SetRunning(true);
            yield return new WaitForSecondsRealtime(.05f);
            var sources = director.GetComponents<AudioSource>();
            Assert.That(sources[0].isPlaying, Is.True);
            Assert.That(sources[1].isPlaying, Is.True);

            GlobalAudioSettings.SetBackgroundMusicEnabled(false);
            Assert.That(sources[0].isPlaying, Is.False);
            Assert.That(sources[1].isPlaying, Is.True);

            GlobalAudioSettings.SetBackgroundMusicEnabled(true);
            yield return new WaitForSecondsRealtime(.05f);
            Assert.That(sources[0].isPlaying, Is.True);
        }

        [Test]
        public void EnsureExistsKeepsOneDirectorAndDoesNotDuplicateSources()
        {
            var first = LocalDemoAudio.EnsureExists();
            var second = LocalDemoAudio.EnsureExists();

            Assert.That(second, Is.SameAs(first));
            Assert.That(Object.FindObjectsByType<LocalDemoAudio>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(first.GetComponents<AudioSource>(), Has.Length.EqualTo(2));
            Assert.That(first.GetComponents<AudioSource>()[0].outputAudioMixerGroup.name, Is.EqualTo("Music"));
            Assert.That(first.GetComponents<AudioSource>()[1].outputAudioMixerGroup.name, Is.EqualTo("SFX"));
        }

        [Test]
        public void FreshDirectorRemainsSilentWithoutInput()
        {
            var lobby = Clip("Lobby", 44100 * 10);
            SetResolver(name => name == "LobbyMusic_BG" ? lobby : null);
            var director = LocalDemoAudio.EnsureExists();
            director.RefreshForScene("MainMenu");
            director.SendMessage("Update");

            var music = director.GetComponents<AudioSource>()[0];
            Assert.That(music.clip, Is.SameAs(lobby));
            Assert.That(music.isPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator KeyboardAndMousePressUnlockMusicThroughUpdate()
        {
            var lobby = Clip("Lobby", 44100 * 10);
            SetResolver(name => name == "LobbyMusic_BG" ? lobby : null);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            var director = LocalDemoAudio.EnsureExists();
            director.RefreshForScene("MainMenu");
            var music = director.GetComponents<AudioSource>()[0];

            director.SendMessage("Update");
            Assert.That(music.isPlaying, Is.False);
            Press(keyboard.spaceKey);
            director.SendMessage("Update");
            yield return new WaitForSecondsRealtime(.05f);
            Assert.That(music.isPlaying, Is.True);

            DestroyDirectors();
            SetUnlocked(false);
            director = LocalDemoAudio.EnsureExists();
            director.RefreshForScene("MainMenu");
            music = director.GetComponents<AudioSource>()[0];
            Assert.That(music.isPlaying, Is.False);
            Press(mouse.leftButton);
            director.SendMessage("Update");
            yield return new WaitForSecondsRealtime(.05f);
            Assert.That(music.isPlaying, Is.True);
        }

        [UnityTest]
        public IEnumerator ActiveSceneChangesReuseDirectorAndSwitchMusic()
        {
            var lobby = Clip("Lobby", 44100 * 10);
            var gameplay = Clip("Gameplay", 44100 * 10);
            SetResolver(name => name == "LobbyMusic_BG" ? lobby : name == "GameplayMusic_BG" ? gameplay : null);
            var director = LocalDemoAudio.EnsureExists();
            var sources = director.GetComponents<AudioSource>();
            LocalDemoAudio.NotifyUserInteraction();

            var menu = SceneManager.CreateScene("MainMenu");
            scenes.Add(menu);
            SceneManager.SetActiveScene(menu);
            yield return new WaitForSecondsRealtime(.05f);
            Assert.That(LocalDemoAudio.EnsureExists(), Is.SameAs(director));
            Assert.That(director.GetComponents<AudioSource>(), Is.EquivalentTo(sources));
            Assert.That(sources[0].clip, Is.SameAs(lobby));
            Assert.That(sources[0].isPlaying, Is.True);

            var play = SceneManager.CreateScene("Gameplay");
            scenes.Add(play);
            SceneManager.SetActiveScene(play);
            yield return new WaitForSecondsRealtime(.05f);
            Assert.That(LocalDemoAudio.EnsureExists(), Is.SameAs(director));
            Assert.That(director.GetComponents<AudioSource>(), Is.EquivalentTo(sources));
            Assert.That(Object.FindObjectsByType<LocalDemoAudio>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(sources[0].clip, Is.SameAs(gameplay));
            Assert.That(sources[0].isPlaying, Is.True);
        }

        [UnityTest]
        public IEnumerator RunningLoopStartsOnceAndStopsForIdleTerminalResetAndDisable()
        {
            var running = Clip("Running", 44100 * 10);
            SetResolver(name => name == "Running" ? running : null);
            var director = LocalDemoAudio.EnsureExists();
            director.RefreshForScene("Gameplay");
            var source = director.GetComponents<AudioSource>()[1];
            var mascot = new GameObject("Mascot").AddComponent<MascotPresentation>();
            try
            {
                mascot.SetMovement(1);
                Assert.That(source.isPlaying, Is.False);

                LocalDemoAudio.NotifyUserInteraction();
                yield return new WaitForSecondsRealtime(.12f);
                Assert.That(source.isPlaying, Is.True);
                int before = source.timeSamples;
                Assert.That(before, Is.GreaterThan(1000));

                mascot.SetMovement(1);
                yield return new WaitForSecondsRealtime(.05f);
                Assert.That(source.clip, Is.SameAs(running));
                Assert.That(source.isPlaying, Is.True);
                Assert.That(source.timeSamples, Is.GreaterThan(before));
                Assert.That(source.loop, Is.True);
                Assert.That(source.volume, Is.EqualTo(.99f).Within(.001f));
                Assert.That(director.GetComponents<AudioSource>(), Has.Length.EqualTo(2));

                mascot.SetMovement(0);
                Assert.That(source.isPlaying, Is.False);

                mascot.SetMovement(-1);
                Assert.That(source.isPlaying, Is.True);
                mascot.SetMovement(0);
                Assert.That(source.isPlaying, Is.False);

                mascot.SetMovement(-1);
                mascot.PlayTerminal(EndReason.HpDepleted);
                Assert.That(source.isPlaying, Is.False);

                mascot.ResetPresentation();
                Assert.That(source.isPlaying, Is.False);

                mascot.SetMovement(1);
                mascot.enabled = false;
                Assert.That(source.isPlaying, Is.False);
            }
            finally { Object.DestroyImmediate(mascot.gameObject); }
        }

        [Test]
        public void MascotLoadsOptionalTerminalClipsByConvention()
        {
            var triumph = Clip("Triumph");
            var death = Clip("Death");
            SetResolver(name => name == "Triumph" ? triumph : name == "Death" ? death : null);

            var mascot = new GameObject("Mascot").AddComponent<MascotPresentation>();
            try
            {
                Assert.That(Field<AudioClip>(mascot, "triumphClip"), Is.SameAs(triumph));
                Assert.That(Field<AudioClip>(mascot, "deathClip"), Is.SameAs(death));
            }
            finally { Object.DestroyImmediate(mascot.gameObject); }
        }

        [Test]
        public void VolumeChangesMixerGainAndMascotUsesSfxGroup()
        {
            var director = LocalDemoAudio.EnsureExists();
            GlobalAudioSettings.SetMasterVolume(.25f);
            var mixer = director.GetComponents<AudioSource>()[0].outputAudioMixerGroup.audioMixer;
            Assert.That(mixer.GetFloat("MasterVolume", out float decibels), Is.True);
            Assert.That(decibels, Is.EqualTo(-.0412f).Within(.001f));

            var root = new GameObject("Mascot");
            root.SetActive(false);
            var source = root.AddComponent<AudioSource>();
            var mascot = root.AddComponent<MascotPresentation>();
            SetField(mascot, "audioSource", source);
            root.SetActive(true);
            try { Assert.That(source.outputAudioMixerGroup.name, Is.EqualTo("SFX")); }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest]
        public IEnumerator ResetStopsLongTerminalOneShotBeforeNextRound()
        {
            var death = Clip("Death", 44100 * 6);
            var triumph = Clip("Triumph", 44100 * 6);
            SetResolver(name => name == "Death" ? death : name == "Triumph" ? triumph : null);
            LocalDemoAudio.NotifyUserInteraction();
            var root = new GameObject("Mascot");
            var source = root.AddComponent<AudioSource>();
            var mascot = root.AddComponent<MascotPresentation>();
            SetField(mascot, "audioSource", source);
            try
            {
                mascot.PlayTerminal(EndReason.HpDepleted);
                yield return new WaitForSecondsRealtime(.1f);
                Assert.That(source.isPlaying, Is.True);

                mascot.ResetPresentation();
                Assert.That(source.isPlaying, Is.False);

                mascot.PlayTerminal(EndReason.Triumph);
                yield return new WaitForSecondsRealtime(.1f);
                Assert.That(source.isPlaying, Is.True);
                mascot.ResetPresentation();
                Assert.That(source.isPlaying, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private AudioClip Clip(string name, int samples = 441)
        {
            var clip = AudioClip.Create(name, samples, 1, 44100, false);
            clips.Add(clip);
            return clip;
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static T Field<T>(object target, string name)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(target);
        }

        private static void SetResolver(Func<string, AudioClip> resolver)
        {
            var type = typeof(MascotPresentation).Assembly.GetType("CPS.KitchenCatch.LocalDemoAudio");
            if (type == null) return;
            var field = type.GetField("clipResolver", BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, resolver ?? new Func<string, AudioClip>(name => Resources.Load<AudioClip>("LocalDemoAudio/" + name)));
        }

        private static void SetUnlocked(bool value)
        {
            var type = typeof(MascotPresentation).Assembly.GetType("CPS.KitchenCatch.LocalDemoAudio");
            var field = type?.GetField("audioUnlocked", BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, value);
        }

        private static void DestroyDirectors()
        {
            var type = typeof(MascotPresentation).Assembly.GetType("CPS.KitchenCatch.LocalDemoAudio");
            if (type == null) return;
            foreach (var item in Object.FindObjectsByType(type, FindObjectsSortMode.None))
                Object.DestroyImmediate(((Component)item).gameObject);
        }
    }
}
