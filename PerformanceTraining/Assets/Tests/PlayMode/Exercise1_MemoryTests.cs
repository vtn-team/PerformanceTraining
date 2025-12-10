using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Unity.Profiling;
using PerformanceTraining.Core;

namespace PerformanceTraining.Tests
{
    /// <summary>
    /// 課題1: メモリ最適化（GC Alloc削減）テスト
    /// </summary>
    [TestFixture]
    [Category("Exercise1_Memory")]
    public class Exercise1_MemoryTests
    {
        private const int MIN_CHARACTER_COUNT = 10;
        private const string GAME_SCENE_NAME = "MainGame";

        // テスト設定
        private const float TEST_DURATION = 10f;          // テスト時間（秒）
        private const float SPIKE_THRESHOLD_MS = 20f;     // スパイク閾値（ms）
        private const int MAX_ALLOWED_SPIKES = 3;         // 許容スパイク回数
        private const long MAX_AVG_GC_ALLOC_BYTES = 1024; // 平均GC Alloc上限（1KB）

        private static bool _sceneLoaded = false;
        private CharacterManager _characterManager;
        private Character[] _characters;

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
            _characters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);

            yield return null;
        }

        // ================================================================
        // Test 1: 環境チェック
        // ================================================================

        [UnityTest]
        [Order(0)]
        public IEnumerator Test_01_EnvironmentCheck()
        {
            // CharacterManager存在確認
            Assert.IsNotNull(_characterManager,
                "環境エラー: CharacterManager がシーン内に見つかりません。\n" +
                "MainGameシーンが正しく設定されているか確認してください。");

            // キャラクター数確認
            Assert.GreaterOrEqual(_characterManager.AliveCount, MIN_CHARACTER_COUNT,
                $"環境エラー: キャラクター数が不足しています（現在: {_characterManager.AliveCount}）。\n" +
                "GameManagerの初期スポーン数を確認してください。");

            // Character配列確認
            Assert.IsTrue(_characters.Length > 0,
                "環境エラー: Characterが見つかりません。");

            Debug.Log($"[Test 1: 環境チェック] OK\n" +
                $"  CharacterManager: 存在\n" +
                $"  キャラクター数: {_characterManager.AliveCount}");

            yield return null;
        }

        // ================================================================
        // Test 2: GC Alloc 平均チェック（1KB以下）
        // ================================================================

        [UnityTest]
        [Order(1)]
        public IEnumerator Test_02_GCAlloc_Average()
        {
            Assert.IsNotNull(_characterManager, "CharacterManager が見つかりません");

            // PlayerLoop内のGC Allocを計測
            using var gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc");

            float elapsed = 0f;
            long totalGCAlloc = 0;
            long maxGCAlloc = 0;
            int frameCount = 0;

            Debug.Log($"[Test 2: GC Alloc] 計測開始 ({TEST_DURATION}秒)");

            while (elapsed < TEST_DURATION)
            {
                yield return null;

                long frameGCAlloc = gcAllocRecorder.LastValue;
                frameCount++;

                totalGCAlloc += frameGCAlloc;
                if (frameGCAlloc > maxGCAlloc)
                {
                    maxGCAlloc = frameGCAlloc;
                }

                elapsed += Time.deltaTime;
            }

            long avgGCAlloc = frameCount > 0 ? totalGCAlloc / frameCount : 0;
            double avgGCAllocKB = avgGCAlloc / 1024.0;

            Debug.Log($"[Test 2: GC Alloc] 結果:\n" +
                $"  テスト時間: {elapsed:F1}s\n" +
                $"  フレーム数: {frameCount}\n" +
                $"  GC Alloc (平均): {avgGCAlloc:N0} bytes ({avgGCAllocKB:F2} KB)\n" +
                $"  GC Alloc (最大): {maxGCAlloc:N0} bytes\n" +
                $"  GC Alloc (合計): {totalGCAlloc:N0} bytes\n" +
                $"  目標: 平均 {MAX_AVG_GC_ALLOC_BYTES:N0} bytes (1 KB) 以下");

            Assert.LessOrEqual(avgGCAlloc, MAX_AVG_GC_ALLOC_BYTES,
                $"GC Alloc: フレームあたりの平均GC Allocが多すぎます。\n" +
                $"現在: {avgGCAlloc:N0} bytes ({avgGCAllocKB:F2} KB) / frame\n" +
                $"目標: {MAX_AVG_GC_ALLOC_BYTES:N0} bytes (1 KB) 以下\n\n" +
                "【原因と対策】\n" +
                "1. StringBuilderで文字列結合を最適化\n" +
                "2. ObjectPoolを実装してInstantiate/Destroyを削減\n" +
                "3. 毎フレームのnew List<>()等を避ける\n" +
                "4. デリゲートやラムダ式をキャッシュ");

            yield return null;
        }

        // ================================================================
        // Test 3: スパイク回数チェック
        // ================================================================

        [UnityTest]
        [Order(2)]
        public IEnumerator Test_03_Spike_Count()
        {
            Assert.IsNotNull(_characterManager, "CharacterManager が見つかりません");

            // PlayerLoopの負荷を計測
            using var playerLoopRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "PlayerLoop");

            float elapsed = 0f;
            int spikeCount = 0;
            double maxFrameTime = 0;
            int frameCount = 0;
            var spikeFrameTimes = new System.Collections.Generic.List<double>();

            Debug.Log($"[Test 3: スパイク] 計測開始 ({TEST_DURATION}秒, 閾値: {SPIKE_THRESHOLD_MS}ms)");

            while (elapsed < TEST_DURATION)
            {
                yield return null;

                // PlayerLoopの時間をナノ秒からミリ秒に変換
                double playerLoopMs = playerLoopRecorder.LastValue / 1_000_000.0;
                frameCount++;

                if (playerLoopMs > maxFrameTime)
                {
                    maxFrameTime = playerLoopMs;
                }

                // スパイク検出
                if (playerLoopMs > SPIKE_THRESHOLD_MS)
                {
                    spikeCount++;
                    spikeFrameTimes.Add(playerLoopMs);
                }

                elapsed += Time.deltaTime;
            }

            string spikeInfo = spikeFrameTimes.Count > 0
                ? string.Join(", ", spikeFrameTimes.ConvertAll(t => $"{t:F1}ms"))
                : "なし";

            Debug.Log($"[Test 3: スパイク] 結果:\n" +
                $"  テスト時間: {elapsed:F1}s\n" +
                $"  フレーム数: {frameCount}\n" +
                $"  スパイク閾値: {SPIKE_THRESHOLD_MS}ms\n" +
                $"  スパイク回数: {spikeCount} (許容: {MAX_ALLOWED_SPIKES}回以下)\n" +
                $"  最大フレーム時間: {maxFrameTime:F2}ms\n" +
                $"  スパイク詳細: {spikeInfo}");

            Assert.LessOrEqual(spikeCount, MAX_ALLOWED_SPIKES,
                $"スパイク: 頻繁なフレーム落ちが検出されました。\n" +
                $"スパイク回数: {spikeCount} 回（{SPIKE_THRESHOLD_MS}ms超過）\n" +
                $"許容回数: {MAX_ALLOWED_SPIKES} 回以下\n" +
                $"最大フレーム時間: {maxFrameTime:F1}ms\n\n" +
                "【原因と対策】\n" +
                "1. ObjectPoolを実装してInstantiate/Destroyを削減\n" +
                "2. 大量のGC Allocを減らしてGCスパイクを防止\n" +
                "3. 重い処理を分散して実行");

            yield return null;
        }
    }
}
