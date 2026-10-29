using UnityEditor;
using UnityEngine;

namespace CPS.KitchenCatch.Editor
{
    public static class GameObjectAssetSetup
    {
        [MenuItem("CPS/Create Missing Object Assets")]
        public static void CreateMissing()
        {
            EnsureFolder("Assets/CPS", "Config");
            EnsureFolder("Assets/CPS", "Prefabs");
            CreateDifficulty("Easy", DifficultyId.Easy, 2, 1.4f, 0, new[] { -6f, 0, 6 });
            CreateDifficulty("Normal", DifficultyId.Normal, 2.8f, 1.1f, 0.15f, new[] { -6f, -3, 0, 3, 6 });
            CreateDifficulty("Hard", DifficultyId.Hard, 3.5f, 0.85f, 0.25f, new[] { -6f, -4, -2, 0, 2, 4, 6 });
            CreateDefinition("Apple", ObjectKind.Food, 10);
            CreateDefinition("Bread", ObjectKind.Food, 15);
            CreateDefinition("Cake", ObjectKind.Food, 20);
            CreateDefinition("Can", ObjectKind.Trash, 0);
            CreateDefinition("BananaPeel", ObjectKind.Trash, 0);
            const string prefabPath = "Assets/CPS/Prefabs/FallingObject.prefab";
            if (AssetDatabase.LoadMainAssetAtPath(prefabPath) == null && !System.IO.File.Exists(prefabPath))
            {
                var instance = new GameObject("FallingObject");
                try
                {
                    instance.AddComponent<FallingObject>();
                    PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                }
                finally { Object.DestroyImmediate(instance); }
            }
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }

        private static void CreateDifficulty(string name, DifficultyId id, float speed, float interval,
            float probability, float[] lanes)
        {
            string path = "Assets/CPS/Config/" + name + ".asset";
            if (AssetDatabase.LoadMainAssetAtPath(path) != null || System.IO.File.Exists(path)) return;
            var asset = ScriptableObject.CreateInstance<DifficultyConfig>();
            asset.difficulty = id; asset.speed = speed; asset.interval = interval;
            asset.trashProbability = probability; asset.lanes = lanes;
            AssetDatabase.CreateAsset(asset, path);
        }

        private static void CreateDefinition(string name, ObjectKind kind, int points)
        {
            string path = "Assets/CPS/Config/" + name + ".asset";
            if (AssetDatabase.LoadMainAssetAtPath(path) != null || System.IO.File.Exists(path)) return;
            var asset = ScriptableObject.CreateInstance<FallingObjectDefinition>();
            asset.kind = kind; asset.points = points;
            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
