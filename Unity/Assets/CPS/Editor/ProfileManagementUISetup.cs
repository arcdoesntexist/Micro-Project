using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Editor
{
    public static class ProfileManagementUISetup
    {
        private const string PrefabPath = "Assets/CPS/Prefabs/MainMenuCanvas.prefab";
        private static readonly Color Cocoa = new Color(.22f, .10f, .07f);
        private static readonly Color Cream = new Color(.99f, .91f, .73f);
        private static readonly Color Light = new Color(1f, .97f, .85f);
        private static readonly Color Orange = new Color(.98f, .49f, .19f);

        [MenuItem("CPS/Author Profile Management UI")]
        public static void Author()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var old = root.transform.Find("Profile manager");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var ui = root.GetComponent<MainMenuUI>();
                var howTo = root.transform.Find("How to play");
                var manager = Panel(root.transform, "Profile manager", new Vector2(585, -90), new Vector2(560, 820), Cream);
                var howToImage = howTo.GetComponent<Image>();
                var managerImage = manager.GetComponent<Image>();
                managerImage.sprite = howToImage.sprite;
                managerImage.type = howToImage.type;
                managerImage.pixelsPerUnitMultiplier = howToImage.pixelsPerUnitMultiplier;

                Label(manager.transform, "ALL PROFILES", new Vector2(0, 330), new Vector2(450, 55), 36);
                var viewport = Panel(manager.transform, "Profile list viewport", new Vector2(0, 145), new Vector2(440, 270), Light);
                viewport.AddComponent<Mask>().showMaskGraphic = false;
                var content = Rect(viewport.transform, "Profile list content", new Vector2(0, 1), Vector2.one,
                    new Vector2(.5f, 1), Vector2.zero, Vector2.zero);
                var layout = content.AddComponent<VerticalLayoutGroup>();
                layout.childControlHeight = false;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.spacing = 6;
                var fitter = content.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var scroll = viewport.AddComponent<ScrollRect>();
                scroll.horizontal = false;
                scroll.viewport = viewport.GetComponent<RectTransform>();
                scroll.content = content.GetComponent<RectTransform>();
                var row = Button(content.transform, "Profile row template", Vector2.zero, new Vector2(420, 58), 22);
                row.gameObject.SetActive(false);

                var status = Label(manager.transform, "No profiles yet", new Vector2(0, -15), new Vector2(440, 48), 23);
                var rename = Input(manager.transform, new Vector2(0, -78), new Vector2(440, 60));
                var save = Button(manager.transform, "SAVE NAME", new Vector2(-112, -150), new Vector2(210, 58), 23);
                var use = Button(manager.transform, "USE PROFILE", new Vector2(112, -150), new Vector2(210, 58), 23);
                var delete = Button(manager.transform, "DELETE", new Vector2(-112, -218), new Vector2(210, 58), 23);
                var back = Button(manager.transform, "BACK", new Vector2(112, -218), new Vector2(210, 58), 23);

                var confirmation = Panel(manager.transform, "Delete confirmation", Vector2.zero, new Vector2(470, 250), Cream);
                var confirmationText = Label(confirmation.transform, "Delete profile?", new Vector2(0, 55), new Vector2(410, 75), 25);
                var confirm = Button(confirmation.transform, "DELETE", new Vector2(-105, -55), new Vector2(190, 58), 22);
                var cancel = Button(confirmation.transform, "CANCEL", new Vector2(105, -55), new Vector2(190, 58), 22);
                confirmation.SetActive(false);
                manager.SetActive(false);

                var manage = root.transform.Find("MANAGE PROFILES")?.GetComponent<Button>();
                if (manage == null)
                    manage = Button(root.transform, "MANAGE PROFILES", new Vector2(585, -425), new Vector2(440, 58), 24);

                var serialized = new SerializedObject(ui);
                var dropdown = (Dropdown)serialized.FindProperty("profileDropdown").objectReferenceValue;
                var easy = (Button)serialized.FindProperty("easyButton").objectReferenceValue;
                var dropdownNavigation = dropdown.navigation;
                dropdownNavigation.mode = Navigation.Mode.Explicit;
                dropdownNavigation.selectOnDown = manage;
                dropdown.navigation = dropdownNavigation;
                var manageNavigation = manage.navigation;
                manageNavigation.mode = Navigation.Mode.Explicit;
                manageNavigation.selectOnUp = dropdown;
                manageNavigation.selectOnDown = easy;
                manage.navigation = manageNavigation;
                var easyNavigation = easy.navigation;
                easyNavigation.mode = Navigation.Mode.Explicit;
                easyNavigation.selectOnUp = manage;
                easy.navigation = easyNavigation;
                Chain(rename, save, use, delete, back);
                Chain(confirm, cancel);
                Set(serialized, "howToPlayPanel", howTo.gameObject);
                var howToObjects = serialized.FindProperty("howToPlayObjects");
                var labels = new[] { "MOVE", "A / ←     D / →", "Use left and right to catch Apple, Bread and Cake. Avoid the can and banana peel.",
                    "3 HP  ·  60 SECONDS", "Keyboard controls are simulated input. Physical force (N) is unavailable until a sensor is connected." };
                howToObjects.arraySize = labels.Length + 1;
                for (int i = 0; i < labels.Length; i++)
                    howToObjects.GetArrayElementAtIndex(i).objectReferenceValue = root.transform.Find(labels[i])?.gameObject;
                howToObjects.GetArrayElementAtIndex(labels.Length).objectReferenceValue = manage.gameObject;
                Set(serialized, "profileManagerPanel", manager);
                Set(serialized, "profileListContent", content.transform);
                Set(serialized, "profileRowTemplate", row);
                Set(serialized, "manageProfilesButton", manage);
                Set(serialized, "renameField", rename);
                Set(serialized, "saveNameButton", save);
                Set(serialized, "useProfileButton", use);
                Set(serialized, "deleteProfileButton", delete);
                Set(serialized, "backProfilesButton", back);
                Set(serialized, "deleteConfirmation", confirmation);
                Set(serialized, "deleteConfirmationText", confirmationText);
                Set(serialized, "confirmDeleteButton", confirm);
                Set(serialized, "cancelDeleteButton", cancel);
                Set(serialized, "profileManagerStatus", status);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
        }

        private static GameObject Panel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = Rect(parent, name, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, size);
            go.AddComponent<Image>().color = color;
            return go;
        }

        private static Text Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize)
        {
            var go = Rect(parent, text, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, size);
            var label = go.AddComponent<Text>();
            label.text = text;
            label.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/CPS/Fonts/FFSpokenTrial-Regular.ttf");
            label.fontSize = fontSize;
            label.color = Cocoa;
            label.alignment = TextAnchor.MiddleCenter;
            label.supportRichText = false;
            return label;
        }

        private static Button Button(Transform parent, string text, Vector2 position, Vector2 size, int fontSize)
        {
            var go = Panel(parent, text, position, size, Orange);
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            Label(go.transform, text, Vector2.zero, size - new Vector2(16, 8), fontSize);
            return button;
        }

        private static InputField Input(Transform parent, Vector2 position, Vector2 size)
        {
            var go = Panel(parent, "Rename profile", position, size, Light);
            var input = go.AddComponent<InputField>();
            input.targetGraphic = go.GetComponent<Image>();
            input.textComponent = Label(go.transform, "", Vector2.zero, size - new Vector2(24, 8), 24);
            input.placeholder = Label(go.transform, "New profile name", Vector2.zero, size - new Vector2(24, 8), 24);
            input.characterLimit = 0;
            return input;
        }

        private static GameObject Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return go;
        }

        private static void Set(SerializedObject serialized, string property, Object value)
        {
            serialized.FindProperty(property).objectReferenceValue = value;
        }

        private static void Chain(params Selectable[] controls)
        {
            for (int i = 0; i < controls.Length; i++)
            {
                var navigation = controls[i].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = controls[(i - 1 + controls.Length) % controls.Length];
                navigation.selectOnDown = controls[(i + 1) % controls.Length];
                controls[i].navigation = navigation;
            }
        }
    }
}
