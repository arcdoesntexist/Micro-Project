using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Tests
{
    public sealed class VersusHybridFlowTests
    {
        private GameObject appRoot;
        private VersusMatchController flow;
        [UnitySetUp] public IEnumerator Load()
        {
            var runner = GameObject.Find("Code-based tests runner");
            if (runner != null) Object.DontDestroyOnLoad(runner);
            if (VersusControllerHub.Instance != null) Object.DestroyImmediate(VersusControllerHub.Instance.gameObject);
            if (AppSession.Instance != null) Object.DestroyImmediate(AppSession.Instance.gameObject);
            appRoot = new GameObject("Hybrid test app"); appRoot.SetActive(false);
            var app = appRoot.AddComponent<AppSession>();
            var values = new Dictionary<string, string>();
            app.Initialize(new ProfileStore(key => values.TryGetValue(key, out var value) ? value : null,
                (key, value) => values[key] = value, () => { }));
            var first = app.Store.Create("Player One"); var second = app.Store.Create("Player Two");
            app.ConfigureVersus(first.PlayerId, second.PlayerId, "#F57C32", "#F57C32", "#3C78D8", "#3C78D8");
            appRoot.SetActive(true);
            yield return SceneManager.LoadSceneAsync("VersusGameplay"); yield return null;
            flow = Object.FindFirstObjectByType<VersusMatchController>(); flow.enabled = false;
            typeof(VersusMatchController).GetField("resultEndpoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(flow, "");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (VersusControllerHub.Instance != null) Object.DestroyImmediate(VersusControllerHub.Instance.gameObject);
            if (appRoot != null) Object.DestroyImmediate(appRoot);
            var old = SceneManager.GetActiveScene();
            var idle = SceneManager.CreateScene("Hybrid cleanup " + Guid.NewGuid()); SceneManager.SetActiveScene(idle);
            if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
        }
        private void Tick(int count) { for (int i = 0; i < count; i++) flow.Step(.25f); }
        private void ConnectP2()
        {
            var hub = VersusControllerHub.Instance;
            flow.Pause();
            Assert.That(hub.BeginSession(2, "TEST-COM4"), Is.True);
            hub.TransportEvent(2, hub.Generation(2), "connected", null, SerialConnection.NowSeconds);
            hub.TransportEvent(2, hub.Generation(2), "chunk", "FSR raw L=0 R=100\n", SerialConnection.NowSeconds);
            flow.Resume();
        }
        [UnityTest] public IEnumerator LostControllerPausesBothAndExplicitKeyboardResumeDoesNotLoop()
        {
            ConnectP2(); Tick(13);
            Assert.That(flow.Phase, Is.EqualTo(VersusPlayPhase.Playing));
            var hub = VersusControllerHub.Instance;
            float time = flow.Model.PlayedSeconds;
            hub.TransportEvent(2, hub.Generation(2), "error", "Disconnected", SerialConnection.NowSeconds);
            flow.Step(.25f);
            Assert.That(flow.Phase, Is.EqualTo(VersusPlayPhase.Paused));
            Assert.That(flow.Model.PlayedSeconds, Is.EqualTo(time));
            Assert.That(flow.ControllerWarning, Is.Not.Null);
            flow.Resume(); Assert.That(flow.Phase, Is.EqualTo(VersusPlayPhase.Paused));
            Capture("Controller-loss-1080", 1920, 1080);
            Capture("Controller-loss-720", 1280, 720);
            flow.ContinueWithKeyboard();
            Assert.That(flow.Phase, Is.EqualTo(VersusPlayPhase.Countdown));
            Tick(13);
            Assert.That(flow.Phase, Is.EqualTo(VersusPlayPhase.Playing));
            Assert.That(flow.Model.PlayedSeconds, Is.GreaterThan(time));
            yield return null;
        }
        [UnityTest] public IEnumerator SetupAndGameplayShareTheHubAndOverlaySuppressesPractice()
        {
            var hub = VersusControllerHub.Instance;
            yield return SceneManager.LoadSceneAsync("VersusSetup"); yield return null;
            Assert.That(VersusControllerHub.Instance, Is.SameAs(hub));
            var panel = Object.FindFirstObjectByType<VersusControllerPanel>();
            Assert.That(panel, Is.Not.Null);
            panel.Show();
            Assert.That(hub.SuppressInput, Is.True);
            Capture("Controllers-Setup-1080", 1920, 1080);
            Capture("Controllers-Setup-720", 1280, 720);
            panel.Hide(); Assert.That(hub.SuppressInput, Is.False);
            yield return SceneManager.LoadSceneAsync("ModeSelect"); yield return null;
            Assert.That(VersusControllerHub.Instance, Is.Null);
        }
        [UnityTest] public IEnumerator ReconnectedStreamResumesThroughCountdown()
        {
            ConnectP2(); Tick(13); flow.Pause();
            var hub = VersusControllerHub.Instance;
            hub.TransportEvent(2, hub.Generation(2), "error", "Disconnected", SerialConnection.NowSeconds);
            Assert.That(hub.BeginSession(2, "TEST-COM4"), Is.True);
            hub.TransportEvent(2, hub.Generation(2), "connected", null, SerialConnection.NowSeconds);
            hub.TransportEvent(2, hub.Generation(2), "chunk", "FSR raw L=4095 R=4095\n", SerialConnection.NowSeconds);
            flow.Resume(); Assert.That(flow.Phase, Is.EqualTo(VersusPlayPhase.Countdown));
            Tick(13); Assert.That(flow.Phase, Is.EqualTo(VersusPlayPhase.Playing));
            yield return null;
        }
        [UnityTest] public IEnumerator OptionalPhysicalStm32FeedsTheRealSetupInput()
        {
            string port = Environment.GetEnvironmentVariable("MICRO_STM32_TEST_PORT");
            if (string.IsNullOrEmpty(port)) Assert.Ignore("Set MICRO_STM32_TEST_PORT for a connected physical Nucleo.");
            yield return SceneManager.LoadSceneAsync("VersusSetup"); yield return null;
            var hub = VersusControllerHub.Instance;
            Assert.That(hub.Connect(2, port), Is.True);
            float deadline = Time.realtimeSinceStartup + 6;
            while (!hub.IsFresh(2, SerialConnection.NowSeconds) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(hub.IsFresh(2, SerialConnection.NowSeconds), Is.True, hub.Status(2));
            var state = hub.Read(2);
            Assert.That(state.Left.Valid, Is.False);
            Assert.That(state.Right.Valid, Is.True);
            Assert.That(state.Right.ForceN, Is.Null);
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../verification/versus-hybrid-input"));
            File.WriteAllText(Path.Combine(directory, "physical-stm32.txt"),
                "Physical receive on " + port + ": " + hub.Status(2) + "\nRight strength=" + state.Right.Strength01 +
                "; left disabled=" + !state.Left.Valid + "; no newton force=" + !state.Right.ForceN.HasValue);
            Object.FindFirstObjectByType<VersusControllerPanel>().Show();
            Capture("Physical-STM32-Controllers-1080", 1920, 1080);
            hub.Disconnect(2);
        }
        private static void Capture(string name, int width, int height)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var camera = Camera.main;
            var mode = canvas.renderMode; var previousCamera = canvas.worldCamera;
            var target = new RenderTexture(width, height, 24);
            var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
                camera.targetTexture = target; camera.GetComponent<GameplayViewport>().Apply(width, height);
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../verification/versus-hybrid-input"));
                Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory, name + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = mode; canvas.worldCamera = previousCamera;
                RenderTexture.active = previousActive; camera.targetTexture = previousTarget;
                Object.DestroyImmediate(texture); Object.DestroyImmediate(target);
            }
        }
    }
}
