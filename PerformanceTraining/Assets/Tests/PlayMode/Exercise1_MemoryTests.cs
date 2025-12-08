using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Profiling;
using PerformanceTraining.Core;
using PerformanceTraining.AI.BehaviorTree;

namespace PerformanceTraining.Tests
{
    /// <summary>
    /// 課題1: メモリ最適化（GC Alloc削減）テスト
    /// Unity Test Runner + ProfilerRecorder を使用
    /// </summary>
    [TestFixture]
    [Category("Exercise1_Memory")]
    public class Exercise1_MemoryTests
    {
        // テスト用の最小キャラクター数
        private const int MIN_CHARACTER_COUNT = 10;

        // GC Alloc許容値（バイト）
        private const long MAX_ALLOWED_GC_ALLOC = 1024; // 1KB

        // Object Pool テスト用の許容新規オブジェクト数
        private const int MAX_NEW_OBJECTS_FOR_POOL = 5;

        private CharacterManager _characterManager;
        private Character[] _characters;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // シーン内のオブジェクトを取得
            _characterManager = Object.FindAnyObjectByType<CharacterManager>();
            _characters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_MinimumCharacterCount()
        {
            // キャラクター数が最低数以上あることを確認（敵全消し防止）
            Assert.IsNotNull(_characterManager, "CharacterManager が見つかりません");

            int aliveCount = _characterManager.AliveCount;
            Assert.GreaterOrEqual(aliveCount, MIN_CHARACTER_COUNT,
                $"キャラクター数が不足しています（現在: {aliveCount}、必要: {MIN_CHARACTER_COUNT}以上）。" +
                "敵を減らすのではなく、GC Allocの原因を修正してください。");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_Character_UpdateDebugStatus_GCAlloc()
        {
            // Character.cs - UpdateDebugStatus のGC Allocテスト
            Assert.IsTrue(_characters.Length > 0, "キャラクターが見つかりません");

            var character = _characters[0];
            var updateMethod = typeof(Character).GetMethod("UpdateDebugStatus",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (updateMethod == null)
            {
                Assert.Pass("UpdateDebugStatus メソッドが存在しません（スキップ）");
                yield break;
            }

            // ProfilerRecorderでGC Allocを計測
            long gcAlloc = MeasureGCAlloc(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    updateMethod.Invoke(character, null);
                }
            });

            Assert.LessOrEqual(gcAlloc, MAX_ALLOWED_GC_ALLOC * 100,
                $"Character.cs UpdateDebugStatus: GC Allocが多い（{gcAlloc} bytes / 100回）。" +
                "StringBuilderを使用して最適化してください。");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_CharacterUI_UpdateNameText_GCAlloc()
        {
            // CharacterUI.cs - UpdateNameText のGC Allocテスト
            var characterUIs = Object.FindObjectsByType<CharacterUI>(FindObjectsSortMode.None);

            if (characterUIs.Length == 0)
            {
                Assert.Pass("CharacterUI が存在しません（スキップ）");
                yield break;
            }

            var ui = characterUIs[0];
            var updateMethod = typeof(CharacterUI).GetMethod("UpdateNameText",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (updateMethod == null)
            {
                Assert.Pass("UpdateNameText メソッドが存在しません（スキップ）");
                yield break;
            }

            long gcAlloc = MeasureGCAlloc(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    updateMethod.Invoke(ui, null);
                }
            });

            Assert.LessOrEqual(gcAlloc, MAX_ALLOWED_GC_ALLOC * 100,
                $"CharacterUI.cs UpdateNameText: GC Allocが多い（{gcAlloc} bytes / 100回）。" +
                "StringBuilderを使用して最適化してください。");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_BehaviorTree_BuildAIDebugLog_GCAlloc()
        {
            // BehaviorTreeBase.cs - BuildAIDebugLog のGC Allocテスト
            var behaviorTrees = Object.FindObjectsByType<BehaviorTreeBase>(FindObjectsSortMode.None);

            if (behaviorTrees.Length == 0)
            {
                Assert.Pass("BehaviorTree が存在しません（スキップ）");
                yield break;
            }

            var bt = behaviorTrees[0];
            var buildMethod = typeof(BehaviorTreeBase).GetMethod("BuildAIDebugLog",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (buildMethod == null)
            {
                Assert.Pass("BuildAIDebugLog メソッドが存在しません（スキップ）");
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
                $"BehaviorTreeBase.cs BuildAIDebugLog: GC Allocが多い（{gcAlloc} bytes / 100回）。" +
                "StringBuilderを使用して最適化してください。");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_CharacterManager_BuildStatsString_GCAlloc()
        {
            // CharacterManager.cs - BuildStatsString のGC Allocテスト
            Assert.IsNotNull(_characterManager, "CharacterManager が見つかりません");

            var buildMethod = typeof(CharacterManager).GetMethod("BuildStatsString",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (buildMethod == null)
            {
                Assert.Pass("BuildStatsString メソッドが存在しません（スキップ）");
                yield break;
            }

            long gcAlloc = MeasureGCAlloc(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    buildMethod.Invoke(_characterManager, null);
                }
            });

            Assert.LessOrEqual(gcAlloc, MAX_ALLOWED_GC_ALLOC * 100,
                $"CharacterManager.cs BuildStatsString: GC Allocが多い（{gcAlloc} bytes / 100回）。" +
                "StringBuilderを使用して最適化してください。");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_ObjectPool_SpawnAttackEffect()
        {
            // Character.cs - SpawnAttackEffect のObject Poolテスト
            Assert.IsTrue(_characters.Length >= 2, "テスト用キャラクターが不足しています");

            var spawnMethod = typeof(Character).GetMethod("SpawnAttackEffect",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (spawnMethod == null)
            {
                Assert.Pass("SpawnAttackEffect メソッドが存在しません（スキップ）");
                yield break;
            }

            // 攻撃エフェクトプレハブを確認
            var prefabField = typeof(Character).GetField("_sharedAttackEffectPrefab",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (prefabField == null)
            {
                Assert.Pass("攻撃エフェクト機能が存在しません（スキップ）");
                yield break;
            }

            var prefab = prefabField.GetValue(null) as GameObject;
            if (prefab == null)
            {
                Assert.Pass("攻撃エフェクトプレハブが未設定です（スキップ）");
                yield break;
            }

            var attacker = _characters[0];
            var target = _characters[1];

            // 攻撃可能な距離に配置
            var originalPos = target.transform.position;
            target.transform.position = attacker.transform.position + Vector3.forward * 1f;

            // テスト前のエフェクト数をカウント
            string effectName = prefab.name;
            int beforeCount = CountObjectsWithName(effectName);

            // 20回攻撃エフェクトを生成
            long gcAlloc = MeasureGCAlloc(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    spawnMethod.Invoke(attacker, new object[] { target });
                }
            });

            // 位置を戻す
            target.transform.position = originalPos;

            // テスト後のエフェクト数をカウント
            int afterCount = CountObjectsWithName(effectName);
            int newObjects = afterCount - beforeCount;

            // Object Poolが実装されていれば新規オブジェクト数は少ないはず
            Assert.LessOrEqual(newObjects, MAX_NEW_OBJECTS_FOR_POOL,
                $"Object Pool: 未実装（20回攻撃で {newObjects} 個の新規オブジェクト生成）。" +
                "Object Poolを実装してInstantiate/Destroyを避けてください。");

            yield return null;
        }

        /// <summary>
        /// ProfilerRecorderを使用してGC Allocを計測
        /// </summary>
        private long MeasureGCAlloc(System.Action action)
        {
            // GC Allocを記録するProfilerRecorderを開始
            var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc");

            // 計測前にGCを実行してベースラインを安定させる
            System.GC.Collect();

            // アクションを実行
            action();

            // 記録を停止
            recorder.Stop();

            // 合計値を取得（複数サンプルの場合はCurrentValueを使用）
            long totalAlloc = 0;
            if (recorder.Valid && recorder.Count > 0)
            {
                // 全サンプルの合計を計算
                for (int i = 0; i < recorder.Count; i++)
                {
                    totalAlloc += recorder.GetSample(i).Value;
                }
            }

            recorder.Dispose();

            return totalAlloc;
        }

        /// <summary>
        /// 指定名を含むオブジェクト数をカウント
        /// </summary>
        private int CountObjectsWithName(string namePart)
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
