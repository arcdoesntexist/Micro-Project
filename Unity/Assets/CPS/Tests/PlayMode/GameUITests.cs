using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Tests
{
    public sealed class GameUITests
    {
        private AppSession app;
        private readonly List<GameObject> objects = new List<GameObject>();
        private bool hadVolume;
        private bool hadBackgroundMusic;
        private float savedVolume;
        private int savedBackgroundMusic;

        [SetUp]
        public void Setup()
        {
            hadVolume = PlayerPrefs.HasKey(GlobalAudioSettings.VolumeKey);
            hadBackgroundMusic = PlayerPrefs.HasKey(GlobalAudioSettings.BackgroundMusicKey);
            savedVolume = PlayerPrefs.GetFloat(GlobalAudioSettings.VolumeKey);
            savedBackgroundMusic = PlayerPrefs.GetInt(GlobalAudioSettings.BackgroundMusicKey);
            PlayerPrefs.DeleteKey(GlobalAudioSettings.VolumeKey);
            PlayerPrefs.DeleteKey(GlobalAudioSettings.BackgroundMusicKey);
            var values = new Dictionary<string, string>();
            var root = new GameObject("UI test AppSession"); root.SetActive(false); objects.Add(root);
            app = root.AddComponent<AppSession>();
            app.Initialize(new ProfileStore(key => values.TryGetValue(key, out var value) ? value : null,
                (key, value) => values[key] = value, () => { }));
            root.SetActive(true);
        }

        [TearDown]
        public void Cleanup()
        {
            if (hadVolume) PlayerPrefs.SetFloat(GlobalAudioSettings.VolumeKey, savedVolume);
            else PlayerPrefs.DeleteKey(GlobalAudioSettings.VolumeKey);
            if (hadBackgroundMusic) PlayerPrefs.SetInt(GlobalAudioSettings.BackgroundMusicKey, savedBackgroundMusic);
            else PlayerPrefs.DeleteKey(GlobalAudioSettings.BackgroundMusicKey);
            PlayerPrefs.Save();
            foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
            objects.Clear();
        }

        [Test]
        public void SettingsPanelEditsGlobalVolumeAndBackgroundMusic()
        {
            var root = new GameObject("Settings"); objects.Add(root); root.SetActive(false);
            var panel = root.AddComponent<AudioSettingsPanel>();
            var slider = New<Slider>("Volume");
            var toggle = New<Toggle>("Background music");
            var value = New<Text>("Volume value");
            var close = New<Button>("Close");
            Set(panel, "volumeSlider", slider);
            Set(panel, "backgroundMusicToggle", toggle);
            Set(panel, "volumeValue", value);
            Set(panel, "closeButton", close);
            var events = New<UnityEngine.EventSystems.EventSystem>("Events");
            var opener = New<Button>("Settings opener");
            events.SetSelectedGameObject(opener.gameObject);

            panel.Show();
            Assert.That(slider.value, Is.EqualTo(1f));
            Assert.That(toggle.isOn, Is.True);
            Assert.That(events.currentSelectedGameObject, Is.SameAs(slider.gameObject));
            slider.value = .4f;
            toggle.isOn = false;
            Assert.That(GlobalAudioSettings.MasterVolume, Is.EqualTo(.4f).Within(.001f));
            Assert.That(GlobalAudioSettings.BackgroundMusicEnabled, Is.False);
            Assert.That(value.text, Is.EqualTo("40"));
            close.onClick.Invoke();
            Assert.That(root.activeSelf, Is.False);
            Assert.That(events.currentSelectedGameObject, Is.SameAs(opener.gameObject));
        }

        [Test]
        public void CreationFieldAcceptsTwentyFourCombiningTextElementsAndShowsLiteralName()
        {
            var menu = New<MainMenuUI>("Menu");
            var field = New<InputField>("Name input");
            var dropdown = New<Dropdown>("Profiles");
            var caption = New<Text>("Profile caption");
            dropdown.captionText = caption;
            Set(menu, "nicknameField", field);
            Set(menu, "profileDropdown", dropdown);
            field.text = string.Concat(System.Linq.Enumerable.Repeat("A\u0301", 24));
            Assert.That(menu.CreateProfile(), Is.True);
            Assert.That(app.SelectedProfile.DisplayName, Is.EqualTo(field.text));
            Assert.That(field.characterLimit, Is.Zero);
            field.text = "<b>มิ้น</b>";
            Assert.That(menu.CreateProfile(), Is.True);
            Assert.That(app.SelectedProfile.DisplayName, Is.EqualTo("<b>มิ้น</b>"));
            Assert.That(caption.supportRichText, Is.False);
        }

        [Test]
        public void DuplicateProfileNamesRemainSeparateDropdownChoicesById()
        {
            var first = app.Store.Create("Mint");
            var second = app.Store.Create("Mint");
            var menu = New<MainMenuUI>("Menu");
            var dropdown = New<Dropdown>("Profiles");
            Set(menu, "profileDropdown", dropdown);
            menu.RefreshProfiles();
            Assert.That(dropdown.options.Count, Is.EqualTo(2));
            Assert.That(dropdown.options[0].text, Does.Contain(first.PlayerId.Substring(0, 6)));
            Assert.That(dropdown.options[1].text, Does.Contain(second.PlayerId.Substring(0, 6)));
            Assert.That(dropdown.options[0].text, Is.Not.EqualTo(dropdown.options[1].text));
        }

        [Test]
        public void SelectingExistingProfileOrDifficultyClearsRejectedNicknameWarning()
        {
            var first = app.Store.Create("Mint");
            var second = app.Store.Create("Arc");
            app.Select(first.PlayerId, DifficultyId.Easy);
            var menu = New<MainMenuUI>("Menu");
            menu.enabled = false;
            var field = New<InputField>("Name input");
            var dropdown = New<Dropdown>("Profiles");
            var status = New<Text>("Status");
            Set(menu, "nicknameField", field);
            Set(menu, "profileDropdown", dropdown);
            Set(menu, "status", status);
            menu.enabled = true;

            Assert.That(menu.CreateProfile(), Is.False);
            Assert.That(status.text, Does.Contain("Nickname must contain"));
            menu.SelectDifficulty(DifficultyId.Normal);
            Assert.That(status.text, Does.Not.Contain("Nickname must contain"));

            Assert.That(menu.CreateProfile(), Is.False);
            dropdown.value = 1;
            Assert.That(app.SelectedProfile.PlayerId, Is.EqualTo(second.PlayerId));
            Assert.That(status.text, Does.Not.Contain("Nickname must contain"));
        }

        [Test]
        public void ChosenDifficultyRemainsVisibleWhenAnotherControlHasFocus()
        {
            var menu = New<MainMenuUI>("Menu");
            var easy = New<Button>("Easy");
            var normal = New<Button>("Normal");
            var hard = New<Button>("Hard");
            Set(menu, "easyButton", easy);
            Set(menu, "normalButton", normal);
            Set(menu, "hardButton", hard);

            menu.SelectDifficulty(DifficultyId.Hard);
            var events = New<UnityEngine.EventSystems.EventSystem>("Events");
            events.SetSelectedGameObject(easy.gameObject);

            Assert.That(hard.colors.normalColor, Is.Not.EqualTo(easy.colors.normalColor));
            Assert.That(hard.colors.normalColor, Is.Not.EqualTo(normal.colors.normalColor));

            var profile = app.Store.Create("Mint");
            app.Select(profile.PlayerId, DifficultyId.Hard);
            menu.RefreshProfiles();
            Assert.That(hard.colors.normalColor, Is.Not.EqualTo(easy.colors.normalColor));
        }

        [Test]
        public void ProfileManagerRenamesSelectedProfileWithoutChangingIdentityOrCount()
        {
            var profile = app.Store.Create("Arc");
            app.Store.RecordResult(profile.PlayerId, "round-1");
            app.Select(profile.PlayerId, DifficultyId.Normal);
            var menu = NewProfileManager(out var howTo, out var manager, out var rename,
                out _, out _, out _);

            menu.ShowProfileManager();
            menu.SelectManagedProfile(profile.PlayerId);
            rename.text = "  Mint  ";

            Assert.That(menu.RenameManagedProfile(), Is.True);
            Assert.That(howTo.activeSelf, Is.False);
            Assert.That(manager.activeSelf, Is.True);
            Assert.That(profile.PlayerId, Is.EqualTo(app.SelectedProfile.PlayerId));
            Assert.That(profile.DisplayName, Is.EqualTo("Mint"));
            Assert.That(profile.PlayCount, Is.EqualTo(1));
        }

        [Test]
        public void ProfileDeletionRequiresConfirmationAndSelectsNextProfile()
        {
            var first = app.Store.Create("Arc");
            var second = app.Store.Create("Mint");
            app.Select(first.PlayerId, DifficultyId.Hard);
            var menu = NewProfileManager(out _, out _, out _, out var confirmation,
                out var confirmationText, out _);
            menu.ShowProfileManager();
            menu.SelectManagedProfile(first.PlayerId);

            menu.RequestDeleteManagedProfile();

            Assert.That(confirmation.activeSelf, Is.True);
            Assert.That(confirmationText.text, Does.Contain("Arc"));
            Assert.That(app.Store.Profiles, Has.Count.EqualTo(2));
            Assert.That(menu.ConfirmDeleteManagedProfile(), Is.True);
            Assert.That(app.Store.Profiles, Has.Count.EqualTo(1));
            Assert.That(app.SelectedProfile.PlayerId, Is.EqualTo(second.PlayerId));
            Assert.That(app.SelectedDifficulty, Is.EqualTo(DifficultyId.Hard));
        }

        [Test]
        public void DeletingLastProfileClearsSelectionAndDisablesStart()
        {
            var only = app.Store.Create("Arc");
            app.Select(only.PlayerId, DifficultyId.Easy);
            var menu = NewProfileManager(out _, out _, out _, out _, out _, out var start);
            menu.ShowProfileManager();
            menu.SelectManagedProfile(only.PlayerId);
            menu.RequestDeleteManagedProfile();

            Assert.That(menu.ConfirmDeleteManagedProfile(), Is.True);

            Assert.That(app.Store.Profiles, Is.Empty);
            Assert.That(app.SelectedProfile, Is.Null);
            Assert.That(start.interactable, Is.False);
        }

        [Test]
        public void ProfileManagerMovesKeyboardFocusAndCancelKeepsProfile()
        {
            var profile = app.Store.Create("Arc");
            app.Select(profile.PlayerId, DifficultyId.Easy);
            var menu = NewProfileManager(out _, out _, out var rename, out var confirmation,
                out _, out _);
            var events = New<UnityEngine.EventSystems.EventSystem>("Events");
            var manage = New<Button>("Manage profiles");
            var delete = New<Button>("Delete profile");
            var confirm = New<Button>("Confirm delete");
            Set(menu, "manageProfilesButton", manage);
            Set(menu, "deleteProfileButton", delete);
            Set(menu, "confirmDeleteButton", confirm);

            menu.ShowProfileManager();
            Assert.That(events.currentSelectedGameObject, Is.SameAs(rename.gameObject));
            menu.RequestDeleteManagedProfile();
            Assert.That(events.currentSelectedGameObject, Is.SameAs(confirm.gameObject));
            menu.CancelDeleteManagedProfile();
            Assert.That(confirmation.activeSelf, Is.False);
            Assert.That(events.currentSelectedGameObject, Is.SameAs(delete.gameObject));
            Assert.That(app.Store.Profiles, Has.Count.EqualTo(1));
            menu.HideProfileManager();
            Assert.That(events.currentSelectedGameObject, Is.SameAs(manage.gameObject));
        }

        [Test]
        public void ResultsShowCompleteRoundAndUnavailableKeyboardForce()
        {
            var panel = New<ResultPanel>("Results");
            var labels = new[] { "identity", "outcome", "score", "roundStats", "leftStats", "rightStats", "balance", "forceStatus" };
            foreach (var label in labels) Set(panel, label, New<Text>(label));
            var result = new SessionResult
            {
                DisplayName = "<b>มิ้น</b>", EndReason = EndReason.HpDepleted, Score = 145,
                Difficulty = DifficultyId.Hard, PlayedSeconds = 27.5f, RemainingHp = 0,
                FoodCaught = 9, FoodMissed = 2, TrashEaten = 1, MaxCombo = 6,
                HighestRank = ComboRank.S, InputSource = "Keyboard",
                InputStatistics = new InputStatistics
                {
                    Left = new HandStatistics { HasStrength = true, MaxStrength01 = 1, MeanStrength01 = .25f, PressedDurationSeconds = 7 },
                    Right = new HandStatistics { HasStrength = true, MaxStrength01 = 1, MeanStrength01 = .5f, PressedDurationSeconds = 12 },
                    BothPressedDurationSeconds = 3, HasBalance = true, LeftBalance01 = 7f / 19f
                }
            };
            panel.Show(result);
            Assert.That(Get<Text>(panel, "identity").text, Does.Contain("<b>มิ้น</b>"));
            Assert.That(Get<Text>(panel, "identity").supportRichText, Is.False);
            Assert.That(Get<Text>(panel, "outcome").text, Does.Contain("HP depleted"));
            Assert.That(Get<Text>(panel, "score").text, Does.Contain("145"));
            Assert.That(Get<Text>(panel, "roundStats").text, Does.Contain("Hard").And.Contain("27.5").And.Contain("Food Caught 9").And.Contain("Food Missed 2").And.Contain("Trash Eaten 1").And.Contain("Max Combo 6").And.Contain("Highest Rank S"));
            Assert.That(Get<Text>(panel, "leftStats").text, Does.Contain("25%").And.Contain("100%").And.Contain("7.0"));
            Assert.That(Get<Text>(panel, "rightStats").text, Does.Contain("50%").And.Contain("100%").And.Contain("12.0"));
            Assert.That(Get<Text>(panel, "balance").text, Does.Contain("Both 3.0").And.Contain("37%"));
            Assert.That(Get<Text>(panel, "forceStatus").text, Does.Contain("Sensor not connected"));
        }

        [Test]
        public void ResultsDistinguishMissingSamplesFromValidZeroStrength()
        {
            var panel = New<ResultPanel>("Results");
            foreach (var label in new[] { "leftStats", "rightStats", "balance", "forceStatus" })
                Set(panel, label, New<Text>(label));
            var collector = new ForceStatsCollector();
            collector.Accumulate(new InputSnapshot(default, default, "Keyboard"), 1);
            var result = new SessionResult { InputSource = "Keyboard", InputStatistics = collector.Snapshot() };
            Assert.That(result.InputStatistics.Left.ValidDurationSeconds, Is.Zero);
            Assert.That(result.InputStatistics.Right.ValidDurationSeconds, Is.Zero);

            panel.Show(result);
            Assert.That(Get<Text>(panel, "leftStats").text, Is.EqualTo("LEFT\nMean — · Max —\nPressed 0.0 s"));
            Assert.That(Get<Text>(panel, "rightStats").text, Is.EqualTo("RIGHT\nMean — · Max —\nPressed 0.0 s"));
            Assert.That(Get<Text>(panel, "balance").text, Is.EqualTo("Both 0.0 s · Left–Right Balance —"));
            Assert.That(Get<Text>(panel, "forceStatus").text, Does.Contain("Force (N): — / Sensor not connected"));

            var released = new HandInput(false, 0, true, null);
            collector.Accumulate(new InputSnapshot(released, released, "Keyboard"), 1);
            result.InputStatistics = collector.Snapshot();
            Assert.That(result.InputStatistics.Left.ValidDurationSeconds, Is.EqualTo(1));
            Assert.That(result.InputStatistics.Right.ValidDurationSeconds, Is.EqualTo(1));

            panel.Show(result);
            Assert.That(Get<Text>(panel, "leftStats").text, Is.EqualTo("LEFT\nMean 0% · Max 0%\nPressed 0.0 s"));
            Assert.That(Get<Text>(panel, "rightStats").text, Is.EqualTo("RIGHT\nMean 0% · Max 0%\nPressed 0.0 s"));
            Assert.That(Get<Text>(panel, "balance").text, Is.EqualTo("Both 0.0 s · Left–Right Balance —"));
            Assert.That(Get<Text>(panel, "forceStatus").text, Does.Contain("Force (N): — / Sensor not connected"));
        }

        [Test]
        public void BoundHudShowsRoundValuesAndSimulatedHandInputsWithoutNewtons()
        {
            var controller = New<GameSessionController>("Round");
            controller.Model.Start();
            controller.Model.Resolve(1, ObjectKind.Food, ObjectOutcome.Caught, 10);
            controller.Model.Resolve(2, ObjectKind.Food, ObjectOutcome.Caught, 15);
            var ui = New<GameplayUI>("HUD");
            foreach (var field in new[] { "profile", "hp", "time", "score", "difficulty", "rank", "leftHand", "rightHand", "inputSource", "status", "rankCallout" })
            {
                var panel = new GameObject(field + " panel"); objects.Add(panel);
                panel.transform.SetParent(ui.transform, false);
                var label = New<Text>(field); label.transform.SetParent(panel.transform, false);
                Set(ui, field, label);
            }
            ui.Bind(controller);
            ui.Refresh();
            Assert.That(Get<Text>(ui, "hp").text, Does.Contain("3"));
            Assert.That(Get<Text>(ui, "time").text, Does.Contain("60"));
            Assert.That(Get<Text>(ui, "score").text, Does.Contain("25"));
            Assert.That(Get<Text>(ui, "rank").text, Does.Contain("C"));
            Assert.That(Get<Text>(ui, "rankCallout").text, Does.Contain("OK").And.Contain("Cookin'"));
            Assert.That(Get<Text>(ui, "inputSource").text, Does.Contain("Simulated Input"));
            Assert.That(Get<Text>(ui, "leftHand").text, Does.Contain("0%").And.Not.Contain("N"));
        }

        [Test]
        public void RankPresentationEscalatesLetterNameColorAndAccentWithoutChangingScore()
        {
            var controller = New<GameSessionController>("Round");
            controller.Model.Start();
            var ui = New<GameplayUI>("HUD");
            var rankName = New<Text>("Rank name");
            var rankLetter = New<Text>("Rank letter");
            var plate = New<Image>("Rank plate");
            var calloutPlate = New<Image>("Rank callout plate");
            var accents = new[] { New<Image>("Accent 1"), New<Image>("Accent 2"), New<Image>("Accent 3") };
            var calloutPanel = new GameObject("Rank callout"); objects.Add(calloutPanel);
            var callout = New<Text>("Rank callout text"); callout.transform.SetParent(calloutPanel.transform, false);
            Set(ui, "rank", rankName);
            Set(ui, "rankLetter", rankLetter);
            Set(ui, "rankPlate", plate);
            Set(ui, "rankCalloutPlate", calloutPlate);
            Set(ui, "rankAccents", accents);
            Set(ui, "rankCallout", callout);
            ui.Bind(controller);

            int[] streaks = { 0, 2, 3, 4, 5, 8, 12 };
            string[] letters = { "D", "C", "B", "A", "S", "SS", "SSS" };
            string[] names = { "Dull Dish", "Cookin'", "Bold Bite", "Appetizing!", "Sizzling!", "Super Savory!", "Supreme Chef!" };
            int[] accentCounts = { 0, 1, 1, 2, 2, 3, 3 };
            int[] letterSizes = { 44, 44, 44, 44, 44, 40, 32 };
            var colors = new HashSet<string>();
            int catchCount = 0;
            for (int i = 0; i < streaks.Length; i++)
            {
                while (catchCount < streaks[i])
                {
                    catchCount++;
                    controller.Model.Resolve(catchCount, ObjectKind.Food, ObjectOutcome.Caught, 10);
                }
                ui.Refresh();
                Assert.That(rankLetter.text, Is.EqualTo(letters[i]));
                Assert.That(rankLetter.fontSize, Is.EqualTo(letterSizes[i]));
                Assert.That(rankName.text, Is.EqualTo(names[i]));
                Assert.That(accents.Count(accent => accent.gameObject.activeSelf), Is.EqualTo(accentCounts[i]));
                colors.Add(ColorUtility.ToHtmlStringRGBA(plate.color));
            }

            Assert.That(colors.Count, Is.EqualTo(7));
            Assert.That(controller.Model.Score.Score, Is.EqualTo(120));
        }

        private MainMenuUI NewProfileManager(out GameObject howTo, out GameObject manager,
            out InputField rename, out GameObject confirmation, out Text confirmationText,
            out Button start)
        {
            var menu = New<MainMenuUI>("Menu");
            howTo = new GameObject("How to play"); objects.Add(howTo);
            manager = new GameObject("Profile manager"); objects.Add(manager);
            rename = New<InputField>("Rename");
            confirmation = new GameObject("Delete confirmation"); objects.Add(confirmation);
            confirmationText = New<Text>("Delete confirmation text");
            start = New<Button>("Start");
            Set(menu, "howToPlayPanel", howTo);
            Set(menu, "profileManagerPanel", manager);
            Set(menu, "renameField", rename);
            Set(menu, "deleteConfirmation", confirmation);
            Set(menu, "deleteConfirmationText", confirmationText);
            Set(menu, "startButton", start);
            Set(menu, "profileDropdown", New<Dropdown>("Profiles"));
            Set(menu, "profileManagerStatus", New<Text>("Profile manager status"));
            confirmation.SetActive(false);
            manager.SetActive(false);
            return menu;
        }

        private T New<T>(string name) where T : Component
        {
            var go = new GameObject(name); objects.Add(go);
            return go.AddComponent<T>();
        }
        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static T Get<T>(object target, string field) where T : class => (T)target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
