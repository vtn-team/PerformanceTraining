using UnityEngine;
using UnityEditor;
using System;
using System.Diagnostics;
using System.Text;
using UnityEngine.Profiling;
using PerformanceTraining.Core;
using PerformanceTraining.Enemy;
using Debug = UnityEngine.Debug;

namespace PerformanceTraining.Editor
{
    /// <summary>
    /// パフォーマンスベンチマークを実行するツール
    /// </summary>
    public static class PerformanceBenchmark
    {
        private static StringBuilder report = new StringBuilder();

        // [MenuItem("PerformanceTraining/Run Benchmark %#b")] // 学生用UIはExerciseManagerWindowを使用
        public static void RunBenchmark()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("エラー", "ベンチマークはPlayモードで実行してください。", "OK");
                return;
            }

            report.Clear();
            report.AppendLine("╔══════════════════════════════════════════════════════╗");
            report.AppendLine("║        パフォーマンスベンチマーク結果                    ║");
            report.AppendLine("╚══════════════════════════════════════════════════════╝\n");

            // 現在の設定を記録
            var settings = GameManager.Instance?.Settings;
            if (settings != null)
            {
                report.AppendLine("【最適化設定】");
                report.AppendLine($"  Object Pool:      {(settings.useObjectPool ? "ON" : "OFF")}");
                report.AppendLine($"  StringBuilder:    {(settings.useStringBuilder ? "ON" : "OFF")}");
                report.AppendLine($"  Delegate Cache:   {(settings.useDelegateCache ? "ON" : "OFF")}");
                report.AppendLine($"  Collection Reuse: {(settings.useCollectionReuse ? "ON" : "OFF")}");
                report.AppendLine($"  Spatial Partition:{(settings.useSpatialPartition ? "ON" : "OFF")}");
                report.AppendLine($"  Staggered Update: {(settings.useStaggeredUpdate ? "ON" : "OFF")}");
                report.AppendLine($"  SqrMagnitude:     {(settings.useSqrMagnitude ? "ON" : "OFF")}");
                report.AppendLine($"  Trig LUT:         {(settings.useTrigLUT ? "ON" : "OFF")}");
                report.AppendLine($"  Visibility Map:   {(settings.useVisibilityMap ? "ON" : "OFF")}");
                report.AppendLine();
            }

            // 基本情報
            report.AppendLine("【環境情報】");
            report.AppendLine($"  Unity Version: {Application.unityVersion}");
            report.AppendLine($"  Platform:      {Application.platform}");
            report.AppendLine($"  CPU:           {SystemInfo.processorType}");
            report.AppendLine($"  GPU:           {SystemInfo.graphicsDeviceName}");
            report.AppendLine($"  Memory:        {SystemInfo.systemMemorySize} MB");
            report.AppendLine();

            // 敵数を取得
            int enemyCount = GameManager.Instance?.CurrentEnemyCount ?? 0;
            report.AppendLine($"【計測条件】");
            report.AppendLine($"  敵の数: {enemyCount}");
            report.AppendLine();

            // パフォーマンス計測
            RunPerformanceTests();

            // メモリ計測
            RunMemoryTests();

            Debug.Log(report.ToString());

            // 結果ウィンドウを表示
            BenchmarkResultWindow.ShowResults(report.ToString());
        }

        private static void RunPerformanceTests()
        {
            report.AppendLine("【パフォーマンス計測】");

            // FPS計測（直近のフレームタイム）
            float fps = 1f / Time.unscaledDeltaTime;
            float frameTime = Time.unscaledDeltaTime * 1000f;

            report.AppendLine($"  FPS:        {fps:F1}");
            report.AppendLine($"  Frame Time: {frameTime:F2} ms");

            // 評価
            if (fps >= 60)
            {
                report.AppendLine("  評価: 目標達成 (60 FPS以上)");
            }
            else if (fps >= 30)
            {
                report.AppendLine("  評価: 改善が必要 (30-60 FPS)");
            }
            else
            {
                report.AppendLine("  評価: 要最適化 (30 FPS未満)");
            }
            report.AppendLine();
        }

        private static void RunMemoryTests()
        {
            report.AppendLine("【メモリ計測】");

            long totalMemory = Profiler.GetTotalAllocatedMemoryLong();
            long reservedMemory = Profiler.GetTotalReservedMemoryLong();
            long monoHeap = Profiler.GetMonoHeapSizeLong();
            long monoUsed = Profiler.GetMonoUsedSizeLong();

            report.AppendLine($"  Total Allocated: {totalMemory / 1024 / 1024} MB");
            report.AppendLine($"  Total Reserved:  {reservedMemory / 1024 / 1024} MB");
            report.AppendLine($"  Mono Heap:       {monoHeap / 1024 / 1024} MB");
            report.AppendLine($"  Mono Used:       {monoUsed / 1024 / 1024} MB");
            report.AppendLine();

            // GCアロケーション計測（簡易）
            report.AppendLine("【GCアロケーション計測】");
            long before = GC.GetTotalMemory(false);

            // テスト用の処理を実行
            var enemySystem = UnityEngine.Object.FindAnyObjectByType<EnemySystem>();
            if (enemySystem != null)
            {
                // 10フレーム分の処理をシミュレート
                for (int i = 0; i < 10; i++)
                {
                    enemySystem.GetStatusText();
                }
            }

            long after = GC.GetTotalMemory(false);
            long allocated = after - before;

            report.AppendLine($"  10フレーム分の推定アロケーション: {allocated} bytes");
            report.AppendLine($"  1フレームあたり: ~{allocated / 10} bytes");

            if (allocated / 10 < 1024)
            {
                report.AppendLine("  評価: 良好 (1KB/frame未満)");
            }
            else if (allocated / 10 < 10240)
            {
                report.AppendLine("  評価: 改善が必要 (1-10KB/frame)");
            }
            else
            {
                report.AppendLine("  評価: 要最適化 (10KB/frame以上)");
            }
            report.AppendLine();
        }

        /// <summary>
        /// Before/After比較用のスナップショットを保存
        /// </summary>
        // [MenuItem("PerformanceTraining/Save Snapshot (Before)")]
        public static void SaveSnapshotBefore()
        {
            SaveSnapshot("Before");
        }

        // [MenuItem("PerformanceTraining/Save Snapshot (After)")]
        public static void SaveSnapshotAfter()
        {
            SaveSnapshot("After");
        }

        private static void SaveSnapshot(string label)
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("エラー", "Playモードで実行してください。", "OK");
                return;
            }

            float fps = 1f / Time.unscaledDeltaTime;
            float frameTime = Time.unscaledDeltaTime * 1000f;
            long memory = Profiler.GetTotalAllocatedMemoryLong();
            int enemyCount = GameManager.Instance?.CurrentEnemyCount ?? 0;

            EditorPrefs.SetFloat($"PerformanceTraining_{label}_FPS", fps);
            EditorPrefs.SetFloat($"PerformanceTraining_{label}_FrameTime", frameTime);
            EditorPrefs.SetFloat($"PerformanceTraining_{label}_Memory", memory);
            EditorPrefs.SetInt($"PerformanceTraining_{label}_Enemies", enemyCount);

            Debug.Log($"スナップショット保存 ({label}): FPS={fps:F1}, Enemies={enemyCount}");
        }

        // [MenuItem("PerformanceTraining/Compare Snapshots")]
        public static void CompareSnapshots()
        {
            float beforeFPS = EditorPrefs.GetFloat("PerformanceTraining_Before_FPS", 0);
            float afterFPS = EditorPrefs.GetFloat("PerformanceTraining_After_FPS", 0);

            if (beforeFPS == 0 || afterFPS == 0)
            {
                EditorUtility.DisplayDialog("エラー", "Before/Afterのスナップショットを先に保存してください。", "OK");
                return;
            }

            float beforeFrame = EditorPrefs.GetFloat("PerformanceTraining_Before_FrameTime", 0);
            float afterFrame = EditorPrefs.GetFloat("PerformanceTraining_After_FrameTime", 0);
            int beforeEnemies = EditorPrefs.GetInt("PerformanceTraining_Before_Enemies", 0);
            int afterEnemies = EditorPrefs.GetInt("PerformanceTraining_After_Enemies", 0);

            var sb = new StringBuilder();
            sb.AppendLine("╔══════════════════════════════════════════════════════╗");
            sb.AppendLine("║            Before / After 比較結果                     ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════╝\n");

            sb.AppendLine($"            Before          After          改善");
            sb.AppendLine($"  FPS:      {beforeFPS,8:F1}      {afterFPS,8:F1}      {(afterFPS / beforeFPS):F2}x");
            sb.AppendLine($"  Frame:    {beforeFrame,8:F2} ms   {afterFrame,8:F2} ms   {(beforeFrame / afterFrame):F2}x");
            sb.AppendLine($"  Enemies:  {beforeEnemies,8}      {afterEnemies,8}");
            sb.AppendLine();

            if (afterFPS >= 60 && beforeFPS < 60)
            {
                sb.AppendLine("  結果: 目標達成！ (60 FPS到達)");
            }
            else if (afterFPS > beforeFPS)
            {
                sb.AppendLine($"  結果: 改善 (+{afterFPS - beforeFPS:F1} FPS)");
            }
            else
            {
                sb.AppendLine("  結果: 変化なしまたは悪化");
            }

            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("比較結果", sb.ToString(), "OK");
        }
    }

    /// <summary>
    /// ベンチマーク結果を表示するウィンドウ
    /// </summary>
    public class BenchmarkResultWindow : EditorWindow
    {
        private string results;
        private Vector2 scrollPosition;

        public static void ShowResults(string results)
        {
            var window = GetWindow<BenchmarkResultWindow>("Benchmark Results");
            window.results = results;
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("ベンチマーク結果", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var style = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                richText = false,
                font = EditorStyles.label.font
            };

            EditorGUILayout.TextArea(results, style, GUILayout.ExpandHeight(true));

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("クリップボードにコピー"))
            {
                EditorGUIUtility.systemCopyBuffer = results;
                Debug.Log("結果をクリップボードにコピーしました");
            }
            if (GUILayout.Button("再計測"))
            {
                PerformanceBenchmark.RunBenchmark();
            }
            if (GUILayout.Button("閉じる"))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
