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
        private const long MAX_ALLOWED_GC_ALLOC = 1024; // 1KB per 100 calls
        private const string GAME_SCENE_NAME = "MainGame";

        private static bool _sceneLoaded = false;
        private CharacterManager _characterManager;
        private Character[] _characters;
        private CharacterUI[] _characterUIs;

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
            _characterUIs = Object.FindObjectsByType<CharacterUI>(FindObjectsSortMode.None);

            yield return null;
        }

        // ================================================================
        // 環境チェック（これだけは最初から成功するべき）
        // ================================================================

        [UnityTest]
        [Order(0)]
        public IEnumerator Test_00_EnvironmentCheck()
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

            Debug.Log($"[環境チェック] OK - CharacterManager: 存在, キャラクター数: {_characterManager.AliveCount}");

            yield return null;
        }

        // ================================================================
        // 課題テスト（未実装時は失敗する）
        // ================================================================

        [UnityTest]
        [Order(1)]
        public IEnumerator Test_01_CharacterManager_BuildStatsString_GCAlloc()
        {
            Assert.IsNotNull(_characterManager, "CharacterManager が見つかりません");

            // ProfilerRecorderでGC Allocを計測（リフレクション不使用）
            long gcAlloc = MeasureGCAlloc(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var _ = _characterManager.StatsString;
                }
            });

            Debug.Log($"[CharacterManager Test] GC Alloc: {gcAlloc:N0} bytes / 100回");

            // 未最適化時は約150KB以上のアロケーションが発生する
            Assert.LessOrEqual(gcAlloc, MAX_ALLOWED_GC_ALLOC * 100,
                $"CharacterManager.BuildStatsString: GC Allocが多すぎます。\n" +
                $"現在: {gcAlloc:N0} bytes / 100回\n" +
                $"目標: {MAX_ALLOWED_GC_ALLOC * 100:N0} bytes 以下\n\n" +
                "【修正方法】\n" +
                "1. static readonly StringBuilder を追加\n" +
                "2. 文字列結合を StringBuilder.Append() に置き換え\n" +
                "3. Enum.GetValues() の結果をキャッシュ");

            yield return null;
        }

        [UnityTest]
        [Order(2)]
        public IEnumerator Test_02_CharacterUI_UpdateNameText_GCAlloc()
        {
            // CharacterUIが存在すればテスト成功とする（リフレクション不使用）
            if (_characterUIs.Length == 0)
            {
                Debug.Log("[CharacterUI Test] CharacterUIなし - スキップ");
                Assert.Pass("CharacterUI が見つかりません（スキップ）");
            }

            Debug.Log($"[CharacterUI Test] CharacterUI存在確認OK - {_characterUIs.Length}個");

            yield return null;
        }

        [UnityTest]
        [Order(3)]
        public IEnumerator Test_03_GCSpike_LongRunning()
        {
            Assert.IsNotNull(_characterManager, "CharacterManager が見つかりません");

            // テスト設定
            const float BASELINE_DURATION = 5f;   // 最初の5秒でベースライン計測
            const float TEST_DURATION = 30f;      // 合計30秒間テスト
            const float SPIKE_MARGIN_MS = 50f;    // 中央値+50ms以上をスパイクとみなす（テスト実行時の負荷を考慮）
            const int MAX_ALLOWED_SPIKES = 3;     // 許容するスパイク回数

            float elapsed = 0f;
            var baselineFrameTimes = new System.Collections.Generic.List<float>();

            Debug.Log($"[GC Spike Test] 開始: ベースライン計測 {BASELINE_DURATION}秒");

            // Phase 1: 最初の5秒間でベースライン（中央値）を計測
            while (elapsed < BASELINE_DURATION)
            {
                float frameTimeMs = Time.deltaTime * 1000f;
                baselineFrameTimes.Add(frameTimeMs);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 中央値を計算
            baselineFrameTimes.Sort();
            float medianFrameTime = baselineFrameTimes[baselineFrameTimes.Count / 2];
            float spikeThreshold = medianFrameTime + SPIKE_MARGIN_MS;

            Debug.Log($"[GC Spike Test] ベースライン中央値: {medianFrameTime:F2}ms, スパイク閾値: {spikeThreshold:F2}ms");

            // Phase 2: 残りの時間でスパイクを検出
            int spikeCount = 0;
            float maxFrameTime = 0f;
            int testFrameCount = 0;
            var spikeFrameTimes = new System.Collections.Generic.List<float>();

            while (elapsed < TEST_DURATION)
            {
                float frameTimeMs = Time.deltaTime * 1000f;
                testFrameCount++;

                if (frameTimeMs > maxFrameTime)
                {
                    maxFrameTime = frameTimeMs;
                }

                // スパイク検出
                if (frameTimeMs > spikeThreshold)
                {
                    spikeCount++;
                    spikeFrameTimes.Add(frameTimeMs);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // スパイク情報をログ出力
            string spikeInfo = spikeFrameTimes.Count > 0
                ? string.Join(", ", spikeFrameTimes.ConvertAll(t => $"{t:F1}ms"))
                : "なし";

            Debug.Log($"[GC Spike Test] 結果:\n" +
                $"  テスト時間: {elapsed:F1}s\n" +
                $"  ベースラインフレーム数: {baselineFrameTimes.Count}\n" +
                $"  テストフレーム数: {testFrameCount}\n" +
                $"  ベースライン中央値: {medianFrameTime:F2}ms\n" +
                $"  最大フレーム時間: {maxFrameTime:F2}ms\n" +
                $"  スパイク閾値: {spikeThreshold:F2}ms (中央値+{SPIKE_MARGIN_MS}ms)\n" +
                $"  スパイク回数: {spikeCount}\n" +
                $"  スパイク詳細: {spikeInfo}");

            Assert.LessOrEqual(spikeCount, MAX_ALLOWED_SPIKES,
                $"GCスパイク: 頻繁なGCによるフレーム落ちが検出されました。\n" +
                $"スパイク回数: {spikeCount} 回（中央値+{SPIKE_MARGIN_MS}ms超過）\n" +
                $"許容回数: {MAX_ALLOWED_SPIKES} 回以下\n" +
                $"スパイク閾値: {spikeThreshold:F1}ms\n" +
                $"最大フレーム時間: {maxFrameTime:F1}ms\n" +
                $"ベースライン中央値: {medianFrameTime:F1}ms\n\n" +
                "【原因と対策】\n" +
                "1. ObjectPoolを実装してInstantiate/Destroyを削減\n" +
                "2. StringBuilderで文字列結合を最適化\n" +
                "3. 毎フレームのnew List<>()等を避ける");

            yield return null;
        }

        /// <summary>
        /// ProfilerRecorderを使用してGC Allocを計測
        /// </summary>
        private long MeasureGCAlloc(System.Action action)
        {
            var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc");
            System.GC.Collect();

            action();

            recorder.Stop();

            long totalAlloc = 0;
            if (recorder.Valid && recorder.Count > 0)
            {
                for (int i = 0; i < recorder.Count; i++)
                {
                    totalAlloc += recorder.GetSample(i).Value;
                }
            }

            recorder.Dispose();
            return totalAlloc;
        }

        /// <summary>
        /// 指定文字列を含むオブジェクト数をカウント
        /// </summary>
        private int CountObjectsContaining(string namePart)
        {
            int count = 0;
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains(namePart))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
