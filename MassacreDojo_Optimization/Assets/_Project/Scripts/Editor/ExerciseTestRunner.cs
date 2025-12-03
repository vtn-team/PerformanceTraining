using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MassacreDojo.Core;
using MassacreDojo.Enemy;
using Debug = UnityEngine.Debug;

#if EXERCISES_DEPLOYED
using StudentExercises.Memory;
using StudentExercises.CPU;
using StudentExercises.Tradeoff;
#else
using MassacreDojo.Exercises.Memory;
using MassacreDojo.Exercises.CPU;
using MassacreDojo.Exercises.Tradeoff;
#endif

namespace MassacreDojo.Editor
{
    /// <summary>
    /// 課題の実装が正しく動作しているかをテストするクラス
    /// </summary>
    public static class ExerciseTestRunner
    {
        private static StringBuilder resultLog = new StringBuilder();
        private static int passCount = 0;
        private static int failCount = 0;

        /// <summary>
        /// 全テストを実行
        /// </summary>
        public static void RunAllTests()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("テストはPlayモードで実行してください。");
                return;
            }

            resultLog.Clear();
            passCount = 0;
            failCount = 0;

            resultLog.AppendLine("========================================");
            resultLog.AppendLine("   パフォーマンス最適化 テスト結果");
            resultLog.AppendLine("========================================\n");

            RunMemoryTests();
            RunCPUTests();
            RunTradeoffTests();

            resultLog.AppendLine("\n========================================");
            resultLog.AppendLine($"   結果: {passCount} PASS / {failCount} FAIL");
            resultLog.AppendLine("========================================");

            Debug.Log(resultLog.ToString());

            // 結果ウィンドウを表示
            TestResultWindow.ShowResults(resultLog.ToString(), passCount, failCount);
        }

        /// <summary>
        /// メモリ最適化テスト
        /// </summary>
        public static void RunMemoryTests()
        {
            resultLog.AppendLine("【課題1: メモリ最適化】");

            var exercise = GameObject.FindObjectOfType<ZeroAllocation_Exercise>();
            if (exercise == null)
            {
                resultLog.AppendLine("  [SKIP] ZeroAllocation_Exercise が見つかりません");
                return;
            }

            // Test 1: オブジェクトプール
            TestObjectPool(exercise);

            // Test 2: StringBuilder
            TestStringBuilder(exercise);

            // Test 3: デリゲートキャッシュ
            TestDelegateCache(exercise);

            // Test 4: コレクション再利用
            TestCollectionReuse(exercise);

            resultLog.AppendLine();
        }

        private static void TestObjectPool(ZeroAllocation_Exercise exercise)
        {
            try
            {
                // プールからの取得テスト
                var enemy1 = exercise.GetFromPool();
                var enemy2 = exercise.GetFromPool();

                if (enemy1 != null && enemy2 != null)
                {
                    // 返却テスト
                    exercise.ReturnToPool(enemy1);
                    var enemy3 = exercise.GetFromPool();

                    // 同じオブジェクトが再利用されているか
                    if (enemy3 == enemy1)
                    {
                        LogPass("Step 1: オブジェクトプール - 再利用が正しく動作");
                    }
                    else
                    {
                        LogFail("Step 1: オブジェクトプール - 再利用が正しく動作していない可能性");
                    }

                    // クリーンアップ
                    exercise.ReturnToPool(enemy2);
                    exercise.ReturnToPool(enemy3);
                }
                else
                {
                    LogFail("Step 1: オブジェクトプール - GetFromPool()がnullを返した");
                }
            }
            catch (Exception e)
            {
                LogFail($"Step 1: オブジェクトプール - 例外発生: {e.Message}");
            }
        }

        private static void TestStringBuilder(ZeroAllocation_Exercise exercise)
        {
            try
            {
                // 複数回呼び出して文字列が正しく生成されるか
                string result1 = exercise.BuildStatusText(100, 50);
                string result2 = exercise.BuildStatusText(200, 75);

                bool hasCorrectFormat = result1.Contains("100") && result1.Contains("50");
                bool hasCorrectFormat2 = result2.Contains("200") && result2.Contains("75");

                if (hasCorrectFormat && hasCorrectFormat2)
                {
                    // GCアロケーションチェック（簡易）
                    long before = GC.GetTotalMemory(false);
                    for (int i = 0; i < 100; i++)
                    {
                        exercise.BuildStatusText(i, i * 2);
                    }
                    long after = GC.GetTotalMemory(false);
                    long allocated = after - before;

                    if (allocated < 5000) // 5KB未満なら成功
                    {
                        LogPass("Step 2: StringBuilder - 文字列生成が最適化されている");
                    }
                    else
                    {
                        LogFail($"Step 2: StringBuilder - GCアロケーションが多い ({allocated} bytes)");
                    }
                }
                else
                {
                    LogFail("Step 2: StringBuilder - 文字列フォーマットが正しくない");
                }
            }
            catch (Exception e)
            {
                LogFail($"Step 2: StringBuilder - 例外発生: {e.Message}");
            }
        }

        private static void TestDelegateCache(ZeroAllocation_Exercise exercise)
        {
            try
            {
                var action1 = exercise.GetCachedUpdateAction();
                var action2 = exercise.GetCachedUpdateAction();

                if (action1 != null && action2 != null)
                {
                    // 同じインスタンスが返されるか
                    if (ReferenceEquals(action1, action2))
                    {
                        LogPass("Step 3: デリゲートキャッシュ - 同じインスタンスが再利用されている");
                    }
                    else
                    {
                        LogFail("Step 3: デリゲートキャッシュ - 毎回新しいインスタンスが生成されている");
                    }
                }
                else
                {
                    LogFail("Step 3: デリゲートキャッシュ - GetCachedUpdateAction()がnullを返した");
                }
            }
            catch (Exception e)
            {
                LogFail($"Step 3: デリゲートキャッシュ - 例外発生: {e.Message}");
            }
        }

        private static void TestCollectionReuse(ZeroAllocation_Exercise exercise)
        {
            try
            {
                var list1 = exercise.GetReusableList();
                list1.Add(null); // ダミー追加
                var list2 = exercise.GetReusableList();

                if (list1 != null && list2 != null)
                {
                    // 同じインスタンスが返されるか
                    if (ReferenceEquals(list1, list2))
                    {
                        // Clear()されているか
                        if (list2.Count == 0)
                        {
                            LogPass("Step 4: コレクション再利用 - 再利用とClear()が正しく動作");
                        }
                        else
                        {
                            LogFail("Step 4: コレクション再利用 - Clear()が呼ばれていない");
                        }
                    }
                    else
                    {
                        LogFail("Step 4: コレクション再利用 - 毎回新しいインスタンスが生成されている");
                    }
                }
                else
                {
                    LogFail("Step 4: コレクション再利用 - GetReusableList()がnullを返した");
                }
            }
            catch (Exception e)
            {
                LogFail($"Step 4: コレクション再利用 - 例外発生: {e.Message}");
            }
        }

        /// <summary>
        /// CPU最適化テスト
        /// </summary>
        public static void RunCPUTests()
        {
            resultLog.AppendLine("【課題2: CPU最適化】");

            var exercise = GameObject.FindObjectOfType<CPUOptimization_Exercise>();
            if (exercise == null)
            {
                resultLog.AppendLine("  [SKIP] CPUOptimization_Exercise が見つかりません");
                return;
            }

            // Test 1: 空間分割
            TestSpatialPartition(exercise);

            // Test 2: 更新分散
            TestStaggeredUpdate(exercise);

            // Test 3: 距離計算
            TestDistanceCalculation(exercise);

            resultLog.AppendLine();
        }

        private static void TestSpatialPartition(CPUOptimization_Exercise exercise)
        {
            try
            {
                // セルインデックス計算テスト
                Vector3 testPos = new Vector3(0, 0, 0);
                int index = exercise.GetCellIndex(testPos);

                // 中心付近のインデックスが正しいか
                int expectedIndex = GameConstants.GRID_SIZE / 2 * GameConstants.GRID_SIZE + GameConstants.GRID_SIZE / 2;

                if (Math.Abs(index - expectedIndex) <= GameConstants.GRID_SIZE)
                {
                    LogPass("Step 1: 空間分割 - GetCellIndex()が正しく動作");
                }
                else
                {
                    LogFail($"Step 1: 空間分割 - GetCellIndex()の計算が不正 (got {index}, expected ~{expectedIndex})");
                }

                // QueryNearbyテスト
                var nearby = exercise.QueryNearbyEnemies(testPos);
                if (nearby != null)
                {
                    LogPass("Step 1: 空間分割 - QueryNearbyEnemies()が正しく動作");
                }
                else
                {
                    LogFail("Step 1: 空間分割 - QueryNearbyEnemies()がnullを返した");
                }
            }
            catch (Exception e)
            {
                LogFail($"Step 1: 空間分割 - 例外発生: {e.Message}");
            }
        }

        private static void TestStaggeredUpdate(CPUOptimization_Exercise exercise)
        {
            try
            {
                // グループ0がフレーム0で更新されるか
                bool frame0 = exercise.ShouldUpdateThisFrame(0, 0);
                bool frame1 = exercise.ShouldUpdateThisFrame(0, 1);
                bool frame10 = exercise.ShouldUpdateThisFrame(0, 10);

                // グループ1がフレーム1で更新されるか
                bool g1frame1 = exercise.ShouldUpdateThisFrame(1, 1);

                if (frame0 && !frame1 && frame10 && g1frame1)
                {
                    LogPass("Step 2: 更新分散 - ShouldUpdateThisFrame()が正しく動作");
                }
                else if (frame0 && frame1 && frame10)
                {
                    LogFail("Step 2: 更新分散 - 常にtrueを返している（未実装）");
                }
                else
                {
                    LogFail("Step 2: 更新分散 - ロジックが正しくない");
                }
            }
            catch (Exception e)
            {
                LogFail($"Step 2: 更新分散 - 例外発生: {e.Message}");
            }
        }

        private static void TestDistanceCalculation(CPUOptimization_Exercise exercise)
        {
            try
            {
                Vector3 a = new Vector3(0, 0, 0);
                Vector3 b = new Vector3(3, 0, 4);

                float distSqr = exercise.CalculateDistanceSqr(a, b);
                float expected = 25f; // 3² + 4² = 25

                if (Mathf.Approximately(distSqr, expected))
                {
                    LogPass("Step 3: 距離計算 - CalculateDistanceSqr()が正しく動作");
                }
                else if (Mathf.Approximately(distSqr, 5f))
                {
                    LogFail("Step 3: 距離計算 - Vector3.Distanceを使用している（2乗されていない）");
                }
                else
                {
                    LogFail($"Step 3: 距離計算 - 計算結果が不正 (got {distSqr}, expected {expected})");
                }

                // IsWithinDistanceテスト
                bool within = exercise.IsWithinDistance(a, b, 6f);
                bool notWithin = exercise.IsWithinDistance(a, b, 4f);

                if (within && !notWithin)
                {
                    LogPass("Step 3: 距離計算 - IsWithinDistance()が正しく動作");
                }
                else
                {
                    LogFail("Step 3: 距離計算 - IsWithinDistance()の判定が不正");
                }
            }
            catch (Exception e)
            {
                LogFail($"Step 3: 距離計算 - 例外発生: {e.Message}");
            }
        }

        /// <summary>
        /// トレードオフテスト
        /// </summary>
        public static void RunTradeoffTests()
        {
            resultLog.AppendLine("【課題3: トレードオフ】");

            // 近傍キャッシュテスト
            var neighborExercise = GameObject.FindObjectOfType<NeighborCache_Exercise>();
            if (neighborExercise != null)
            {
                TestNeighborCache(neighborExercise);
            }
            else
            {
                resultLog.AppendLine("  [SKIP] NeighborCache_Exercise が見つかりません");
            }

            // AI判断キャッシュテスト
            var decisionExercise = GameObject.FindObjectOfType<DecisionCache_Exercise>();
            if (decisionExercise != null)
            {
                TestDecisionCache(decisionExercise);
            }
            else
            {
                resultLog.AppendLine("  [SKIP] DecisionCache_Exercise が見つかりません");
            }

            resultLog.AppendLine();
        }

        private static void TestNeighborCache(NeighborCache_Exercise exercise)
        {
            try
            {
                // キャッシュサイズテスト
                exercise.ClearAllCache();
                int initialSize = exercise.GetCacheSize();

                if (initialSize == 0)
                {
                    LogPass("3-A: NeighborCache - ClearAllCache()が正しく動作");
                }
                else
                {
                    LogFail($"3-A: NeighborCache - ClearAllCache()後もキャッシュが残っている ({initialSize})");
                }

                // ヒット率テスト（初期状態では0）
                float hitRate = exercise.GetHitRate();
                if (hitRate == 0f)
                {
                    LogPass("3-A: NeighborCache - GetHitRate()が正しく動作");
                }
                else
                {
                    resultLog.AppendLine($"  [INFO] 3-A: NeighborCache - ヒット率: {hitRate * 100:F1}%");
                }

                // メモリ推定テスト
                int memUsage = exercise.GetEstimatedMemoryUsage();
                resultLog.AppendLine($"  [INFO] 3-A: NeighborCache - 推定メモリ使用量: {memUsage} bytes");
            }
            catch (Exception e)
            {
                LogFail($"3-A: NeighborCache - 例外発生: {e.Message}");
            }
        }

        private static void TestDecisionCache(DecisionCache_Exercise exercise)
        {
            try
            {
                // キャッシュサイズテスト
                exercise.ClearAllCache();
                int initialSize = exercise.GetCacheSize();

                if (initialSize == 0)
                {
                    LogPass("3-B: DecisionCache - ClearAllCache()が正しく動作");
                }
                else
                {
                    LogFail($"3-B: DecisionCache - ClearAllCache()後もキャッシュが残っている ({initialSize})");
                }

                // ヒット率テスト（初期状態では0）
                float hitRate = exercise.GetHitRate();
                if (hitRate == 0f)
                {
                    LogPass("3-B: DecisionCache - GetHitRate()が正しく動作");
                }
                else
                {
                    resultLog.AppendLine($"  [INFO] 3-B: DecisionCache - ヒット率: {hitRate * 100:F1}%");
                }

                // メモリ推定テスト
                int memUsage = exercise.GetEstimatedMemoryUsage();
                resultLog.AppendLine($"  [INFO] 3-B: DecisionCache - 推定メモリ使用量: {memUsage} bytes");
            }
            catch (Exception e)
            {
                LogFail($"3-B: DecisionCache - 例外発生: {e.Message}");
            }
        }

        private static void LogPass(string message)
        {
            resultLog.AppendLine($"  [PASS] {message}");
            passCount++;
        }

        private static void LogFail(string message)
        {
            resultLog.AppendLine($"  [FAIL] {message}");
            failCount++;
        }
    }

    /// <summary>
    /// テスト結果を表示するウィンドウ
    /// </summary>
    public class TestResultWindow : EditorWindow
    {
        private string results;
        private int passCount;
        private int failCount;
        private Vector2 scrollPosition;

        public static void ShowResults(string results, int passCount, int failCount)
        {
            var window = GetWindow<TestResultWindow>("Test Results");
            window.results = results;
            window.passCount = passCount;
            window.failCount = failCount;
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnGUI()
        {
            // ヘッダー
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField($"結果: {passCount} PASS / {failCount} FAIL",
                failCount == 0 ? EditorStyles.boldLabel : EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            // 結果表示
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var style = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                richText = false
            };

            EditorGUILayout.TextArea(results, style, GUILayout.ExpandHeight(true));

            EditorGUILayout.EndScrollView();

            // ボタン
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("クリップボードにコピー"))
            {
                EditorGUIUtility.systemCopyBuffer = results;
                Debug.Log("テスト結果をクリップボードにコピーしました");
            }
            if (GUILayout.Button("閉じる"))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
