using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Unity.Profiling;
using PerformanceTraining.Core;
using PerformanceTraining.AI.BehaviorTree;

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

            var buildMethod = typeof(CharacterManager).GetMethod("BuildStatsString",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(buildMethod, "BuildStatsString メソッドが見つかりません");

            long gcAlloc = MeasureGCAlloc(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    buildMethod.Invoke(_characterManager, null);
                }
            });

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
        public IEnumerator Test_02_Character_UpdateDebugStatus_GCAlloc()
        {
            Assert.IsTrue(_characters.Length > 0, "キャラクターが見つかりません");

            var character = _characters[0];
            var updateMethod = typeof(Character).GetMethod("UpdateDebugStatus",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (updateMethod == null)
            {
                Assert.Inconclusive("UpdateDebugStatus メソッドが存在しません（スキップ）");
                yield break;
            }

            long gcAlloc = MeasureGCAlloc(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    updateMethod.Invoke(character, null);
                }
            });

            Assert.LessOrEqual(gcAlloc, MAX_ALLOWED_GC_ALLOC * 100,
                $"Character.UpdateDebugStatus: GC Allocが多すぎます。\n" +
                $"現在: {gcAlloc:N0} bytes / 100回\n" +
                $"目標: {MAX_ALLOWED_GC_ALLOC * 100:N0} bytes 以下\n\n" +
                "【修正方法】StringBuilderを使用して文字列結合を最適化");

            yield return null;
        }

        [UnityTest]
        [Order(3)]
        public IEnumerator Test_03_BehaviorTree_BuildAIDebugLog_GCAlloc()
        {
            var behaviorTrees = Object.FindObjectsByType<BehaviorTreeBase>(FindObjectsSortMode.None);

            if (behaviorTrees.Length == 0)
            {
                Assert.Inconclusive("BehaviorTree が存在しません（スキップ）");
                yield break;
            }

            var bt = behaviorTrees[0];
            var buildMethod = typeof(BehaviorTreeBase).GetMethod("BuildAIDebugLog",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (buildMethod == null)
            {
                Assert.Inconclusive("BuildAIDebugLog メソッドが存在しません（スキップ）");
                yield break;
            }

            long gcAlloc = MeasureGCAlloc(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    buildMethod.Invoke(bt, null);
                }
            });

            Assert.LessOrEqual(gcAlloc, MAX_ALLOWED_GC_ALLOC * 100,
                $"BehaviorTreeBase.BuildAIDebugLog: GC Allocが多すぎます。\n" +
                $"現在: {gcAlloc:N0} bytes / 100回\n" +
                $"目標: {MAX_ALLOWED_GC_ALLOC * 100:N0} bytes 以下\n\n" +
                "【修正方法】StringBuilderを使用して文字列結合を最適化");

            yield return null;
        }

        [UnityTest]
        [Order(4)]
        public IEnumerator Test_04_ObjectPool_SpawnAttackEffect()
        {
            Assert.IsNotNull(_characterManager, "CharacterManager が見つかりません");
            Assert.GreaterOrEqual(_characterManager.AliveCount, MIN_CHARACTER_COUNT,
                "キャラクターが不足しています");

            // ゲームプレイ中の最大エフェクト数を記録
            int maxEffectCount = 0;
            int sampleCount = 0;

            // 3秒間ゲームプレイを観察
            float observeTime = 3.0f;
            float elapsed = 0f;

            while (elapsed < observeTime)
            {
                // 現在のエフェクト数をカウント
                int currentCount = CountObjectsContaining("Effect");
                if (currentCount > maxEffectCount)
                {
                    maxEffectCount = currentCount;
                }
                sampleCount++;

                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            // Object Poolが実装されていれば、最大エフェクト数はプールサイズ程度に収まる
            // 未実装の場合、エフェクトが大量に生成される（Destroyまでの0.5秒間蓄積）
            const int MAX_EXPECTED_EFFECTS = 20; // Pool実装時の想定最大数

            Debug.Log($"[ObjectPool Test] 観測時間: {observeTime}s, サンプル数: {sampleCount}, 最大エフェクト数: {maxEffectCount}");

            Assert.LessOrEqual(maxEffectCount, MAX_EXPECTED_EFFECTS,
                $"ObjectPool: 未実装または不十分です。\n" +
                $"ゲームプレイ中の最大エフェクト数: {maxEffectCount} 個\n" +
                $"目標: {MAX_EXPECTED_EFFECTS} 個以下（プールから再利用）\n\n" +
                "【実装方法】Character.cs の SpawnAttackEffect を修正:\n" +
                "1. ObjectPoolを作成（Queue<GameObject>等）\n" +
                "2. Instantiate → Pool.Get() に置き換え\n" +
                "3. Destroy → Pool.Return() に置き換え");

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
