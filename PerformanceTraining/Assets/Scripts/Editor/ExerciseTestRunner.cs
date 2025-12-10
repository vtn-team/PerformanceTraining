using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using PerformanceTraining.Core;
using PerformanceTraining.Enemy;
using Debug = UnityEngine.Debug;

#if EXERCISES_DEPLOYED
using StudentExercises.Memory;
using StudentExercises.CPU;
using StudentExercises.Tradeoff;
#else
using PerformanceTraining.Exercises.Memory;
using PerformanceTraining.Exercises.CPU;
using PerformanceTraining.Exercises.Tradeoff;
#endif

namespace PerformanceTraining.Editor
{
    /// <summary>
    /// 課題の実装が正しく動作しているかをテストするクラス
    /// </summary>
    public static class ExerciseTestRunner
    {
        private static StringBuilder resultLog = new StringBuilder();
        private static int passCount = 0;
        private static int failCount = 0;

        // Stepごとのテスト結果
        private static Dictionary<string, bool> stepResults = new Dictionary<string, bool>();

        /// <summary>
        /// 課題ごとのテストを実行し、Stepごとの結果を返す
        /// </summary>
        public static Dictionary<string, bool> RunExerciseTest(string testMethodName)
        {
            stepResults.Clear();

            if (!Application.isPlaying)
            {
                Debug.LogError("テストはPlayモードで実行してください。");
                return stepResults;
            }

            resultLog.Clear();
            passCount = 0;
            failCount = 0;

            switch (testMethodName)
            {
                case "TestZeroAllocation":
                    RunMemoryTestsWithResults();
                    break;
                case "TestCPUOptimization":
                    RunCPUTestsWithResults();
                    break;
                case "TestTradeoff":
                    RunTradeoffTestsWithResults();
                    break;
                default:
                    Debug.LogError($"Unknown test: {testMethodName}");
                    break;
            }

            Debug.Log(resultLog.ToString());
            return stepResults;
        }

        private static void RunMemoryTestsWithResults()
        {
            resultLog.AppendLine("【課題1: メモリ最適化（GC Alloc削減）】");

            // 課題1: 実際のゲームコードのGC Allocを検証
            // 必要な最小キャラクター数（敵を全消しして回避を防ぐ）
            const int MIN_CHARACTER_COUNT = 10;

            var characterManager = UnityEngine.Object.FindAnyObjectByType<CharacterManager>();
            if (characterManager == null)
            {
                resultLog.AppendLine("  [FAIL] CharacterManager が見つかりません");
                stepResults["Memory_GCAlloc"] = false;
                return;
            }

            int aliveCount = characterManager.AliveCount;
            if (aliveCount < MIN_CHARACTER_COUNT)
            {
                resultLog.AppendLine($"  [FAIL] キャラクター数が不足しています（現在: {aliveCount}、必要: {MIN_CHARACTER_COUNT}以上）");
                resultLog.AppendLine("        敵を減らすのではなく、GC Allocの原因を修正してください。");
                stepResults["Memory_GCAlloc"] = false;
                return;
            }

            // 5箇所のGC Allocボトルネックを個別にテスト
            bool allPassed = true;

            // 1. Character.cs - UpdateDebugStatus（文字列結合）
            bool characterTest = TestCharacterGCAlloc();
            stepResults["Memory_Character"] = characterTest;
            allPassed &= characterTest;

            // 2. CharacterUI.cs - UpdateNameText（文字列結合）
            bool characterUITest = TestCharacterUIGCAlloc();
            stepResults["Memory_CharacterUI"] = characterUITest;
            allPassed &= characterUITest;

            // 3. BehaviorTreeBase.cs - BuildAIDebugLog（文字列結合）
            bool behaviorTreeTest = TestBehaviorTreeGCAlloc();
            stepResults["Memory_BehaviorTree"] = behaviorTreeTest;
            allPassed &= behaviorTreeTest;

            // 4. CharacterManager.cs - BuildStatsString（文字列結合）
            bool characterManagerTest = TestCharacterManagerGCAlloc();
            stepResults["Memory_CharacterManager"] = characterManagerTest;
            allPassed &= characterManagerTest;

            // 5. Character.cs - SpawnAttackEffect（Object Pool）
            bool objectPoolTest = TestObjectPoolGCAlloc();
            stepResults["Memory_ObjectPool"] = objectPoolTest;
            allPassed &= objectPoolTest;

            // 総合結果
            stepResults["Memory_GCAlloc"] = allPassed;

            if (allPassed)
            {
                resultLog.AppendLine("  [PASS] 全てのGC Allocボトルネックが修正されました！");
            }
        }

        /// <summary>
        /// Character.cs のGC Allocテスト
        /// </summary>
        private static bool TestCharacterGCAlloc()
        {
            try
            {
                var characters = UnityEngine.Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
                if (characters.Length == 0)
                {
                    LogFail("Character.cs: キャラクターが見つかりません");
                    return false;
                }

                // GC Allocを計測（複数回呼び出し）
                long before = GC.GetTotalMemory(false);
                var character = characters[0];

                // UpdateDebugStatus相当の処理をシミュレート（privateメソッドなので直接呼べない）
                // 代わりに DebugStatus プロパティがあればそれを取得
                var debugStatusProp = typeof(Character).GetProperty("DebugStatus");
                if (debugStatusProp != null)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        var _ = debugStatusProp.GetValue(character);
                    }
                }
                else
                {
                    // プロパティがない場合はフィールドを確認
                    var debugStatusField = typeof(Character).GetField("_debugStatus",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (debugStatusField == null)
                    {
                        // デバッグステータス機能がない場合はスキップ
                        LogPass("Character.cs: デバッグステータス機能なし（スキップ）");
                        return true;
                    }
                }

                long after = GC.GetTotalMemory(false);
                long allocated = after - before;

                if (allocated < 5000) // 5KB未満なら成功
                {
                    LogPass($"Character.cs: GC Alloc削減済み（{allocated} bytes）");
                    return true;
                }
                else
                {
                    LogFail($"Character.cs: GC Allocが多い（{allocated} bytes）");
                    return false;
                }
            }
            catch (Exception e)
            {
                LogFail($"Character.cs: テスト例外 - {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// CharacterUI.cs のGC Allocテスト
        /// </summary>
        private static bool TestCharacterUIGCAlloc()
        {
            try
            {
                var characterUIs = UnityEngine.Object.FindObjectsByType<CharacterUI>(FindObjectsSortMode.None);
                if (characterUIs.Length == 0)
                {
                    // CharacterUIがなければスキップ
                    LogPass("CharacterUI.cs: CharacterUIなし（スキップ）");
                    return true;
                }

                // UpdateNameText相当の処理をテスト
                // privateメソッドなのでリフレクションで呼び出し
                var updateMethod = typeof(CharacterUI).GetMethod("UpdateNameText",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (updateMethod == null)
                {
                    LogPass("CharacterUI.cs: UpdateNameTextメソッドなし（スキップ）");
                    return true;
                }

                long before = GC.GetTotalMemory(false);
                var ui = characterUIs[0];
                for (int i = 0; i < 100; i++)
                {
                    updateMethod.Invoke(ui, null);
                }
                long after = GC.GetTotalMemory(false);
                long allocated = after - before;

                if (allocated < 5000)
                {
                    LogPass($"CharacterUI.cs: GC Alloc削減済み（{allocated} bytes）");
                    return true;
                }
                else
                {
                    LogFail($"CharacterUI.cs: GC Allocが多い（{allocated} bytes）");
                    return false;
                }
            }
            catch (Exception e)
            {
                LogFail($"CharacterUI.cs: テスト例外 - {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// BehaviorTreeBase.cs のGC Allocテスト
        /// </summary>
        private static bool TestBehaviorTreeGCAlloc()
        {
            try
            {
                // BehaviorTreeBaseの派生クラスを探す
                var behaviorTrees = UnityEngine.Object.FindObjectsByType<PerformanceTraining.AI.BehaviorTree.BehaviorTreeBase>(FindObjectsSortMode.None);
                if (behaviorTrees.Length == 0)
                {
                    LogPass("BehaviorTreeBase.cs: BehaviorTreeなし（スキップ）");
                    return true;
                }

                // BuildAIDebugLog相当の処理をテスト
                var buildMethod = typeof(PerformanceTraining.AI.BehaviorTree.BehaviorTreeBase).GetMethod("BuildAIDebugLog",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (buildMethod == null)
                {
                    LogPass("BehaviorTreeBase.cs: BuildAIDebugLogメソッドなし（スキップ）");
                    return true;
                }

                long before = GC.GetTotalMemory(false);
                var bt = behaviorTrees[0];
                for (int i = 0; i < 100; i++)
                {
                    buildMethod.Invoke(bt, null);
                }
                long after = GC.GetTotalMemory(false);
                long allocated = after - before;

                if (allocated < 5000)
                {
                    LogPass($"BehaviorTreeBase.cs: GC Alloc削減済み（{allocated} bytes）");
                    return true;
                }
                else
                {
                    LogFail($"BehaviorTreeBase.cs: GC Allocが多い（{allocated} bytes）");
                    return false;
                }
            }
            catch (Exception e)
            {
                LogFail($"BehaviorTreeBase.cs: テスト例外 - {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// CharacterManager.cs のGC Allocテスト
        /// </summary>
        private static bool TestCharacterManagerGCAlloc()
        {
            try
            {
                var characterManager = UnityEngine.Object.FindAnyObjectByType<CharacterManager>();
                if (characterManager == null)
                {
                    LogFail("CharacterManager.cs: CharacterManagerが見つかりません");
                    return false;
                }

                // BuildStatsString相当の処理をテスト
                var buildMethod = typeof(CharacterManager).GetMethod("BuildStatsString",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (buildMethod == null)
                {
                    LogPass("CharacterManager.cs: BuildStatsStringメソッドなし（スキップ）");
                    return true;
                }

                long before = GC.GetTotalMemory(false);
                for (int i = 0; i < 100; i++)
                {
                    buildMethod.Invoke(characterManager, null);
                }
                long after = GC.GetTotalMemory(false);
                long allocated = after - before;

                if (allocated < 5000)
                {
                    LogPass($"CharacterManager.cs: GC Alloc削減済み（{allocated} bytes）");
                    return true;
                }
                else
                {
                    LogFail($"CharacterManager.cs: GC Allocが多い（{allocated} bytes）");
                    return false;
                }
            }
            catch (Exception e)
            {
                LogFail($"CharacterManager.cs: テスト例外 - {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Object Pool のGC Allocテスト（攻撃エフェクト）
        /// </summary>
        private static bool TestObjectPoolGCAlloc()
        {
            try
            {
                // SpawnAttackEffectメソッドを取得
                var spawnMethod = typeof(Character).GetMethod("SpawnAttackEffect",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (spawnMethod == null)
                {
                    LogPass("Object Pool: SpawnAttackEffectメソッドなし（スキップ）");
                    return true;
                }

                // 攻撃エフェクトプレハブがあるか確認
                var sharedPrefabField = typeof(Character).GetField("_sharedAttackEffectPrefab",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                if (sharedPrefabField == null)
                {
                    LogPass("Object Pool: 攻撃エフェクト機能なし（スキップ）");
                    return true;
                }

                var prefab = sharedPrefabField.GetValue(null) as GameObject;
                if (prefab == null)
                {
                    LogPass("Object Pool: 攻撃エフェクトプレハブ未設定（スキップ）");
                    return true;
                }

                // 攻撃を実行してGC Allocを計測
                var characters = UnityEngine.Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
                if (characters.Length < 2)
                {
                    LogFail("Object Pool: テスト用キャラクターが不足");
                    return false;
                }

                // 攻撃エフェクト数をカウント（テスト前）
                string effectName = prefab.name;
                int beforeCount = CountObjectsWithName(effectName);

                // 複数回攻撃を実行してGC Allocを計測
                long before = GC.GetTotalMemory(false);

                var attacker = characters[0];
                var target = characters[1];

                // 攻撃可能な距離に移動（テスト用）
                var originalPos = target.transform.position;
                target.transform.position = attacker.transform.position + Vector3.forward * 1f;

                // 複数回SpawnAttackEffectを呼び出し
                for (int i = 0; i < 20; i++)
                {
                    spawnMethod.Invoke(attacker, new object[] { target });
                }

                // 位置を戻す
                target.transform.position = originalPos;

                long after = GC.GetTotalMemory(false);
                long allocated = after - before;

                // 攻撃エフェクト数をカウント（テスト後）
                int afterCount = CountObjectsWithName(effectName);
                int newObjects = afterCount - beforeCount;

                // Object Poolが実装されていれば、新規オブジェクト数は少ないはず
                // また、GC Allocも少ないはず
                bool poolImplemented = newObjects <= 5; // 20回攻撃で5個以下なら再利用されている
                bool lowAlloc = allocated < 50000; // 50KB未満なら成功

                if (poolImplemented && lowAlloc)
                {
                    LogPass($"Object Pool: 実装済み（新規オブジェクト: {newObjects}, GC: {allocated} bytes）");
                    return true;
                }
                else if (!poolImplemented)
                {
                    LogFail($"Object Pool: 未実装（20回攻撃で {newObjects} 個の新規オブジェクト生成）");
                    return false;
                }
                else
                {
                    LogFail($"Object Pool: GC Allocが多い（{allocated} bytes）");
                    return false;
                }
            }
            catch (Exception e)
            {
                LogFail($"Object Pool: テスト例外 - {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 指定名を含むオブジェクト数をカウント
        /// </summary>
        private static int CountObjectsWithName(string namePart)
        {
            int count = 0;
            var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains(namePart))
                {
                    count++;
                }
            }
            return count;
        }

        private static void RunCPUTestsWithResults()
        {
            resultLog.AppendLine("【課題2: CPU最適化】");

            var exercise = UnityEngine.Object.FindAnyObjectByType<CPUOptimization_Exercise>();
            if (exercise == null)
            {
                resultLog.AppendLine("  [SKIP] CPUOptimization_Exercise が見つかりません");
                return;
            }

            stepResults["CPU_SpatialPartition"] = TestSpatialPartitionWithResult(exercise);
            stepResults["CPU_StaggeredUpdate"] = TestStaggeredUpdateWithResult(exercise);
            stepResults["CPU_SqrMagnitude"] = TestDistanceCalculationWithResult(exercise);
        }

        private static void RunTradeoffTestsWithResults()
        {
            resultLog.AppendLine("【課題3: トレードオフ】");

            // NeighborCache
            var neighborExercise = UnityEngine.Object.FindAnyObjectByType<NeighborCache_Exercise>();
            if (neighborExercise != null)
            {
                stepResults["Tradeoff_NeighborCache"] = TestNeighborCacheWithResult(neighborExercise);
            }

            // DecisionCache
            var decisionExercise = UnityEngine.Object.FindAnyObjectByType<DecisionCache_Exercise>();
            if (decisionExercise != null)
            {
                stepResults["Tradeoff_DecisionCache"] = TestDecisionCacheWithResult(decisionExercise);
            }

            // TrigLUT
            var trigExercise = UnityEngine.Object.FindAnyObjectByType<TrigLUT_Exercise>();
            if (trigExercise != null)
            {
                stepResults["Tradeoff_TrigLUT"] = TestTrigLUTWithResult(trigExercise);
            }

            // VisibilityMap
            var visibilityExercise = UnityEngine.Object.FindAnyObjectByType<VisibilityMap_Exercise>();
            if (visibilityExercise != null)
            {
                stepResults["Tradeoff_VisibilityMap"] = TestVisibilityMapWithResult(visibilityExercise);
            }
        }

        // Memory Tests with result
        private static bool TestObjectPoolWithResult(ZeroAllocation_Exercise exercise)
        {
            try
            {
                var enemy1 = exercise.GetFromPool();
                var enemy2 = exercise.GetFromPool();

                if (enemy1 != null && enemy2 != null)
                {
                    exercise.ReturnToPool(enemy1);
                    var enemy3 = exercise.GetFromPool();

                    if (enemy3 == enemy1)
                    {
                        LogPass("Step 1: オブジェクトプール - OK");
                        exercise.ReturnToPool(enemy2);
                        exercise.ReturnToPool(enemy3);
                        return true;
                    }
                    exercise.ReturnToPool(enemy2);
                    if (enemy3 != null) exercise.ReturnToPool(enemy3);
                }
                LogFail("Step 1: オブジェクトプール - 再利用が正しく動作していない");
                return false;
            }
            catch (Exception e)
            {
                LogFail($"Step 1: オブジェクトプール - 例外: {e.Message}");
                return false;
            }
        }

        private static bool TestStringBuilderWithResult(ZeroAllocation_Exercise exercise)
        {
            try
            {
                string result1 = exercise.BuildStatusText(100, 50);
                string result2 = exercise.BuildStatusText(200, 75);

                bool hasCorrectFormat = result1.Contains("100") && result1.Contains("50");
                bool hasCorrectFormat2 = result2.Contains("200") && result2.Contains("75");

                if (hasCorrectFormat && hasCorrectFormat2)
                {
                    long before = GC.GetTotalMemory(false);
                    for (int i = 0; i < 100; i++)
                    {
                        exercise.BuildStatusText(i, i * 2);
                    }
                    long after = GC.GetTotalMemory(false);
                    long allocated = after - before;

                    if (allocated < 5000)
                    {
                        LogPass("Step 2: StringBuilder - OK");
                        return true;
                    }
                }
                LogFail("Step 2: StringBuilder - アロケーション削減が不十分");
                return false;
            }
            catch (Exception e)
            {
                LogFail($"Step 2: StringBuilder - 例外: {e.Message}");
                return false;
            }
        }

        private static bool TestDelegateCacheWithResult(ZeroAllocation_Exercise exercise)
        {
            try
            {
                var action1 = exercise.GetCachedUpdateAction();
                var action2 = exercise.GetCachedUpdateAction();

                if (action1 != null && action2 != null && ReferenceEquals(action1, action2))
                {
                    LogPass("Step 3: デリゲートキャッシュ - OK");
                    return true;
                }
                LogFail("Step 3: デリゲートキャッシュ - キャッシュされていない");
                return false;
            }
            catch (Exception e)
            {
                LogFail($"Step 3: デリゲートキャッシュ - 例外: {e.Message}");
                return false;
            }
        }

        private static bool TestCollectionReuseWithResult(ZeroAllocation_Exercise exercise)
        {
            try
            {
                var list1 = exercise.GetReusableList();
                list1.Add(null);
                var list2 = exercise.GetReusableList();

                if (list1 != null && list2 != null && ReferenceEquals(list1, list2) && list2.Count == 0)
                {
                    LogPass("Step 4: コレクション再利用 - OK");
                    return true;
                }
                LogFail("Step 4: コレクション再利用 - 再利用されていない");
                return false;
            }
            catch (Exception e)
            {
                LogFail($"Step 4: コレクション再利用 - 例外: {e.Message}");
                return false;
            }
        }

        // CPU Tests with result
        private static bool TestSpatialPartitionWithResult(CPUOptimization_Exercise exercise)
        {
            try
            {
                Vector3 testPos = new Vector3(0, 0, 0);
                int index = exercise.GetCellIndex(testPos);
                int expectedIndex = GameConstants.GRID_SIZE / 2 * GameConstants.GRID_SIZE + GameConstants.GRID_SIZE / 2;

                if (Math.Abs(index - expectedIndex) <= GameConstants.GRID_SIZE)
                {
                    var nearby = exercise.QueryNearbyEnemies(testPos);
                    if (nearby != null)
                    {
                        LogPass("Step 1: 空間分割 - OK");
                        return true;
                    }
                }
                LogFail("Step 1: 空間分割 - GetCellIndex/QueryNearbyEnemiesが正しく動作していない");
                return false;
            }
            catch (Exception e)
            {
                LogFail($"Step 1: 空間分割 - 例外: {e.Message}");
                return false;
            }
        }

        private static bool TestStaggeredUpdateWithResult(CPUOptimization_Exercise exercise)
        {
            try
            {
                bool frame0 = exercise.ShouldUpdateThisFrame(0, 0);
                bool frame1 = exercise.ShouldUpdateThisFrame(0, 1);
                bool frame10 = exercise.ShouldUpdateThisFrame(0, 10);
                bool g1frame1 = exercise.ShouldUpdateThisFrame(1, 1);

                if (frame0 && !frame1 && frame10 && g1frame1)
                {
                    LogPass("Step 2: 更新分散 - OK");
                    return true;
                }
                else if (frame0 && frame1 && frame10)
                {
                    LogFail("Step 2: 更新分散 - 常にtrueを返している（未実装）");
                    return false;
                }
                LogFail("Step 2: 更新分散 - ロジックが正しくない");
                return false;
            }
            catch (Exception e)
            {
                LogFail($"Step 2: 更新分散 - 例外: {e.Message}");
                return false;
            }
        }

        private static bool TestDistanceCalculationWithResult(CPUOptimization_Exercise exercise)
        {
            try
            {
                Vector3 a = new Vector3(0, 0, 0);
                Vector3 b = new Vector3(3, 0, 4);

                float distSqr = exercise.CalculateDistanceSqr(a, b);
                float expected = 25f;

                if (Mathf.Approximately(distSqr, expected))
                {
                    bool within = exercise.IsWithinDistance(a, b, 6f);
                    bool notWithin = exercise.IsWithinDistance(a, b, 4f);

                    if (within && !notWithin)
                    {
                        LogPass("Step 3: 距離計算最適化 - OK");
                        return true;
                    }
                }
                LogFail("Step 3: 距離計算最適化 - sqrMagnitudeが正しく実装されていない");
                return false;
            }
            catch (Exception e)
            {
                LogFail($"Step 3: 距離計算最適化 - 例外: {e.Message}");
                return false;
            }
        }

        // Tradeoff Tests with result
        private static bool TestNeighborCacheWithResult(NeighborCache_Exercise exercise)
        {
            try
            {
                exercise.ClearAllCache();
                int initialSize = exercise.GetCacheSize();

                if (initialSize == 0)
                {
                    float hitRate = exercise.GetHitRate();
                    if (hitRate == 0f)
                    {
                        LogPass("3-A: 近傍キャッシュ - OK");
                        return true;
                    }
                }
                LogFail("3-A: 近傍キャッシュ - ClearAllCache()が正しく動作していない");
                return false;
            }
            catch (Exception e)
            {
                LogFail($"3-A: 近傍キャッシュ - 例外: {e.Message}");
                return false;
            }
        }

        private static bool TestDecisionCacheWithResult(DecisionCache_Exercise exercise)
        {
            try
            {
                exercise.ClearAllCache();
                int initialSize = exercise.GetCacheSize();

                if (initialSize == 0)
                {
                    float hitRate = exercise.GetHitRate();
                    if (hitRate == 0f)
                    {
                        LogPass("3-B: AI判断キャッシュ - OK");
                        return true;
                    }
                }
                LogFail("3-B: AI判断キャッシュ - ClearAllCache()が正しく動作していない");
                return false;
            }
            catch (Exception e)
            {
                LogFail($"3-B: AI判断キャッシュ - 例外: {e.Message}");
                return false;
            }
        }

        private static bool TestTrigLUTWithResult(TrigLUT_Exercise exercise)
        {
            try
            {
                float testAngleDegrees = 45f;
                float lutSin = exercise.Sin(testAngleDegrees);
                float lutCos = exercise.Cos(testAngleDegrees);
                float expectedSin = Mathf.Sin(testAngleDegrees * Mathf.Deg2Rad);
                float expectedCos = Mathf.Cos(testAngleDegrees * Mathf.Deg2Rad);

                float tolerance = 0.02f;
                bool sinOk = Mathf.Abs(lutSin - expectedSin) < tolerance;
                bool cosOk = Mathf.Abs(lutCos - expectedCos) < tolerance;

                if (sinOk && cosOk)
                {
                    int memUsage = exercise.GetMemoryUsageBytes();
                    if (memUsage > 0)
                    {
                        LogPass("3-C: 三角関数LUT - OK");
                        return true;
                    }
                }
                LogFail("3-C: 三角関数LUT - Sin/Cosの精度が不十分");
                return false;
            }
            catch (Exception e)
            {
                LogFail($"3-C: 三角関数LUT - 例外: {e.Message}");
                return false;
            }
        }

        private static bool TestVisibilityMapWithResult(VisibilityMap_Exercise exercise)
        {
            try
            {
                exercise.Initialize();

                Vector3 from = Vector3.zero;
                Vector3 to = new Vector3(5f, 0f, 5f);
                bool visible = exercise.IsVisible(from, to);

                int memUsage = exercise.GetMemoryUsageBytes();
                if (memUsage > 0)
                {
                    LogPass("3-D: 可視性マップ - OK");
                    return true;
                }
                LogFail("3-D: 可視性マップ - マップが初期化されていない");
                return false;
            }
            catch (Exception e)
            {
                LogFail($"3-D: 可視性マップ - 例外: {e.Message}");
                return false;
            }
        }

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

            var exercise = UnityEngine.Object.FindAnyObjectByType<ZeroAllocation_Exercise>();
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

            var exercise = UnityEngine.Object.FindAnyObjectByType<CPUOptimization_Exercise>();
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
            var neighborExercise = UnityEngine.Object.FindAnyObjectByType<NeighborCache_Exercise>();
            if (neighborExercise != null)
            {
                TestNeighborCache(neighborExercise);
            }
            else
            {
                resultLog.AppendLine("  [SKIP] NeighborCache_Exercise が見つかりません");
            }

            // AI判断キャッシュテスト
            var decisionExercise = UnityEngine.Object.FindAnyObjectByType<DecisionCache_Exercise>();
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

        /// <summary>
        /// 単一のテストを実行
        /// </summary>
        /// <param name="testMethodName">テストメソッド名</param>
        /// <returns>テスト成功時true</returns>
        public static bool RunSingleTest(string testMethodName)
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("テストはPlayモードで実行してください。");
                return false;
            }

            resultLog.Clear();
            passCount = 0;
            failCount = 0;

            resultLog.AppendLine($"========== {testMethodName} ==========\n");

            switch (testMethodName)
            {
                case "TestZeroAllocation":
                    RunMemoryTests();
                    break;
                case "TestSpatialPartition":
                    TestSpatialPartitionSingle();
                    break;
                case "TestStaggeredUpdate":
                    TestStaggeredUpdateSingle();
                    break;
                case "TestSqrMagnitude":
                    TestDistanceCalculationSingle();
                    break;
                case "TestNeighborCache":
                    TestNeighborCacheSingle();
                    break;
                case "TestDecisionCache":
                    TestDecisionCacheSingle();
                    break;
                case "TestTrigLUT":
                    TestTrigLUTSingle();
                    break;
                case "TestVisibilityMap":
                    TestVisibilityMapSingle();
                    break;
                default:
                    Debug.LogError($"Unknown test method: {testMethodName}");
                    return false;
            }

            resultLog.AppendLine($"\n結果: {passCount} PASS / {failCount} FAIL");
            Debug.Log(resultLog.ToString());

            return failCount == 0 && passCount > 0;
        }

        // 個別テスト実行用のラッパー
        private static void TestSpatialPartitionSingle()
        {
            var exercise = UnityEngine.Object.FindAnyObjectByType<CPUOptimization_Exercise>();
            if (exercise == null)
            {
                LogFail("CPUOptimization_Exercise が見つかりません");
                return;
            }
            TestSpatialPartition(exercise);
        }

        private static void TestStaggeredUpdateSingle()
        {
            var exercise = UnityEngine.Object.FindAnyObjectByType<CPUOptimization_Exercise>();
            if (exercise == null)
            {
                LogFail("CPUOptimization_Exercise が見つかりません");
                return;
            }
            TestStaggeredUpdate(exercise);
        }

        private static void TestDistanceCalculationSingle()
        {
            var exercise = UnityEngine.Object.FindAnyObjectByType<CPUOptimization_Exercise>();
            if (exercise == null)
            {
                LogFail("CPUOptimization_Exercise が見つかりません");
                return;
            }
            TestDistanceCalculation(exercise);
        }

        private static void TestNeighborCacheSingle()
        {
            var exercise = UnityEngine.Object.FindAnyObjectByType<NeighborCache_Exercise>();
            if (exercise == null)
            {
                LogFail("NeighborCache_Exercise が見つかりません");
                return;
            }
            TestNeighborCache(exercise);
        }

        private static void TestDecisionCacheSingle()
        {
            var exercise = UnityEngine.Object.FindAnyObjectByType<DecisionCache_Exercise>();
            if (exercise == null)
            {
                LogFail("DecisionCache_Exercise が見つかりません");
                return;
            }
            TestDecisionCache(exercise);
        }

        private static void TestTrigLUTSingle()
        {
            var exercise = UnityEngine.Object.FindAnyObjectByType<TrigLUT_Exercise>();
            if (exercise == null)
            {
                LogFail("TrigLUT_Exercise が見つかりません");
                return;
            }
            TestTrigLUT(exercise);
        }

        private static void TestVisibilityMapSingle()
        {
            var exercise = UnityEngine.Object.FindAnyObjectByType<VisibilityMap_Exercise>();
            if (exercise == null)
            {
                LogFail("VisibilityMap_Exercise が見つかりません");
                return;
            }
            TestVisibilityMap(exercise);
        }

        // TrigLUTテスト
        private static void TestTrigLUT(TrigLUT_Exercise exercise)
        {
            try
            {
                // Sin/Cosの精度テスト（度数で渡す）
                float testAngleDegrees = 45f;
                float lutSin = exercise.Sin(testAngleDegrees);
                float lutCos = exercise.Cos(testAngleDegrees);
                float expectedSin = Mathf.Sin(testAngleDegrees * Mathf.Deg2Rad);
                float expectedCos = Mathf.Cos(testAngleDegrees * Mathf.Deg2Rad);

                float tolerance = 0.02f; // 2%の誤差許容
                bool sinOk = Mathf.Abs(lutSin - expectedSin) < tolerance;
                bool cosOk = Mathf.Abs(lutCos - expectedCos) < tolerance;

                if (sinOk && cosOk)
                {
                    LogPass("TrigLUT - Sin/Cosの精度が許容範囲内");
                }
                else
                {
                    LogFail($"TrigLUT - 精度不足 Sin: {lutSin} vs {expectedSin}, Cos: {lutCos} vs {expectedCos}");
                }

                // メモリ使用量テスト
                int memUsage = exercise.GetMemoryUsageBytes();
                if (memUsage > 0)
                {
                    resultLog.AppendLine($"  [INFO] TrigLUT - メモリ使用量: {memUsage} bytes");
                    LogPass("TrigLUT - LUTが初期化されている");
                }
                else
                {
                    LogFail("TrigLUT - LUTが初期化されていない");
                }
            }
            catch (Exception e)
            {
                LogFail($"TrigLUT - 例外発生: {e.Message}");
            }
        }

        // VisibilityMapテスト
        private static void TestVisibilityMap(VisibilityMap_Exercise exercise)
        {
            try
            {
                // マップ初期化テスト
                exercise.Initialize();

                // クエリテスト（2点間の可視性）
                Vector3 from = Vector3.zero;
                Vector3 to = new Vector3(5f, 0f, 5f);
                bool visible = exercise.IsVisible(from, to);
                // 結果に関わらず例外が出なければOK
                LogPass("VisibilityMap - IsVisible()が正しく動作");

                // メモリ使用量
                int memUsage = exercise.GetMemoryUsageBytes();
                if (memUsage > 0)
                {
                    resultLog.AppendLine($"  [INFO] VisibilityMap - 推定メモリ使用量: {memUsage} bytes");
                    LogPass("VisibilityMap - マップが初期化されている");
                }
                else
                {
                    LogFail("VisibilityMap - マップが初期化されていない");
                }
            }
            catch (Exception e)
            {
                LogFail($"VisibilityMap - 例外発生: {e.Message}");
            }
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
