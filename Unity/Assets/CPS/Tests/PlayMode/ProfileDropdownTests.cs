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
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Tests
{
    public sealed class ProfileDropdownTests
    {
        private GameObject sessionObject;
        private ProfileStore store;

        [UnityTest] public IEnumerator SinglePlayerShowsOneProfile() => Check("MainMenu", 1);
        [UnityTest] public IEnumerator SinglePlayerShowsAllTwelveProfiles() => Check("MainMenu", 12);
        [UnityTest] public IEnumerator SinglePlayerScrollsToFortiethProfile() => Check("MainMenu", 40);
        [UnityTest] public IEnumerator BothVersusListsShowAllTwelveProfiles() => Check("VersusSetup", 12);
        [UnityTest] public IEnumerator BothVersusListsScrollToFortiethProfile() => Check("VersusSetup", 40);

        private IEnumerator Check(string scene, int count)
        {
            var values = new Dictionary<string, string>();
            store = new ProfileStore(key => values.TryGetValue(key, out var value) ? value : null,
                (key, value) => values[key] = value, () => { });
            sessionObject = new GameObject("In-memory dropdown test session");
            sessionObject.SetActive(false);
            var session = sessionObject.AddComponent<AppSession>();
            session.Initialize(store);
            for (int i = 0; i < count; i++) store.Create(i % 3 == 0 ? "Same Name" : "Player " + (i + 1));
            sessionObject.SetActive(true);
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Dropdown[] dropdowns;
            if (scene == "MainMenu")
            {
                var menu = Object.FindFirstObjectByType<MainMenuUI>();
                dropdowns = new[] { (Dropdown)typeof(MainMenuUI).GetField("profileDropdown", flags).GetValue(menu) };
            }
            else
            {
                var setup = Object.FindFirstObjectByType<VersusSetupUI>();
                dropdowns = new[] { (Dropdown)typeof(VersusSetupUI).GetField("player1Profiles", flags).GetValue(setup),
                    (Dropdown)typeof(VersusSetupUI).GetField("player2Profiles", flags).GetValue(setup) };
            }
            for (int slot = 0; slot < dropdowns.Length; slot++)
            {
                var dropdown = dropdowns[slot];
                Assert.That(dropdown.options.Count, Is.EqualTo(count), "All stored profiles must be included.");
                var row = dropdown.template.GetComponentInChildren<Toggle>(true).GetComponent<RectTransform>();
                var content = dropdown.template.GetComponent<ScrollRect>().content;
                Assert.That(row.rect.height, Is.GreaterThanOrEqualTo(52), "Rows must fit the game font.");
                Assert.That(content.rect.height, Is.GreaterThanOrEqualTo(row.rect.height), "Content seed must include the row.");
                dropdown.alphaFadeSpeed = 0;
                dropdown.Show();
                yield return null;
                var list = dropdown.transform.Find("Dropdown List");
                Assert.That(list, Is.Not.Null);
                var items = list.GetComponentsInChildren<Toggle>(false);
                Assert.That(items.Length, Is.EqualTo(count));
                for (int i = 0; i < count; i++)
                {
                    var label = items[i].GetComponentInChildren<Text>();
                    Assert.That(label.text, Does.StartWith(store.Profiles[i].DisplayName));
                    Assert.That(label.rectTransform.rect.height, Is.GreaterThanOrEqualTo(label.fontSize));
                }
                var scroll = list.GetComponent<ScrollRect>();
                Canvas.ForceUpdateCanvases();
                AssertVisible(items[0].GetComponent<RectTransform>(), scroll.viewport);
                if (count == 12) Capture(scene + "-P" + (slot + 1) + "-open");
                scroll.verticalNormalizedPosition = 0;
                Canvas.ForceUpdateCanvases();
                AssertVisible(items[count - 1].GetComponent<RectTransform>(), scroll.viewport);
                if (count == 40) Capture(scene + "-P" + (slot + 1) + "-last");
                items[count - 1].isOn = true;
                yield return null;
                Assert.That(dropdown.value, Is.EqualTo(count - 1));
                if (scene == "MainMenu") Assert.That(session.SelectedProfile.PlayerId, Is.EqualTo(store.Profiles[count - 1].PlayerId));
                else
                {
                    var setup = Object.FindFirstObjectByType<VersusSetupUI>();
                    Assert.That(slot == 0 ? setup.Player1ProfileId : setup.Player2ProfileId, Is.EqualTo(store.Profiles[count - 1].PlayerId));
                }
                dropdown.Hide();
                yield return null;
            }
        }

        private static void AssertVisible(RectTransform row, RectTransform viewport)
        {
            var corners = new Vector3[4]; row.GetWorldCorners(corners);
            var bounds = viewport.rect;
            float min = corners.Min(corner => viewport.InverseTransformPoint(corner).y);
            float max = corners.Max(corner => viewport.InverseTransformPoint(corner).y);
            Assert.That(min, Is.GreaterThanOrEqualTo(bounds.yMin - 1));
            Assert.That(max, Is.LessThanOrEqualTo(bounds.yMax + 1));
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            var target = new RenderTexture(1280, 720, 24);
            camera.targetTexture = target;
            var viewport = camera.GetComponent<GameplayViewport>();
            if (viewport != null) viewport.Apply(1280, 720);
            Canvas.ForceUpdateCanvases();
            for (int pass = 0; pass < 3; pass++)
            {
                foreach (var label in Object.FindObjectsByType<Text>(FindObjectsSortMode.None)) label.SetAllDirty();
                Canvas.ForceUpdateCanvases();
            }
            camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
            string directory = Path.GetFullPath(Application.dataPath + "/../../verification/profile-dropdown-fix");
            Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory, name + ".png"), texture.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null;
            Object.DestroyImmediate(texture); Object.DestroyImmediate(target);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (sessionObject != null) Object.DestroyImmediate(sessionObject);
            var blank = SceneManager.CreateScene("Dropdown cleanup " + Guid.NewGuid());
            var active = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(blank);
            if (active.IsValid() && active.isLoaded) yield return SceneManager.UnloadSceneAsync(active);
        }
    }
}
