using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CPS.KitchenCatch
{
    public enum VersusPlayPhase { Countdown, Playing, Paused, Terminal, Results }

    public sealed class VersusMatchController : MonoBehaviour
    {
        [SerializeField] private VersusChallenge challenge = new VersusChallenge();
        [SerializeField, Min(.1f)] private float movementSpeed = 7;
        [SerializeField] private Vector2 catchHalfExtents = new Vector2(1, .4f);
        [SerializeField] private Font font;
        [SerializeField] private Sprite kitchen;
        [SerializeField] private Sprite frame;
        [SerializeField] private Sprite orangeButton;
        [SerializeField] private Sprite cocoaButton;
        [SerializeField] private Sprite inputPlate;
        [SerializeField] private Sprite[] bodyFrames;
        [SerializeField] private VersusPracticePlayer.HatVariant[] hats;
        [SerializeField] private Sprite[] decorations;
        [SerializeField] private FallingObjectDefinition[] definitions;
        [SerializeField] private GameObject settingsPrefab;
        [SerializeField] private AudioClip versusMusic;
        [SerializeField] private string resultEndpoint = "https://script.google.com/macros/s/AKfycbxmUwlzaqkbO04UAfhy2CxkpmMR5fEXioOeCoTGxHrwuX4V6VCA9zC3RJ_YQlq-gq8h/exec";
        private VersusMatchSetup setup;
        private VersusMatchModel model;
        private VersusSpawnStream stream;
        private VersusGameplayUI ui;
        private VersusFieldController field1;
        private VersusFieldController field2;
        private VersusKeyboardInput input1;
        private VersusKeyboardInput input2;
        private SceneNavigator navigator;
        private ForceStatsCollector stats1 = new ForceStatsCollector();
        private ForceStatsCollector stats2 = new ForceStatsCollector();
        private AudioSource music;
        private AudioSource effects;
        private AudioClip eatClip;
        private AudioClip hurtClip;
        private float countdown;
        private float accumulator;
        private float terminalSeconds;
        private bool started;
        private string matchId;
        private VersusControllerHub controllers;
        public string ControllerWarning { get; private set; }
        private string source1;
        private string source2;
        public VersusPlayPhase Phase { get; private set; }
        public VersusMatchModel Model => model;
        public Sprite[] BodyFrames => bodyFrames;
        public FallingObjectDefinition[] Definitions => definitions;
        public Font Font => font;
        public Sprite Kitchen => kitchen;
        public Sprite Frame => frame;
        public Sprite OrangeButton => orangeButton;
        public Sprite CocoaButton => cocoaButton;
        public Sprite InputPlate => inputPlate;
        public Sprite[] Decorations => decorations;
        public float MovementSpeed => movementSpeed;
        public Vector2 CatchHalfExtents => catchHalfExtents;

        private void Start()
        {
            navigator = gameObject.AddComponent<SceneNavigator>();
            setup = AppSession.Instance?.VersusSetup?.Copy();
            if (setup == null) { navigator.GoTo("VersusSetup"); return; }
            controllers = VersusControllerHub.EnsureExists();
            input1 = gameObject.AddComponent<VersusKeyboardInput>();
            input2 = gameObject.AddComponent<VersusKeyboardInput>();
            input1.Configure(Key.A, Key.D, "Keyboard P1");
            input2.Configure(Key.LeftArrow, Key.RightArrow, "Keyboard P2");
            ui = gameObject.AddComponent<VersusGameplayUI>();
            ui.Build(this, setup, out field1, out field2);
            ui.AttachSettings(settingsPrefab);
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.clip = versusMusic != null ? versusMusic : LocalDemoAudio.LoadClip("GameplayMusic_BG");
            var mixer = Resources.Load<UnityEngine.Audio.AudioMixer>("Audio/KitchenAudioMixer");
            var groups = mixer == null ? null : mixer.FindMatchingGroups("Music");
            if (groups != null && groups.Length > 0) music.outputAudioMixerGroup = groups[0];
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            LocalDemoAudio.ConfigureSfxSource(effects);
            eatClip = LocalDemoAudio.LoadClip("Chomping");
            hurtClip = LocalDemoAudio.LoadClip("Hurt");
            GlobalAudioSettings.Changed += ApplyAudio;
            started = true;
            Restart();
            ApplyAudio();
        }

        public void GetHat(string color, out Sprite right, out Sprite left)
        {
            var selected = hats[0];
            foreach (var variant in hats)
                if (string.Equals("#" + ColorUtility.ToHtmlStringRGB(variant.Color), color, StringComparison.OrdinalIgnoreCase))
                { selected = variant; break; }
            right = selected.Right;
            left = selected.Left;
        }

        public void Restart()
        {
            controllers.StopWatching();
            controllers.WatchMatch();
            ControllerWarning = null;
            source1 = controllers.Port(1) == null ? "Keyboard P1" : "Keyboard + FSR P1";
            source2 = controllers.Port(2) == null ? "Keyboard P2" : "Keyboard + FSR P2";
            model = new VersusMatchModel(challenge);
            model.Start();
            model.Pause();
            matchId = Guid.NewGuid().ToString("N");
            stream = new VersusSpawnStream(challenge, Guid.NewGuid().GetHashCode());
            stats1.Reset(); stats2.Reset();
            field1.ResetField(); field2.ResetField();
            ui.HideOverlays();
            BeginCountdown();
            ui.Refresh(model);
        }

        private void BeginCountdown()
        {
            countdown = 3;
            accumulator = 0;
            Phase = VersusPlayPhase.Countdown;
            controllers.ControlsLocked = true;
            ui.SetCountdown("3");
        }

        private void Update()
        {
            if (!started) return;
            if (!Application.isFocused) Pause();
            var keyboard = Keyboard.current;
            if (!controllers.SuppressInput && !ui.SettingsOpen &&
                (keyboard?.escapeKey.wasPressedThisFrame == true || keyboard?.spaceKey.wasPressedThisFrame == true))
            {
                if (Phase == VersusPlayPhase.Paused) Resume(); else Pause();
            }
            Step(Time.unscaledDeltaTime);
        }

        public void Step(float seconds)
        {
            if (!started || !float.IsFinite(seconds) || seconds <= 0) return;
            float dt = Mathf.Min(seconds, .25f);
            if ((Phase == VersusPlayPhase.Playing || Phase == VersusPlayPhase.Countdown) &&
                controllers.HasStreamLoss(SerialConnection.NowSeconds))
            {
                ControllerWarning = "Controller data lost. Reconnect in Controllers, or continue with keyboard.";
                Pause();
                return;
            }
            if (Phase == VersusPlayPhase.Countdown)
            {
                countdown -= dt;
                ui.SetCountdown(Mathf.CeilToInt(countdown).ToString());
                if (countdown <= 0) { ui.SetCountdown(null); model.Resume(); Phase = VersusPlayPhase.Playing; }
                return;
            }
            if (Phase == VersusPlayPhase.Terminal)
            {
                terminalSeconds += dt;
                field1.Terminal(model.Player1.Health.CurrentHp == 0, terminalSeconds);
                field2.Terminal(model.Player2.Health.CurrentHp == 0, terminalSeconds);
                if (terminalSeconds >= 1.6f) { Phase = VersusPlayPhase.Results; ui.ShowResults(setup, model.Result); }
                return;
            }
            if (Phase != VersusPlayPhase.Playing) return;
            var first = input1.Read(); var second = input2.Read();
            if (first.Source.Contains("FSR")) source1 = first.Source;
            if (second.Source.Contains("FSR")) source2 = second.Source;
            accumulator += dt;
            const float step = 1f / 120;
            while (accumulator >= step && Phase == VersusPlayPhase.Playing)
            {
                accumulator -= step;
                while (stream.TryNext(model.PlayedSeconds, out var record))
                { field1.Spawn(record); field2.Spawn(record); }
                var left = field1.Step(first, step);
                var right = field2.Step(second, step);
                stats1.Accumulate(first, step); stats2.Accumulate(second, step);
                model.ApplyStep(step, left, right);
                if (model.State == VersusMatchState.Finished) Finish();
            }
            ui.Refresh(model);
        }

        public void Pause()
        {
            if (!started || Phase != VersusPlayPhase.Playing && Phase != VersusPlayPhase.Countdown) return;
            model.Pause();
            Phase = VersusPlayPhase.Paused;
            controllers.ControlsLocked = false;
            field1.SetPose(0); field2.SetPose(0);
            ui.SetCountdown(null);
            ui.ShowPause(true);
        }

        public void Resume()
        {
            if (Phase != VersusPlayPhase.Paused) return;
            if (!controllers.CanPlay(SerialConnection.NowSeconds, out var reason))
            { ControllerWarning = reason; ui.ShowPause(true); return; }
            controllers.WatchMatch();
            ControllerWarning = null;
            ui.ShowPause(false);
            BeginCountdown();
        }

        public void ContinueWithKeyboard()
        {
            if (Phase != VersusPlayPhase.Paused) return;
            controllers.ContinueWithKeyboard();
            Resume();
        }

        public void ReturnToMenu() => navigator.GoTo("ModeSelect");
        public void BackToSetup() => navigator.GoTo("VersusSetup");

        private void Finish()
        {
            Phase = VersusPlayPhase.Terminal;
            controllers.StopWatching();
            terminalSeconds = 0;
            field1.SetPose(0); field2.SetPose(0);
            var clip = LocalDemoAudio.LoadClip("Death");
            if (clip != null) effects.PlayOneShot(clip, .55f);
            var payload = VersusResultUploadPayload.Create(matchId, setup, model.Result, stats1.Snapshot(), stats2.Snapshot(), source1, source2);
            VersusResultDelivery.SaveAndSubmit(payload, resultEndpoint,
                status => { if (this != null && ui != null && matchId == payload.MatchId) ui.SetUploadStatus(status); });
        }

        public void PlayCatch(bool trash)
        {
            var clip = trash ? hurtClip : eatClip;
            if (clip != null) effects.PlayOneShot(clip, trash ? .65f : .6f);
        }

        private void ApplyAudio()
        {
            if (music == null) return;
            music.volume = music.outputAudioMixerGroup != null ? .99f : GlobalAudioSettings.MasterVolume;
            if (GlobalAudioSettings.BackgroundMusicEnabled && music.clip != null)
            { if (!music.isPlaying) music.Play(); }
            else music.Stop();
        }

        private void OnApplicationFocus(bool focus) { if (!focus) Pause(); }
        private void OnApplicationPause(bool paused) { if (paused) Pause(); }
        private void OnDestroy() => GlobalAudioSettings.Changed -= ApplyAudio;
    }
}
