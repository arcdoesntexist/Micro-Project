#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Reflection;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CPS.KitchenCatch
{
    public sealed class DevelopmentBrowserHarness : MonoBehaviour
    {
        private static readonly MethodInfo FinishRound = typeof(GameSessionController).GetMethod("FinishRound", BindingFlags.Instance | BindingFlags.NonPublic);
        private GameSessionController controller;
        private SessionState lastState = SessionState.Ready;
        private ProfilerRecorder gcRecorder;
        private int frameCount;
        private float elapsedSeconds;
        private float maximumFrameMilliseconds;
        private long totalGcBytes;
        private long maximumGcBytes;
        private bool sustainMode;
        private string roundMode = "natural";

#if DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var host = new GameObject("DevelopmentBrowserHarness");
            DontDestroyOnLoad(host);
            host.AddComponent<DevelopmentBrowserHarness>();
        }
#endif

        private void Awake()
        {
            gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f6Key.wasPressedThisFrame) Application.targetFrameRate = 30;
            if (keyboard != null && keyboard.f7Key.wasPressedThisFrame) Application.targetFrameRate = 60;
            if (keyboard != null && keyboard.f10Key.wasPressedThisFrame) sustainMode = !sustainMode;

            if (controller == null) controller = FindFirstObjectByType<GameSessionController>();
            if (controller == null) return;

            SessionState current = controller.State;
            if (current == SessionState.Playing && lastState != SessionState.Playing)
            {
                ResetMetrics();
                if (sustainMode) controller.Model.Health.Reset(100000);
            }
            if (current == SessionState.Playing) SampleMetrics();

            if (current == SessionState.Playing && keyboard != null && keyboard.f8Key.wasPressedThisFrame) Finish(EndReason.Triumph);
            if (current == SessionState.Playing && keyboard != null && keyboard.f9Key.wasPressedThisFrame) Finish(EndReason.HpDepleted);

            current = controller.State;
            if (current == SessionState.Results && lastState != SessionState.Results) LogMetrics();
            lastState = current;
        }

        private void Finish(EndReason reason)
        {
            roundMode = reason == EndReason.Triumph ? "terminal-shortcut" : "forced-death";
            if (reason == EndReason.Triumph)
            {
                controller.Model.CompleteStep(controller.Model.RemainingSeconds);
            }
            else
            {
                for (long id = -1; id >= -3; id--)
                    controller.Model.Resolve(id, ObjectKind.Food, ObjectOutcome.Missed, 0);
                controller.Model.CompleteStep(0);
            }

            if (controller.Model.State == SessionState.Triumph || controller.Model.State == SessionState.Dying)
                FinishRound?.Invoke(controller, null);
        }

        private void ResetMetrics()
        {
            roundMode = sustainMode ? "sustained" : "natural";
            frameCount = 0;
            elapsedSeconds = 0;
            maximumFrameMilliseconds = 0;
            totalGcBytes = 0;
            maximumGcBytes = 0;
        }

        private void SampleMetrics()
        {
            float seconds = Time.unscaledDeltaTime;
            long gcBytes = gcRecorder.Valid ? gcRecorder.LastValue : -1;
            frameCount++;
            elapsedSeconds += seconds;
            maximumFrameMilliseconds = Math.Max(maximumFrameMilliseconds, seconds * 1000);
            if (gcBytes >= 0)
            {
                totalGcBytes += gcBytes;
                maximumGcBytes = Math.Max(maximumGcBytes, gcBytes);
            }
        }

        private void LogMetrics()
        {
            float averageFrameMilliseconds = frameCount > 0 ? elapsedSeconds * 1000 / frameCount : 0;
            float framesPerSecond = elapsedSeconds > 0 ? frameCount / elapsedSeconds : 0;
            long averageGcBytes = frameCount > 0 && gcRecorder.Valid ? totalGcBytes / frameCount : -1;
            long peakGcBytes = gcRecorder.Valid ? maximumGcBytes : -1;
            Debug.Log($"CPS_DEV_METRICS difficulty={controller.LastResult.Difficulty} end={controller.LastResult.EndReason} mode={roundMode} target={Application.targetFrameRate} frames={frameCount} elapsed={elapsedSeconds:F3} fps={framesPerSecond:F2} avgFrameMs={averageFrameMilliseconds:F3} maxFrameMs={maximumFrameMilliseconds:F3} avgGcBytes={averageGcBytes} maxGcBytes={peakGcBytes}");
        }

        private void OnDestroy()
        {
            gcRecorder.Dispose();
        }
    }
}
#endif
