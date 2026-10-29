using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace CPS.KitchenCatch.Tests
{
    public sealed class PresentationAuthoringTests
    {
        [Test]
        public void ProductionSpritesHaveTightFootPivotsAndStretchablePanel()
        {
            var body = AssetDatabase.LoadAllAssetsAtPath("Assets/CPS/Art/Mascot/mascot-body-atlas-v1.png").OfType<Sprite>().ToArray();
            var items = AssetDatabase.LoadAllAssetsAtPath("Assets/CPS/Art/Items/items-atlas-v1.png").OfType<Sprite>().ToArray();
            var ambient = AssetDatabase.LoadAllAssetsAtPath("Assets/CPS/Art/Kitchen/ambient-atlas-v1.png").OfType<Sprite>().ToArray();
            var panel = AssetDatabase.LoadAllAssetsAtPath("Assets/CPS/Art/UI/ui-panel-v1.png").OfType<Sprite>().SingleOrDefault();

            Assert.That(body, Has.Length.EqualTo(12));
            Assert.That(items, Has.Length.EqualTo(5));
            Assert.That(ambient, Has.Length.EqualTo(3));
            Assert.That(body[0].pivot.y, Is.LessThan(4f));
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.border.x, Is.GreaterThan(0));
            Assert.That(panel.border.y, Is.GreaterThan(0));
        }

        [Test]
        public void BananaPeelSliceIncludesCompleteSourceCurlWithoutMovingItsRuntimePivot()
        {
            var peel = AssetDatabase.LoadAllAssetsAtPath("Assets/CPS/Art/Items/items-atlas-v1.png")
                .OfType<Sprite>().Single(sprite => sprite.name == "BananaPeel");
            Vector2 texturePivot = peel.rect.position + peel.pivot;

            Assert.That(peel.rect.xMin, Is.EqualTo(569).Within(.01f));
            Assert.That(peel.rect.xMax, Is.EqualTo(1053).Within(.01f), "Right curl is clipped or includes unrelated pixels");
            Assert.That(texturePivot.x, Is.EqualTo(796.5f).Within(.01f), "Expanding the slice moved the runtime pivot");
            Assert.That(peel.pixelsPerUnit, Is.EqualTo(350).Within(.01f));
        }

        [Test]
        public void ProductionIdleAndRunClipsHaveAnimatorLoopTime()
        {
            foreach (string name in new[] { "Idle", "Run" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/CPS/Animation/Mascot/" + name + ".anim");
                Assert.That(clip, Is.Not.Null);
                var loop = new SerializedObject(clip).FindProperty("m_AnimationClipSettings.m_LoopTime");
                Assert.That(loop, Is.Not.Null);
                Assert.That(loop.boolValue, Is.True, name + " must repeat in Animator");
            }
        }

        [Test]
        public void MascotPrefabHasReadableSpritesForBothFacingsAndHatTouchesHead()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/Mascot.prefab");
            var presentation = new SerializedObject(prefab.GetComponent<MascotPresentation>());
            var serializedHat = presentation.FindProperty("hat").objectReferenceValue as SpriteRenderer;
            var rightHat = presentation.FindProperty("rightFacingHat").objectReferenceValue as Sprite;
            var leftHat = presentation.FindProperty("leftFacingHat").objectReferenceValue as Sprite;
            var body = prefab.transform.Find("Body").GetComponent<SpriteRenderer>();
            var hat = prefab.transform.Find("Hat").GetComponent<SpriteRenderer>();
            float bodyTop = body.transform.localPosition.y + body.sprite.bounds.max.y;
            float hatBottom = hat.transform.localPosition.y + hat.sprite.bounds.min.y;

            Assert.That(serializedHat, Is.SameAs(hat));
            Assert.That(rightHat, Is.Not.Null);
            Assert.That(leftHat, Is.Not.Null);
            Assert.That(rightHat.name, Is.EqualTo("EngineerHat"));
            Assert.That(leftHat.name, Is.EqualTo("EngineerHatLeft"));
            Assert.That(leftHat.bounds.size.x, Is.EqualTo(rightHat.bounds.size.x).Within(.03f));
            Assert.That(leftHat.bounds.size.y, Is.EqualTo(rightHat.bounds.size.y).Within(.03f));
            Assert.That(hatBottom, Is.LessThanOrEqualTo(bodyTop + .03f), "Hat still floats above the mascot head");
            Assert.That(hat.transform.localPosition.x, Is.EqualTo(-.14f).Within(.001f));
            Assert.That(hat.transform.localPosition.y, Is.EqualTo(1.25f).Within(.001f));
        }

        [Test]
        public void ProductionAmbientPrefabHasNoGameplayColliders()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/KitchenAmbient.prefab");
            Assert.That(prefab, Is.Not.Null);
            var instance = Object.Instantiate(prefab);
            try
            {
                Assert.That(instance.GetComponent<KitchenAmbient>(), Is.Not.Null);
                Assert.That(instance.GetComponentsInChildren<Collider2D>(true), Is.Empty);
                Assert.That(instance.GetComponentsInChildren<Collider>(true), Is.Empty);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void SharedRigUsesOriginalProductionDefinitionsAndCounterHeight()
        {
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/GameplayRig.prefab");
            var player = rig.GetComponentInChildren<PlayerController>();
            var presentation = rig.GetComponentInChildren<MascotPresentation>();
            var spawner = rig.GetComponent<FallingObjectSpawner>();
            var serialized = new SerializedObject(spawner);

            Assert.That(presentation, Is.Not.Null);
            Assert.That(player.transform.position.y, Is.EqualTo(-2.5f).Within(.1f));
            Assert.That(player.ReachableCatchCenterRange.x, Is.LessThanOrEqualTo(-6f));
            Assert.That(player.ReachableCatchCenterRange.y, Is.GreaterThanOrEqualTo(6f));
            Assert.That(player.CatchZone.center.x, Is.EqualTo(0));
            Assert.That(player.CatchZone.size, Is.EqualTo(new Vector3(2f, .8f, .2f)));
            string[] names = { "Apple", "Bread", "Cake" };
            var foods = serialized.FindProperty("foodDefinitions");
            for (int i = 0; i < names.Length; i++)
            {
                var definition = AssetDatabase.LoadAssetAtPath<FallingObjectDefinition>("Assets/CPS/Config/" + names[i] + ".asset");
                Assert.That(foods.GetArrayElementAtIndex(i).objectReferenceValue, Is.SameAs(definition));
                Assert.That(definition.sprite, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(definition.sprite), Does.StartWith("Assets/CPS/Art/Items/"));
            }
            names = new[] { "Can", "BananaPeel" };
            var trash = serialized.FindProperty("trashDefinitions");
            for (int i = 0; i < names.Length; i++)
            {
                var definition = AssetDatabase.LoadAssetAtPath<FallingObjectDefinition>("Assets/CPS/Config/" + names[i] + ".asset");
                Assert.That(trash.GetArrayElementAtIndex(i).objectReferenceValue, Is.SameAs(definition));
                Assert.That(definition.sprite, Is.Not.Null);
            }
        }

        [Test]
        public void MainPanelsUseNineSliceMaterialWithoutCoveringEveryStatistic()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/MainMenuCanvas.prefab");
            var result = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/ResultPanel.prefab");
            var panel = AssetDatabase.LoadAllAssetsAtPath("Assets/CPS/Art/UI/ui-panel-v1.png").OfType<Sprite>().SingleOrDefault();
            var primary = menu.transform.Find("Profile and difficulty").GetComponent<Image>();
            var card = result.transform.Find("Results card").GetComponent<Image>();

            Assert.That(primary.sprite.name, Is.EqualTo("CreamPanel"));
            Assert.That(primary.sprite.border.x, Is.GreaterThan(0));
            Assert.That(card.sprite, Is.SameAs(panel));
            Assert.That(primary.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(card.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(result.transform.Find("Results card/Left statistics").GetComponent<Image>().sprite, Is.Null);
        }

        [Test]
        public void MainMenuProvidesBoundProfileManagementPanel()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/MainMenuCanvas.prefab");
            var ui = new SerializedObject(menu.GetComponent<MainMenuUI>());
            var howTo = menu.transform.Find("Approved practice");
            var manager = menu.transform.Find("Profile manager");

            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.gameObject.activeSelf, Is.False);
            Assert.That(ui.FindProperty("howToPlayPanel").objectReferenceValue, Is.SameAs(howTo.gameObject));
            Assert.That(ui.FindProperty("profileManagerPanel").objectReferenceValue, Is.SameAs(manager.gameObject));
            Assert.That(ui.FindProperty("manageProfilesButton").objectReferenceValue, Is.Not.Null);
            Assert.That(ui.FindProperty("profileListContent").objectReferenceValue, Is.Not.Null);
            Assert.That(ui.FindProperty("renameField").objectReferenceValue, Is.Not.Null);
            Assert.That(ui.FindProperty("confirmDeleteButton").objectReferenceValue, Is.Not.Null);
            Assert.That(manager.Find("Delete confirmation").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void MainMenuProvidesApprovedPracticeAndRulesOverlay()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/MainMenuCanvas.prefab");
            var practice = menu.transform.Find("Approved practice");
            var rules = menu.transform.Find("Game rules overlay");
            var controller = menu.GetComponent<MenuPracticeUI>();
            var serialized = new SerializedObject(controller);

            Assert.That(practice, Is.Not.Null);
            Assert.That(practice.Find("Practice track/Practice mascot/Body").GetComponent<Image>().sprite.name, Is.EqualTo("IdleA"));
            Assert.That(practice.Find("Practice track/Practice mascot/Hat").GetComponent<Image>().sprite.name, Is.EqualTo("EngineerHat"));
            Assert.That(rules, Is.Not.Null);
            Assert.That(rules.gameObject.activeSelf, Is.False);
            Assert.That(serialized.FindProperty("runFrames").arraySize, Is.EqualTo(4));
            Assert.That(serialized.FindProperty("rulesButton").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("closeRulesButton").objectReferenceValue, Is.Not.Null);
            var text = rules.GetComponentsInChildren<Text>(true).Select(label => label.text).ToArray();
            CollectionAssert.IsSubsetOf(new[] { "GAME RULES", "Apple\n+10 points", "Bread\n+15 points", "Cake\n+20 points",
                "Can\n−1 HP", "Peel\n−1 HP", "D", "C", "B", "A", "S", "SS", "SSS" }, text);
        }

        [Test]
        public void OverlayPanelsCoverMascotAndKeepControlsInsideCream()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/MainMenuCanvas.prefab");
            var gameplay = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/GameplayCanvas.prefab");
            var result = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/ResultPanel.prefab");
            var mascot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/Mascot.prefab");
            int mascotOrder = mascot.GetComponentsInChildren<SpriteRenderer>(true).Max(renderer => renderer.sortingOrder);

            Assert.That(gameplay.GetComponent<Canvas>().sortingOrder, Is.GreaterThan(mascotOrder));
            Assert.That(menu.GetComponent<Canvas>().sortingOrder, Is.GreaterThan(mascotOrder));
            Assert.That(gameplay.transform.Find("Player").GetComponent<Image>().sprite.name, Is.EqualTo("Input"));
            Assert.That(gameplay.transform.Find("Score").GetComponent<Image>().sprite.name, Is.EqualTo("Input"));
            AssertInsideCream(menu.transform, "Profile and difficulty", "CHOOSE YOUR PLAYER");
            AssertInsideCream(menu.transform, "Profile and difficulty", "Select or create a profile.");
            AssertInsideCream(menu.transform, "How to play", "MOVE");
            AssertInsideCream(menu.transform, "How to play", "Keyboard controls are simulated input. Physical force (N) is unavailable until a sensor is connected.");
            AssertInsideCream(gameplay.transform, "Pause overlay/Pause card", "Pause overlay/RESUME");
            AssertInsideCream(gameplay.transform, "Pause overlay/Pause card", "Pause overlay/SETTINGS");
            AssertInsideCream(gameplay.transform, "Pause overlay/Pause card", "Pause overlay/MAIN MENU");
            AssertInsideCream(result.transform, "Results card", "Results card/RESTART");
            AssertInsideCream(result.transform, "Results card", "Results card/MAIN MENU");
        }

        [Test]
        public void ResultsActionsLeaveInputDisclosureReadableAndMenuStatusPadded()
        {
            var result = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/ResultPanel.prefab");
            var card = result.transform.Find("Results card");
            var forceLabel = (Text)new SerializedObject(result.GetComponent<ResultPanel>())
                .FindProperty("forceStatus").objectReferenceValue;
            var force = forceLabel.GetComponent<RectTransform>();
            float forceBottom = force.anchoredPosition.y - force.sizeDelta.y * .5f;
            foreach (string name in new[] { "RESTART", "MAIN MENU" })
            {
                var button = card.Find(name).GetComponent<RectTransform>();
                float buttonTop = button.anchoredPosition.y + button.sizeDelta.y * .5f;
                Assert.That(buttonTop, Is.LessThan(forceBottom - 8), name + " obscures input disclosure");
                AssertInsideCream(result.transform, "Results card", "Results card/" + name);
            }

            var menu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/MainMenuCanvas.prefab");
            var panel = menu.transform.Find("Profile and difficulty").GetComponent<RectTransform>();
            var image = panel.GetComponent<Image>();
            var status = menu.transform.Find("Select or create a profile.").GetComponent<RectTransform>();
            float bottom = status.anchoredPosition.y - status.sizeDelta.y * .5f - panel.anchoredPosition.y;
            float creamBottom = panel.rect.yMin + image.sprite.border.y / image.pixelsPerUnitMultiplier;
            Assert.That(bottom, Is.GreaterThanOrEqualTo(creamBottom + 20), "Menu status crowds the wood rim");
        }

        [Test]
        public void RankBadgeUsesLiveLetterNameAndProgressInsideReferenceFootprint()
        {
            var gameplay = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/GameplayCanvas.prefab");
            var ui = new SerializedObject(gameplay.GetComponent<GameplayUI>());
            var badge = gameplay.transform.Find("Rank badge").GetComponent<RectTransform>();
            var letter = ui.FindProperty("rankLetter").objectReferenceValue as Text;
            var name = ui.FindProperty("rank").objectReferenceValue as Text;
            var accents = ui.FindProperty("rankAccents");

            Assert.That(badge.sizeDelta.x, Is.EqualTo(420).Within(.01f));
            Assert.That(badge.sizeDelta.y, Is.EqualTo(134.4f).Within(.01f));
            Assert.That(badge.anchorMin, Is.EqualTo(new Vector2(.5f,.5f)));
            Assert.That(badge.anchorMax, Is.EqualTo(new Vector2(.5f,.5f)));
            Assert.That(letter, Is.Not.Null);
            Assert.That(name, Is.Not.Null);
            Assert.That(letter.fontStyle, Is.EqualTo(FontStyle.Bold));
            Assert.That(ui.FindProperty("streakFill").objectReferenceValue, Is.Not.Null);
            Assert.That(accents.arraySize, Is.Zero);
            for (int i = 0; i < accents.arraySize; i++)
                Assert.That(accents.GetArrayElementAtIndex(i).objectReferenceValue, Is.TypeOf<Image>());
        }

        [Test]
        public void GlobalSettingsUseBoostMixerAndFfSpokenAcrossAllUi()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/CPS/Resources/Audio/KitchenAudioMixer.mixer");
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/CPS/Fonts/FFSpokenTrial-Regular.ttf");
            var settings = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/AudioSettingsPanel.prefab");
            Assert.That(mixer, Is.Not.Null);
            Assert.That(mixer.FindMatchingGroups("Music"), Has.Length.EqualTo(1));
            Assert.That(mixer.FindMatchingGroups("SFX"), Has.Length.EqualTo(1));
            Assert.That(mixer.GetFloat("MasterVolume", out _), Is.True);
            Assert.That(font, Is.Not.Null);
            Assert.That(settings.GetComponent<AudioSettingsPanel>(), Is.Not.Null);

            foreach (string path in new[] { "Assets/CPS/Prefabs/MainMenuCanvas.prefab", "Assets/CPS/Prefabs/GameplayCanvas.prefab", "Assets/CPS/Prefabs/ResultPanel.prefab", "Assets/CPS/Prefabs/AudioSettingsPanel.prefab" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Not.Empty, path);
                foreach (var label in prefab.GetComponentsInChildren<Text>(true))
                {
                    var resultView=label.GetComponentInParent<ResultReferenceView>(true);
                    bool numeric=resultView != null && label.transform.parent == resultView.details &&
                        (label.name == "Round stats" || label.name == "Left" || label.name == "Right" || label.name == "Balance" || label.name == "Force");
                    if(numeric) Assert.That(label.font.HasCharacter('%'),Is.True,"Result units must render");
                    else Assert.That(label.font, Is.SameAs(font), label.name + " in " + path);
                }
            }

            var menu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/MainMenuCanvas.prefab");
            var gameplay = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CPS/Prefabs/GameplayCanvas.prefab");
            var menuUi = new SerializedObject(menu.GetComponent<MainMenuUI>());
            var menuSettings = menu.transform.Find("SETTINGS").GetComponent<Button>();
            var name = (InputField)menuUi.FindProperty("nicknameField").objectReferenceValue;
            var start = (Button)menuUi.FindProperty("startButton").objectReferenceValue;
            Assert.That(menuSettings, Is.Not.Null);
            Assert.That(menuUi.FindProperty("settingsButton").objectReferenceValue, Is.SameAs(menuSettings));
            Assert.That(menuSettings.navigation.mode, Is.EqualTo(Navigation.Mode.Automatic));
            Assert.That(menuSettings.navigation.selectOnDown, Is.Not.SameAs(name), "Settings must not point into the closed creation modal");
            Assert.That(menu.transform.Find("Settings panel").GetComponent<AudioSettingsPanel>(), Is.Not.Null);
            var gameplayUi = new SerializedObject(gameplay.GetComponent<GameplayUI>());
            var pauseSettings = gameplay.transform.Find("Pause overlay/SETTINGS").GetComponent<Button>();
            var pauseMenu = gameplay.transform.Find("Pause overlay/MAIN MENU").GetComponent<Button>();
            Assert.That(gameplayUi.FindProperty("settingsButton").objectReferenceValue, Is.SameAs(pauseSettings));
            Assert.That(gameplayUi.FindProperty("backToMenuButton").objectReferenceValue, Is.SameAs(pauseMenu));
            Assert.That(pauseSettings.navigation.selectOnDown, Is.SameAs(pauseMenu));
            Assert.That(gameplay.transform.Find("Settings panel").GetComponent<AudioSettingsPanel>(), Is.Not.Null);
        }

        private static void AssertInsideCream(Transform root, string panelPath, string contentPath)
        {
            var panel = root.Find(panelPath).GetComponent<RectTransform>();
            var sprite = panel.GetComponent<Image>().sprite;
            var content = root.Find(contentPath).GetComponent<RectTransform>();
            Assert.That(panel.anchorMin, Is.EqualTo(new Vector2(.5f, .5f)));
            Assert.That(panel.anchorMax, Is.EqualTo(panel.anchorMin));
            Assert.That(content.anchorMin, Is.EqualTo(new Vector2(.5f, .5f)));
            Assert.That(content.anchorMax, Is.EqualTo(content.anchorMin));
            var center = PositionInRoot(content, root) - PositionInRoot(panel, root);
            var half = content.sizeDelta * .5f;
            var border = sprite.border / panel.GetComponent<Image>().pixelsPerUnitMultiplier;
            Assert.That(center.x - half.x, Is.GreaterThanOrEqualTo(panel.rect.xMin + border.x), contentPath + " crosses left rim");
            Assert.That(center.x + half.x, Is.LessThanOrEqualTo(panel.rect.xMax - border.z), contentPath + " crosses right rim");
            Assert.That(center.y - half.y, Is.GreaterThanOrEqualTo(panel.rect.yMin + border.y), contentPath + " crosses bottom rim");
            Assert.That(center.y + half.y, Is.LessThanOrEqualTo(panel.rect.yMax - border.w), contentPath + " crosses top rim");
        }

        private static Vector2 PositionInRoot(RectTransform rect, Transform root)
        {
            var position = Vector2.zero;
            for (Transform current = rect; current != root; current = current.parent)
                position += ((RectTransform)current).anchoredPosition;
            return position;
        }
    }
}
