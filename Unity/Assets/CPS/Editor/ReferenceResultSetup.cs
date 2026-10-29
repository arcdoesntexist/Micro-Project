using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CPS.KitchenCatch.Editor
{
    public static class ReferenceResultSetup
    {
        private static readonly Color Ink=new Color(.32f,.17f,.10f);
        private static Font Font=>AssetDatabase.LoadAssetAtPath<Font>("Assets/CPS/Fonts/FFSpokenTrial-Regular.ttf");
        private static Sprite Art(string name)=>AssetDatabase.LoadAllAssetsAtPath("Assets/CPS/Art/UI/home-ui-atlas-v1.png").OfType<Sprite>().First(s=>s.name==name);
        [MenuItem("CPS/Apply Reference Results")]
        public static void Apply()
        {
            string[] items={"Apple","Bread","Cake","Can","BananaPeel"};
            for(int i=0;i<items.Length;i++)
            {var def=AssetDatabase.LoadAssetAtPath<FallingObjectDefinition>("Assets/CPS/Config/"+items[i]+".asset");def.itemIndex=i;EditorUtility.SetDirty(def);}
            const string path="Assets/CPS/Prefabs/GameplayCanvas.prefab";
            var canvas=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var panel=canvas.GetComponentInChildren<ResultPanel>(true);
                var root=panel.transform;
                var previous=root.Find("Reference result");
                if(previous!=null)Object.DestroyImmediate(previous.gameObject);
                foreach(Transform child in root)child.gameObject.SetActive(false);
                var rootRect=(RectTransform)root;rootRect.anchorMin=Vector2.zero;rootRect.anchorMax=Vector2.one;rootRect.offsetMin=rootRect.offsetMax=Vector2.zero;rootRect.localScale=Vector3.one;
                var dim=root.GetComponent<Image>()??root.gameObject.AddComponent<Image>();dim.sprite=null;dim.color=new Color(.2f,.13f,.09f,.55f);dim.raycastTarget=true;
                var container=Node(root,"Reference result",0,0,1600,900);
                var main=Node(container,"Summary",1,0,820,816);Skin(main,"CreamPanel");
                var details=Node(container,"Details",430,0,600,816);Skin(details,"CreamPanel");
                var view=root.GetComponent<ResultReferenceView>()??root.gameObject.AddComponent<ResultReferenceView>();
                view.summary=(RectTransform)main;view.details=(RectTransform)details;
                view.closedPosition=new Vector2(1,0)*1.2f;view.openPosition=new Vector2(-320,0)*1.2f;
                var name=Text(main,"Identity","arc",-131,302,300,42,23);name.alignment=TextAnchor.MiddleLeft;
                name.resizeTextForBestFit=true;name.resizeTextMinSize=18;name.resizeTextMaxSize=28;
                Text(main,"Outcome","Triumph!",0,268,650,70,54);
                Text(main,"Message","A delicious minute in the kitchen.",0,211,650,42,22);
                var score=Text(main,"Score","215",-150,130,250,95,77);score.resizeTextForBestFit=true;score.resizeTextMinSize=40;score.resizeTextMaxSize=92;
                Text(main,"Score caption","FINAL SCORE",-150,66,250,32,17);
                var rank=Node(main,"Rank badge",93,120,180,145);Image(rank,new Color(.58f,.35f,.06f));rank.localRotation=Quaternion.Euler(0,0,3);
                Text(main,"Rank","S",93,150,170,65,47);
                Text(main,"Rank name","Sizzling!",93,107,180,33,20);
                Text(main,"Rank caption","HIGHEST RANK",93,78,170,24,12);
                foreach(string child in new[]{"Rank","Rank name","Rank caption"})main.Find(child).GetComponent<Text>().color=new Color(1,.97f,.87f);
                Text(main,"Combo","Best streak  7  catches in a row",0,18,660,45,23);
                var foodTitle=Text(main,"Food heading","On the menu",-195,-39,300,42,27);foodTitle.alignment=TextAnchor.MiddleLeft;
                for(int i=0;i<5;i++)
                {
                    float x=i<3?-290+i*220:-290+(i-3)*270;float y=i<3?-111:-226;float size=i<3?74:50;
                    var image=Node(main,"Item "+i,x,y,size,size);var graphic=Image(image,Color.white);graphic.sprite=AssetDatabase.LoadAssetAtPath<FallingObjectDefinition>("Assets/CPS/Config/"+items[i]+".asset").sprite;graphic.preserveAspect=true;
                    Text(main,"Count "+i,"x 0",x+80,y+12,108,40,i<3?29:24);
                    Text(main,"Name "+i,i==4?"Banana peel":items[i],x+80,y-19,125,32,18);
                }
                Image(Node(main,"Trash divider",0,-171,660,1),new Color(.78f,.66f,.47f));
                var trash=Text(main,"Trash heading","Not on the menu   Trash eaten",-100,-193,490,32,22);trash.alignment=TextAnchor.MiddleLeft;
                var restart=Button(main,"Restart","Restart",-168,-310,322,76,true);
                var menu=Button(main,"Menu","Main Menu",168,-310,322,76,false);
                var toggle=Button(main,"Toggle","Round details  +",-1,-373,245,42,true);toggle.GetComponent<Image>().sprite=null;toggle.GetComponent<Image>().color=new Color(.94f,.72f,.41f);
                view.toggle=toggle;
                string[] levels={"EASY","NORMAL","HARD"};Color[] starColors={new Color(.55f,.78f,.36f),new Color(1,.83f,.35f),new Color(1,.54f,.34f)};
                for(int i=0;i<3;i++)
                {
                    var badge=Node(main,"Difficulty "+i,260,126,165,91);Skin(badge,"CocoaButton");
                    Text(badge,"Name",levels[i],0,-20,150,30,20).color=new Color(1,.97f,.87f);
                    for(int j=0;j<=i;j++){var star=Node(badge,"Star "+j,(j-i*.5f)*32,18,31,31);Skin(star,"Star");star.GetComponent<Image>().color=starColors[i];}
                }
                Text(details,"Title","A closer look",-18,300,420,52,34);
                view.close=Button(details,"Close","X",210,315,42,42,false);
                Image(Node(details,"Round highlight",0,252,450,36),new Color(.93f,.84f,.68f));
                Text(details,"Round heading","This round",0,252,430,36,25).alignment=TextAnchor.MiddleLeft;
                var round=Text(details,"Round stats","",0,156,450,148,20);round.alignment=TextAnchor.UpperLeft;round.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");round.lineSpacing=1.1f;
                Text(details,"Hands heading","Your hands",0,55,450,40,25).alignment=TextAnchor.MiddleLeft;
                Text(details,"Input source","Keyboard - strength 0-100",0,19,450,30,17).alignment=TextAnchor.MiddleLeft;
                foreach(string side in new[]{"Left","Right"}){var t=Text(details,side,"",side=="Left"?-118:118,-65,218,135,19);t.alignment=TextAnchor.UpperLeft;t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.lineSpacing=1.15f;}
                var balance=Text(details,"Balance","",0,-163,450,70,18);balance.alignment=TextAnchor.UpperLeft;balance.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                Text(details,"Force heading","Force (N)",0,-216,450,35,25).alignment=TextAnchor.MiddleLeft;
                var force=Text(details,"Force","",0,-293,450,117,18);force.alignment=TextAnchor.UpperLeft;force.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");force.lineSpacing=1.1f;
                var serialized=new SerializedObject(panel);serialized.FindProperty("restartButton").objectReferenceValue=restart;serialized.FindProperty("menuButton").objectReferenceValue=menu;serialized.ApplyModifiedPropertiesWithoutUndo();
                details.gameObject.SetActive(false);root.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(canvas,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(canvas);}
            AssetDatabase.SaveAssets();
        }
        private static Transform Node(Transform parent,string name,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y)*1.2f;r.sizeDelta=new Vector2(w,h)*1.2f;return r;
        }
        private static Image Image(Transform t,Color color){var image=t.GetComponent<Image>()??t.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
        private static void Skin(Transform t,string sprite){var image=Image(t,Color.white);image.sprite=Art(sprite);image.type=UnityEngine.UI.Image.Type.Simple;}
        private static Text Text(Transform parent,string name,string value,float x,float y,float w,float h,int size)
        {var t=Node(parent,name,x,y,w,h).gameObject.AddComponent<Text>();t.text=value;t.font=Font;t.fontSize=Mathf.RoundToInt(size*1.2f);t.color=Ink;t.alignment=TextAnchor.MiddleCenter;t.supportRichText=false;t.raycastTarget=false;return t;}
        private static Button Button(Transform parent,string name,string caption,float x,float y,float w,float h,bool orange)
        {
            var t=Node(parent,name,x,y,w,h);Skin(t,orange?"OrangeButton":"CocoaButton");t.GetComponent<Image>().raycastTarget=true;
            var button=t.gameObject.AddComponent<Button>();button.targetGraphic=t.GetComponent<Image>();var colors=button.colors;colors.selectedColor=Color.white;colors.highlightedColor=new Color(1,.95f,.85f);button.colors=colors;
            var label=Text(t,"Label",caption,0,0,w-20,h-10,h<50?22:25);label.color=orange?Ink:new Color(1,.97f,.87f);
            var outline=t.gameObject.AddComponent<Outline>();outline.effectColor=new Color(1,.86f,.45f,.9f);outline.effectDistance=new Vector2(2,-2);outline.enabled=false;t.gameObject.AddComponent<ButtonFocusOutline>();
            return button;
        }
    }
}
