using System;
using UnityEngine;

namespace CPS.KitchenCatch
{
    public sealed class GameSessionController : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private KeyboardInput keyboardInput;
        [SerializeField] private FallingObjectSpawner spawner;
        [SerializeField] private DifficultyConfig[] difficulties;
        [SerializeField] private SceneNavigator navigator;
        [SerializeField] private MascotPresentation presentation;
        private IPlayerInput input;
        private AppSession app;
        private ForceStatsCollector stats = new ForceStatsCollector();
        private DifficultyConfig roundConfig;
        private SessionResult result;
        private SessionState state = SessionState.Ready;
        private float terminalSeconds;
        private string playerId;
        private string displayName;
        private string inputSource;
        private string calibrationVersion;
        private int connectionGeneration;
        private uint bootRevision;
        private bool hasRound;
        public GameSessionModel Model { get; private set; } = new GameSessionModel();
        public SessionState State => state;
        public bool IsPaused { get; private set; }
        public string InputIssue { get; private set; }
        public ControllerInput Router => input as ControllerInput;
        public bool CanResume => IsPaused && (Router == null || Router.CanPlay(out _));
        public bool CanRestart => Router == null || Router.CanPlay(out _);
        public InputSnapshot LastInput { get; private set; }
        public float CountdownSeconds { get; private set; }
        public SessionResult LastResult => result?.Copy();
        public string ValidationError => GetValidationError(app != null ? app : AppSession.Instance);
        public event Action<SessionState> StateChanged;
        public event Action<SessionResult> Finished;
        public event Action<DifficultyId> RoundStarting;
        public void Initialize(PlayerController player, IPlayerInput input, FallingObjectSpawner spawner,
            DifficultyConfig[] difficulties, AppSession app)
        {
            if (hasRound) throw new InvalidOperationException("Cannot rewire an active round.");
            this.player = player; this.input = input; this.spawner = spawner;
            this.difficulties = difficulties; this.app = app;
        }

        public string GetValidationError(AppSession session)
        {
            if (session == null || session.Store == null) return "AppSession is missing or uninitialized.";
            if (session.SelectedProfile == null) return "Select a profile before starting.";
            if (player == null) return "GameSessionController player reference is missing.";
            if (input == null && keyboardInput == null) return "Keyboard input reference is missing.";
            if (input is ControllerInput selected && !selected.CanPlay(out string inputError)) return inputError;
            if (spawner == null) return "FallingObjectSpawner reference is missing.";
            if (spawner.Player != null && spawner.Player != player)
                return "GameSessionController player must match FallingObjectSpawner player.";
            return spawner.GetValidationError(FindDifficulty(session.SelectedDifficulty));
        }

        private DifficultyConfig FindDifficulty(DifficultyId id)
        {
            if (difficulties != null)
                foreach (var config in difficulties)
                    if (config != null && config.difficulty == id) return config;
            return null;
        }

        public bool BeginRound()
        {
            if (app == null) app = AppSession.Instance;
            if (GetValidationError(app) != null) return false;
            input ??= ControllerInput.EnsureExists();
            if (Router != null)
            {
                Router.ReleaseRound();
                if (!Router.CanPlay(out string reason)) { InputIssue = reason; return false; }
                Router.FreezeForRound();
                connectionGeneration = Router.Connection.ConnectionGeneration;
                bootRevision = Router.Connection.Packets.BootRevision;
            }
            spawner.StopAndClear();
            if (roundConfig != null) Destroy(roundConfig);
            roundConfig = Instantiate(FindDifficulty(app.SelectedDifficulty));
            RoundStarting?.Invoke(roundConfig.difficulty);
            playerId = app.SelectedProfile.PlayerId;
            displayName = app.SelectedProfile.DisplayName;
            Model = new GameSessionModel();
            Model.Health.Reset(3);
            stats.Reset(); result = null; inputSource = null;
            inputSource = Router?.SelectedMode.ToString();
            calibrationVersion = Router?.SelectedMode == ControllerMode.Keyboard ? null : Router?.Connection.Packets.Latest?.CalibrationVersion;
            InputIssue = null;
            LastInput = default;
            IsPaused = false; CountdownSeconds = 3; terminalSeconds = 0;
            player.ResetPosition();
            presentation?.ResetPresentation();
            hasRound = true;
            SetState(SessionState.Ready);
            return true;
        }

        public void Step(float dt)
        {
            if (!hasRound || IsPaused || !DifficultyConfig.FinitePositive(dt)) return;
            if ((State == SessionState.Ready || State == SessionState.Playing) && Router != null &&
                Router.SelectedMode != ControllerMode.Keyboard &&
                (connectionGeneration != Router.Connection.ConnectionGeneration || bootRevision != Router.Connection.Packets.BootRevision))
            {
                InputIssue = "USB controller restarted. Check the connection, then resume.";
                Pause(); return;
            }
            if ((State == SessionState.Ready || State == SessionState.Playing) && Router != null && !Router.CanPlay(out string reason))
            {
                InputIssue = reason; Pause(); return;
            }
            if (State == SessionState.Triumph || State == SessionState.Dying)
            {
                terminalSeconds -= dt;
                if (terminalSeconds <= 0.00001f) SetState(SessionState.Results);
                return;
            }
            if (State == SessionState.Results) return;
            if (CountdownSeconds > 0)
            {
                CountdownSeconds = Mathf.Max(0, CountdownSeconds - dt);
                if (CountdownSeconds <= 0.00001f)
                {
                    CountdownSeconds = 0;
                    if (State == SessionState.Ready)
                    {
                        Model.Start();
                        spawner.Begin(roundConfig, Resolve);
                        SetState(SessionState.Playing);
                    }
                }
                return;
            }
            float step = Mathf.Min(dt, Model.RemainingSeconds);
            var snapshot = input.Read();
            LastInput = snapshot;
            inputSource ??= snapshot.Source;
            calibrationVersion ??= snapshot.CalibrationVersion;
            player.Step(snapshot, step);
            presentation?.SetMovement(player.MovementDirection);
            spawner.Step(step);
            stats.Accumulate(snapshot, step);
            Model.CompleteStep(step);
            if (Model.State == SessionState.Triumph || Model.State == SessionState.Dying) FinishRound();
        }

        private void Resolve(long id, FallingObjectDefinition definition, ObjectOutcome outcome)
        {
            if (State == SessionState.Playing && !IsPaused && CountdownSeconds == 0)
            {
                Model.Resolve(id, definition.kind, outcome, definition.points, definition.itemIndex);
                if (definition.kind == ObjectKind.Food && outcome == ObjectOutcome.Caught) presentation?.PlayEat();
                else if (definition.kind == ObjectKind.Trash && outcome == ObjectOutcome.Caught ||
                    definition.kind == ObjectKind.Food && outcome == ObjectOutcome.Missed) presentation?.PlayHurt();
            }
        }

        private void FinishRound()
        {
            var score = Model.Score;
            result = new SessionResult
            {
                SessionId = Guid.NewGuid().ToString("D"), PlayerId = playerId, DisplayName = displayName,
                Difficulty = roundConfig.difficulty, EndedAtUtc = DateTime.UtcNow.ToString("O"),
                EndReason = Model.State == SessionState.Dying ? EndReason.HpDepleted : EndReason.Triumph,
                PlayedSeconds = Model.PlayedSeconds, RemainingHp = Model.Health.CurrentHp,
                Score = score.Score, FoodCaught = score.FoodCaught, FoodMissed = score.FoodMissed,
                TrashEaten = score.TrashEaten, MaxCombo = score.MaxCombo, HighestRank = score.HighestRank,
                InputSource = inputSource, CalibrationVersion = calibrationVersion, InputStatistics = stats.Snapshot(),
                CaughtItems = (int[])Model.CaughtItems.Clone()
            };
            var frozen = result;
            app.Store.RecordResult(playerId, frozen.SessionId);
            spawner.StopAndClear();
            player.Step(default, 0);
            LastInput = default;
            IsPaused = false;
            terminalSeconds = Model.State == SessionState.Dying ? 1.4f : 1.2f;
            SetState(Model.State);
            presentation?.PlayTerminal(result.EndReason);
            if (Finished != null)
                foreach (Action<SessionResult> listener in Finished.GetInvocationList()) listener(frozen.Copy());
        }

        public void Pause()
        {
            if (!hasRound || (State != SessionState.Ready && State != SessionState.Playing)) return;
            IsPaused = true;
            if (Router != null) Router.Connection.ControlsLocked = false;
            player.Step(default, 0);
            presentation?.SetMovement(0);
            LastInput = default;
        }
        public void Resume()
        {
            if (!CanResume) return;
            InputIssue = null;
            if (Router != null)
            {
                connectionGeneration = Router.Connection.ConnectionGeneration;
                bootRevision = Router.Connection.Packets.BootRevision;
                Router.Connection.ControlsLocked = true;
            }
            IsPaused = false; CountdownSeconds = 3;
        }
        public bool Restart() => BeginRound();
        public void ReturnToMenu()
        {
            spawner?.StopAndClear(); hasRound = false; IsPaused = false;
            Router?.ReleaseRound();
            LastInput = default;
            navigator?.GoTo("MainMenu");
        }
        private void SetState(SessionState next) { state = next; StateChanged?.Invoke(next); }
        private void Start() => BeginRound();
        private void Update() => Step(Time.deltaTime);
        private void OnApplicationFocus(bool focused) { if (!focused) Pause(); }
        private void OnDestroy()
        {
            Router?.ReleaseRound();
            spawner?.StopAndClear();
            if (roundConfig != null) Destroy(roundConfig);
        }
    }
}
