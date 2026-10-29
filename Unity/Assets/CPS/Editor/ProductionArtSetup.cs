using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Editor
{
    public static class ProductionArtSetup
    {
        private const string Art = "Assets/CPS/Art/";
        private const string Prefabs = "Assets/CPS/Prefabs/";
        private const string Animations = "Assets/CPS/Animation/Mascot/";
        private const string BodyPath = Art + "Mascot/mascot-body-atlas-v1.png";
        private const string HatPath = Art + "Mascot/engineer-hat-v1.png";
        private const string LeftHatPath = Art + "Mascot/engineer-hat-left-v1.png";
        private const string ItemPath = Art + "Items/items-atlas-v1.png";
        private const string BackgroundPath = Art + "Kitchen/kitchen-background-v1.png";
        private const string AmbientPath = Art + "Kitchen/ambient-atlas-v1.png";
        private const string PanelPath = Art + "UI/ui-panel-v1.png";
        private static readonly string[] BodyNames =
        {
            "IdleA", "IdleBlink", "RunA", "RunB", "RunC", "RunD",
            "EatOpen", "EatChomp", "Hurt", "DeadXX", "TriumphUp", "TriumphLand"
        };
        private static readonly string[] ItemNames = { "Apple", "Bread", "Cake", "Can", "BananaPeel" };
        private static readonly string[] AmbientNames = { "Ivy", "Whisk", "Ladle" };

        [MenuItem("CPS/Author Production Artwork")]
        public static void Author()
        {
            EnsureFolder("Assets/CPS", "Animation");
            EnsureFolder("Assets/CPS/Animation", "Mascot");
            ImportGrid(BodyPath, 4, 3, 180, BodyNames, true);
            ImportGrid(ItemPath, 3, 2, 350, ItemNames, false);
            ImportGrid(AmbientPath, 3, 1, 500, AmbientNames, false);
            ImportSingleCropped(HatPath, "EngineerHat", 1000, Vector4.zero);
            ImportSingleCropped(LeftHatPath, "EngineerHatLeft", 1025, Vector4.zero);
            ImportSingleCropped(PanelPath, "KitchenPanel", 100, new Vector4(62, 62, 62, 62));
            ImportBackground();
            var material = EnsureMaterial();
            CreateAnimations();
            CreateFoodBurst(material);
            CreateMascot(material);
            CreateAmbient(material);
            UpdateOriginalDefinitions();
            UpdateSharedRig();
            UpdateUI();
            GlobalSettingsSetup.Apply();
            UpdateScene("MainMenu");
            UpdateScene("Gameplay");
            AssetDatabase.SaveAssets();
            Debug.Log("Task7 production artwork authored from six prompt-preserving PNGs.");
        }

        [MenuItem("CPS/Author Mascot Hat Facing")]
        public static void AuthorMascotHatFacing()
        {
            ImportSingleCropped(LeftHatPath, "EngineerHatLeft", 1025, Vector4.zero);
            CreateAnimations();
            const string path = "Assets/CPS/Prefabs/Mascot.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var body = root.transform.Find("Body").GetComponent<SpriteRenderer>();
                var hat = root.transform.Find("Hat").GetComponent<SpriteRenderer>();
                hat.transform.localPosition = new Vector3(-.14f, 1.25f, 0);
                var presentation = root.GetComponent<MascotPresentation>();
                Set(presentation, "hat", hat);
                Set(presentation, "rightFacingHat", Sprite(HatPath, "EngineerHat"));
                Set(presentation, "leftFacingHat", Sprite(LeftHatPath, "EngineerHatLeft"));
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
        }

        private static void ImportGrid(string path, int columns, int rows, float pixelsPerUnit, string[] names, bool feetPivot)
        {
            var texture = PrepareTexture(path, pixelsPerUnit, true);
            var pixels = texture.GetPixels32();
            int cellWidth = texture.width / columns;
            int cellHeight = texture.height / rows;
            var slices = new List<SpriteRect>();
            for (int i = 0; i < names.Length; i++)
            {
                int x = i % columns * cellWidth;
                int y = texture.height - (i / columns + 1) * cellHeight;
                var nominalRect = AlphaRect(pixels, texture.width, new RectInt(x, y, cellWidth, cellHeight));
                bool extendedPeel = path == ItemPath && names[i] == "BananaPeel";
                var rect = extendedPeel
                    ? AlphaRect(pixels, texture.width, new RectInt(x, y, cellWidth + 29, cellHeight))
                    : nominalRect;
                var pivot = feetPivot ? new Vector2(.5f, 0) : new Vector2(.5f, .5f);
                if (extendedPeel)
                    pivot = new Vector2((nominalRect.center.x - rect.x) / rect.width,
                        (nominalRect.center.y - rect.y) / rect.height);
                slices.Add(new SpriteRect
                {
                    name = names[i], rect = rect, alignment = SpriteAlignment.Custom,
                    pivot = pivot
                });
                Debug.Log($"Task7 slice {names[i]} rect={rect} pivot={(feetPivot ? "foot" : "center")}");
            }
            FinishImport(path, slices.ToArray());
        }

        private static void ImportSingleCropped(string path, string name, float pixelsPerUnit, Vector4 border)
        {
            var texture = PrepareTexture(path, pixelsPerUnit, true);
            var rect = AlphaRect(texture.GetPixels32(), texture.width, new RectInt(0, 0, texture.width, texture.height));
            FinishImport(path, new[] { new SpriteRect
            {
                name = name, rect = rect, alignment = SpriteAlignment.Custom,
                pivot = new Vector2(.5f, .5f), border = border
            } });
            Debug.Log($"Task7 slice {name} rect={rect} border={border}");
        }

        private static Texture2D PrepareTexture(string path, float pixelsPerUnit, bool point)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new InvalidOperationException("Missing source PNG: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = point ? FilterMode.Point : FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Rect AlphaRect(Color32[] pixels, int stride, RectInt cell)
        {
            int minX = cell.xMax, minY = cell.yMax, maxX = cell.xMin - 1, maxY = cell.yMin - 1;
            for (int y = cell.yMin; y < cell.yMax; y++)
                for (int x = cell.xMin; x < cell.xMax; x++)
                    if (pixels[y * stride + x].a >= 32)
                    {
                        minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
                    }
            if (maxX < minX) throw new InvalidOperationException("Empty production sprite cell: " + cell);
            minX = Mathf.Max(cell.xMin, minX - 2); minY = Mathf.Max(cell.yMin, minY - 2);
            maxX = Mathf.Min(cell.xMax - 1, maxX + 2); maxY = Mathf.Min(cell.yMax - 1, maxY + 2);
            return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static void FinishImport(string path, SpriteRect[] slices)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new InvalidOperationException("Sprite data provider unavailable: " + path);
            provider.InitSpriteEditorDataProvider();
            var existing = provider.GetSpriteRects().ToDictionary(sprite => sprite.name);
            foreach (var slice in slices)
                slice.spriteID = existing.TryGetValue(slice.name, out var old) ? old.spriteID : GUID.Generate();
            provider.SetSpriteRects(slices);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(
                slices.Select(slice => new SpriteNameFileIdPair(slice.name, slice.spriteID)));
            provider.Apply();
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static void ImportBackground()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(BackgroundPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 104.5f;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Sprite Sprite(string path, string name) => AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>().First(sprite => sprite.name == name);

        private static Material EnsureMaterial()
        {
            string path = Art + "SpriteUnlit.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) throw new InvalidOperationException("Installed URP 2D Sprite-Unlit shader is unavailable.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void CreateAnimations()
        {
            var clips = new Dictionary<string, AnimationClip>
            {
                ["Idle"] = Clip("Idle", new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }, true),
                ["Run"] = Clip("Run", new[] { 2, 3, 4, 5 }, true),
                ["Eat"] = Clip("Eat", new[] { 6, 7 }, false),
                ["Hurt"] = Clip("Hurt", new[] { 8, 8, 8 }, false),
                ["Dead"] = Clip("Dead", new[] { 9 }, false),
                ["Triumph"] = Clip("Triumph", new[] { 10, 10, 11, 11 }, false)
            };
            string path = Animations + "Mascot.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ??
                AnimatorController.CreateAnimatorControllerAtPath(path);
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var pair in clips)
            {
                var state = stateMachine.states.Select(child => child.state).FirstOrDefault(candidate => candidate.name == pair.Key)
                    ?? stateMachine.AddState(pair.Key);
                state.motion = pair.Value;
                if (pair.Key == "Idle") stateMachine.defaultState = state;
            }
            EditorUtility.SetDirty(controller);
        }

        private static AnimationClip Clip(string name, int[] poses, bool loop)
        {
            string path = Animations + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }
            clip.frameRate = 10;
            var serialized = new SerializedObject(clip);
            var loopTime = serialized.FindProperty("m_AnimationClipSettings.m_LoopTime");
            if (loopTime == null) throw new InvalidOperationException("Animator Loop Time is unavailable: " + path);
            loopTime.boolValue = loop;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var sprites = new ObjectReferenceKeyframe[poses.Length + (loop ? 1 : 0)];
            var x = new Keyframe[sprites.Length];
            var y = new Keyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                int pose = poses[i % poses.Length];
                float time = i / 10f;
                var body = Sprite(BodyPath, BodyNames[pose]);
                sprites[i] = new ObjectReferenceKeyframe { time = time, value = body };
                float hatX = pose == 9 ? -1.1f : pose == 6 ? -.22f : -.14f;
                float hatY = pose == 9 ? .5155556f : body.rect.height / 180f + .0055556f;
                x[i] = new Keyframe(time, hatX);
                y[i] = new Keyframe(time, hatY);
            }
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve("Body", typeof(SpriteRenderer), "m_Sprite"), sprites);
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve("Hat", typeof(Transform), "m_LocalPosition.x"), new AnimationCurve(x));
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve("Hat", typeof(Transform), "m_LocalPosition.y"), new AnimationCurve(y));
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static Mesh PixelQuad()
        {
            string path = Art + "PixelQuad.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = new Mesh { name = "PixelQuad" };
            mesh.vertices = new[] { new Vector3(-.5f, -.5f), new Vector3(.5f, -.5f), new Vector3(-.5f, .5f), new Vector3(.5f, .5f) };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static ParticleSystem Particles(Transform parent, string name, Material material, int order)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            var system = go.GetComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = PixelQuad();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            return system;
        }

        private static void CreateFoodBurst(Material material)
        {
            var go = new GameObject("FoodBurst", typeof(SpriteRenderer), typeof(FoodBurst));
            try
            {
                go.transform.localScale = Vector3.one * .42f;
                var renderer = go.GetComponent<SpriteRenderer>();
                renderer.sharedMaterial = material;
                renderer.sortingOrder = 25;
                PrefabUtility.SaveAsPrefabAsset(go, Prefabs + "FoodBurst.prefab");
            }
            finally { Object.DestroyImmediate(go); }
        }

        private static void CreateMascot(Material material)
        {
            var root = new GameObject("Mascot", typeof(Animator), typeof(AudioSource), typeof(MascotPresentation));
            try
            {
                var animator = root.GetComponent<Animator>();
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Animations + "Mascot.controller");
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                var audio = root.GetComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.volume = .45f;
                var body = ChildSprite(root.transform, "Body", Sprite(BodyPath, "IdleA"), material, 10);
                var hat = ChildSprite(root.transform, "Hat", Sprite(HatPath, "EngineerHat"), material, 11);
                hat.transform.localPosition = new Vector3(-.14f, 1.25f, 0);
                var dissolve = Particles(root.transform, "Dissolve", material, 12);
                var main = dissolve.main;
                main.playOnAwake = false; main.loop = false; main.duration = .6f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .65f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(.3f, 1.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(.035f, .09f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .86f, .65f, .75f));
                main.maxParticles = 32; main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.useUnscaledTime = true;
                var emission = dissolve.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0, 28) });
                var shape = dissolve.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(1.1f, .8f, .1f);
                Set(root.GetComponent<MascotPresentation>(), "body", body);
                Set(root.GetComponent<MascotPresentation>(), "hat", hat);
                Set(root.GetComponent<MascotPresentation>(), "rightFacingHat", Sprite(HatPath, "EngineerHat"));
                Set(root.GetComponent<MascotPresentation>(), "leftFacingHat", Sprite(LeftHatPath, "EngineerHatLeft"));
                Set(root.GetComponent<MascotPresentation>(), "animator", animator);
                Set(root.GetComponent<MascotPresentation>(), "dissolve", dissolve);
                Set(root.GetComponent<MascotPresentation>(), "audioSource", audio);
                Set(root.GetComponent<MascotPresentation>(), "burstPrefab", AssetDatabase.LoadAssetAtPath<FoodBurst>(Prefabs + "FoodBurst.prefab"));
                var foods = new[] { Sprite(ItemPath, "Apple"), Sprite(ItemPath, "Bread"), Sprite(ItemPath, "Cake") };
                SetArray(root.GetComponent<MascotPresentation>(), "foodSprites", foods);
                PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "Mascot.prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static SpriteRenderer ChildSprite(Transform parent, string name, Sprite sprite, Material material, int order)
        {
            var child = new GameObject(name, typeof(SpriteRenderer));
            child.transform.SetParent(parent, false);
            var renderer = child.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static void CreateAmbient(Material material)
        {
            var root = new GameObject("KitchenAmbient", typeof(KitchenAmbient));
            try
            {
                var ivy = ChildSprite(root.transform, "Ivy", Sprite(AmbientPath, "Ivy"), material, -30);
                ivy.transform.localPosition = new Vector3(7.25f, 3.25f);
                var whisk = ChildSprite(root.transform, "Whisk", Sprite(AmbientPath, "Whisk"), material, -30);
                whisk.transform.localPosition = new Vector3(-5.8f, 2.85f);
                var ladle = ChildSprite(root.transform, "Ladle", Sprite(AmbientPath, "Ladle"), material, -30);
                ladle.transform.localPosition = new Vector3(-6.6f, 2.85f);
                var steam = Particles(root.transform, "Steam", material, -25);
                steam.transform.localPosition = new Vector3(7.25f, -.55f);
                var main = steam.main;
                main.loop = true; main.playOnAwake = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.2f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(.18f, .3f);
                main.startSize = new ParticleSystem.MinMaxCurve(.025f, .06f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .96f, .83f, .28f));
                main.maxParticles = 12; main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.useUnscaledTime = true;
                var emission = steam.emission; emission.rateOverTime = 3;
                var shape = steam.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(.3f, .03f, .1f);
                var ambient = root.GetComponent<KitchenAmbient>();
                Set(ambient, "ivy", ivy.transform); Set(ambient, "whisk", whisk.transform);
                Set(ambient, "ladle", ladle.transform); Set(ambient, "steam", steam);
                PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "KitchenAmbient.prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void UpdateOriginalDefinitions()
        {
            foreach (string name in ItemNames)
            {
                var definition = AssetDatabase.LoadAssetAtPath<FallingObjectDefinition>("Assets/CPS/Config/" + name + ".asset");
                if (definition == null) throw new InvalidOperationException("Missing original item definition: " + name);
                definition.sprite = Sprite(ItemPath, name);
                EditorUtility.SetDirty(definition);
            }
        }

        private static void UpdateSharedRig()
        {
            string path = Prefabs + "GameplayRig.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var player = root.GetComponentInChildren<PlayerController>();
                if (player == null) throw new InvalidOperationException("Shared GameplayRig player is missing.");
                player.name = "MascotPlayer";
                player.transform.localPosition = new Vector3(0, -2.5f, 0);
                var oldSprite = player.GetComponent<SpriteRenderer>();
                if (oldSprite != null) Object.DestroyImmediate(oldSprite);
                SetFloat(player, "mascotHalfWidth", .9f);
                SetVector3(player, "catchZoneHalfExtents", new Vector3(1f, .4f, .1f));
                var presentation = player.GetComponentInChildren<MascotPresentation>();
                if (presentation == null)
                {
                    var mascot = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Mascot.prefab");
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(mascot, player.transform);
                    instance.transform.localPosition = Vector3.zero;
                    presentation = instance.GetComponent<MascotPresentation>();
                }
                Set(root.GetComponent<GameSessionController>(), "presentation", presentation);
                var spawner = root.GetComponent<FallingObjectSpawner>();
                Set(spawner, "prefab", AssetDatabase.LoadAssetAtPath<FallingObject>(Prefabs + "FallingObject.prefab"));
                SetArray(spawner, "foodDefinitions", new Object[]
                {
                    Original("Apple"), Original("Bread"), Original("Cake")
                });
                SetArray(spawner, "trashDefinitions", new Object[]
                {
                    Original("Can"), Original("BananaPeel")
                });
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static FallingObjectDefinition Original(string name) =>
            AssetDatabase.LoadAssetAtPath<FallingObjectDefinition>("Assets/CPS/Config/" + name + ".asset");

        private static void UpdateUI()
        {
            var panel = Sprite(PanelPath, "KitchenPanel");
            PanelPrefab(Prefabs + "MainMenuCanvas.prefab", panel,
                "Profile and difficulty", "How to play");
            PanelPrefab(Prefabs + "GameplayCanvas.prefab", panel,
                "Pause overlay/Pause card");
            PanelPrefab(Prefabs + "ResultPanel.prefab", panel, "Results card");
            string path = Prefabs + "MainMenuCanvas.prefab";
            var menu = PrefabUtility.LoadPrefabContents(path);
            try
            {
                menu.GetComponent<Canvas>().sortingOrder = 100;
                SetHeight(menu.transform.Find("Profile and difficulty"), 820);
                SetHeight(menu.transform.Find("How to play"), 820);
                menu.transform.Find("Select or create a profile.").GetComponent<RectTransform>().sizeDelta = new Vector2(740, 50);
                foreach (string label in new[] { "MOVE", "A / ←     D / →",
                    "Use left and right to catch Apple, Bread and Cake. Avoid the can and banana peel.",
                    "3 HP  ·  60 SECONDS",
                    "Keyboard controls are simulated input. Physical force (N) is unavailable until a sensor is connected." })
                {
                    var rect = menu.GetComponentsInChildren<RectTransform>(true).First(item => item.name == label);
                    rect.sizeDelta = new Vector2(420, rect.sizeDelta.y);
                }
                var backdrop = menu.transform.Find("Kitchen backdrop").GetComponent<Image>();
                backdrop.color = Color.clear;
                PrefabUtility.SaveAsPrefabAsset(menu, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(menu); }

            path = Prefabs + "GameplayCanvas.prefab";
            var gameplay = PrefabUtility.LoadPrefabContents(path);
            try
            {
                gameplay.GetComponent<Canvas>().sortingOrder = 100;
                var ui = gameplay.GetComponent<GameplayUI>();
                foreach (string name in new[] { "Player", "Score" })
                {
                    var image = gameplay.transform.Find(name).GetComponent<Image>();
                    image.sprite = null;
                    image.type = Image.Type.Simple;
                    image.color = new Color(.99f, .91f, .73f);
                }
                var rankBadge = gameplay.transform.Find("Rank badge");
                var rankName = (Text)new SerializedObject(ui).FindProperty("rank").objectReferenceValue;
                rankName.gameObject.name = "Rank name";
                SetRect(rankName.rectTransform, new Vector2(31, 0), new Vector2(116, 58));
                rankName.fontSize = 17;
                rankName.fontStyle = FontStyle.Bold;
                rankName.alignment = TextAnchor.MiddleCenter;
                rankName.color = new Color(1, .96f, .82f);
                var nameOutline = rankName.GetComponent<Outline>() ?? rankName.gameObject.AddComponent<Outline>();
                nameOutline.effectColor = new Color(.22f, .10f, .07f);
                nameOutline.effectDistance = new Vector2(1, -1);
                var rankLetter = EnsureText(rankBadge, "Rank letter");
                SetRect(rankLetter.rectTransform, new Vector2(-59, 0), new Vector2(72, 62));
                rankLetter.fontSize = 44;
                rankLetter.fontStyle = FontStyle.Bold;
                rankLetter.alignment = TextAnchor.MiddleCenter;
                rankLetter.color = new Color(1, .96f, .82f);
                var outline = rankLetter.GetComponent<Outline>() ?? rankLetter.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(.22f, .10f, .07f);
                outline.effectDistance = new Vector2(2, -2);
                var accents = new Image[3];
                for (int i = 0; i < accents.Length; i++)
                {
                    accents[i] = EnsureImage(rankBadge, "Rank accent " + (i + 1));
                    SetRect(accents[i].rectTransform, new Vector2(-70 + i * 23, -26), new Vector2(36, 7));
                    accents[i].rectTransform.localRotation = Quaternion.Euler(0, 0, -12);
                    accents[i].color = new Color(1, .91f, .52f);
                    accents[i].raycastTarget = false;
                    accents[i].transform.SetSiblingIndex(i);
                }
                rankLetter.transform.SetAsLastSibling();
                rankName.transform.SetAsLastSibling();
                Set(ui, "rankLetter", rankLetter);
                Set(ui, "rankPlate", rankBadge.GetComponent<Image>());
                Set(ui, "rankCalloutPlate", gameplay.transform.Find("Rank callout").GetComponent<Image>());
                SetArray(ui, "rankAccents", accents.Cast<Object>().ToArray());
                gameplay.transform.Find("Pause overlay/RESUME").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 70);
                PrefabUtility.SaveAsPrefabAsset(gameplay, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(gameplay); }

            path = Prefabs + "ResultPanel.prefab";
            var result = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (string name in new[] { "RESTART", "MAIN MENU" })
                {
                    var button = result.transform.Find("Results card/" + name).GetComponent<RectTransform>();
                    button.anchoredPosition = new Vector2(button.anchoredPosition.x, -425);
                    button.sizeDelta = new Vector2(button.sizeDelta.x, 64);
                }
                PrefabUtility.SaveAsPrefabAsset(result, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(result); }
        }

        private static void SetHeight(Transform panel, float height)
        {
            var rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
        }

        private static Text EnsureText(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Text));
                go.transform.SetParent(parent, false);
                child = go.transform;
            }
            var text = child.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.supportRichText = false;
            text.raycastTarget = false;
            return text;
        }

        private static Image EnsureImage(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                child = go.transform;
            }
            return child.GetComponent<Image>();
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void PanelPrefab(string path, Sprite sprite, params string[] names)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (string name in names)
                {
                    var child = root.transform.Find(name);
                    if (child == null) throw new InvalidOperationException("Missing authored UI panel: " + path + "/" + name);
                    var image = child.GetComponent<Image>();
                    image.sprite = sprite;
                    image.type = Image.Type.Sliced;
                    image.color = Color.white;
                    image.pixelsPerUnitMultiplier = 2;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void UpdateScene(string name)
        {
            string path = "Assets/CPS/Scenes/" + name + ".unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (!scene.GetRootGameObjects().Any(root => root.name == "KitchenBackground"))
            {
                var background = new GameObject("KitchenBackground", typeof(SpriteRenderer));
                SceneManager.MoveGameObjectToScene(background, scene);
                var renderer = background.GetComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
                renderer.sortingOrder = -100;
                renderer.color = new Color(.91f, .88f, .84f, 1);
            }
            if (!scene.GetRootGameObjects().Any(root => root.name == "KitchenAmbient"))
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "KitchenAmbient.prefab"), scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void Set(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            var field = serialized.FindProperty(property);
            if (field == null) throw new InvalidOperationException("Missing serialized property: " + target + "." + property);
            field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string property, Object[] values)
        {
            var serialized = new SerializedObject(target);
            var field = serialized.FindProperty(property);
            if (field == null) throw new InvalidOperationException("Missing serialized array: " + target + "." + property);
            field.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) field.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string property, float value)
        {
            var serialized = new SerializedObject(target);
            var field = serialized.FindProperty(property);
            if (field == null) throw new InvalidOperationException("Missing serialized number: " + target + "." + property);
            field.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector3(Object target, string property, Vector3 value)
        {
            var serialized = new SerializedObject(target);
            var field = serialized.FindProperty(property);
            if (field == null) throw new InvalidOperationException("Missing serialized vector: " + target + "." + property);
            field.vector3Value = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
