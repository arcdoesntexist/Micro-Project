using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace CPS.KitchenCatch.Editor
{
    public static class ControllerUISetup
    {
        private const string Home = "Assets/CPS/Prefabs/MainMenuCanvas.prefab";
        private const string Gameplay = "Assets/CPS/Prefabs/GameplayCanvas.prefab";
        private static readonly Color Cocoa = new Color(.25f,.12f,.07f);
        private static Font Font => AssetDatabase.LoadAssetAtPath<Font>("Assets/CPS/Fonts/FFSpokenTrial-Regular.ttf");
        private static Sprite Art(string name) => AssetDatabase.LoadAllAssetsAtPath("Assets/CPS/Art/UI/home-ui-atlas-v1.png").OfType<Sprite>().First(sprite => sprite.name == name);
        [MenuItem("CPS/Apply USB Controllers")]
        public static void Apply()
        {
            if (AssetDatabase.LoadAssetAtPath<ControllerSettings>("Assets/CPS/Resources/ControllerSettings.asset") == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ControllerSettings>(), "Assets/CPS/Resources/ControllerSettings.asset");
            var homeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Home);
            var menuData = new SerializedObject(homeAsset.GetComponent<MainMenuUI>());
            var dropdownTemplate = (Dropdown)menuData.FindProperty("profileDropdown").objectReferenceValue;
            ApplyCanvas(Home, dropdownTemplate, false);
            ApplyCanvas(Gameplay, dropdownTemplate, true);
            AssetDatabase.SaveAssets();
        }
        private static void ApplyCanvas(string path, Dropdown template, bool gameplay)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var panel = root.GetComponent<ControllerPanel>() ?? root.AddComponent<ControllerPanel>();
                var data = new SerializedObject(panel);
                if (root.transform.Find("Controller panel") == null)
                {
                    Transform launchParent = root.transform;
                    SerializedObject ui = null;
                    if (gameplay)
                    {
                        ui = new SerializedObject(root.GetComponent<GameplayUI>());
                        launchParent = ((GameObject)ui.FindProperty("pauseOverlay").objectReferenceValue).transform;
                    }
                    var launcher = Button(launchParent,"Controller launcher","Controller",gameplay ? 450 : -260,gameplay ? 40 : -75,gameplay ? 220 : 300,64);
                    if (gameplay) launcher.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                        selectOnLeft = (Button)ui.FindProperty("resumeButton").objectReferenceValue,
                        selectOnDown = (Button)ui.FindProperty("settingsButton").objectReferenceValue };
                    if (!gameplay)
                    {
                        var rect = (RectTransform)launcher.transform; rect.anchorMin = rect.anchorMax = new Vector2(1,1);
                    }
                    var overlay = Rect(root.transform,"Controller panel",0,0,0,0);
                    overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = overlay.offsetMax = Vector2.zero;
                    var shade = overlay.gameObject.AddComponent<Image>(); shade.color = new Color(.17f,.10f,.07f,.78f);
                    var card = Rect(overlay,"Controller card",0,0,820,710); Skin(card.gameObject.AddComponent<Image>(),"CreamPanel");
                    Label(card,"Heading","Controller",0,285,720,65,46);
                    var modes = new[] { Button(card,"Keyboard","Keyboard",-245,205,210,62),Button(card,"FSR","FSR",0,205,210,62),Button(card,"Load Cell","Load Cell",245,205,210,62) };
                    var state = Label(card,"Connection state","Choose a controller",0,120,700,85,28);
                    var left = Label(card,"Left live","LEFT\nN/A",-180,25,330,90,28);
                    var right = Label(card,"Right live","RIGHT\nN/A",180,25,330,90,28);
                    var dropdown = Object.Instantiate(template.gameObject,card,false).GetComponent<Dropdown>(); dropdown.name = "Serial ports";
                    Place((RectTransform)dropdown.transform,-100,-70,440,55);
                    for(int i=dropdown.onValueChanged.GetPersistentEventCount()-1;i>=0;i--) UnityEventTools.RemovePersistentListener(dropdown.onValueChanged,i);
                    dropdown.onValueChanged.RemoveAllListeners(); dropdown.ClearOptions(); dropdown.AddOptions(new System.Collections.Generic.List<string>{"Refresh ports"});
                    dropdown.captionText.fontSize = 24; dropdown.itemText.fontSize = 24;
                    var refresh = Button(card,"Refresh ports","Refresh",260,-70,160,55);
                    var connect = Button(card,"Connect USB","Connect USB",-245,-150,210,65,true);
                    var disconnect = Button(card,"Disconnect","Disconnect",0,-150,210,65);
                    var tare = Button(card,"Tare","Tare both",245,-150,210,65);
                    var guide = Label(card,"Guide","",0,-225,700,72,24);
                    var close = Button(card,"Close","Back",0,-300,300,62,true);
                    Set(data,"panelRoot",overlay.gameObject); Set(data,"launcher",launcher); Set(data,"launcherCaption",launcher.GetComponentInChildren<Text>());
                    var array = data.FindProperty("modeButtons"); array.arraySize=3; for(int i=0;i<3;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=modes[i];
                    Set(data,"stateText",state);Set(data,"leftValue",left);Set(data,"rightValue",right);Set(data,"guide",guide);Set(data,"ports",dropdown);
                    Set(data,"refresh",refresh);Set(data,"connect",connect);Set(data,"disconnect",disconnect);Set(data,"tare",tare);Set(data,"close",close);
                    data.FindProperty("compactLauncher").boolValue=gameplay; data.ApplyModifiedPropertiesWithoutUndo();
                    if (gameplay)
                    {
                        var hotkeys = root.GetComponent<PauseHotkeys>() ?? root.AddComponent<PauseHotkeys>();
                        Set(ui,"controllerPanel",panel);Set(ui,"controllerButton",launcher);Set(ui,"pauseHotkeys",hotkeys);
                        Set(ui,"controllerNotice",Label(launchParent,"Controller notice","Space / Esc\nPause or resume",450,-65,250,100,25));
                        ui.ApplyModifiedPropertiesWithoutUndo();
                    }
                    else
                    {
                        var menu = new SerializedObject(root.GetComponent<MainMenuUI>()); Set(menu,"controllerPanel",panel);Set(menu,"controllerButton",launcher);menu.ApplyModifiedPropertiesWithoutUndo();
                        var practice = new SerializedObject(root.GetComponent<MenuPracticeUI>());
                        var blockers=practice.FindProperty("inputBlockers");int count=blockers.arraySize;blockers.arraySize=count+1;blockers.GetArrayElementAtIndex(count).objectReferenceValue=overlay.gameObject;
                        practice.ApplyModifiedPropertiesWithoutUndo();
                        var settingsButton=(Button)menu.FindProperty("settingsButton").objectReferenceValue;
                        var nav=settingsButton.navigation;nav.selectOnLeft=launcher;settingsButton.navigation=nav;
                    }
                    overlay.gameObject.SetActive(false);
                }
                if (gameplay)
                {
                    var ui = new SerializedObject(root.GetComponent<GameplayUI>());
                    var launcher = (Button)ui.FindProperty("controllerButton").objectReferenceValue;
                    launcher.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                        selectOnLeft = (Button)ui.FindProperty("resumeButton").objectReferenceValue,
                        selectOnDown = (Button)ui.FindProperty("settingsButton").objectReferenceValue };
                    foreach (string name in new[] { "resumeButton", "settingsButton", "backToMenuButton" })
                    {
                        var button = (Button)ui.FindProperty(name).objectReferenceValue;
                        var nav = button.navigation; nav.selectOnRight = launcher; button.navigation = nav;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void Set(SerializedObject data,string name,Object value)=>data.FindProperty(name).objectReferenceValue=value;
        private static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
        {
            var go=new GameObject(name,typeof(RectTransform));var rect=(RectTransform)go.transform;rect.SetParent(parent,false);Place(rect,x,y,width,height);return rect;
        }
        private static void Place(RectTransform rect,float x,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(width,height);
        }
        private static void Skin(Image image,string sprite) {image.sprite=Art(sprite);image.type=Image.Type.Sliced;image.color=Color.white;}
        private static Text Label(Transform parent,string name,string text,float x,float y,float width,float height,int size)
        {
            var rect=Rect(parent,name,x,y,width,height);var label=rect.gameObject.AddComponent<Text>();label.text=text;label.font=Font;label.fontSize=size;
            label.color=Cocoa;label.alignment=TextAnchor.MiddleCenter;label.supportRichText=false;label.raycastTarget=false;return label;
        }
        private static Button Button(Transform parent,string name,string text,float x,float y,float width,float height,bool orange=false)
        {
            var rect=Rect(parent,name,x,y,width,height);var image=rect.gameObject.AddComponent<Image>();Skin(image,orange?"OrangeButton":"Input");
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=Color.white;colors.selectedColor=Color.white;colors.highlightedColor=new Color(1,.94f,.82f);colors.pressedColor=new Color(.86f,.72f,.56f);button.colors=colors;
            Label(rect,"Label",text,0,0,width-20,height-8,26);return button;
        }
    }
}
