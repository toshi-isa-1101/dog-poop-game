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
            var boot = go.AddComponent<Bootstrap>();

            // アセット参照をシーンへ焼き込む（ビルドにも含まれる）。
            boot.dogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ithappy/Animals_FREE/Prefabs/Dog_001.prefab");
            boot.dogTexture = AssetDatabase.LoadAssetAtPath<Texture>(
                "Assets/ithappy/Animals_FREE/Textures/Texture.png");
            boot.groundTilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SimpleNaturePack/Prefabs/Ground_01.prefab");
            boot.groundTileSize = MeasureFootprint(boot.groundTilePrefab);
            boot.envProps = LoadEnvProps();

            EditorUtility.SetDirty(boot);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[BuildTool] Scene created. dogPrefab={(boot.dogPrefab != null)}, " +
                      $"dogTex={(boot.dogTexture != null)}, groundTile={(boot.groundTilePrefab != null)}, " +
                      $"tileSize={boot.groundTileSize:0.00}, envProps={boot.envProps.Length}");
            return ScenePath;
        }

        /// <summary>プレハブの水平 footprint(min of X,Z) を測って返す（地面タイル敷き詰め用）。</summary>
        private static float MeasureFootprint(GameObject prefab)
        {
            if (prefab == null) return 4f;
            var temp = (GameObject)Object.Instantiate(prefab);
            var rends = temp.GetComponentsInChildren<Renderer>();
            float size = 4f;
            if (rends.Length > 0)
            {
                var b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                size = Mathf.Max(0.1f, Mathf.Min(b.size.x, b.size.z));
            }
            Object.DestroyImmediate(temp);
            return size;
        }

        private static GameObject[] LoadEnvProps()
        {
            string[] names =
            {
                "Tree_01", "Tree_02", "Tree_03", "Tree_04", "Tree_05",
                "Bush_01", "Bush_02", "Bush_03",
                "Rock_01", "Rock_02", "Rock_03",
                "Stump_01", "Grass_01", "Grass_02", "Flowers_01", "Flowers_02", "Mushroom_01"
            };
            var list = new System.Collections.Generic.List<GameObject>();
            foreach (var n in names)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/SimpleNaturePack/Prefabs/{n}.prefab");
                if (p != null) list.Add(p);
            }
            return list.ToArray();
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
