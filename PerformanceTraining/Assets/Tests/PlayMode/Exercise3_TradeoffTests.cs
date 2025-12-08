using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PerformanceTraining.Core;

#if EXERCISES_DEPLOYED
using StudentExercises.Tradeoff;
#else
using PerformanceTraining.Exercises.Tradeoff;
#endif

namespace PerformanceTraining.Tests
{
    /// <summary>
    /// 課題3: トレードオフテスト（GPU Instancing）
    /// Unity Test Runner + PlayMode テストを使用
    ///
    /// 【テストの実行方法】
    /// Window > General > Test Runner > PlayMode タブ
    /// Exercise3_TradeoffTests を選択して Run Selected
    /// </summary>
    [TestFixture]
    [Category("Exercise3_Tradeoff")]
    public class Exercise3_TradeoffTests
    {
        private CharacterManager _characterManager;

        // テスト用の最小キャラクター数
        private const int MIN_CHARACTER_COUNT = 10;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _characterManager = Object.FindAnyObjectByType<CharacterManager>();
            yield return null;
        }

        // ================================================================
        // GPU Instancing テスト
        // ================================================================

        [UnityTest]
        public IEnumerator Test_GPUInstancing_ExerciseExists()
        {
            var exercise = Object.FindAnyObjectByType<GPUInstancing_Exercise>();
            Assert.IsNotNull(exercise,
                "GPUInstancing_Exercise がシーン内に見つかりません。\n" +
                "シーンに GPUInstancing_Exercise コンポーネントを追加してください。");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_GPUInstancing_CollectInstanceData()
        {
            var exercise = Object.FindAnyObjectByType<GPUInstancing_Exercise>();
            if (exercise == null)
            {
                Assert.Fail("GPUInstancing_Exercise が見つかりません");
                yield break;
            }

            if (_characterManager == null || _characterManager.AliveCount < MIN_CHARACTER_COUNT)
            {
                Assert.Fail("キャラクターが不足しています（最低10体必要）");
                yield break;
            }

            // インスタンシングを有効化
            exercise.UseInstancing = true;

            // 1フレーム待機して描画を実行
            yield return null;
            yield return null;

            // インスタンス数を確認
            int instanceCount = exercise.LastInstanceCount;
            int aliveCount = _characterManager.AliveCount;

            // 未実装の場合は0を返す
            if (instanceCount == 0)
            {
                Assert.Fail(
                    "GPUInstancing CollectInstanceData: 未実装です（インスタンス数が0です）。\n" +
                    "各キャラクターのTransformからMatrix4x4を作成してください。\n" +
                    "ヒント:\n" +
                    "  _matrices[i] = characters[i].transform.localToWorldMatrix;\n" +
                    "  _colors[i] = GetColorForCharacter(characters[i]);");
            }
            else
            {
                // インスタンス数がキャラクター数と一致するか確認
                int expectedCount = Mathf.Min(aliveCount, 1023);
                Assert.That(instanceCount, Is.EqualTo(expectedCount),
                    $"GPUInstancing: インスタンス数({instanceCount})がキャラクター数({expectedCount})と一致しません。");
            }

            // クリーンアップ
            exercise.UseInstancing = false;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_GPUInstancing_RenderInstanced()
        {
            var exercise = Object.FindAnyObjectByType<GPUInstancing_Exercise>();
            if (exercise == null)
            {
                Assert.Fail("GPUInstancing_Exercise が見つかりません");
                yield break;
            }

            if (_characterManager == null || _characterManager.AliveCount < MIN_CHARACTER_COUNT)
            {
                Assert.Fail("キャラクターが不足しています（最低10体必要）");
                yield break;
            }

            // RenderInstancedメソッドを取得（プライベートなのでリフレクション）
            var renderMethod = typeof(GPUInstancing_Exercise).GetMethod("RenderInstanced",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (renderMethod == null)
            {
                Assert.Fail("RenderInstanced メソッドが見つかりません");
                yield break;
            }

            // インスタンシングを有効化して実行
            exercise.UseInstancing = true;
            yield return null;

            // エラーなく実行されれば成功
            // 実際の描画はFrame Debuggerで確認
            Assert.IsTrue(exercise.UseInstancing,
                "GPUInstancing: UseInstancing が false になっています。");

            // クリーンアップ
            exercise.UseInstancing = false;
            yield return null;
        }
    }
}
