using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using PerformanceTraining.Core;

namespace PerformanceTraining.Tests
{
    /// <summary>
    /// 課題2: CPU最適化テスト
    /// </summary>
    [TestFixture]
    [Category("Exercise2_CPU")]
    public class Exercise2_CPUTests
    {
        private const int MIN_CHARACTER_COUNT = 10;
        private const float MAX_EXECUTION_TIME_MS = 5.0f;
        private const string GAME_SCENE_NAME = "MainGame";

        private static bool _sceneLoaded = false;
        private CharacterManager _characterManager;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _sceneLoaded = false;
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (!_sceneLoaded)
            {
                var currentScene = SceneManager.GetActiveScene();
                if (currentScene.name != GAME_SCENE_NAME)
                {
                    var loadOp = SceneManager.LoadSceneAsync(GAME_SCENE_NAME, LoadSceneMode.Single);
                    while (!loadOp.isDone)
                    {
                        yield return null;
                    }
                }
                _sceneLoaded = true;
                yield return new WaitForSeconds(1.0f);
            }

            _characterManager = Object.FindAnyObjectByType<CharacterManager>();
            yield return null;
        }

        // ================================================================
        // 環境チェック（これだけは最初から成功するべき）
        // ================================================================

        [UnityTest]
        [Order(0)]
        public IEnumerator Test_00_EnvironmentCheck()
        {
            Assert.IsNotNull(_characterManager,
                "環境エラー: CharacterManager がシーン内に見つかりません。\n" +
                "MainGameシーンが正しく設定されているか確認してください。");

            Assert.GreaterOrEqual(_characterManager.AliveCount, MIN_CHARACTER_COUNT,
                $"環境エラー: キャラクター数が不足しています（現在: {_characterManager.AliveCount}）。\n" +
                "GameManagerの初期スポーン数を確認してください。");

            Debug.Log($"[環境チェック] OK - CharacterManager: 存在, キャラクター数: {_characterManager.AliveCount}");

            yield return null;
        }

        // ================================================================
        // 課題テスト（未実装時は失敗する）
        // ================================================================

        [UnityTest]
        [Order(1)]
        public IEnumerator Test_01_SpatialPartition_GetCellIndex()
        {
            Assert.IsNotNull(_characterManager, "CharacterManager が見つかりません");

            // 異なる2点でテスト
            Vector3 pos1 = Vector3.zero;
            Vector3 pos2 = new Vector3(20, 0, 20);

            int index1 = _characterManager.GetCellIndex(pos1);
            int index2 = _characterManager.GetCellIndex(pos2);

            // 両方0を返す場合は未実装
            if (index1 == 0 && index2 == 0)
            {
                Assert.Fail(
                    "GetCellIndex: 未実装です（常に0を返しています）。\n\n" +
                    "【実装方法】CharacterManager.cs の GetCellIndex を実装してください:\n" +
                    "int x = (int)((position.x + FIELD_HALF_SIZE) / _cellSize);\n" +
                    "int z = (int)((position.z + FIELD_HALF_SIZE) / _cellSize);\n" +
                    "x = Mathf.Clamp(x, 0, _gridWidth - 1);\n" +
                    "z = Mathf.Clamp(z, 0, _gridWidth - 1);\n" +
                    "return z * _gridWidth + x;");
            }

            // 異なる位置で異なるインデックスを返すべき
            Assert.AreNotEqual(index1, index2,
                "GetCellIndex: 異なる座標で同じインデックスを返しています。\n" +
                "座標からセルインデックスへの変換を確認してください。");

            yield return null;
        }

        [UnityTest]
        [Order(2)]
        public IEnumerator Test_02_SpatialPartition_GetNearbyCharacters()
        {
            Assert.IsNotNull(_characterManager, "CharacterManager が見つかりません");

            if (_characterManager.AliveCount < MIN_CHARACTER_COUNT)
            {
                Assert.Inconclusive("キャラクターが不足しています（スキップ）");
                yield break;
            }

            // 空間グリッドを更新
            _characterManager.UpdateSpatialGrid();

            var firstChar = _characterManager.AliveCharacters[0];
            var nearby = _characterManager.GetNearbyCharactersOptimized(firstChar.transform.position, firstChar);

            Assert.IsNotNull(nearby, "GetNearbyCharactersOptimized: nullを返してはいけません");

            // 全キャラクターを返している場合は未実装
            int allCount = _characterManager.AliveCount - 1;
            if (nearby.Count == allCount && allCount > 20)
            {
                Assert.Fail(
                    $"GetNearbyCharactersOptimized: 最適化が未実装です。\n" +
                    $"全キャラクター({allCount}体)を返しています。\n\n" +
                    "【実装方法】周辺9セル（3x3）のみからキャラクターを取得:\n" +
                    "1. GetCellIndex で中心セルを取得\n" +
                    "2. 周辺8セル + 中心セルをループ\n" +
                    "3. 各セルのキャラクターをリストに追加");
            }

            yield return null;
        }

        [UnityTest]
        [Order(3)]
        public IEnumerator Test_03_ProcessingOrder_FindBestAttackTarget()
        {
            Assert.IsNotNull(_characterManager, "CharacterManager が見つかりません");

            if (_characterManager.AliveCount < 2)
            {
                Assert.Inconclusive("テスト用キャラクターが不足しています（スキップ）");
                yield break;
            }

            var attacker = _characterManager.AliveCharacters[0];

            // 複数回実行して平均を取る
            float totalTime = 0f;
            int iterations = 5;

            for (int i = 0; i < iterations; i++)
            {
                _characterManager.FindBestAttackTarget(attacker);
                totalTime += _characterManager.GetLastExecutionTimeMs();
                yield return null;
            }

            float avgTime = totalTime / iterations;

            Assert.That(avgTime, Is.LessThan(MAX_EXECUTION_TIME_MS),
                $"FindBestAttackTarget: 実行時間が長すぎます。\n" +
                $"現在: {avgTime:F2}ms（平均）\n" +
                $"目標: {MAX_EXECUTION_TIME_MS}ms 未満\n\n" +
                "【最適化方法】処理順序を並び替えてください:\n" +
                "1. FilterByDistance（軽い）を最初に\n" +
                "2. FilterByHP（軽い）を次に\n" +
                "3. SortByPathfindingDistance（重い）を最後に\n" +
                "また GetNearbyCharactersOptimized を使用して検索範囲を限定");

            yield return null;
        }
    }
}
