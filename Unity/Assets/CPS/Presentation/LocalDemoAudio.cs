using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace CPS.KitchenCatch
{
    public sealed class LocalDemoAudio : MonoBehaviour
    {
        private static LocalDemoAudio instance;
        private static Func<string, AudioClip> clipResolver = name => Resources.Load<AudioClip>("LocalDemoAudio/" + name);
        private static bool audioUnlocked;
        private AudioSource musicSource;
        private AudioSource runningSource;
        private AudioMixer mixer;
        private string currentScene;
        private bool wantsRunning;
        private bool isRunning;
        internal static bool AudioUnlocked => audioUnlocked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() => EnsureExists();

        public static LocalDemoAudio EnsureExists()
        {
            if (instance != null) return instance;
            instance = FindFirstObjectByType<LocalDemoAudio>();
            if (instance != null) return instance;
            var root = new GameObject(nameof(LocalDemoAudio));
            instance = root.AddComponent<LocalDemoAudio>();
            return instance;
        }

        internal static AudioClip LoadClip(string name) => clipResolver?.Invoke(name);

        public static void NotifyUserInteraction()
        {
            audioUnlocked = true;
            var director = EnsureExists();
            director.ApplyMusic();
            director.ApplyRunning();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            musicSource = gameObject.AddComponent<AudioSource>();
            runningSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            runningSource.loop = true;
            runningSource.volume = .99f;
            mixer = Resources.Load<AudioMixer>("Audio/KitchenAudioMixer");
            ConfigureSource(musicSource, "Music");
            ConfigureSource(runningSource, "SFX");
            GlobalAudioSettings.Changed += ApplyAudioSettings;
            ApplyAudioSettings();
            SceneManager.activeSceneChanged += SceneChanged;
            RefreshForScene(SceneManager.GetActiveScene().name);
        }

        private void Update()
        {
            if (audioUnlocked) return;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame ||
                mouse != null && mouse.leftButton.wasPressedThisFrame) NotifyUserInteraction();
        }

        private void SceneChanged(Scene previous, Scene next) => RefreshForScene(next.name);

        public void RefreshForScene(string sceneName)
        {
            currentScene = sceneName;
            SetRunning(false);
            ApplyMusic();
        }

        private void ApplyMusic()
        {
            if (musicSource == null) return;
            string clipName = currentScene == "MainMenu" ? "LobbyMusic_BG" :
                currentScene == "Gameplay" ? "GameplayMusic_BG" : null;
            float volume = currentScene == "MainMenu" ? .99f : .99f;
            var clip = clipName == null ? null : LoadClip(clipName);
            if (musicSource.clip != clip)
            {
                musicSource.Stop();
                musicSource.clip = clip;
            }
            musicSource.volume = volume;
            bool shouldPlay = audioUnlocked && clip != null && GlobalAudioSettings.BackgroundMusicEnabled;
            if (shouldPlay && !musicSource.isPlaying) musicSource.Play();
            else if (!shouldPlay) musicSource.Stop();
        }

        public void SetRunning(bool running)
        {
            wantsRunning = running;
            if (runningSource == null) return;
            if (runningSource.clip == null) runningSource.clip = LoadClip("Running");
            ApplyRunning();
        }

        public static void SetMascotRunning(bool running)
        {
            if (running) EnsureExists().SetRunning(true);
            else instance?.SetRunning(false);
        }

        public static void ConfigureSfxSource(AudioSource source)
        {
            EnsureExists().ConfigureSource(source, "SFX");
        }

        private void ConfigureSource(AudioSource source, string groupName)
        {
            if (source == null || mixer == null) return;
            var groups = mixer.FindMatchingGroups(groupName);
            if (groups.Length > 0) source.outputAudioMixerGroup = groups[0];
        }

        private void ApplyAudioSettings()
        {
            mixer?.SetFloat("MasterVolume", GlobalAudioSettings.VolumeToDecibels(GlobalAudioSettings.MasterVolume));
            ApplyMusic();
        }

        private void ApplyRunning()
        {
            bool shouldPlay = audioUnlocked && wantsRunning && currentScene == "Gameplay" && runningSource.clip != null;
            if (shouldPlay && !isRunning) runningSource.Play();
            else if (!shouldPlay && isRunning) runningSource.Stop();
            isRunning = shouldPlay;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SetRunning(false);
        }

        private void OnDisable()
        {
            if (runningSource != null) runningSource.Stop();
            isRunning = false;
            wantsRunning = false;
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= SceneChanged;
            GlobalAudioSettings.Changed -= ApplyAudioSettings;
            if (instance == this) instance = null;
        }
    }
}
