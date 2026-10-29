using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Tests
{
    public sealed class VersusMatchFlowTests
    {
        private const string RecordsKey = "Micro.Versus.Results.v1";
        private GameObject sessionRoot;
        private VersusMatchController flow;
        private VersusFieldController first;
        private VersusFieldController second;
        private bool hadRecords;
        private string originalRecords;

        [UnitySetUp]
        public IEnumerator LoadMatch()
        {
            var runner = GameObject.Find("Code-based tests runner");
            if (runner != null) Object.DontDestroyOnLoad(runner);
            hadRecords = PlayerPrefs.HasKey(RecordsKey);
            originalRecords = PlayerPrefs.GetString(RecordsKey, "[]");
            if (AppSession.Instance != null) Object.DestroyImmediate(AppSession.Instance.gameObject);
            sessionRoot = new GameObject("Injected Versus test session");
            sessionRoot.SetActive(false);
            var app = sessionRoot.AddComponent<AppSession>();
            var values = new Dictionary<string, string>();
            app.Initialize(new ProfileStore(key => values.TryGetValue(key, out var value) ? value : null,
                (key, value) => values[key] = value, () => { }));
            var p1 = app.Store.Create("Player One"); var p2 = app.Store.Create("Player Two");
            app.ConfigureVersus(p1.PlayerId, p2.PlayerId, "#F57C32", "#F57C32", "#3C78D8", "#3C78D8");
            sessionRoot.SetActive(true);
            yield return SceneManager.LoadSceneAsync("VersusGameplay");
            yield return null;
            flow = Object.FindFirstObjectByType<VersusMatchController>();
            Assert.That(flow, Is.Not.Null);
            flow.enabled = false;
            Set("resultEndpoint", "");
            first = Field("P1 playfield"); second = Field("P2 playfield");
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (hadRecords) PlayerPrefs.SetString(RecordsKey, originalRecords); else PlayerPrefs.DeleteKey(RecordsKey);
            PlayerPrefs.Save();
            var delivery = Object.FindFirstObjectByType<VersusResultDelivery>();
            if (delivery != null) Object.DestroyImmediate(delivery.gameObject);
            if (sessionRoot != null) Object.DestroyImmediate(sessionRoot);
            var old = SceneManager.GetActiveScene();
            var idle = SceneManager.CreateScene("Versus QA cleanup " + Guid.NewGuid());
            SceneManager.SetActiveScene(idle);
            if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
        }

        [UnityTest]
        public IEnumerator SceneHasTwoEqualFieldsAndExactlyOneAudioListener()
        {
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            foreach (var field in new[] { first, second })
            {
                var rect = field.GetComponent<RectTransform>();
                Assert.That(rect.rect.width / rect.rect.height, Is.EqualTo(16f / 9).Within(.0001));
                Assert.That(field.GetComponent<RectMask2D>(), Is.Not.Null);
                var body = field.transform.Find("Mascot/Body").GetComponent<Image>();
                var hat = field.transform.Find("Mascot/Hat").GetComponent<Image>();
                Assert.That(body.sprite, Is.Not.Null);
                Assert.That(body.type, Is.EqualTo(Image.Type.Simple));
                Assert.That(hat.color, Is.EqualTo(Color.white));
                Assert.That(body.rectTransform.rect.width / rect.rect.width, Is.EqualTo(1.638889f / 16).Within(.00001));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SelectedBlueShoesRenderBluePixelsInTheFootArea()
        {
            StartClock();
            var camera = Camera.main;
            var target = new RenderTexture(1920, 1080, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.GetComponent<GameplayViewport>().Apply(1920, 1080);
                Canvas.ForceUpdateCanvases(); camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); texture.Apply();
                var body = second.transform.Find("Mascot/Body").GetComponent<RectTransform>();
                var corners = new Vector3[4]; body.GetWorldCorners(corners);
                var minimum = camera.WorldToScreenPoint(corners[0]);
                var maximum = camera.WorldToScreenPoint(corners[2]);
                int blue = 0;
                int coloredShoePixels = 0;
                for (int y = Mathf.CeilToInt(minimum.y); y < minimum.y + (maximum.y - minimum.y) * .2f; y++)
                    for (int x = Mathf.CeilToInt(minimum.x); x < maximum.x; x++)
                    {
                        var color = texture.GetPixel(x, y);
                        if (color.g > color.r * 1.1f && Mathf.Max(color.g, color.b) > .03f)
                        {
                            coloredShoePixels++;
                            if (color.b > color.g * 1.2f) blue++;
                        }
                    }
                Assert.That(blue, Is.GreaterThan(2), "Blue shoe pixels at " + minimum + " to " + maximum + "; candidates=" + coloredShoePixels);
                Assert.That(blue, Is.GreaterThan(coloredShoePixels * .7f), "Most colored shoe pixels must use the selected blue hue, not an olive mixture.");
            }
            finally
            {
                RenderTexture.active = previousActive; camera.targetTexture = previousTarget;
                Object.DestroyImmediate(texture); Object.DestroyImmediate(target);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SharedCountdownAndPauseFreezeBothFieldsAndActiveClock()
        {
            StartClock();
            Tick(.25f);
            Assert.That(first.ActiveCount, Is.EqualTo(second.ActiveCount));
            Assert.That(first.ActiveCount, Is.GreaterThan(0));
            var one = Drops(first); var two = Drops(second);
            Assert.That(one[0].sprite, Is.SameAs(two[0].sprite));
            Assert.That(one[0].rectTransform.anchoredPosition, Is.EqualTo(two[0].rectTransform.anchoredPosition));
            flow.Pause();
            float time = flow.Model.PlayedSeconds;
            var position = one[0].rectTransform.anchoredPosition;
            for (int i = 0; i < 20; i++) Tick(.25f);
            Assert.That(flow.Model.PlayedSeconds, Is.EqualTo(time));
            Assert.That(one[0].rectTransform.anchoredPosition, Is.EqualTo(position));
            Capture("Versus-Pause", 1280, 720);
            flow.Resume();
            Tick(.25f);
            Assert.That(flow.Phase, Is.EqualTo(VersusPlayPhase.Countdown));
            Assert.That(flow.Model.PlayedSeconds, Is.EqualTo(time));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FieldsCanCatchDifferentOutcomesWithoutMissedFoodHpDamage()
        {
            StartClock(); first.ResetField(); second.ResetField();
            var right = new InputSnapshot(new HandInput(false, 0, true, null), new HandInput(true, 1, true, null), "Test");
            second.Step(right, .5f);
            var record = new VersusSpawnRecord(100001, ObjectKind.Food, 0, 0, 0, 100);
            first.Spawn(record); second.Spawn(record);
            var a = first.Step(default, .1f); var b = second.Step(default, .1f);
            flow.Model.ApplyStep(.1f, a, b);
            Assert.That(flow.Model.Player1.Score.FoodCaught, Is.EqualTo(1));
            Assert.That(flow.Model.Player2.Score.FoodMissed, Is.EqualTo(1));
            Assert.That(flow.Model.Player1.Health.CurrentHp, Is.EqualTo(3));
            Assert.That(flow.Model.Player2.Health.CurrentHp, Is.EqualTo(3));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SimultaneousDeathShowsDrawAndRematchUsesANewId()
        {
            StartClock(); first.ResetField(); second.ResetField();
            var originalId = Get<string>("matchId");
            for (int i = 0; i < 3; i++)
            {
                var trash = new VersusSpawnRecord(100001 + i, ObjectKind.Trash, 0, 0, 0, 100);
                first.Spawn(trash); second.Spawn(trash);
            }
            Tick(.1f);
            Assert.That(flow.Phase, Is.EqualTo(VersusPlayPhase.Terminal));
            Assert.That(flow.Model.Result.Winner, Is.EqualTo(VersusWinner.Draw));
            for (int i = 0; i < 7; i++) Tick(.25f);
            Assert.That(flow.Phase, Is.EqualTo(VersusPlayPhase.Results));
            Assert.That(GameObject.Find("Winner").GetComponent<Text>().text, Does.Contain("DRAW"));
            float time = flow.Model.PlayedSeconds;
            Tick(.25f);
            Assert.That(flow.Model.PlayedSeconds, Is.EqualTo(time));
            flow.Restart();
            Assert.That(Get<string>("matchId"), Is.Not.EqualTo(originalId));
            Assert.That(flow.Phase, Is.EqualTo(VersusPlayPhase.Countdown));
            Assert.That(flow.Model.Player1.Health.CurrentHp, Is.EqualTo(3));
            Assert.That(first.ActiveCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameplayAndWinnerResultsRenderAtBothTargetSizes()
        {
            StartClock();
            Tick(.25f); Tick(.25f); Tick(.25f);
            Capture("Versus-Gameplay-720", 1280, 720);
            Capture("Versus-Gameplay-1080", 1920, 1080);
            first.ResetField(); second.ResetField();
            for (int i = 0; i < 3; i++)
                first.Spawn(new VersusSpawnRecord(100001 + i, ObjectKind.Trash, 0, 0, 0, 100));
            Tick(.1f);
            Assert.That(flow.Model.Result.Winner, Is.EqualTo(VersusWinner.P2));
            for (int i = 0; i < 7; i++) Tick(.25f);
            flow.GetComponent<VersusGameplayUI>().SetUploadStatus("Verification run · Upload disabled");
            Capture("Versus-Result-720", 1280, 720);
            Capture("Versus-Result-1080", 1920, 1080);
            yield return null;
        }

        private void StartClock()
        {
            if (flow.Phase == VersusPlayPhase.Paused) flow.Resume();
            for (int i = 0; i < 13; i++) Tick(.25f);
            Assert.That(flow.Phase, Is.EqualTo(VersusPlayPhase.Playing));
        }

        [UnityTest]
        public IEnumerator SetupShoesMatchGameplayAndKeepTheirColorAcrossAnimationFrames()
        {
            var gameplayBody = second.transform.Find("Mascot/Body").GetComponent<Image>();
            var gameplayBlue = gameplayBody.sprite;
            var defaultMaterial = Graphic.defaultGraphicMaterial;
            Assert.That(gameplayBody.material, Is.SameAs(defaultMaterial));
            yield return SceneManager.LoadSceneAsync("VersusSetup");
            yield return null;
            var setup = Object.FindFirstObjectByType<VersusSetupUI>();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var practice = (VersusPracticePlayer)typeof(VersusSetupUI).GetField("player2Practice", flags).GetValue(setup);
            practice.enabled = false;
            var body = (Image)typeof(VersusPracticePlayer).GetField("body", flags).GetValue(practice);
            Assert.That(body.material, Is.SameAs(defaultMaterial));
            Assert.That(body.sprite, Is.SameAs(gameplayBlue));
            foreach (string hex in new[] { "#F57C32", "#3C78D8", "#68A832", "#9B59B6", "#E6B422", "#D94A64" })
            {
                setup.SetColors(2, hex, hex);
                Assert.That(body.material, Is.SameAs(defaultMaterial));
                foreach (bool moving in new[] { true, true, true, true, false })
                {
                    practice.StepPractice(new InputSnapshot(default, new HandInput(moving, moving ? 1 : 0, true, null), "Test"), .1f);
                    Assert.That(body.sprite, Is.SameAs(MascotBodyPalette.ForFrame(body.sprite, hex)));
                    Assert.That(body.color, Is.EqualTo(Color.white));
                }
            }
            setup.SetColors(2, "#3C78D8", "#3C78D8");
            Capture("Versus-Setup-Shoes-720", 1280, 720);
            Capture("Versus-Setup-Shoes-1080", 1920, 1080);
        }

        private void Tick(float seconds)
        {
            var method = typeof(VersusMatchController).GetMethod("Step", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, "Match simulation needs the same explicit Step entry point as Single Player for deterministic scene tests.");
            method.Invoke(flow, new object[] { seconds });
        }

        private T Get<T>(string name) => (T)typeof(VersusMatchController).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(flow);
        private void Set(string name, object value) => typeof(VersusMatchController).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(flow, value);
        private static VersusFieldController Field(string name) => GameObject.Find(name).GetComponent<VersusFieldController>();
        private static Image[] Drops(VersusFieldController field) => field.GetComponentsInChildren<Image>().Where(image => image.name == "Falling item").ToArray();

        [UnityTest]
        public IEnumerator ReturningToModeSelectAbandonsWithoutSavingAMatch()
        {
            StartClock(); Tick(.25f);
            var originalIdle = flow.BodyFrames[0];
            string before = PlayerPrefs.GetString(RecordsKey, "[]");
            flow.ReturnToMenu();
            float deadline = Time.realtimeSinceStartup + 8;
            while (SceneManager.GetActiveScene().name != "ModeSelect" && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("ModeSelect"));
            yield return null;
            Assert.That(PlayerPrefs.GetString(RecordsKey, "[]"), Is.EqualTo(before));
            var images = Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
            var blue = images.Single(image => image.name == "Body" && image.transform.parent.name == "Versus Mascot 2");
            var solo = images.Single(image => image.name == "Body" && image.transform.parent.name == "Solo Mascot");
            Assert.That(blue.sprite, Is.SameAs(MascotBodyPalette.ForFrame(originalIdle, "#3C78D8")));
            Assert.That(solo.sprite, Is.SameAs(originalIdle));
            Capture("ModeSelect-BodyColors-720", 1280, 720);
            Capture("ModeSelect-BodyColors-1080", 1920, 1080);
        }

        [UnityTest]
        public IEnumerator KeyboardChannelsMoveIndependentlyAndOpposingDirectionsCancel()
        {
            StartClock();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                foreach (var input in flow.GetComponents<VersusKeyboardInput>()) input.SendMessage("OnApplicationFocus", true);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.RightArrow));
                InputSystem.Update(); Tick(.25f);
                Assert.That(first.PositionX, Is.LessThan(0));
                Assert.That(second.PositionX, Is.GreaterThan(0));
                float before = first.PositionX;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.D));
                InputSystem.Update(); Tick(.25f);
                Assert.That(first.PositionX, Is.EqualTo(before));
                flow.SendMessage("OnApplicationFocus", false);
                Assert.That(flow.Phase, Is.EqualTo(VersusPlayPhase.Paused));
            }
            finally { InputSystem.RemoveDevice(keyboard); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator EndlessSceneSurvivesPastSixtySecondsWithBoundedActiveObjects()
        {
            StartClock();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                foreach (var input in flow.GetComponents<VersusKeyboardInput>()) input.SendMessage("OnApplicationFocus", true);
                for (int i = 0; i < 2400; i++)
                {
                    var keys = new List<Key>();
                    int a = AvoidTrash(first); int b = AvoidTrash(second);
                    if (a != 0) keys.Add(a < 0 ? Key.A : Key.D);
                    if (b != 0) keys.Add(b < 0 ? Key.LeftArrow : Key.RightArrow);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray()));
                    InputSystem.Update(); Tick(.05f);
                    Assert.That(first.ActiveCount, Is.LessThanOrEqualTo(8));
                    Assert.That(second.ActiveCount, Is.LessThanOrEqualTo(8));
                    Assert.That(flow.Phase, Is.EqualTo(VersusPlayPhase.Playing), "Simulation step " + i);
                }
                Assert.That(flow.Model.PlayedSeconds, Is.GreaterThan(119));
                Assert.That(flow.Model.CurrentTier, Is.GreaterThanOrEqualTo(5));
            }
            finally { InputSystem.RemoveDevice(keyboard); }
            yield return null;
        }

        private int AvoidTrash(VersusFieldController field)
        {
            foreach (var drop in Drops(field))
            {
                if (drop.sprite != flow.Definitions[3].sprite && drop.sprite != flow.Definitions[4].sprite) continue;
                var p = drop.rectTransform.anchoredPosition / 56.25f;
                if (p.y > 2 || p.y < -2.4f || Mathf.Abs(p.x - field.PositionX) >= 1.4f) continue;
                if (Mathf.Approximately(p.x, field.PositionX)) return field.PositionX > 0 ? -1 : 1;
                int direction = p.x > field.PositionX ? -1 : 1;
                if (field.PositionX < -6.7f && direction < 0) return 1;
                if (field.PositionX > 6.7f && direction > 0) return -1;
                return direction;
            }
            return 0;
        }

        private static void Capture(string name, int width, int height)
        {
            var camera = Camera.main;
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            camera.GetComponent<GameplayViewport>().Apply(width, height);
            for (int i = 0; i < 3; i++)
            {
                foreach (var label in Object.FindObjectsByType<Text>(FindObjectsSortMode.None)) label.SetAllDirty();
                Canvas.ForceUpdateCanvases();
            }
            camera.Render();
            var old = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
            var directory = Path.GetFullPath(Application.dataPath + "/../../verification/versus-validation");
            Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory, name + ".png"), texture.EncodeToPNG());
            RenderTexture.active = old; camera.targetTexture = null;
            Object.DestroyImmediate(texture); Object.DestroyImmediate(target);
        }
    }
}
