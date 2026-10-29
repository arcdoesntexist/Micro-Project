using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Editor
{
    public static class ApprovedMenuDesignSetup
    {
        private const string PrefabPath = "Assets/CPS/Prefabs/MainMenuCanvas.prefab";
        private static readonly Color Cocoa = new Color(.22f, .10f, .07f);
        private static readonly Color Cream = new Color(.99f, .91f, .73f);
        private static readonly Color Light = new Color(1f, .97f, .85f);
        private static readonly Color Orange = new Color(.98f, .49f, .19f);
        private static Font Font => AssetDatabase.LoadAssetAtPath<Font>("Assets/CPS/Fonts/FFSpokenTrial-Regular.ttf");

        [MenuItem("CPS/Apply Approved Menu Design")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Remove(root.transform, "Approved practice");
                Remove(root.transform, "Game rules overlay");
                var oldPractice = root.GetComponent<MenuPracticeUI>();
                if (oldPractice != null) Object.DestroyImmediate(oldPractice);

                var menu = root.GetComponent<MainMenuUI>();
                var serialized = new SerializedObject(menu);
                ArrangeProfilePanel(root.transform, serialized);
                var practice = BuildPractice(root.transform);
                var rules = BuildRules(root.transform);
                BindPractice(root, practice, rules);
                ArrangeProfileManager(root.transform);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
        }

        private static void ArrangeProfilePanel(Transform root, SerializedObject menu)
        {
            SetRect(root.Find("Profile and difficulty"), new Vector2(-340, -90), new Vector2(890, 820));
            Move(root, "CHOOSE YOUR PLAYER", new Vector2(-340, 240), new Vector2(760, 55));
            Move(root, "Or choose a local profile", new Vector2(-340, 190), new Vector2(700, 38));
            Move((Dropdown)menu.FindProperty("profileDropdown").objectReferenceValue, new Vector2(-385, 135), new Vector2(610, 68));
            var manage = (Button)menu.FindProperty("manageProfilesButton").objectReferenceValue;
            SetRect(manage.transform, new Vector2(-25, 135), new Vector2(90, 68));
            SetButtonText(manage, "EDIT");
            MoveText(root, "Nickname", new Vector2(-340, 70), new Vector2(700, 40), "NEW PLAYER · 1–24 CHARACTERS");
            Move((InputField)menu.FindProperty("nicknameField").objectReferenceValue, new Vector2(-340, 15), new Vector2(700, 68));
            Move((Button)menu.FindProperty("createButton").objectReferenceValue, new Vector2(-340, -67), new Vector2(700, 62));
            Move(root, "DIFFICULTY", new Vector2(-340, -142), new Vector2(700, 45));
            Move((Button)menu.FindProperty("easyButton").objectReferenceValue, new Vector2(-573, -202), new Vector2(215, 66));
            Move((Button)menu.FindProperty("normalButton").objectReferenceValue, new Vector2(-340, -202), new Vector2(215, 66));
            Move((Button)menu.FindProperty("hardButton").objectReferenceValue, new Vector2(-107, -202), new Vector2(215, 66));
            Move((Button)menu.FindProperty("startButton").objectReferenceValue, new Vector2(-340, -300), new Vector2(700, 80));
            Move((Text)menu.FindProperty("status").objectReferenceValue, new Vector2(-340, -382), new Vector2(740, 50));
            var oldPanel = root.Find("How to play");
            if (oldPanel != null) oldPanel.gameObject.SetActive(false);
            var oldObjects = menu.FindProperty("howToPlayObjects");
            oldObjects.arraySize = 0;
        }

        private static GameObject BuildPractice(Transform root)
        {
            var panel = Panel(root, "Approved practice", new Vector2(585, -90), new Vector2(560, 820), Cream);
            CopyPanelStyle(root.Find("Profile and difficulty"), panel.transform);
            Label(panel.transform, "A LITTLE PRACTICE?", new Vector2(-100, 330), new Vector2(300, 50), 31, Cocoa);
            var rules = Button(panel.transform, "GAME RULES", new Vector2(170, 330), new Vector2(180, 52), 22);
            rules.name = "GAME RULES";
            Key(panel.transform, "A / ←", new Vector2(-120, 258));
            Key(panel.transform, "D / →", new Vector2(120, 258));
            Label(panel.transform, "Move left and right", new Vector2(0, 205), new Vector2(430, 38), 22, Cocoa);
            var track = Panel(panel.transform, "Practice track", new Vector2(0, -40), new Vector2(500, 390), new Color(1f, 1f, 1f, 0));
            var mascot = Rect(track.transform, "Practice mascot", new Vector2(0, -75), new Vector2(210, 210));
            var body = Picture(mascot.transform, "Body", Vector2.zero, new Vector2(210, 210), Sprite("Assets/CPS/Art/Mascot/mascot-body-atlas-v1.png", "IdleA"));
            var hat = Picture(mascot.transform, "Hat", new Vector2(-15, 58), new Vector2(150, 115), Sprite("Assets/CPS/Art/Mascot/engineer-hat-v1.png", "EngineerHat"));
            var hint = Panel(panel.transform, "Catch hint", new Vector2(0, -330), new Vector2(470, 105), Cream);
            var border = hint.AddComponent<Outline>(); border.effectColor = new Color(.72f, .48f, .26f); border.effectDistance = new Vector2(3, -3);
            Label(hint.transform, "Catch food. Dodge rubbish.\n3 hearts · 60 seconds", Vector2.zero, new Vector2(430, 88), 23, Cocoa);
            panel.AddComponent<CanvasGroup>();
            return panel;
        }

        private static GameObject BuildRules(Transform root)
        {
            var overlay = Panel(root, "Game rules overlay", Vector2.zero, new Vector2(1920, 1080), new Color(.12f, .06f, .04f, .84f));
            var card = Panel(overlay.transform, "Rules card", Vector2.zero, new Vector2(1220, 990), Cream);
            CopyPanelStyle(root.Find("Profile and difficulty"), card.transform);
            Label(card.transform, "GAME RULES", new Vector2(0, 426), new Vector2(1050, 65), 48, Cocoa);
            Label(card.transform, "A hungry mascot. A busy kitchen. One minute!", new Vector2(0, 372), new Vector2(1050, 40), 24, Cocoa);
            Label(card.transform, "CATCH FOOD TO SCORE", new Vector2(-350, 318), new Vector2(440, 40), 26, Cocoa);
            Food(card.transform, "Apple", "Apple\n+10 points", new Vector2(-470, 220));
            Food(card.transform, "Bread", "Bread\n+15 points", new Vector2(-350, 220));
            Food(card.transform, "Cake", "Cake\n+20 points", new Vector2(-230, 220));
            Label(card.transform, "DODGE THE RUBBISH", new Vector2(305, 318), new Vector2(440, 40), 26, Cocoa);
            Food(card.transform, "Can", "Can\n−1 HP", new Vector2(245, 220));
            Food(card.transform, "BananaPeel", "Peel\n−1 HP", new Vector2(405, 220));
            Label(card.transform, "Rubbish gives no points. Catching it resets Combo.\nLet it fall past you safely.", new Vector2(320, 100), new Vector2(470, 80), 22, Cocoa);
            Label(card.transform, "BUILD YOUR COMBO", new Vector2(0, 35), new Vector2(500, 45), 28, Cocoa);
            string[] marks = { "D", "C", "B", "A", "S", "SS", "SSS" };
            string[] counts = { "0–1", "2", "3", "4", "5–7", "8–11", "12+" };
            Color[] colors = { new Color(.45f,.36f,.29f), new Color(.94f,.45f,.14f), new Color(.88f,.24f,.14f), new Color(.87f,.18f,.40f), new Color(.96f,.61f,.05f), new Color(.72f,.10f,.43f), new Color(.02f,.58f,.48f) };
            for (int i = 0; i < marks.Length; i++)
            {
                float x = -450 + i * 150;
                var badge = Panel(card.transform, "Rank " + marks[i], new Vector2(x, -38), new Vector2(125, 58), colors[i]);
                Label(badge.transform, marks[i], Vector2.zero, new Vector2(120, 54), marks[i].Length == 3 ? 25 : 31, Light);
                Label(card.transform, counts[i], new Vector2(x, -88), new Vector2(125, 30), 18, Cocoa);
            }
            Label(card.transform, "Catch food in a row to climb the ranks. Missing food or catching rubbish costs 1 HP and resets the streak to D.\nEarned score stays, and Combo never multiplies points.", new Vector2(0, -158), new Vector2(1050, 86), 22, Cocoa);
            Label(card.transform, "MOVE  A / ←     D / →    ·    SURVIVE 60 SECONDS    ·    EASY: NO RUBBISH", new Vector2(0, -255), new Vector2(1050, 50), 22, Cocoa);
            Label(card.transform, "Practice is free and does not save a score.", new Vector2(0, -310), new Vector2(1050, 38), 20, Cocoa);
            var close = Button(card.transform, "BACK TO PRACTICE", new Vector2(0, -400), new Vector2(470, 70), 28);
            close.name = "BACK TO PRACTICE";
            overlay.SetActive(false);
            return overlay;
        }

        private static void BindPractice(GameObject root, GameObject practice, GameObject rules)
        {
            var controller = root.AddComponent<MenuPracticeUI>();
            var serialized = new SerializedObject(controller);
            Set(serialized, "mascot", practice.transform.Find("Practice track/Practice mascot").GetComponent<RectTransform>());
            Set(serialized, "body", practice.transform.Find("Practice track/Practice mascot/Body").GetComponent<Image>());
            Set(serialized, "hat", practice.transform.Find("Practice track/Practice mascot/Hat").GetComponent<Image>());
            Set(serialized, "idle", Sprite("Assets/CPS/Art/Mascot/mascot-body-atlas-v1.png", "IdleA"));
            var frames = serialized.FindProperty("runFrames");
            frames.arraySize = 4;
            string[] names = { "RunA", "RunB", "RunC", "RunD" };
            for (int i = 0; i < names.Length; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = Sprite("Assets/CPS/Art/Mascot/mascot-body-atlas-v1.png", names[i]);
            Set(serialized, "rightHat", Sprite("Assets/CPS/Art/Mascot/engineer-hat-v1.png", "EngineerHat"));
            Set(serialized, "leftHat", Sprite("Assets/CPS/Art/Mascot/engineer-hat-left-v1.png", "EngineerHatLeft"));
            Set(serialized, "rulesOverlay", rules);
            Set(serialized, "rulesButton", practice.transform.Find("GAME RULES").GetComponent<Button>());
            Set(serialized, "closeRulesButton", rules.transform.Find("Rules card/BACK TO PRACTICE").GetComponent<Button>());
            serialized.FindProperty("leftLimit").floatValue = -145f;
            serialized.FindProperty("rightLimit").floatValue = 145f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var menu = new SerializedObject(root.GetComponent<MainMenuUI>());
            Set(menu, "howToPlayPanel", practice);
            menu.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ArrangeProfileManager(Transform root)
        {
            var manager = root.Find("Profile manager");
            if (manager == null) return;
            SetRect(manager, new Vector2(585, -90), new Vector2(560, 820));
            CopyPanelStyle(root.Find("Profile and difficulty"), manager);
        }

        private static void Food(Transform parent, string spriteName, string caption, Vector2 position)
        {
            Picture(parent, spriteName, position, new Vector2(105, 105), Sprite("Assets/CPS/Art/Items/items-atlas-v1.png", spriteName));
            Label(parent, caption, position + new Vector2(0, -75), new Vector2(145, 55), 20, Cocoa);
        }

        private static void Key(Transform parent, string caption, Vector2 position)
        {
            var key = Panel(parent, caption, position, new Vector2(180, 58), Light);
            Label(key.transform, caption, Vector2.zero, new Vector2(174, 52), 30, Cocoa);
        }

        private static Sprite Sprite(string path, string name) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First(sprite => sprite.name == name);

        private static void CopyPanelStyle(Transform source, Transform destination)
        {
            if (source == null || destination == null) return;
            var from = source.GetComponent<Image>();
            var to = destination.GetComponent<Image>();
            to.sprite = from.sprite;
            to.type = from.type;
            to.pixelsPerUnitMultiplier = from.pixelsPerUnitMultiplier;
        }

        private static GameObject Panel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = Rect(parent, name, position, size);
            go.AddComponent<Image>().color = color;
            return go;
        }

        private static Text Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var go = Rect(parent, text, position, size);
            var label = go.AddComponent<Text>();
            label.text = text;
            label.font = Font;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.supportRichText = false;
            label.raycastTarget = false;
            return label;
        }

        private static Image Picture(Transform parent, string name, Vector2 position, Vector2 size, Sprite sprite)
        {
            var go = Rect(parent, name, position, size);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static Button Button(Transform parent, string caption, Vector2 position, Vector2 size, int fontSize)
        {
            var go = Panel(parent, caption, position, size, Orange);
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            Label(go.transform, caption, Vector2.zero, size - new Vector2(12, 8), fontSize, Cocoa);
            return button;
        }

        private static GameObject Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetRect(go.transform, position, size);
            return go;
        }

        private static void SetRect(Transform transform, Vector2 position, Vector2 size)
        {
            var rect = transform.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Move(Transform root, string name, Vector2 position, Vector2 size, string replacement = null)
        {
            var target = root.GetComponentsInChildren<Transform>(true).First(item => item.name == name);
            SetRect(target, position, size);
            if (replacement != null)
            {
                target.name = replacement;
                target.GetComponent<Text>().text = replacement;
            }
        }

        private static void Move(Component component, Vector2 position, Vector2 size) => SetRect(component.transform, position, size);

        private static void MoveText(Transform root, string prefix, Vector2 position, Vector2 size, string replacement)
        {
            var target = root.GetComponentsInChildren<Text>(true).First(item =>
                item.text.StartsWith(prefix, StringComparison.Ordinal) || item.text.StartsWith(replacement, StringComparison.Ordinal));
            SetRect(target.transform, position, size);
            target.text = replacement;
            target.gameObject.name = replacement;
        }

        private static void SetButtonText(Button button, string text)
        {
            var label = button.GetComponentInChildren<Text>(true);
            label.text = text;
            label.gameObject.name = text;
        }

        private static void Remove(Transform root, string name)
        {
            var child = root.Find(name);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }

        private static void Set(SerializedObject target, string property, Object value) => target.FindProperty(property).objectReferenceValue = value;
    }
}
