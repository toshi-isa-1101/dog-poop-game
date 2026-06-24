#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PoopPanic
{
    /// <summary>
    /// バッチモードからシーンを生成してスタンドアロンをビルドするためのツール。
    /// このプロジェクトは .unity シーンをリポジトリに含めない方針なので、
    /// ビルド時にここで Bootstrap 入りのシーンを組み立てる。
    /// </summary>
    public static class BuildTool
    {
        private const string ScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("PoopPanic/Create Game Scene")]
        public static string CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Bootstrap");
            go.AddComponent<Bootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[BuildTool] Scene created at {ScenePath}");
            return ScenePath;
        }

        // batchmode: -executeMethod PoopPanic.BuildTool.BuildWindows
        [MenuItem("PoopPanic/Build Windows")]
        public static void BuildWindows()
        {
            CreateScene();

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/PoopPanic.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[BuildTool] Build result: {summary.result}, " +
                      $"errors: {summary.totalErrors}, size: {summary.totalSize} bytes, " +
                      $"output: {summary.outputPath}");

            if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
#endif
