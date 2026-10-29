using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Editor
{
    public static class ProfileDropdownSetup
    {
        private const string Atlas = "Assets/CPS/Art/UI/home-ui-atlas-v1.png";
        private const float RowHeight = 56;

        [MenuItem("CPS/Repair Profile Dropdowns")]
        public static void Apply()
        {
            const string prefabPath = "Assets/CPS/Prefabs/MainMenuCanvas.prefab";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var menu = new SerializedObject(root.GetComponent<MainMenuUI>());
                Configure((Dropdown)menu.FindProperty("profileDropdown").objectReferenceValue);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var scene = EditorSceneManager.OpenScene("Assets/CPS/Scenes/VersusSetup.unity", OpenSceneMode.Single);
            var setup = new SerializedObject(Object.FindFirstObjectByType<VersusSetupUI>());
            Configure((Dropdown)setup.FindProperty("player1Profiles").objectReferenceValue);
            Configure((Dropdown)setup.FindProperty("player2Profiles").objectReferenceValue);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            var stored = new ProfileStore(key => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null,
                (key, value) => { }, () => { });
            stored.Load();
            Debug.Log("PROFILE_DROPDOWNS_REPAIRED storedProfileCount=" + stored.Profiles.Count + " warning=" + stored.Warning);
        }

        public static void Configure(Dropdown dropdown)
        {
            var font = dropdown.captionText.font;
            if (dropdown.template != null) Object.DestroyImmediate(dropdown.template.gameObject);
            var template = Rect(dropdown.transform, "Template", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(.5f, 1), new Vector2(0, -8), new Vector2(0, 320));
            var panel = template.gameObject.AddComponent<Image>();
            panel.sprite = AssetDatabase.LoadAllAssetsAtPath(Atlas).OfType<Sprite>().First(sprite => sprite.name == "CreamPanel");
            panel.type = Image.Type.Sliced; panel.pixelsPerUnitMultiplier = 5;
            var viewport = Rect(template, "Viewport", Vector2.zero, Vector2.one, Vector2.one * .5f, Vector2.zero, Vector2.zero);
            viewport.offsetMin = new Vector2(12, 0); viewport.offsetMax = new Vector2(-30, 0);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect(viewport, "Content", new Vector2(0, 1), Vector2.one, new Vector2(.5f, 1),
                Vector2.zero, new Vector2(0, RowHeight + 24));
            var row = Rect(content, "Item", new Vector2(0, 1), Vector2.one, new Vector2(.5f, .5f),
                new Vector2(0, -12 - RowHeight / 2), new Vector2(0, RowHeight));
            var background = row.gameObject.AddComponent<Image>(); background.color = Hex("#FFF7D9");
            var toggle = row.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = background;
            var colors = toggle.colors;
            colors.normalColor = Color.white; colors.highlightedColor = Hex("#F5D39D");
            colors.selectedColor = Hex("#F5D39D"); colors.pressedColor = Hex("#E9B875");
            toggle.colors = colors;
            var check = Rect(row, "Selected", new Vector2(0, .5f), new Vector2(0, .5f), Vector2.one * .5f,
                new Vector2(18, 0), new Vector2(19, 19));
            var checkImage = check.gameObject.AddComponent<Image>();
            checkImage.sprite = AssetDatabase.LoadAllAssetsAtPath(Atlas).OfType<Sprite>().First(sprite => sprite.name == "Star");
            checkImage.preserveAspect = true; checkImage.raycastTarget = false; toggle.graphic = checkImage;
            var labelRect = Rect(row, "Item Label", Vector2.zero, Vector2.one, Vector2.one * .5f, Vector2.zero, Vector2.zero);
            labelRect.offsetMin = new Vector2(40, 2); labelRect.offsetMax = new Vector2(-10, -2);
            var label = labelRect.gameObject.AddComponent<Text>(); label.font = font; label.fontSize = 30;
            label.color = Hex("#61331F"); label.alignment = TextAnchor.MiddleLeft; label.supportRichText = false;
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 20; label.resizeTextMaxSize = 30;
            label.raycastTarget = false;
            var track = Rect(template, "Scrollbar", new Vector2(1, 0), Vector2.one, new Vector2(1, .5f), Vector2.zero, new Vector2(14, 0));
            track.offsetMin = new Vector2(-25, 12); track.offsetMax = new Vector2(-11, -12);
            var trackImage = track.gameObject.AddComponent<Image>(); trackImage.color = Hex("#EDD3A7");
            var handle = Rect(track, "Handle", Vector2.zero, Vector2.one, Vector2.one * .5f, Vector2.zero, Vector2.zero);
            var handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = Hex("#9A6642");
            var scrollbar = track.gameObject.AddComponent<Scrollbar>(); scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage; scrollbar.direction = Scrollbar.Direction.BottomToTop;
            var scroll = template.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 36;
            scroll.verticalScrollbar = scrollbar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            dropdown.template = template; dropdown.itemText = label; dropdown.itemImage = null;
            template.gameObject.SetActive(false);
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var root = new GameObject(name, typeof(RectTransform)); root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max;
            rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }

        private static Color Hex(string value) { ColorUtility.TryParseHtmlString(value, out var color); return color; }
    }
}
