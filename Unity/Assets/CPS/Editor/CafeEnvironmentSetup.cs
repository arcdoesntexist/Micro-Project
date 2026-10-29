using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CPS.KitchenCatch.Editor
{
    public static class CafeEnvironmentSetup
    {
        private const string Art="Assets/CPS/Art/Cafe/";
        private static readonly string[] Names={"Curtain","PendantLamp","HangingIvy","CoffeeCup","PottedFern","Cookbooks"};
        private static readonly Rect[] Rects={new Rect(83,461,377,527),new Rect(617,496,303,484),new Rect(1104,401,369,586),new Rect(91,83,324,212),new Rect(563,58,400,362),new Rect(1063,66,441,242)};
        [MenuItem("CPS/Apply Cafe Environments")]
        public static void Apply()
        {
            ImportBackground("cafe-day-background-v1.png");ImportBackground("cafe-night-background-v1.png");ImportProps();
            var scene=EditorSceneManager.OpenScene("Assets/CPS/Scenes/Gameplay.unity",OpenSceneMode.Single);
            var roots=scene.GetRootGameObjects();
            var background=roots.First(x=>x.name=="KitchenBackground").GetComponent<SpriteRenderer>();
            var kitchen=roots.First(x=>x.name=="KitchenAmbient");
            var cafe=roots.FirstOrDefault(x=>x.name=="CafeAmbient")??new GameObject("CafeAmbient");
            SceneManager.MoveGameObjectToScene(cafe,scene);
            var props=new SpriteRenderer[Names.Length];
            Vector2[] positions={new Vector2(140,20),new Vector2(1455,155),new Vector2(1580,235),new Vector2(1260,608),new Vector2(45,590),new Vector2(165,590)};
            float[] scales={1.55f,.8f,.8f,.35f,.42f,.5f};
            var sprites=AssetDatabase.LoadAllAssetsAtPath(Art+"cafe-props-atlas-v1.png").OfType<Sprite>().ToDictionary(s=>s.name);
            for(int i=0;i<Names.Length;i++)
            {
                var child=cafe.transform.Find(Names[i]);
                if(child==null){child=new GameObject(Names[i],typeof(SpriteRenderer)).transform;child.SetParent(cafe.transform,false);}
                props[i]=child.GetComponent<SpriteRenderer>();props[i].sprite=sprites[Names[i]];
                props[i].sharedMaterial=background.sharedMaterial;props[i].sortingOrder=-35;
                child.localPosition=new Vector3((positions[i].x-836f)/104.5f,(470.5f-positions[i].y)/104.5f,0);
                child.localScale=Vector3.one*scales[i];child.localRotation=Quaternion.identity;
            }
            var environmentRoot=roots.FirstOrDefault(x=>x.name=="LevelEnvironment")??new GameObject("LevelEnvironment");
            SceneManager.MoveGameObjectToScene(environmentRoot,scene);
            var environment=environmentRoot.GetComponent<LevelEnvironment>()??environmentRoot.AddComponent<LevelEnvironment>();
            var data=new SerializedObject(environment);
            Set(data,"background",background);Set(data,"kitchenSprite",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CPS/Art/Kitchen/kitchen-background-v1.png"));
            Set(data,"daySprite",AssetDatabase.LoadAssetAtPath<Sprite>(Art+"cafe-day-background-v1.png"));
            Set(data,"nightSprite",AssetDatabase.LoadAssetAtPath<Sprite>(Art+"cafe-night-background-v1.png"));
            Set(data,"kitchenAmbient",kitchen);Set(data,"cafeAmbient",cafe);Set(data,"pendant",props[1]);
            data.FindProperty("kitchenTint").colorValue=new Color(.91f,.88f,.84f,1);
            var sway=data.FindProperty("swayingProps");sway.arraySize=3;
            for(int i=0;i<3;i++)sway.GetArrayElementAtIndex(i).objectReferenceValue=props[i].transform;
            var renderers=data.FindProperty("cafeProps");renderers.arraySize=props.Length;
            for(int i=0;i<props.Length;i++)renderers.GetArrayElementAtIndex(i).objectReferenceValue=props[i];
            var flow=roots.SelectMany(x=>x.GetComponentsInChildren<GameSessionController>(true)).First();
            Set(data,"controller",flow);data.ApplyModifiedPropertiesWithoutUndo();
            var rig=PrefabUtility.GetOutermostPrefabInstanceRoot(flow);
            var sourceFlow=PrefabUtility.GetCorrespondingObjectFromSource(flow);
            var modifications=PrefabUtility.GetPropertyModifications(rig);
            if(modifications!=null)PrefabUtility.SetPropertyModifications(rig,modifications.Where(m=>!(m.target==sourceFlow && m.propertyPath=="environment")).ToArray());
            environment.Apply(DifficultyId.Easy);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        private static void Set(SerializedObject obj,string field,Object value)=>obj.FindProperty(field).objectReferenceValue=value;
        private static void ImportBackground(string name)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Art+name);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=104.5f;importer.maxTextureSize=2048;importer.mipmapEnabled=false;
            importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency=false;importer.SaveAndReimport();
        }
        private static void ImportProps()
        {
            var path=Art+"cafe-props-atlas-v1.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit=200;importer.maxTextureSize=2048;importer.mipmapEnabled=false;
            importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency=true;importer.SaveAndReimport();
            var factories=new SpriteDataProviderFactories();factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
            var existing=provider.GetSpriteRects().ToDictionary(s=>s.name);
            var slices=Names.Select((name,i)=>new SpriteRect{name=name,rect=Rects[i],alignment=SpriteAlignment.Custom,pivot=new Vector2(.5f,i<3?1:0),spriteID=existing.TryGetValue(name,out var old)?old.spriteID:GUID.Generate()}).ToArray();
            provider.SetSpriteRects(slices);provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(slices.Select(s=>new SpriteNameFileIdPair(s.name,s.spriteID)));
            provider.Apply();importer.SaveAndReimport();
        }
    }
}
