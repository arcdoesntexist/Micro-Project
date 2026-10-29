using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CPS.KitchenCatch.Editor
{
    public static class VersusGameSetup
    {
        private const string ScenePath = "Assets/CPS/Scenes/VersusGameplay.unity";

        [InitializeOnLoadMethod]
        private static void OnReload() => EditorApplication.delayCall += RegisterScene;

        [MenuItem("CPS/Register Versus Gameplay Scene")]
        public static void RegisterScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(ScenePath)) return;
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path != ScenePath) continue;
                if (!scenes[i].enabled) { scenes[i].enabled = true; EditorBuildSettings.scenes = scenes.ToArray(); }
                return;
            }
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
