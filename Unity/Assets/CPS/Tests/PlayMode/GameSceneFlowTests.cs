using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Tests
{
    public sealed class GameSceneFlowTests
    {
        private AppSession app;
        [SetUp]
        public void Setup()
        {
            var runner = GameObject.Find("Code-based tests runner");
            if (runner != null) Object.DontDestroyOnLoad(runner);
            var values = new Dictionary<string, string>();
            var go = new GameObject("Injected scene-test AppSession"); go.SetActive(false);
            app = go.AddComponent<AppSession>();
            app.Initialize(new ProfileStore(key => values.TryGetValue(key, out var value) ? value : null,
                (key, value) => values[key] = value, () => { }));
            go.SetActive(true);
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (app != null) Object.DestroyImmediate(app.gameObject);
            var loaded = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == "Boot" || scene.name == "ModeSelect" || scene.name == "VersusSetup" ||
                    scene.name == "MainMenu" || scene.name == "Gameplay") loaded.Add(scene);
            }
            if (loaded.Count == 0) yield break;
            var idle = SceneManager.GetSceneByName("Task5TestIdle");
            if (!idle.IsValid()) idle = SceneManager.CreateScene("Task5TestIdle");
            SceneManager.SetActiveScene(idle);
            foreach (var scene in loaded) yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest]
        public IEnumerator BootRoutesToModeSelectionThenSinglePlayerAndRejectsDuplicateTransition()
        {
            Assert.That(Application.CanStreamedLevelBeLoaded("Boot"), Is.True);
            yield return SceneManager.LoadSceneAsync("Boot");
            yield return WaitForActiveScene("ModeSelect");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("ModeSelect"));
            Object.FindFirstObjectByType<ModeSelectionUI>().OpenSinglePlayer();
            yield return WaitForActiveScene("MainMenu");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            Assert.That(Object.FindObjectsByType<AppSession>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            var menu = Object.FindFirstObjectByType<MainMenuUI>();
            Assert.That(menu.ValidationError, Does.Contain("profile"));
            CreateSelectedProfile(menu, DifficultyId.Normal);
            string firstId = app.SelectedProfile.PlayerId;
            var input = (InputField)typeof(MainMenuUI).GetField("nicknameField", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
            input.text = "Demo Player";
            Assert.That(menu.CreateProfile(), Is.True);
            string secondId = app.SelectedProfile.PlayerId;
            var dropdown = (Dropdown)typeof(MainMenuUI).GetField("profileDropdown", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
            Assert.That(dropdown.options.Count, Is.EqualTo(2));
            Assert.That(dropdown.options[0].text, Is.Not.EqualTo(dropdown.options[1].text));
            dropdown.value = 0;
            Assert.That(app.SelectedProfile.PlayerId, Is.EqualTo(firstId));
            dropdown.value = 1;
            Assert.That(app.SelectedProfile.PlayerId, Is.EqualTo(secondId));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(menu.transform.Find("CREATE PROFILE").gameObject));
            var create = (Button)typeof(MainMenuUI).GetField("createButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
            Assert.That(input.navigation.selectOnDown, Is.SameAs(create));
            var start = (Button)typeof(MainMenuUI).GetField("startButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
            start.onClick.Invoke();
            Assert.That(menu.GetComponent<SceneNavigator>().IsTransitioning, Is.True);
            start.onClick.Invoke();
            Assert.That(menu.StartSelectedGame(), Is.False);
            yield return WaitForActiveScene("Gameplay");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Gameplay"));
            var flow = Object.FindFirstObjectByType<GameSessionController>();
            Assert.That(flow.ValidationError, Is.Null);
            Assert.That(flow.State, Is.EqualTo(SessionState.Ready));
        }
        [UnityTest]
        public IEnumerator ReferenceHomeCreationCancelAndValidationPreserveProfiles()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            for (int i = 0; i < 5; i++) yield return null;
            var menu = Object.FindFirstObjectByType<MainMenuUI>();
            var open = menu.transform.Find("CREATE PROFILE").GetComponent<Button>();
            var overlay = menu.transform.Find("Create profile overlay");
            var input = overlay.GetComponentInChildren<InputField>(true);
            Assert.That(overlay.gameObject.activeSelf, Is.False);
            open.onClick.Invoke(); yield return null;
            Assert.That(overlay.gameObject.activeSelf, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(input.gameObject));
            input.text = "";
            var confirm = (Button)typeof(MainMenuUI).GetField("createButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
            confirm.onClick.Invoke();
            Assert.That(app.Store.Profiles.Count, Is.Zero);
            Assert.That(overlay.gameObject.activeSelf, Is.True, "Invalid names must keep dialog open");
            input.text = "Arc";
            yield return CaptureProduction("create", new Vector2Int(1920,1080), "CPS_HOME_CAPTURE_DIR", "home");
            confirm.onClick.Invoke(); yield return null;
            Assert.That(app.Store.Profiles.Count, Is.EqualTo(1));
            Assert.That(app.SelectedProfile.DisplayName, Is.EqualTo("Arc"));
            Assert.That(overlay.gameObject.activeSelf, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(open.gameObject));
            open.onClick.Invoke(); input.text = "Unsaved";
            overlay.Find("Create card/Cancel creation").GetComponent<Button>().onClick.Invoke();
            Assert.That(app.Store.Profiles.Count, Is.EqualTo(1));
            yield return CaptureProduction("menu", new Vector2Int(1920,1080), "CPS_HOME_CAPTURE_DIR", "home");
            yield return CaptureProduction("menu", new Vector2Int(1280,720), "CPS_HOME_CAPTURE_DIR", "home");
            menu.ShowProfileManager(); yield return null;
            Assert.That(menu.transform.Find("Approved practice").gameObject.activeSelf, Is.False);
            yield return CaptureProduction("profiles", new Vector2Int(1920,1080), "CPS_HOME_CAPTURE_DIR", "home");
            menu.HideProfileManager();
            var practice = menu.GetComponent<MenuPracticeUI>(); practice.ShowRules(); yield return null;
            yield return CaptureProduction("rules", new Vector2Int(1920,1080), "CPS_HOME_CAPTURE_DIR", "home");
            practice.HideRules();
            var gear = (Button)typeof(MainMenuUI).GetField("settingsButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
            gear.onClick.Invoke(); yield return null;
            yield return CaptureProduction("settings", new Vector2Int(1920,1080), "CPS_HOME_CAPTURE_DIR", "home");
        }

        [UnityTest]
        public IEnumerator FreshSessionSelectsSolePersistedProfileWhenProductionMenuLoads()
        {
            var values = new Dictionary<string, string>();
            var persisted = new ProfileStore(key => values.TryGetValue(key, out var value) ? value : null,
                (key, value) => values[key] = value, () => { });
            persisted.Load();
            string playerId = persisted.Create("Returning Player").PlayerId;
            Object.DestroyImmediate(app.gameObject);
            var root = new GameObject("Reloaded AppSession"); root.SetActive(false);
            app = root.AddComponent<AppSession>();
            app.Initialize(new ProfileStore(key => values.TryGetValue(key, out var value) ? value : null,
                (key, value) => values[key] = value, () => { }));
            root.SetActive(true);
            Assert.That(app.Store.Profiles.Count, Is.EqualTo(1));
            Assert.That(app.SelectedProfile, Is.Null);

            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;

            var menu = Object.FindFirstObjectByType<MainMenuUI>();
            var dropdown = (Dropdown)typeof(MainMenuUI).GetField("profileDropdown", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
            var start = (Button)typeof(MainMenuUI).GetField("startButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
            Assert.That(dropdown.value, Is.Zero);
            Assert.That(dropdown.options.Count, Is.EqualTo(1));
            Assert.That(dropdown.captionText.text, Does.Contain(playerId.Substring(0, 6)));
            Assert.That(app.SelectedProfile, Is.Not.Null, "The displayed saved profile must become the session selection when the menu mounts.");
            Assert.That(app.SelectedProfile.PlayerId, Is.EqualTo(playerId));
            Assert.That(app.SelectedDifficulty, Is.EqualTo(DifficultyId.Easy));
            Assert.That(start.interactable, Is.True);
            Assert.That(menu.ValidationError, Is.Null);
            Assert.That(app.Store.Profiles.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ProductionSettingsOpenFromMenuAndPauseThenBackDiscardsRound()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
            var menu = Object.FindFirstObjectByType<MainMenuUI>();
            var menuSettings = (Button)typeof(MainMenuUI).GetField("settingsButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
            var menuPanel = (AudioSettingsPanel)typeof(MainMenuUI).GetField("settingsPanel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
            EventSystem.current.SetSelectedGameObject(menuSettings.gameObject);
            menuSettings.onClick.Invoke(); yield return null;
            Assert.That(menuPanel.gameObject.activeSelf, Is.True);
            var close = (Button)typeof(AudioSettingsPanel).GetField("closeButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menuPanel);
            close.onClick.Invoke();
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(menuSettings.gameObject));

            CreateSelectedProfile(menu, DifficultyId.Easy);
            Assert.That(menu.StartSelectedGame(), Is.True);
            yield return WaitForActiveScene("Gameplay"); yield return null;
            var flow = Object.FindFirstObjectByType<GameSessionController>();
            flow.enabled = false; flow.Restart(); flow.Step(3);
            var ui = Object.FindFirstObjectByType<GameplayUI>();
            var pause = (Button)typeof(GameplayUI).GetField("pauseButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            var settings = (Button)typeof(GameplayUI).GetField("settingsButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            var back = (Button)typeof(GameplayUI).GetField("backToMenuButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            var panel = (AudioSettingsPanel)typeof(GameplayUI).GetField("settingsPanel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            pause.onClick.Invoke(); yield return null;
            EventSystem.current.SetSelectedGameObject(settings.gameObject);
            settings.onClick.Invoke(); yield return null;
            Assert.That(panel.gameObject.activeSelf, Is.True);
            close = (Button)typeof(AudioSettingsPanel).GetField("closeButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panel);
            close.onClick.Invoke();
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(settings.gameObject));
            back.onClick.Invoke();
            yield return WaitForActiveScene("MainMenu");
            Assert.That(app.SelectedProfile.PlayCount, Is.Zero);
        }
        [UnityTest]
        public IEnumerator AuthoredScenesCompleteAllDifficultiesAndReturnWithPersistentSelection()
        {
            Assert.That(Application.CanStreamedLevelBeLoaded("MainMenu"), Is.True);
            app.Store.Create("Other Player");
            var selected = app.Store.Create("Selected Player");
            app.Select(selected.PlayerId, DifficultyId.Easy);
            foreach (DifficultyId difficulty in new[] { DifficultyId.Easy, DifficultyId.Normal, DifficultyId.Hard })
            {
                yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
                var menu = Object.FindFirstObjectByType<MainMenuUI>();
                CreateSelectedProfile(menu, difficulty);
                Assert.That(menu.ValidationError, Is.Null);
                Assert.That(menu.StartSelectedGame(), Is.True);
                yield return WaitForActiveScene("Gameplay");
                yield return null;
                var flow = Object.FindFirstObjectByType<GameSessionController>(); flow.enabled = false;
                flow.Restart(); flow.Step(3);
                var spawner = Object.FindFirstObjectByType<FallingObjectSpawner>();
                for (int second = 0; second < 60; second++)
                {
                    foreach (var item in spawner.GetComponentsInChildren<FallingObject>())
                    {
                        var definition = (FallingObjectDefinition)typeof(FallingObject)
                            .GetField("definition", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(item);
                        item.TryResolve(item.SpawnId, definition.kind == ObjectKind.Food ? ObjectOutcome.Caught : ObjectOutcome.Missed);
                    }
                    flow.Step(1);
                    if (difficulty == DifficultyId.Easy && second == 0)
                    {
                        string captureDirectory = System.Environment.GetEnvironmentVariable("CPS_TASK5_CAPTURE_DIR");
                        if (!string.IsNullOrEmpty(captureDirectory))
                        {
                            string capturePath = Path.Combine(captureDirectory, "task7-gameplay-production-" +
                                System.DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".png");
                            yield return null;
                            CaptureRender(capturePath);
                        }
                    }
                }
                Assert.That(flow.State, Is.EqualTo(SessionState.Triumph));
                Assert.That(flow.LastResult.PlayedSeconds, Is.EqualTo(60).Within(0.001));
                Assert.That(flow.LastResult.Difficulty, Is.EqualTo(difficulty));
                Assert.That(flow.LastResult.FoodCaught, Is.GreaterThan(0));
                flow.Step(1.2f); Assert.That(flow.State, Is.EqualTo(SessionState.Results));
                var results = Object.FindFirstObjectByType<ResultPanel>();
                Assert.That(results, Is.Not.Null);
                Assert.That(results.gameObject.activeInHierarchy, Is.True);
                var restart = (Button)typeof(ResultPanel).GetField("restartButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(results);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(restart.gameObject));
                restart.onClick.Invoke(); flow.Step(3);
                var source = (Text)typeof(GameplayUI).GetField("inputSource", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(Object.FindFirstObjectByType<GameplayUI>());
                Assert.That(source.gameObject.activeInHierarchy, Is.False, "Reference HUD hides the simulated-input footer.");
                var leftInput = (Text)typeof(GameplayUI).GetField("leftHand", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(Object.FindFirstObjectByType<GameplayUI>());
                Assert.That(leftInput.gameObject.activeInHierarchy, Is.True, "Strength HUD must return on Restart.");
                for (int second = 0; second < 20 && flow.State == SessionState.Playing; second++)
                {
                    foreach (var item in spawner.GetComponentsInChildren<FallingObject>())
                        item.TryResolve(item.SpawnId, ObjectOutcome.Missed);
                    flow.Step(1);
                }
                Assert.That(flow.State, Is.EqualTo(SessionState.Dying));
                flow.Step(1.4f); Assert.That(flow.State, Is.EqualTo(SessionState.Results));
                var menuButton = (Button)typeof(ResultPanel).GetField("menuButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(results);
                menuButton.onClick.Invoke();
                yield return WaitForActiveScene("MainMenu");
                Assert.That(app.SelectedProfile.PlayerId, Is.EqualTo(selected.PlayerId));
                Assert.That(app.SelectedDifficulty, Is.EqualTo(difficulty));
                var returnedMenu = Object.FindFirstObjectByType<MainMenuUI>();
                var returnedDropdown = (Dropdown)typeof(MainMenuUI).GetField("profileDropdown", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(returnedMenu);
                var returnedStart = (Button)typeof(MainMenuUI).GetField("startButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(returnedMenu);
                Assert.That(returnedDropdown.value, Is.EqualTo(1));
                Assert.That(returnedStart.interactable, Is.True);
                string selectedField = difficulty == DifficultyId.Easy ? "easyButton" :
                    difficulty == DifficultyId.Normal ? "normalButton" : "hardButton";
                var selectedButton = (Button)typeof(MainMenuUI).GetField(selectedField,
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(returnedMenu);
                Assert.That(selectedButton.colors.normalColor, Is.EqualTo(Color.white));
                Assert.That(Object.FindObjectsByType<AppSession>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            }
            Assert.That(app.SelectedProfile.PlayCount, Is.EqualTo(6));
        }
        [UnityTest]
        public IEnumerator MenuAndActualGameplayInstanceRejectMissingDependenciesWithoutStarting()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
            var menu = Object.FindFirstObjectByType<MainMenuUI>(); CreateSelectedProfile(menu, DifficultyId.Easy);
            var templateField = typeof(MainMenuUI).GetField("gameplayTemplate", BindingFlags.Instance | BindingFlags.NonPublic);
            var template = templateField.GetValue(menu);
            templateField.SetValue(menu, null);
            Assert.That(menu.ValidationError, Does.Contain("template"));
            Assert.That(menu.StartSelectedGame(), Is.False);
            templateField.SetValue(menu, template);
            yield return SceneManager.LoadSceneAsync("Gameplay"); yield return null;
            var flow = Object.FindFirstObjectByType<GameSessionController>(); flow.enabled = false;
            var spawner = Object.FindFirstObjectByType<FallingObjectSpawner>();
            typeof(FallingObjectSpawner).GetField("prefab", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(spawner, null);
            Assert.That(flow.ValidationError, Does.Contain("prefab"));
            Assert.That(flow.BeginRound(), Is.False);
            Assert.That(spawner.ActiveCount, Is.Zero);
            Assert.That(app.SelectedProfile.PlayCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator MenuRejectsDifferentSpawnerPlayerOnSharedTemplate()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            var menu = Object.FindFirstObjectByType<MainMenuUI>();
            CreateSelectedProfile(menu, DifficultyId.Easy);
            var template = (GameSessionController)typeof(MainMenuUI)
                .GetField("gameplayTemplate", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
            var spawner = template.GetComponent<FallingObjectSpawner>();
            var field = typeof(FallingObjectSpawner).GetField("player", BindingFlags.Instance | BindingFlags.NonPublic);
            var original = field.GetValue(spawner);
            var different = new GameObject("Different catcher").AddComponent<PlayerController>();
            try
            {
                field.SetValue(spawner, different);
                string error = menu.ValidationError;
                Assert.That(error, Is.Not.Null);
                Assert.That(error, Does.Contain("player"));
                Assert.That(menu.StartSelectedGame(), Is.False);
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            }
            finally
            {
                field.SetValue(spawner, original);
                Object.DestroyImmediate(different.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator ViewportKeepsCanvasOnCameraAtReferenceSizeAcrossAspectRatios()
        {
            yield return SceneManager.LoadSceneAsync("Gameplay");
            var camera = Camera.main;
            var viewport = camera.GetComponent<GameplayViewport>();
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            var originalMode = canvas.renderMode;
            var originalCamera = canvas.worldCamera;
            var originalScaleMode = scaler.uiScaleMode;
            var originalResolution = scaler.referenceResolution;
            var originalMatch = scaler.matchWidthOrHeight;
            var originalScreenMatch = scaler.screenMatchMode;
            var originalTarget = camera.targetTexture;
            viewport.enabled = false;
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.referenceResolution = new Vector2(100, 100);
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080),
                    new Vector2Int(1024, 768), new Vector2Int(1080, 1920), new Vector2Int(2560, 1080) })
                {
                    var target = new RenderTexture(size.x, size.y, 24);
                    try
                    {
                        camera.targetTexture = target;
                        viewport.Apply(size.x, size.y);
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera));
                        Assert.That(canvas.worldCamera, Is.SameAs(camera));
                        Assert.That(canvas.pixelPerfect, Is.True, "Screen-space text must stay pixel-aligned after viewport changes");
                        Assert.That(scaler.uiScaleMode, Is.EqualTo(UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize));
                        Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920, 1080)));
                        Assert.That(scaler.screenMatchMode, Is.EqualTo(UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand));
                        float scale = Mathf.Min(size.x / 1920f, size.y / 1080f);
                        Assert.That(camera.pixelRect.width, Is.EqualTo(1920 * scale).Within(1));
                        Assert.That(camera.pixelRect.height, Is.EqualTo(1080 * scale).Within(1));
                        Assert.That(canvas.pixelRect, Is.EqualTo(camera.pixelRect));
                        Assert.That(canvas.scaleFactor, Is.EqualTo(scale).Within(.01f));
                        var bounds = canvas.GetComponent<RectTransform>().rect;
                        Assert.That(bounds.width, Is.EqualTo(1920).Within(2));
                        Assert.That(bounds.height, Is.EqualTo(1080).Within(2));
                        foreach (var child in canvas.GetComponentsInChildren<RectTransform>(false))
                        {
                            if (child == canvas.transform) continue;
                            var uiBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas.transform, child);
                            Assert.That(uiBounds.min.x, Is.GreaterThanOrEqualTo(-961), child.name + " left outside playfield");
                            Assert.That(uiBounds.max.x, Is.LessThanOrEqualTo(961), child.name + " right outside playfield");
                            Assert.That(uiBounds.min.y, Is.GreaterThanOrEqualTo(-541), child.name + " below playfield");
                            Assert.That(uiBounds.max.y, Is.LessThanOrEqualTo(541), child.name + " above playfield");
                        }
                        TestContext.WriteLine("Target " + target.width + "x" + target.height +
                            "; camera=" + camera.pixelRect + "; canvas=" + canvas.pixelRect +
                            "; scale=" + canvas.scaleFactor + "; root=" + bounds.size);
                    }
                    finally
                    {
                        camera.targetTexture = null;
                        Object.DestroyImmediate(target);
                    }
                }
            }
            finally
            {
                camera.targetTexture = originalTarget;
                canvas.renderMode = originalMode;
                canvas.worldCamera = originalCamera;
                scaler.uiScaleMode = originalScaleMode;
                scaler.referenceResolution = originalResolution;
                scaler.matchWidthOrHeight = originalMatch;
                scaler.screenMatchMode = originalScreenMatch;
                viewport.enabled = true;
            }
        }
        [Test]
        public void ViewportPreservesSixteenByNineInsideTallAndWideScreens()
        {
            var go = new GameObject("Viewport");
            try
            {
                var camera = go.AddComponent<Camera>(); var viewport = go.AddComponent<GameplayViewport>();
                viewport.Apply(1024, 768);
                Assert.That(camera.rect.height, Is.EqualTo(0.75f).Within(0.0001));
                Assert.That(camera.rect.y, Is.EqualTo(0.125f).Within(0.0001));
                viewport.Apply(2560, 1080);
                Assert.That(camera.rect.width, Is.EqualTo(0.75f).Within(0.0001));
                Assert.That(camera.rect.x, Is.EqualTo(0.125f).Within(0.0001));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [UnityTest]
        public IEnumerator ProductionMenuGameplayAndResultsRenderAtRequiredSizes()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
            yield return CaptureProduction("menu", new Vector2Int(1920, 1080));
            var menu = Object.FindFirstObjectByType<MainMenuUI>();
            CreateSelectedProfile(menu, DifficultyId.Normal);
            Assert.That(menu.StartSelectedGame(), Is.True);
            yield return WaitForActiveScene("Gameplay");
            yield return null;
            var flow = Object.FindFirstObjectByType<GameSessionController>(); flow.enabled = false;
            flow.Restart(); flow.Step(3);
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080),
                new Vector2Int(1024, 768), new Vector2Int(1080, 1920) })
                yield return CaptureProduction("gameplay", size);
            var ui = Object.FindFirstObjectByType<GameplayUI>();
            var pause = (Button)typeof(GameplayUI).GetField("pauseButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            var resume = (Button)typeof(GameplayUI).GetField("resumeButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            pause.onClick.Invoke();
            yield return null;
            Assert.That(flow.IsPaused, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(resume.gameObject));
            yield return CaptureProduction("paused", new Vector2Int(1920, 1080));
            resume.onClick.Invoke();
            Assert.That(flow.IsPaused, Is.False);
            flow.Step(3);
            var spawner = Object.FindFirstObjectByType<FallingObjectSpawner>();
            for (int second = 0; second < 60; second++)
            {
                foreach (var item in spawner.GetComponentsInChildren<FallingObject>())
                {
                    var definition = (FallingObjectDefinition)typeof(FallingObject)
                        .GetField("definition", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(item);
                    item.TryResolve(item.SpawnId, definition.kind == ObjectKind.Food ? ObjectOutcome.Caught : ObjectOutcome.Missed);
                }
                flow.Step(1);
            }
            Assert.That(flow.State, Is.EqualTo(SessionState.Triumph));
            flow.Step(1.2f);
            Assert.That(flow.State, Is.EqualTo(SessionState.Results));
            var sourceLabel = (Text)typeof(GameplayUI).GetField("inputSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            Assert.That(sourceLabel.gameObject.activeInHierarchy, Is.False, "Gameplay source hint must not peek beneath Results actions.");
            yield return CaptureProduction("results", new Vector2Int(1920, 1080));
        }

        [UnityTest]
        public IEnumerator ProductionMascotClipsAndTerminalPhasesRenderInBothFacings()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            var menu = Object.FindFirstObjectByType<MainMenuUI>();
            CreateSelectedProfile(menu, DifficultyId.Normal);
            Assert.That(menu.StartSelectedGame(), Is.True);
            yield return WaitForActiveScene("Gameplay");
            yield return null;
            var flow = Object.FindFirstObjectByType<GameSessionController>();
            flow.enabled = false;
            flow.Restart(); flow.Step(3);
            var mascot = Object.FindFirstObjectByType<MascotPresentation>();
            var animator = mascot.GetComponent<Animator>();
            var body = mascot.transform.Find("Body").GetComponent<SpriteRenderer>();
            var hat = mascot.transform.Find("Hat").GetComponent<SpriteRenderer>();
            mascot.enabled = false;
            string[] states = { "Idle", "Idle", "Run", "Run", "Run", "Run", "Eat", "Eat", "Hurt", "Dead", "Triumph", "Triumph" };
            string[] sprites = { "IdleA", "IdleBlink", "RunA", "RunB", "RunC", "RunD", "EatOpen", "EatChomp", "Hurt", "DeadXX", "TriumphUp", "TriumphLand" };
            float[] times = { 0, .9f, 0, .25f, .5f, .75f, 0, .5f, 0, 0, 0, .75f };
            bool capture = !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("CPS_TASK7_CAPTURE_DIR"));
            foreach (int direction in new[] { 1, -1 })
            {
                mascot.ResetPresentation();
                mascot.SetMovement(direction);
                for (int i = 0; i < states.Length; i++)
                {
                    animator.speed = 1;
                    animator.Play(states[i], 0, times[i]);
                    animator.Update(.001f);
                    animator.speed = 0;
                    Assert.That(body.sprite.name, Is.EqualTo(sprites[i]));
                    Assert.That(body.flipX, Is.EqualTo(direction < 0));
                    Assert.That(hat.flipX, Is.False);
                    Assert.That(hat.transform.lossyScale.x, Is.GreaterThan(0));
                    if (sprites[i] == "DeadXX")
                        Assert.That(hat.bounds.max.x, Is.LessThan(body.bounds.center.x - .35f),
                            "Fallen hat must leave both X eyes clear.");
                    if (capture) yield return CaptureProduction("pose-" + sprites[i] + "-" + direction,
                        new Vector2Int(1280, 720), "CPS_TASK7_CAPTURE_DIR", "task7");
                }
                mascot.ResetPresentation();
                mascot.SetMovement(direction);
                mascot.PlayTerminal(EndReason.HpDepleted);
                float prior = 0;
                foreach (float phase in new[] { .06f, .26f, .56f, .8f, 1.25f })
                {
                    mascot.StepPresentation(phase - prior);
                    prior = phase;
                    animator.speed = 1; animator.Update(.001f); animator.speed = 0;
                    if (capture) yield return CaptureProduction("death-" + (int)(phase * 100) + "-" + direction,
                        new Vector2Int(1280, 720), "CPS_TASK7_CAPTURE_DIR", "task7");
                }
                mascot.ResetPresentation();
                mascot.SetMovement(direction);
                mascot.PlayTerminal(EndReason.Triumph);
                prior = 0;
                foreach (float phase in new[] { .25f, .8f })
                {
                    mascot.StepPresentation(phase - prior);
                    prior = phase;
                    animator.speed = 1; animator.Update(.001f); animator.speed = 0;
                    if (capture) yield return CaptureProduction("triumph-" + (int)(phase * 100) + "-" + direction,
                        new Vector2Int(1280, 720), "CPS_TASK7_CAPTURE_DIR", "task7");
                }
            }
        }

        [UnityTest]
        public IEnumerator ProductionIdleAndRunAnimatorRepeatBeyondClipEnd()
        {
            yield return SceneManager.LoadSceneAsync("Gameplay");
            yield return null;
            var mascot = Object.FindFirstObjectByType<MascotPresentation>();
            mascot.enabled = false;
            var animator = mascot.GetComponent<Animator>();
            var body = mascot.transform.Find("Body").GetComponent<SpriteRenderer>();

            animator.Play("Idle", 0, 0);
            animator.Update(0);
            animator.Update(.91f);
            string firstBlink = body.sprite.name;
            animator.Update(animator.GetCurrentAnimatorClipInfo(0)[0].clip.length);
            string nextBlink = body.sprite.name;

            animator.Play("Run", 0, 0);
            animator.Update(0);
            animator.Update(.21f);
            string firstRun = body.sprite.name;
            animator.Update(animator.GetCurrentAnimatorClipInfo(0)[0].clip.length);
            string nextRun = body.sprite.name;

            Assert.That(firstBlink, Is.EqualTo("IdleBlink"));
            Assert.That(nextBlink, Is.EqualTo("IdleBlink"), "Idle blink must recur after a full cycle");
            Assert.That(firstRun, Is.EqualTo("RunC"));
            Assert.That(nextRun, Is.EqualTo("RunC"), "Run must recur after a full cycle");
        }

        [UnityTest]
        public IEnumerator ProductionMascotResetClearsDeadSpriteAndDissolve()
        {
            yield return SceneManager.LoadSceneAsync("Gameplay");
            yield return null;
            var mascot = Object.FindFirstObjectByType<MascotPresentation>();
            mascot.enabled = false;
            var animator = mascot.GetComponent<Animator>();
            var body = mascot.transform.Find("Body").GetComponent<SpriteRenderer>();
            var dissolve = mascot.GetComponentInChildren<ParticleSystem>(true);
            Assert.That(dissolve, Is.Not.Null);

            mascot.ResetPresentation();
            mascot.PlayTerminal(EndReason.HpDepleted);
            mascot.StepPresentation(.8f);
            animator.Update(.001f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Dead"), Is.True);
            Assert.That(body.sprite.name, Is.EqualTo("DeadXX"));
            Assert.That(dissolve.isPlaying, Is.True);

            mascot.ResetPresentation();
            animator.Update(.001f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), Is.True);
            Assert.That(body.sprite.name, Is.EqualTo("IdleA"));
            Assert.That(dissolve.isPlaying, Is.False);
            Assert.That(dissolve.IsAlive(true), Is.False);
            Assert.That(body.color, Is.EqualTo(Color.white));
            Assert.That(body.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator ProductionVisualFixCaptureAtRequiredSizes()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            for (int i = 0; i < 10; i++) yield return null;
            yield return CaptureProduction("menu", new Vector2Int(1920, 1080),
                "CPS_TASK7_VISUAL_FIX_CAPTURE_DIR", "task7-visual-fix");
            yield return CaptureProduction("menu", new Vector2Int(1280, 720),
                "CPS_TASK7_VISUAL_FIX_CAPTURE_DIR", "task7-visual-fix");

            var menu = Object.FindFirstObjectByType<MainMenuUI>();
            CreateSelectedProfile(menu, DifficultyId.Normal);
            Assert.That(menu.StartSelectedGame(), Is.True);
            yield return WaitForActiveScene("Gameplay");
            yield return null;
            var flow = Object.FindFirstObjectByType<GameSessionController>();
            flow.enabled = false;
            flow.Restart(); flow.Step(3);
            Assert.That(flow.State, Is.EqualTo(SessionState.Playing));
            for (int i = 0; i < 10; i++) yield return null;
            yield return CaptureProduction("gameplay", new Vector2Int(1920, 1080),
                "CPS_TASK7_VISUAL_FIX_CAPTURE_DIR", "task7-visual-fix");
            yield return CaptureProduction("gameplay", new Vector2Int(1280, 720),
                "CPS_TASK7_VISUAL_FIX_CAPTURE_DIR", "task7-visual-fix");

            var spawner = Object.FindFirstObjectByType<FallingObjectSpawner>();
            var type = typeof(FallingObjectSpawner);
            var prefab = (FallingObject)type.GetField("prefab", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(spawner);
            var foods = (FallingObjectDefinition[])type.GetField("foodDefinitions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(spawner);
            var trash = (FallingObjectDefinition[])type.GetField("trashDefinitions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(spawner);
            Assert.That(foods.Length, Is.EqualTo(3));
            Assert.That(trash.Length, Is.EqualTo(2));
            var definitions = new[] { foods[0], foods[1], foods[2], trash[0], trash[1] };
            var positions = new[] { new Vector2(-5.3f, 2.3f), new Vector2(-2.5f, .8f),
                new Vector2(.7f, 1.7f), new Vector2(3.1f, -1.2f), new Vector2(5.1f, .3f) };
            for (int i = 0; i < definitions.Length; i++)
            {
                var item = Object.Instantiate(prefab, spawner.transform);
                item.Spawn(200001 + i, definitions[i], positions[i], 1);
                Assert.That(item.GetComponent<SpriteRenderer>().sprite, Is.SameAs(definitions[i].sprite));
                TestContext.WriteLine("Diagnostic item " + definitions[i].name + " at " + positions[i]);
            }
            for (int i = 0; i < 12; i++)
                flow.Model.Resolve(300001 + i, ObjectKind.Food, ObjectOutcome.Caught, foods[0].points);
            var ui = Object.FindFirstObjectByType<GameplayUI>();
            ui.Refresh();
            var rank = (Text)typeof(GameplayUI).GetField("rank", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            var rankLetter = (Text)typeof(GameplayUI).GetField("rankLetter", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            var callout = (Text)typeof(GameplayUI).GetField("rankCallout", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            Assert.That(rank.text, Is.EqualTo("Supreme Chef!"));
            Assert.That(rankLetter.text, Is.EqualTo("SSS"));
            Assert.That(callout.text, Does.Contain("Supreme Chef!"));
            Assert.That(callout.transform.parent.gameObject.activeInHierarchy, Is.True);
            TestContext.WriteLine("Deterministic diagnostic composition: five production FallingObjects and 12 resolved food catches for SSS; not an organic play sequence.");
            yield return CaptureProduction("diagnostic-active-sss-five-items", new Vector2Int(1920, 1080),
                "CPS_TASK7_VISUAL_FIX_CAPTURE_DIR", "task7-visual-fix");
        }

        [UnityTest]
        public IEnumerator ApprovedMenuDesignRendersPracticeAndRules()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            for (int i = 0; i < 5; i++) yield return null;
            var practice = Object.FindFirstObjectByType<MenuPracticeUI>();
            Assert.That(practice, Is.Not.Null);
            yield return CaptureProduction("menu", new Vector2Int(1920, 1080),
                "CPS_APPROVED_MENU_CAPTURE_DIR", "approved");
            practice.ShowRules();
            yield return null;
            Assert.That(GameObject.Find("Game rules overlay"), Is.Not.Null);
            yield return CaptureProduction("rules", new Vector2Int(1920, 1080),
                "CPS_APPROVED_MENU_CAPTURE_DIR", "approved");
            practice.HideRules();
        }

        [UnityTest]
        public IEnumerator ReferenceGameplayRendersHudAndPauseAtBothSizes()
        {
            var profile = app.Store.Create("arc"); app.Select(profile.PlayerId, DifficultyId.Hard);
            yield return SceneManager.LoadSceneAsync("Gameplay");
            for (int i=0;i<5;i++) yield return null;
            var flow=Object.FindFirstObjectByType<GameSessionController>();flow.enabled=false;
            flow.Restart();flow.Step(3);
            for(int i=0;i<4;i++) flow.Model.Resolve(10000+i,ObjectKind.Food,ObjectOutcome.Caught,10);
            var ui=Object.FindFirstObjectByType<GameplayUI>();ui.Refresh();
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})
            {
                yield return CaptureProduction("gameplay",size,"CPS_GAMEPLAY_CAPTURE_DIR","reference");
                flow.Pause();ui.Refresh();
                yield return CaptureProduction("pause",size,"CPS_GAMEPLAY_CAPTURE_DIR","reference");
                flow.Resume();flow.Step(3);ui.Refresh();
            }
            var longName=app.Store.Create("WWWWWWWWWWWWWWWWWWWWWWWW");app.Select(longName.PlayerId,DifficultyId.Hard);
            for(int i=4;i<12;i++) flow.Model.Resolve(10000+i,ObjectKind.Food,ObjectOutcome.Caught,20000);
            ui.Refresh();
            yield return CaptureProduction("sss",new Vector2Int(1280,720),"CPS_GAMEPLAY_CAPTURE_DIR","reference");
            Assert.That(flow.Model.Score.CurrentRank,Is.EqualTo(ComboRank.SSS));
            Assert.That(flow.Model.Score.Score,Is.EqualTo(160040));
        }

        [UnityTest]
        public IEnumerator ReferenceResultsSnapshotAndDrawerRenderAtBothSizes()
        {
            var profile=app.Store.Create("arc");app.Select(profile.PlayerId,DifficultyId.Normal);
            yield return SceneManager.LoadSceneAsync("Gameplay");
            for(int i=0;i<5;i++)yield return null;
            var flow=Object.FindFirstObjectByType<GameSessionController>();flow.enabled=false;
            flow.Restart();flow.Step(3);
            flow.Model.Resolve(99001,ObjectKind.Food,ObjectOutcome.Caught,10,0);
            flow.Model.Resolve(99002,ObjectKind.Food,ObjectOutcome.Caught,15,1);
            flow.Model.Resolve(99003,ObjectKind.Food,ObjectOutcome.Caught,20,2);
            flow.Model.CompleteStep(60);flow.Step(.01f);flow.Step(2);
            Assert.That(flow.LastResult.CaughtItems,Is.EqualTo(new[]{1,1,1,0,0}));
            var panel=Object.FindFirstObjectByType<ResultPanel>();
            var view=panel.GetComponent<ResultReferenceView>();
            Assert.That(panel.transform.parent.Find("Player").gameObject.activeSelf,Is.False);
            var result=flow.LastResult;result.Score=215;result.FoodCaught=16;result.CaughtItems=new[]{8,5,3,0,0};result.MaxCombo=7;result.HighestRank=ComboRank.S;
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})
            {
                panel.Show(result);
                yield return CaptureProduction("closed",size,"CPS_RESULT_CAPTURE_DIR","result");
                view.toggle.onClick.Invoke();Assert.That(view.details.gameObject.activeSelf,Is.True);
                view.close.Select();view.close.onClick.Invoke();
                Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(view.toggle.gameObject));
                view.toggle.onClick.Invoke();
                yield return CaptureProduction("details",size,"CPS_RESULT_CAPTURE_DIR","result");
            }
            result.Difficulty=DifficultyId.Easy;result.EndReason=EndReason.HpDepleted;result.RemainingHp=0;
            panel.Show(result);Assert.That(view.summary.Find("Item 3").gameObject.activeSelf,Is.False);
            yield return CaptureProduction("easy-death",new Vector2Int(1280,720),"CPS_RESULT_CAPTURE_DIR","result");
            panel.Hide();
            Assert.That(panel.transform.parent.Find("Player").gameObject.activeSelf,Is.True);
        }

        [UnityTest]
        public IEnumerator DifficultySelectsEnvironmentBeforeCountdownAndRestartRestoresKitchen()
        {
            var profile=app.Store.Create("arc");app.Select(profile.PlayerId,DifficultyId.Easy);
            yield return SceneManager.LoadSceneAsync("Gameplay");
            for(int i=0;i<5;i++)yield return null;
            var flow=Object.FindFirstObjectByType<GameSessionController>();flow.enabled=false;
            var background=GameObject.Find("KitchenBackground").GetComponent<SpriteRenderer>();
            var environment=Object.FindFirstObjectByType<LevelEnvironment>();
            Assert.That(environment,Is.Not.Null);
            var player=Object.FindFirstObjectByType<PlayerController>();var origin=player.transform.position;
            string[] names={"kitchen-background-v1","cafe-day-background-v1","cafe-night-background-v1"};
            foreach(var difficulty in new[]{DifficultyId.Easy,DifficultyId.Normal,DifficultyId.Hard})
            {
                app.Select(profile.PlayerId,difficulty);Assert.That(flow.BeginRound(),Is.True);
                Assert.That(background.sprite.texture.name,Is.EqualTo(names[(int)difficulty]));
                Assert.That(flow.CountdownSeconds,Is.EqualTo(3));
                Assert.That(player.transform.position,Is.EqualTo(origin));
                flow.Step(3);flow.Step(.5f);
                foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})
                    yield return CaptureProduction(difficulty.ToString().ToLowerInvariant(),size,"CPS_CAFE_CAPTURE_DIR","cafe");
            }
            app.Select(profile.PlayerId,DifficultyId.Easy);
            yield return null;
            Assert.That(background.sprite.texture.name,Is.EqualTo("cafe-night-background-v1"),"Selection must not change a running round");
            flow.Restart();
            Assert.That(background.sprite.texture.name,Is.EqualTo("kitchen-background-v1"));
            Assert.That(GameObject.Find("KitchenAmbient"),Is.Not.Null);
        }

        private static IEnumerator CaptureProduction(string label, Vector2Int size,
            string directoryVariable = "CPS_TASK6_CAPTURE_DIR", string prefix = "task6")
        {
            var camera = Camera.main;
            var viewport = camera.GetComponent<GameplayViewport>();
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var target = new RenderTexture(size.x, size.y, 24);
            var priorTarget = camera.targetTexture;
            var priorActive = RenderTexture.active;
            bool priorEnabled = viewport.enabled;
            viewport.enabled = false;
            try
            {
                camera.targetTexture = target;
                viewport.Apply(size.x, size.y);
                yield return null;
                yield return null;
                Canvas.ForceUpdateCanvases();
                Assert.That(canvas.pixelRect, Is.EqualTo(camera.pixelRect));
                Assert.That(canvas.scaleFactor, Is.EqualTo(Mathf.Min(size.x / 1920f, size.y / 1080f)).Within(.01f));
                TestContext.WriteLine(label + " " + size.x + "x" + size.y + " scale=" + canvas.scaleFactor);
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                var image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                try
                {
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); image.Apply();
                    var bytes = image.EncodeToPNG();
                    Assert.That(bytes.Length, Is.GreaterThan(1000));
                    string dir = System.Environment.GetEnvironmentVariable(directoryVariable);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        Directory.CreateDirectory(dir);
                        File.WriteAllBytes(Path.Combine(dir, prefix + "-" + label + "-" + size.x + "x" + size.y + ".png"), bytes);
                    }
                }
                finally { Object.DestroyImmediate(image); }
            }
            finally
            {
                RenderTexture.active = priorActive;
                camera.targetTexture = priorTarget;
                viewport.enabled = priorEnabled;
                Object.DestroyImmediate(target);
            }
        }

        private static void CaptureRender(string path)
        {
            var mascot = Object.FindFirstObjectByType<MascotPresentation>();
            var body = mascot.transform.Find("Body").GetComponent<SpriteRenderer>();
            var hat = mascot.transform.Find("Hat").GetComponent<SpriteRenderer>();
            Assert.That(body.sprite.texture.name, Is.EqualTo("mascot-body-atlas-v1"));
            Assert.That(hat.sprite.texture.name, Is.EqualTo("engineer-hat-v1"));
            var target = RenderTexture.GetTemporary(960, 540, 24);
            var previous = RenderTexture.active;
            var image = new Texture2D(960, 540, TextureFormat.RGB24, false);
            try
            {
                Camera.main.GetComponent<GameplayViewport>().Apply(960, 540);
                RenderPipeline.SubmitRenderRequest(Camera.main, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                Assert.That(image.GetPixel(480, 145).g, Is.GreaterThan(.7f), "Production mascot body must render cream at its lower center.");
            }
            finally
            {
                RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Object.DestroyImmediate(image);
            }
        }

        private static IEnumerator WaitForActiveScene(string sceneName)
        {
            float deadline = Time.realtimeSinceStartup + 5;
            while (SceneManager.GetActiveScene().name != sceneName && Time.realtimeSinceStartup < deadline)
                yield return null;
        }

        private static void CreateSelectedProfile(MainMenuUI menu, DifficultyId difficulty)
        {
            if (AppSession.Instance.SelectedProfile == null)
            {
                var field = (InputField)typeof(MainMenuUI).GetField("nicknameField", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
                field.text = "Demo Player";
                Assert.That(menu.CreateProfile(), Is.True);
            }
            menu.SelectDifficulty(difficulty);
        }
    }
}
