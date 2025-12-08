using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PerformanceTraining.Core;

#if EXERCISES_DEPLOYED
using StudentExercises.CPU;
#else
using PerformanceTraining.Exercises.CPU;
#endif

namespace PerformanceTraining.Tests
{
    /// <summary>
    /// 課題2: CPU最適化テスト
    /// Unity Test Runner + PlayMode テストを使用
    ///
    /// 【テストの実行方法】
    /// Window > General > Test Runner > PlayMode タブ
    /// Exercise2_CPUTests を選択して Run Selected
    /// </summary>
    [TestFixture]
    [Category("Exercise2_CPU")]
    public class Exercise2_CPUTests
    {
        private CPUOptimization_Exercise _exercise;
        private CharacterManager _characterManager;

        // テスト用の最小キャラクター数
        private const int MIN_CHARACTER_COUNT = 10;

        // 処理時間の許容値（ミリ秒）
        private const float MAX_EXECUTION_TIME_MS = 5.0f;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // シーン内のオブジェクトを取得
            _exercise = Object.FindAnyObjectByType<CPUOptimization_Exercise>();
            _characterManager = Object.FindAnyObjectByType<CharacterManager>();

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_CPUOptimization_ExerciseExists()
        {
            // CPUOptimization_Exercise がシーンに存在することを確認
            Assert.IsNotNull(_exercise,
                "CPUOptimization_Exercise がシーン内に見つかりません。\n" +
                "シーンに CPUOptimization_Exercise コンポーネントを追加してください。");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_MinimumCharacterCount()
        {
            // キャラクター数が最低数以上あることを確認
            Assert.IsNotNull(_characterManager, "CharacterManager が見つかりません");

            int aliveCount = _characterManager.AliveCount;
            Assert.GreaterOrEqual(aliveCount, MIN_CHARACTER_COUNT,
                $"キャラクター数が不足しています（現在: {aliveCount}、必要: {MIN_CHARACTER_COUNT}以上）。");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_SpatialPartition_GetCellIndex()
        {
            if (_exercise == null)
            {
                Assert.Pass("CPUOptimization_Exercise が見つかりません（スキップ）");
                yield break;
            }

            // 中心座標(0,0,0)でのセルインデックスをテスト
            Vector3 centerPos = Vector3.zero;
            int index = _exercise.GetCellIndex(centerPos);

            // 未実装の場合は0を返す
            if (index == 0)
            {
                // 別の位置でも0なら未実装
                Vector3 otherPos = new Vector3(10, 0, 10);
                int otherIndex = _exercise.GetCellIndex(otherPos);

                if (otherIndex == 0)
                {
                    Assert.Fail(
                        "GetCellIndex: 未実装です（常に0を返しています）。\n" +
                        "座標からセルインデックスを計算してください。\n" +
                        "ヒント: x = (position.x + FIELD_HALF_SIZE) / cellSize\n" +
                        "       index = z * gridWidth + x");
                }
            }

            // 中心のセルインデックスは (GRID_SIZE/2) * GRID_SIZE + (GRID_SIZE/2) に近いはず
            int expectedCenter = (GameConstants.GRID_SIZE / 2) * GameConstants.GRID_SIZE + (GameConstants.GRID_SIZE / 2);

            // 許容範囲内かチェック（グリッドサイズの1行分の誤差を許容）
            Assert.That(Mathf.Abs(index - expectedCenter), Is.LessThanOrEqualTo(GameConstants.GRID_SIZE),
                $"GetCellIndex: セルインデックスの計算が不正です。\n" +
                $"座標 (0,0,0) に対して index={index} が返されましたが、" +
                $"約 {expectedCenter} 付近を期待しています。");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_SpatialPartition_GetNearbyCharacters()
        {
            if (_exercise == null)
            {
                Assert.Pass("CPUOptimization_Exercise が見つかりません（スキップ）");
                yield break;
            }

            if (_characterManager == null || _characterManager.AliveCount < MIN_CHARACTER_COUNT)
            {
                Assert.Pass("キャラクターが不足しています（スキップ）");
                yield break;
            }

            // 空間グリッドを更新
            _exercise.UpdateSpatialGrid();

            // 最初のキャラクターの位置で近傍検索
            var firstChar = _characterManager.AliveCharacters[0];
            var nearby = _exercise.GetNearbyCharacters(firstChar.transform.position, firstChar);

            // 空のリストが返される場合は未実装の可能性
            if (nearby.Count == 0)
            {
                // 全キャラクターを取得して比較
                var all = _exercise.GetAllCharacters(firstChar);

                if (all.Count > 0)
                {
                    Assert.Fail(
                        "GetNearbyCharacters: 未実装です（空のリストを返しています）。\n" +
                        "周辺9セル（3x3）からキャラクターを取得してください。\n" +
                        "ヒント: 中心セルと周囲8セルをループして、各セルのキャラクターを追加");
                }
            }

            // nullを返さないことを確認
            Assert.IsNotNull(nearby,
                "GetNearbyCharacters: nullを返してはいけません。\n" +
                "空のListを返すか、周辺セルのキャラクターを返してください。");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_ProcessingOrder_ExecuteAttackSequence()
        {
            if (_exercise == null)
            {
                Assert.Pass("CPUOptimization_Exercise が見つかりません（スキップ）");
                yield break;
            }

            if (_characterManager == null || _characterManager.AliveCount < 2)
            {
                Assert.Pass("テスト用キャラクターが不足しています（スキップ）");
                yield break;
            }

            var attacker = _characterManager.AliveCharacters[0];

            // 複数回実行して平均を取る
            float totalTime = 0f;
            int iterations = 5;

            for (int i = 0; i < iterations; i++)
            {
                _exercise.ExecuteAttackSequence(attacker);
                totalTime += _exercise.GetLastExecutionTimeMs();
                yield return null;
            }

            float avgTime = totalTime / iterations;

            // 処理時間が許容範囲内かチェック
            Assert.That(avgTime, Is.LessThan(MAX_EXECUTION_TIME_MS),
                $"ExecuteAttackSequence: 実行時間が長すぎます（平均 {avgTime:F2}ms）。\n" +
                $"目標: {MAX_EXECUTION_TIME_MS}ms 未満\n" +
                "ヒント: 処理順序を最適化してください。\n" +
                "① FilterByDistance（軽い）を先に実行\n" +
                "② FilterByHP（軽い）を次に実行\n" +
                "③ SortByPathfindingDistance（重い）を最後に実行\n" +
                "また、GetAllCharacters を GetNearbyCharacters に置き換えてください。");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_DistanceCalculation_CalculateDistanceSqr()
        {
            if (_exercise == null)
            {
                Assert.Pass("CPUOptimization_Exercise が見つかりません（スキップ）");
                yield break;
            }

            // 3-4-5の直角三角形でテスト
            Vector3 a = new Vector3(0, 0, 0);
            Vector3 b = new Vector3(3, 0, 4);

            float distSqr = _exercise.CalculateDistanceSqr(a, b);
            float expected = 25f; // 3² + 4² = 25

            Assert.That(distSqr, Is.EqualTo(expected).Within(0.001f),
                $"CalculateDistanceSqr: 計算結果が不正です。\n" +
                $"結果: {distSqr}, 期待値: {expected}\n" +
                "ヒント: (a - b).sqrMagnitude を使用してください。");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_DistanceCalculation_IsWithinDistance()
        {
            if (_exercise == null)
            {
                Assert.Pass("CPUOptimization_Exercise が見つかりません（スキップ）");
                yield break;
            }

            Vector3 a = new Vector3(0, 0, 0);
            Vector3 b = new Vector3(3, 0, 4); // 距離 = 5

            // 距離6以内 → true（5 < 6）
            bool within6 = _exercise.IsWithinDistance(a, b, 6f);
            Assert.IsTrue(within6,
                "IsWithinDistance: 距離5の2点が距離6以内と判定されませんでした。");

            // 距離4以内 → false（5 > 4）
            bool within4 = _exercise.IsWithinDistance(a, b, 4f);
            Assert.IsFalse(within4,
                "IsWithinDistance: 距離5の2点が距離4以内と判定されました。");

            // 距離5以内 → true（5 <= 5、境界値）
            bool within5 = _exercise.IsWithinDistance(a, b, 5f);
            Assert.IsTrue(within5,
                "IsWithinDistance: 距離5の2点が距離5以内と判定されませんでした（境界値テスト）。");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_StaggeredUpdate_ShouldUpdateThisFrame()
        {
            if (_exercise == null)
            {
                Assert.Pass("CPUOptimization_Exercise が見つかりません（スキップ）");
                yield break;
            }

            // グループ0: フレーム0で更新
            bool g0f0 = _exercise.ShouldUpdateThisFrame(0, 0);
            bool g0f1 = _exercise.ShouldUpdateThisFrame(0, 1);
            bool g0f4 = _exercise.ShouldUpdateThisFrame(0, 4);

            // グループ1: フレーム1で更新
            bool g1f0 = _exercise.ShouldUpdateThisFrame(1, 0);
            bool g1f1 = _exercise.ShouldUpdateThisFrame(1, 1);

            // 未実装の場合は常にtrueを返す
            if (g0f0 && g0f1 && g0f4 && g1f0 && g1f1)
            {
                Assert.Fail(
                    "ShouldUpdateThisFrame: 未実装です（常にtrueを返しています）。\n" +
                    "ヒント: (frameCount % UPDATE_INTERVAL) == updateGroup で更新分散を実装してください。\n" +
                    "例: UPDATE_INTERVAL=4 の場合\n" +
                    "  グループ0は フレーム0, 4, 8... で更新\n" +
                    "  グループ1は フレーム1, 5, 9... で更新");
            }

            // グループ0はフレーム0で更新されるべき
            Assert.IsTrue(g0f0,
                "ShouldUpdateThisFrame: グループ0はフレーム0で更新されるべきです。");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_QueryNearbyEnemies()
        {
            if (_exercise == null)
            {
                Assert.Pass("CPUOptimization_Exercise が見つかりません（スキップ）");
                yield break;
            }

            // 中心位置での近傍敵取得
            Vector3 centerPos = Vector3.zero;
            var nearbyEnemies = _exercise.QueryNearbyEnemies(centerPos);

            // nullを返さないことを確認
            Assert.IsNotNull(nearbyEnemies,
                "QueryNearbyEnemies: nullを返してはいけません。\n" +
                "空のListを返すか、近傍の敵を返してください。");

            yield return null;
        }
    }
}
