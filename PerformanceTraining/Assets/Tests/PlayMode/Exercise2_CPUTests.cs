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
    /// 課題2: CPU最適化テスト
    /// </summary>
    [TestFixture]
    [Category("Exercise2_CPU")]
    public class Exercise2_CPUTests
    {
        private const int MIN_CHARACTER_COUNT = 10;
        private const string GAME_SCENE_NAME = "MainGame";

        // テスト設定
        private const float TEST_DURATION = 10f;            // テスト時間（秒）
        private const float TARGET_FRAME_TIME_MS = 16f;     // 目標フレーム時間（16ms = 60fps）

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
        // Test 1: 環境チェック
        // ================================================================

        [UnityTest]
        [Order(0)]
        public IEnumerator Test_01_EnvironmentCheck()
        {
            Assert.IsNotNull(_characterManager,
                "環境エラー: CharacterManager がシーン内に見つかりません。\n" +
                "MainGameシーンが正しく設定されているか確認してください。");

            Assert.GreaterOrEqual(_characterManager.AliveCount, MIN_CHARACTER_COUNT,
                $"環境エラー: キャラクター数が不足しています（現在: {_characterManager.AliveCount}）。\n" +
                "GameManagerの初期スポーン数を確認してください。");

            Debug.Log($"[Test 1: 環境チェック] OK\n" +
                $"  CharacterManager: 存在\n" +
                $"  キャラクター数: {_characterManager.AliveCount}");

            yield return null;
        }

        // ================================================================
        // Test 2: CPU速度チェック（16ms以下）
        // ================================================================

        [UnityTest]
        [Order(1)]
        public IEnumerator Test_02_CPU_FrameTime()
        {
            Assert.IsNotNull(_characterManager, "CharacterManager が見つかりません");

            // PlayerLoop内の時間を計測
            using var playerLoopRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "PlayerLoop");

            float elapsed = 0f;
            double totalFrameTime = 0;
            double maxFrameTime = 0;
            int frameCount = 0;

            Debug.Log($"[Test 2: CPU速度] 計測開始 ({TEST_DURATION}秒)");

            while (elapsed < TEST_DURATION)
            {
                yield return null;

                // PlayerLoopの時間をナノ秒からミリ秒に変換
                double playerLoopMs = playerLoopRecorder.LastValue / 1_000_000.0;
                frameCount++;

                totalFrameTime += playerLoopMs;
                if (playerLoopMs > maxFrameTime)
                {
                    maxFrameTime = playerLoopMs;
                }

                elapsed += Time.deltaTime;
            }

            double avgFrameTime = frameCount > 0 ? totalFrameTime / frameCount : 0;

            Debug.Log($"[Test 2: CPU速度] 結果:\n" +
                $"  テスト時間: {elapsed:F1}s\n" +
                $"  フレーム数: {frameCount}\n" +
                $"  フレーム時間 (平均): {avgFrameTime:F2}ms\n" +
                $"  フレーム時間 (最大): {maxFrameTime:F2}ms\n" +
                $"  目標: 平均 {TARGET_FRAME_TIME_MS}ms (60fps) 以下");

            Assert.LessOrEqual(avgFrameTime, TARGET_FRAME_TIME_MS,
                $"CPU速度: フレーム時間が長すぎます。\n" +
                $"現在: {avgFrameTime:F2}ms / frame\n" +
                $"目標: {TARGET_FRAME_TIME_MS}ms (60fps) 以下\n\n" +
                "【原因と対策】\n" +
                "1. 空間分割を実装して近傍検索を最適化\n" +
                "2. 処理順序を最適化（軽い処理を先に）\n" +
                "3. 不要な計算やアロケーションを削減");

            yield return null;
        }
    }
}
