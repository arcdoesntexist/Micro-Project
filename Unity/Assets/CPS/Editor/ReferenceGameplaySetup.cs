using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Editor
{
    public static class ReferenceGameplaySetup
    {
        private const string Path = "Assets/CPS/Prefabs/GameplayCanvas.prefab";
        private static readonly Color Cocoa = new Color(.32f,.17f,.10f);
        private static readonly Color Cream = new Color(1f,.96f,.85f);
        private static Font Font => AssetDatabase.LoadAssetAtPath<Font>("Assets/CPS/Fonts/FFSpokenTrial-Regular.ttf");
        private static Sprite Art(string name) => AssetDatabase.LoadAllAssetsAtPath("Assets/CPS/Art/UI/home-ui-atlas-v1.png")
            .OfType<Sprite>().First(s => s.name == name);

        [MenuItem("CPS/Apply Reference Gameplay")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(Path);
            try
            {
                var ui = new SerializedObject(root.GetComponent<GameplayUI>());
                ui.FindProperty("referenceHud").boolValue = true;
                foreach (var text in root.GetComponentsInChildren<Text>(true))
                    if (!text.transform.IsChildOf(root.transform.Find("ResultPanel")) &&
                        !text.transform.IsChildOf(root.transform.Find("Settings panel")))
                    { text.font = Font; text.raycastTarget = false; }

                var player = root.transform.Find("Player"); Place(player,35,28,375,110); Skin(player,"Input");
                var profile = Ref<Text>(ui,"profile"); Label(profile,32,Cocoa); Local(profile.transform,-86,0,150,55);
                profile.resizeTextForBestFit = true; profile.resizeTextMinSize=21; profile.resizeTextMaxSize=38;
                Ref<Text>(ui,"hp").gameObject.SetActive(false);
                var hearts = ui.FindProperty("heartImages"); hearts.arraySize=3;
                for(int i=0;i<3;i++)
                {
                    var heart = Ensure(player,"Reference heart " + i);
                    Local(heart,22+i*43,0,35,36);
                    if (heart.GetComponent<CanvasRenderer>() == null) heart.gameObject.AddComponent<CanvasRenderer>();
                    var graphic = heart.GetComponent<HeartIcon>() ?? heart.gameObject.AddComponent<HeartIcon>();
                    graphic.color = new Color(.83f,.29f,.21f); graphic.raycastTarget=false;
                    hearts.GetArrayElementAtIndex(i).objectReferenceValue=graphic;
                }
                var timer=root.transform.Find("Timer");Place(timer,660,28,280,110);Skin(timer,"Input");
                var time=Ref<Text>(ui,"time");Label(time,45,Cocoa);Local(time.transform,18,0,180,70);
                var clock=Text(timer,"Clock","",22,Cocoa); clock.gameObject.SetActive(false);
                var clockIcon=Ensure(timer,"Clock icon");Local(clockIcon,-90,0,32,32);
                var circle=Image(clockIcon);circle.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");circle.color=Cocoa;
                var clockFace=Ensure(clockIcon,"Face");Local(clockFace,0,0,25,25);Image(clockFace).sprite=circle.sprite;Image(clockFace).color=Cream;
                Bar(clockIcon,"Hour",1,5,3,12,Cocoa);Bar(clockIcon,"Minute",5,0,10,3,Cocoa);

                var scorePanel=root.transform.Find("Score");Place(scorePanel,1160,28,290,110);Skin(scorePanel,"Input");
                var score=Ref<Text>(ui,"score");Label(score,40,Cocoa);Local(score.transform,35,12,150,50);score.alignment=TextAnchor.MiddleRight;
                score.resizeTextForBestFit=true;score.resizeTextMinSize=26;score.resizeTextMaxSize=48;
                var caption=Text(scorePanel,"Score caption","SCORE",24,Cocoa);Local(caption.transform,-72,12,80,40);
                var difficulty=Ref<Text>(ui,"difficulty");Label(difficulty,19,Cocoa);Local(difficulty.transform,35,-22,150,28);difficulty.alignment=TextAnchor.MiddleRight;
                var pause=Ref<Button>(ui,"pauseButton");Place(pause.transform,1464,36,96,96);Button(pause,"OrangeButton","",28);
                Bar(pause.transform,"Pause stroke L",-10,0,7,34,Cocoa);Bar(pause.transform,"Pause stroke R",10,0,7,34,Cocoa);

                var badge=root.transform.Find("Rank badge");Place(badge,1200,160,350,112);
                badge.localRotation=Quaternion.Euler(0,0,2);
                var rank=Ref<Text>(ui,"rank");Label(rank,25,Cream);Local(rank.transform,40,22,225,38);
                var letter=Ref<Text>(ui,"rankLetter");Label(letter,73,Cream);Local(letter.transform,-112,0,108,90);
                foreach(var outline in badge.GetComponentsInChildren<Outline>(true)) Object.DestroyImmediate(outline);
                var streak=Text(badge,"Reference streak","0 IN A ROW",17,Cream);Local(streak.transform,40,-9,225,28);Bind(ui,"streakLabel",streak);
                Bind(ui,"streakFill",Fill(badge,"Reference combo fill",40,-35,215,6));
                var accents=ui.FindProperty("rankAccents");
                for(int i=0;i<accents.arraySize;i++)
                {var accent=accents.GetArrayElementAtIndex(i).objectReferenceValue as Image;if(accent!=null)accent.gameObject.SetActive(false);}
                accents.arraySize=0;
                Place(root.transform.Find("Rank callout"),1160,288,390,64);
                Label(Ref<Text>(ui,"rankCallout"),23,Cream);Local(Ref<Text>(ui,"rankCallout").transform,0,0,365,54);

                string[] panels={"Left input","Right input"};string[] labels={"leftHand","rightHand"};string[] fills={"leftFill","rightFill"};
                for(int i=0;i<2;i++)
                {
                    var panel=root.transform.Find(panels[i]);Place(panel,i==0?38:1262,787,300,95);Skin(panel,"Input");
                    Label(Ref<Text>(ui,labels[i]),28,Cocoa);Local(Ref<Text>(ui,labels[i]).transform,0,7,260,50);
                    Bind(ui,fills[i],Fill(panel,"Reference strength fill",0,-24,240,7));
                }
                Ref<Text>(ui,"inputSource").gameObject.SetActive(false);

                var overlay=Ref<GameObject>(ui,"pauseOverlay").transform;
                var overlayRect=(RectTransform)overlay;overlayRect.anchorMin=Vector2.zero;overlayRect.anchorMax=Vector2.one;
                overlayRect.offsetMin=overlayRect.offsetMax=Vector2.zero;
                Image(overlay).color=new Color(.20f,.13f,.09f,.66f);Image(overlay).raycastTarget=true;
                var card=overlay.Find("Pause card");Local(card,0,0,610,620);Skin(card,"CreamPanel");
                var title=overlay.Find("PAUSED").GetComponent<Text>();title.text="A little breather?";Label(title,43,Cocoa);Local(title.transform,0,220,510,60);
                var subtitle=Text(overlay,"Pause subtitle","The kitchen can wait.",25,Cocoa);Local(subtitle.transform,0,170,480,40);
                var resume=Ref<Button>(ui,"resumeButton");Local(resume.transform,0,80,480,88);Button(resume,"OrangeButton","Keep cooking",30);
                var settings=Ref<Button>(ui,"settingsButton");Local(settings.transform,0,-16,480,88);Button(settings,"CocoaButton","Settings",30);
                var back=Ref<Button>(ui,"backToMenuButton");Local(back.transform,0,-112,480,88);Button(back,"CocoaButton","Back to Main Menu",28);
                var notice=Text(overlay,"Discard notice","This round's score won't be saved.",19,Cocoa);Local(notice.transform,0,-197,500,40);
                var buttons=new[]{resume,settings,back};
                for(int i=0;i<buttons.Length;i++) buttons[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,
                    selectOnUp=buttons[(i+2)%3],selectOnDown=buttons[(i+1)%3]};
                overlay.gameObject.SetActive(false);
                ui.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,Path);
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
        }
        private static T Ref<T>(SerializedObject ui,string name) where T:Object => (T)ui.FindProperty(name).objectReferenceValue;
        private static void Bind(SerializedObject ui,string name,Object value)=>ui.FindProperty(name).objectReferenceValue=value;
        private static Transform Ensure(Transform parent,string name)
        {var child=parent.Find(name);if(child!=null)return child;var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return go.transform;}
        private static Image Image(Transform t)
        {var image=t.GetComponent<Image>()??t.gameObject.AddComponent<Image>();image.raycastTarget=false;return image;}
        private static void Skin(Transform t,string sprite)
        {var image=Image(t);image.sprite=Art(sprite);image.type=UnityEngine.UI.Image.Type.Simple;image.pixelsPerUnitMultiplier=1;image.color=Color.white;}
        private static void Place(Transform t,float x,float y,float w,float h)
        {var r=(RectTransform)t;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x+w/2-800,450-y-h/2)*1.2f;r.sizeDelta=new Vector2(w,h)*1.2f;r.localScale=Vector3.one;}
        private static void Local(Transform t,float x,float y,float w,float h)
        {var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y)*1.2f;r.sizeDelta=new Vector2(w,h)*1.2f;r.localScale=Vector3.one;}
        private static void Label(Text text,int size,Color color)
        {text.font=Font;text.fontSize=Mathf.RoundToInt(size*1.2f);text.color=color;text.alignment=TextAnchor.MiddleCenter;text.supportRichText=false;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;}
        private static Text Text(Transform parent,string name,string value,int size,Color color)
        {var t=Ensure(parent,name);var label=t.GetComponent<Text>()??t.gameObject.AddComponent<Text>();label.text=value;Label(label,size,color);return label;}
        private static void Button(Button button,string sprite,string caption,int size)
        {Skin(button.transform,sprite);button.GetComponent<Image>().raycastTarget=true;var label=button.GetComponentInChildren<Text>(true);label.gameObject.SetActive(true);label.text=caption;Label(label,size,sprite=="OrangeButton"?Cocoa:Cream);var r=label.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(20,8);r.offsetMax=new Vector2(-20,-8);
            var colors=ColorBlock.defaultColorBlock;colors.highlightedColor=new Color(1,.95f,.85f);colors.selectedColor=Color.white;button.colors=colors;
            var outline=button.GetComponent<Outline>()??button.gameObject.AddComponent<Outline>();
            outline.effectColor=new Color(1f,.86f,.45f,.9f);outline.effectDistance=new Vector2(2,-2);outline.enabled=false;
            if(button.GetComponent<ButtonFocusOutline>()==null)button.gameObject.AddComponent<ButtonFocusOutline>();}
        private static Image Bar(Transform parent,string name,float x,float y,float w,float h,Color color)
        {var t=Ensure(parent,name);Local(t,x,y,w,h);var image=Image(t);image.color=color;return image;}
        private static Image Fill(Transform parent,string name,float x,float y,float w,float h)
        {Bar(parent,name+" track",x,y,w,h,new Color(.73f,.56f,.37f));var fill=Bar(parent,name,x,y,w,h,new Color(.67f,.28f,.12f));fill.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;fill.fillAmount=0;return fill;}
    }
}
