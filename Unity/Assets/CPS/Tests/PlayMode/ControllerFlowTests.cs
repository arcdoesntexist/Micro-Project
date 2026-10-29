using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CPS.KitchenCatch.Tests
{
    public sealed class ControllerFlowTests : InputTestFixture
    {
        private readonly List<Object> owned = new List<Object>();
        private GameSessionController controller;
        private ControllerInput input;
        private SerialConnection serial;
        private PlayerController player;
        private int generation;
        private bool hadPreference;
        private int preference;
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private T Own<T>(T item) where T : Object { owned.Add(item); return item; }
        [SetUp] public override void Setup()
        {
            base.Setup();
            hadPreference = PlayerPrefs.HasKey(ControllerInput.PreferenceKey); preference = PlayerPrefs.GetInt(ControllerInput.PreferenceKey);
            PlayerPrefs.DeleteKey(ControllerInput.PreferenceKey);
            var values = new Dictionary<string, string>();
            var appRoot = Own(new GameObject("USB app")); appRoot.SetActive(false);
            var app = appRoot.AddComponent<AppSession>();
            app.Initialize(new ProfileStore(key => values.TryGetValue(key, out var value) ? value : null, (key, value) => values[key] = value, () => { }));
            appRoot.SetActive(true); var profile = app.Store.Create("USB test"); app.Select(profile.PlayerId, DifficultyId.Easy);
            var inputRoot = Own(new GameObject("USB flow inputs")); serial = inputRoot.AddComponent<SerialConnection>(); input = inputRoot.AddComponent<ControllerInput>();
            input.SelectMode(ControllerMode.FSR);
            var settings = Own(ScriptableObject.CreateInstance<ControllerSettings>()); Set(input, "settings", settings);
            player = Own(new GameObject("USB player")).AddComponent<PlayerController>();
            var prefab = Own(new GameObject("USB food prefab")).AddComponent<FallingObject>(); prefab.gameObject.SetActive(false);
            var config = Own(ScriptableObject.CreateInstance<DifficultyConfig>());
            config.difficulty = DifficultyId.Easy; config.speed = .1f; config.interval = 1; config.lanes = new[] { 0f };
            var food = Own(ScriptableObject.CreateInstance<FallingObjectDefinition>()); food.kind = ObjectKind.Food; food.points = 10;
            var trash = Own(ScriptableObject.CreateInstance<FallingObjectDefinition>()); trash.kind = ObjectKind.Trash;
            var spawner = Own(new GameObject("USB spawner")).AddComponent<FallingObjectSpawner>();
            Set(spawner, "player", player); Set(spawner, "prefab", prefab); Set(spawner, "foodDefinitions", new[] { food, food, food }); Set(spawner, "trashDefinitions", new[] { trash, trash });
            controller = Own(new GameObject("USB flow")).AddComponent<GameSessionController>(); controller.enabled = false;
            controller.Initialize(player, input, spawner, new[] { config }, app);
            Connect();
        }
        private void Connect()
        {
            generation = serial.BeginSession(); serial.TransportOpened(generation);
            string frame = File.ReadAllText(Path.Combine(Application.dataPath, "CPS/Tests/Fixtures/Usb/valid-v1.json"));
            serial.Receive(frame + "\n", generation, SerialConnection.NowSeconds);
        }
        [TearDown] public void Cleanup()
        {
            for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
            if (hadPreference) PlayerPrefs.SetInt(ControllerInput.PreferenceKey, preference); else PlayerPrefs.DeleteKey(ControllerInput.PreferenceKey);
        }
        [Test] public void MissingInputFreezesReadyCountdownAndResumeCannotBypassRecovery()
        {
            Assert.That(controller.BeginRound(), Is.True);
            serial.OnWebSerialStatus(generation + ":error:unplugged");
            controller.Step(1);
            Assert.That(controller.IsPaused, Is.True);
            Assert.That(controller.CountdownSeconds, Is.EqualTo(3));
            controller.Resume(); Assert.That(controller.IsPaused, Is.True);
            Connect(); Assert.That(controller.IsPaused, Is.True);
            controller.Resume(); Assert.That(controller.IsPaused, Is.False);
        }
        [Test] public void PlayingLossStopsMovementBeforeAdvancingHpTimerOrStatistics()
        {
            controller.BeginRound(); controller.Step(3); controller.Step(.1f);
            float elapsed = controller.Model.PlayedSeconds;
            serial.OnWebSerialStatus(generation + ":error:unplugged"); controller.Step(1);
            Assert.That(controller.IsPaused, Is.True); Assert.That(player.MovementDirection, Is.Zero);
            Assert.That(controller.Model.PlayedSeconds, Is.EqualTo(elapsed)); Assert.That(controller.Model.Health.CurrentHp, Is.EqualTo(3));
        }
        [Test] public void FreshNewBootStillPausesUntilExplicitResume()
        {
            controller.BeginRound(); controller.Step(3); controller.Step(.1f);
            float elapsed = controller.Model.PlayedSeconds;
            string frame = File.ReadAllText(Path.Combine(Application.dataPath, "CPS/Tests/Fixtures/Usb/valid-v1.json"));
            serial.Receive(frame.Replace("\"bootId\":17", "\"bootId\":18") + "\n", generation, SerialConnection.NowSeconds);
            Assert.That(input.CanPlay(out _), Is.True);
            controller.Step(.1f);
            Assert.That(controller.IsPaused, Is.True);
            Assert.That(controller.Model.PlayedSeconds, Is.EqualTo(elapsed));
            controller.Resume(); Assert.That(controller.IsPaused, Is.False);
            controller.Step(.1f); Assert.That(controller.IsPaused, Is.False);
        }
        [Test] public void SpacebarPausesAndResumesButDoesNotOperateThroughAModal()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var hotkeys = controller.gameObject.AddComponent<PauseHotkeys>();
            var blocker = Own(new GameObject("Connection modal")); blocker.SetActive(false);
            hotkeys.Bind(controller, new[] { blocker });
            controller.BeginRound(); controller.Step(3);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space)); InputSystem.Update(); hotkeys.SendMessage("Update");
            Assert.That(controller.IsPaused, Is.True);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            blocker.SetActive(true);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space)); InputSystem.Update(); hotkeys.SendMessage("Update");
            Assert.That(controller.IsPaused, Is.True);
            blocker.SetActive(false); InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space)); InputSystem.Update(); hotkeys.SendMessage("Update");
            Assert.That(controller.IsPaused, Is.False); Assert.That(controller.CountdownSeconds, Is.EqualTo(3));
        }
        [Test] public void BRemainsARightAliasWithoutInventingForce()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var adapter = player.gameObject.AddComponent<KeyboardInput>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.B)); InputSystem.Update();
            Assert.That(adapter.Read().Right.Pressed, Is.True); Assert.That(adapter.Read().Right.ForceN, Is.Null);
        }
        [Test] public void SpacebarDoesNotSubmitAFocusedButtonOrToggleWhileTyping()
        {
            var keyboard=InputSystem.AddDevice<Keyboard>();
            var events=Own(new GameObject("USB UI events",typeof(EventSystem),typeof(InputSystemUIInputModule)));
            var module=events.GetComponent<InputSystemUIInputModule>();module.AssignDefaultActions();module.submit.action.AddBinding("<Keyboard>/space");
            var buttonRoot=Own(new GameObject("Focused button",typeof(RectTransform),typeof(Button)));
            var button=buttonRoot.GetComponent<Button>();int clicks=0;button.onClick.AddListener(()=>clicks++);
            events.GetComponent<EventSystem>().SetSelectedGameObject(buttonRoot);
            var hotkeys=controller.gameObject.AddComponent<PauseHotkeys>();hotkeys.Bind(controller,new GameObject[0]);
            controller.BeginRound();controller.Step(3);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));InputSystem.Update();hotkeys.SendMessage("Update");module.Process();
            Assert.That(controller.IsPaused,Is.True);Assert.That(clicks,Is.Zero);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));InputSystem.Update();module.Process();
            Assert.That(clicks,Is.EqualTo(1));
            var fieldRoot=Own(new GameObject("Nickname",typeof(RectTransform),typeof(InputField)));
            events.GetComponent<EventSystem>().SetSelectedGameObject(fieldRoot);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));InputSystem.Update();hotkeys.SendMessage("Update");
            Assert.That(controller.IsPaused,Is.True);
        }
    }
}
