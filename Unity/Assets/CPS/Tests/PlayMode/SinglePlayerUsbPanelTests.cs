using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Tests
{
    public sealed class SinglePlayerUsbPanelTests
    {
        private GameObject appRoot;
        private ControllerInput input;
        private ControllerPanel panel;
        private bool hadPreference;
        private int preference;
        private static T Field<T>(object owner, string name) => (T)owner.GetType()
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(owner);

        [UnitySetUp] public IEnumerator Load()
        {
            var runner = GameObject.Find("Code-based tests runner");
            if (runner != null) Object.DontDestroyOnLoad(runner);
            if (AppSession.Instance != null) Object.DestroyImmediate(AppSession.Instance.gameObject);
            hadPreference = PlayerPrefs.HasKey(ControllerInput.PreferenceKey);
            preference = PlayerPrefs.GetInt(ControllerInput.PreferenceKey);
            input = ControllerInput.EnsureExists(); input.ReleaseRound(); input.SelectMode(ControllerMode.Keyboard);
            input.Connection.Disconnect();
            appRoot = new GameObject("In-memory single USB app"); appRoot.SetActive(false);
            var app = appRoot.AddComponent<AppSession>();
            var values = new Dictionary<string, string>();
            app.Initialize(new ProfileStore(key => values.TryGetValue(key, out var value) ? value : null,
                (key, value) => values[key] = value, () => { }));
            var profile = app.Store.Create("USB test"); app.Select(profile.PlayerId, DifficultyId.Easy);
            appRoot.SetActive(true);
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
            panel = Object.FindFirstObjectByType<MainMenuUI>().GetComponent<ControllerPanel>();
            panel.Show(); input.SendMessage("OnApplicationFocus", true);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var old = SceneManager.GetActiveScene();
            var idle = SceneManager.CreateScene("Single USB cleanup " + Guid.NewGuid()); SceneManager.SetActiveScene(idle);
            if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
            if (input != null)
            {
                input.ReleaseRound(); input.Connection.Disconnect();
                var connection = input.Connection;
                Object.DestroyImmediate(input.gameObject);
                if (connection != null) Object.DestroyImmediate(connection.gameObject);
            }
            if (appRoot != null) Object.DestroyImmediate(appRoot);
            if (hadPreference) PlayerPrefs.SetInt(ControllerInput.PreferenceKey, preference);
            else PlayerPrefs.DeleteKey(ControllerInput.PreferenceKey);
            PlayerPrefs.Save();
        }
        [UnityTest] public IEnumerator PortPopupShowsTwoReadableRowsAndSelectsTheChosenPort()
        {
            var dropdown = Field<Dropdown>(panel, "ports");
            dropdown.ClearOptions(); dropdown.AddOptions(new List<string> { "COM3", "COM4" });
            dropdown.alphaFadeSpeed = 0; dropdown.Show(); yield return null; Canvas.ForceUpdateCanvases();
            var items = dropdown.GetComponentsInChildren<Toggle>().Where(item => item.gameObject.activeInHierarchy).ToArray();
            Assert.That(items.Length, Is.EqualTo(2));
            foreach (var item in items)
                Assert.That(item.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(52));
            Capture("port-dropdown", 1280, 720); Capture("port-dropdown", 1920, 1080);
            items[1].isOn = true; yield return null;
            Assert.That(dropdown.value, Is.EqualTo(1));
            Assert.That(dropdown.captionText.text, Is.EqualTo("COM4"));
            dropdown.Hide();
        }
        [UnityTest] public IEnumerator OptionalPhysicalEsp32FeedsFsrAndAcknowledgesLoadCellTare()
        {
            string port = Environment.GetEnvironmentVariable("MICRO_ESP32_TEST_PORT");
            if (string.IsNullOrEmpty(port)) Assert.Ignore("Set MICRO_ESP32_TEST_PORT after confirming both load cells are unloaded.");
            var connection = input.Connection;
            Field<Button[]>(panel, "modeButtons")[1].onClick.Invoke();
            var ports = Field<Dropdown>(panel, "ports");
            int portIndex = ports.options.FindIndex(option => option.text == port);
            Assert.That(portIndex, Is.GreaterThanOrEqualTo(0), "The hardware COM port must appear in the chooser.");
            ports.value = portIndex;
            Field<Button>(panel, "connect").onClick.Invoke();
            float deadline = Time.realtimeSinceStartup + 6;
            while (!connection.IsFresh(SerialConnection.NowSeconds) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(connection.IsFresh(SerialConnection.NowSeconds), Is.True, connection.LastError ?? connection.Status);
            Assert.That(input.CanPlay(out var fsrIssue), Is.True, fsrIssue);
            yield return null;
            Assert.That(Field<Text>(panel, "leftValue").text, Does.Not.Contain("N/A"));
            Assert.That(Field<Text>(panel, "rightValue").text, Does.Not.Contain("N/A"));
            Capture("physical-esp32-fsr", 1280, 720); Capture("physical-esp32-fsr", 1920, 1080);
            Field<Button[]>(panel, "modeButtons")[2].onClick.Invoke(); yield return null;
            var tare = Field<Button>(panel, "tare");
            Assert.That(tare.interactable, Is.True);
            tare.onClick.Invoke();
            deadline = Time.realtimeSinceStartup + 8;
            while ((!connection.LoadTareReady || !input.CanPlay(out _)) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(connection.LoadTareReady, Is.True, connection.LastError ?? connection.Status);
            Assert.That(input.CanPlay(out var loadIssue), Is.True, loadIssue);
            input.SendMessage("OnApplicationFocus", true);
            Assert.That(input.Read().Left.ForceN, Is.Not.Null);
            Assert.That(input.Read().Right.ForceN, Is.Not.Null);
            yield return null;
            Assert.That(Field<Text>(panel, "leftValue").text, Does.Not.Contain("N/A"));
            Assert.That(Field<Text>(panel, "rightValue").text, Does.Not.Contain("N/A"));
            Capture("physical-esp32-load", 1280, 720); Capture("physical-esp32-load", 1920, 1080);
            string directory = Environment.GetEnvironmentVariable("MICRO_SINGLE_USB_EVIDENCE");
            if (!string.IsNullOrEmpty(directory)) File.WriteAllText(Path.Combine(directory, "physical-esp32.txt"),
                "Native ESP32 JSON received; FSR UI values available; both Load Cell Tare acknowledged; both N values available. No reference-load accuracy test performed.");
        }
        private static void Capture(string name, int width, int height)
        {
            string directory = Environment.GetEnvironmentVariable("MICRO_SINGLE_USB_EVIDENCE");
            if (string.IsNullOrEmpty(directory)) return;
            Directory.CreateDirectory(directory);
            var camera = Camera.main; var viewport = camera.GetComponent<GameplayViewport>();
            var target = new RenderTexture(width, height, 24);
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; viewport.Apply(width, height); Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                File.WriteAllBytes(Path.Combine(directory, name + "-" + width + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
                Object.DestroyImmediate(texture); Object.DestroyImmediate(target);
            }
        }
    }
}
