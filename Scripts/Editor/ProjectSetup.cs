#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hollow.EditorTools
{
    /// <summary>
    /// One-click project setup and build helpers.
    /// Menu:  Game > Setup Project        (creates Main scene, build settings, shader inclusion)
    ///        Game > Build Windows Player
    /// Batch: Unity -batchmode -quit -projectPath . -executeMethod Hollow.EditorTools.ProjectSetup.Setup
    /// </summary>
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Game/Setup Project")]
        public static void Setup()
        {
            Directory.CreateDirectory("Assets/Scenes");

            // The scene is intentionally empty: Hollow.Game builds everything at runtime.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.productName = "Hollow Spire";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;

            AddAlwaysIncludedShaders();
            AssetDatabase.SaveAssets();
            Debug.Log("[HollowSpire] Setup complete. Press Play to start the game.");
        }

        /// <summary>Shaders are looked up by name at runtime, so make sure they survive build stripping.</summary>
        static void AddAlwaysIncludedShaders()
        {
            var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            if (gs == null)
            {
                var all = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
                if (all != null && all.Length > 0) gs = all[0];
            }
            if (gs == null) { Debug.LogWarning("[HollowSpire] Could not open GraphicsSettings; add the URP Lit/Unlit shaders to 'Always Included Shaders' manually if builds look pink."); return; }
            var so = new SerializedObject(gs);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            if (arr == null) { Debug.LogWarning("[HollowSpire] m_AlwaysIncludedShaders not found."); return; }

            string[] names =
            {
                "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit",
                "Standard", "Unlit/Color", "GUI/Text Shader", "Sprites/Default"
            };
            foreach (var name in names)
            {
                var sh = Shader.Find(name);
                if (sh == null) continue;
                bool has = false;
                for (int i = 0; i < arr.arraySize; i++)
                    if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) { has = true; break; }
                if (has) continue;
                arr.arraySize++;
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("Game/Build Windows Player")]
        public static void BuildWindows()
        {
            Setup();
            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/HollowSpire/HollowSpire.exe",
                                                   BuildTarget.StandaloneWindows64, BuildOptions.None);
            bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            Debug.Log("[HollowSpire] Build " + (ok ? "succeeded" : "FAILED") + ": " + report.summary.outputPath);
            if (!ok && Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
#endif
