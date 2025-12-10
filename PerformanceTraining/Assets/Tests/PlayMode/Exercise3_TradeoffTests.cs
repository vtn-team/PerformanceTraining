using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
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
    /// </summary>
    [TestFixture]
    [Category("Exercise3_Tradeoff")]
    public class Exercise3_TradeoffTests
    {
        private const int MIN_CHARACTER_COUNT = 10;
        private const string GAME_SCENE_NAME = "MainGame";

        private static bool _sceneLoaded = false;
        private CharacterManager _characterManager;
        private GPUInstancing_Exercise _exercise;

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
            _exercise = Object.FindAnyObjectByType<GPUInstancing_Exercise>();

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

            Assert.IsNotNull(_exercise,
                "環境エラー: GPUInstancing_Exercise がシーン内に見つかりません。\n" +
                "シーンに GPUInstancing_Exercise コンポーネントを追加してください。");

            Assert.GreaterOrEqual(_characterManager.AliveCount, MIN_CHARACTER_COUNT,
                $"環境エラー: キャラクター数が不足しています（現在: {_characterManager.AliveCount}）。");

            Debug.Log($"[環境チェック] OK - GPUInstancing_Exercise: 存在, キャラクター数: {_characterManager.AliveCount}");

            yield return null;
        }

        // ================================================================
        // 課題テスト（未実装時は失敗する）
        // ================================================================

        [UnityTest]
        [Order(1)]
        public IEnumerator Test_01_GPUInstancing_CollectInstanceData()
        {
            Assert.IsNotNull(_exercise, "GPUInstancing_Exercise が見つかりません");
            Assert.IsNotNull(_characterManager, "CharacterManager が見つかりません");

            if (_characterManager.AliveCount < MIN_CHARACTER_COUNT)
            {
                Assert.Inconclusive("キャラクターが不足しています（スキップ）");
                yield break;
            }

            // インスタンシングを有効化
            _exercise.UseInstancing = true;
            yield return null;
            yield return null;

            int instanceCount = _exercise.LastInstanceCount;
            int aliveCount = _characterManager.AliveCount;

            // 未実装の場合は0を返す
            if (instanceCount == 0)
            {
                Assert.Fail(
                    "CollectInstanceData: 未実装です（インスタンス数が0）。\n\n" +
                    "【実装方法】GPUInstancing_Exercise.cs の CollectInstanceData を実装:\n" +
                    "for (int i = 0; i < count; i++)\n" +
                    "{\n" +
                    "    _matrices[i] = characters[i].transform.localToWorldMatrix;\n" +
                    "    _colors[i] = GetColorForCharacterType(characters[i].Type);\n" +
                    "}");
            }

            // インスタンス数がキャラクター数と一致するか確認
            int expectedCount = Mathf.Min(aliveCount, 1023);
            Assert.That(instanceCount, Is.EqualTo(expectedCount),
                $"CollectInstanceData: インスタンス数({instanceCount})がキャラクター数({expectedCount})と一致しません。");

            _exercise.UseInstancing = false;
            yield return null;
        }

        [UnityTest]
        [Order(2)]
        public IEnumerator Test_02_GPUInstancing_RenderInstanced()
        {
            Assert.IsNotNull(_exercise, "GPUInstancing_Exercise が見つかりません");
            Assert.IsNotNull(_characterManager, "CharacterManager が見つかりません");

            if (_characterManager.AliveCount < MIN_CHARACTER_COUNT)
            {
                Assert.Inconclusive("キャラクターが不足しています（スキップ）");
                yield break;
            }

            // インスタンシングを有効化して数フレーム待機
            _exercise.UseInstancing = true;
            yield return null;
            yield return null;
            yield return null;

            int instanceCount = _exercise.LastInstanceCount;

            // インスタンス数が0の場合はCollectInstanceDataが未実装
            Assert.That(instanceCount, Is.GreaterThan(0),
                "RenderInstanced: CollectInstanceDataが未実装のため描画できません。\n\n" +
                "【実装方法】GPUInstancing_Exercise.cs を実装:\n" +
                "1. CollectInstanceData で _matrices と _colors を設定\n" +
                "2. RenderInstanced で Graphics.DrawMeshInstanced を呼び出す");

            // Frame Debuggerで確認するようメッセージを出力
            Debug.Log($"[GPU Instancing] インスタンス数: {instanceCount}\n" +
                "Draw Call数の確認: Window > Analysis > Frame Debugger");

            _exercise.UseInstancing = false;
            yield return null;
        }
    }
}
