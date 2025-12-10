using UnityEngine;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using System.Collections.Generic;

namespace PerformanceTraining.Editor
{
    /// <summary>
    /// Unity Test Runner のテスト完了コールバック
    /// テスト終了後に自動でスコアをサーバに送信する
    /// </summary>
    [InitializeOnLoad]
    public class TestResultCallback : ICallbacks
    {
        private static TestResultCallback _instance;
        private static TestRunnerApi _api;

        // 課題ごとのテスト結果を集計
        private Dictionary<string, TestResultSummary> _exerciseResults = new Dictionary<string, TestResultSummary>();

        private class TestResultSummary
        {
            public int Passed;
            public int Failed;
            public int Total => Passed + Failed;
        }

        static TestResultCallback()
        {
            // エディタ起動時に登録
            _instance = new TestResultCallback();
            _api = ScriptableObject.CreateInstance<TestRunnerApi>();
            _api.RegisterCallbacks(_instance);
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            // テスト開始時に結果をクリア
            _exerciseResults.Clear();
            Debug.Log("[TestResultCallback] テスト開始");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            // テスト完了時にスコアを送信
            Debug.Log("[TestResultCallback] テスト完了");

            // 集計結果を送信
            foreach (var kvp in _exerciseResults)
            {
                string exerciseId = kvp.Key;
                var summary = kvp.Value;

                if (summary.Total > 0)
                {
                    Debug.Log($"[TestResultCallback] {exerciseId}: {summary.Passed}/{summary.Total} 合格");

                    // サーバに送信
                    ScoreSubmitter.SubmitScore(
                        exerciseId,
                        summary.Passed,
                        summary.Total,
                        onComplete: (success, message) =>
                        {
                            if (success)
                            {
                                Debug.Log($"[TestResultCallback] {exerciseId} スコア送信成功: {message}");
                            }
                            else
                            {
                                Debug.LogWarning($"[TestResultCallback] {exerciseId} スコア送信失敗: {message}");
                            }
                        });
                }
            }
        }

        public void TestStarted(ITestAdaptor test)
        {
            // 個別テスト開始時（特に処理なし）
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            // 個別テスト終了時に結果を集計
            if (result.Test.IsSuite)
            {
                return; // スイートはスキップ
            }

            // カテゴリから課題IDを特定
            string exerciseId = GetExerciseIdFromTest(result.Test);

            if (string.IsNullOrEmpty(exerciseId))
            {
                return;
            }

            // 結果を集計
            if (!_exerciseResults.ContainsKey(exerciseId))
            {
                _exerciseResults[exerciseId] = new TestResultSummary();
            }

            var summary = _exerciseResults[exerciseId];

            if (result.TestStatus == TestStatus.Passed)
            {
                summary.Passed++;
            }
            else if (result.TestStatus == TestStatus.Failed)
            {
                summary.Failed++;
            }
            // Skipped や Inconclusive はカウントしない
        }

        /// <summary>
        /// テストから課題IDを取得
        /// </summary>
        private string GetExerciseIdFromTest(ITestAdaptor test)
        {
            string fullName = test.FullName ?? "";
            string typeName = test.TypeInfo?.FullName ?? "";

            // テストクラス名から判定
            if (typeName.Contains("Exercise1_Memory") || fullName.Contains("Exercise1_Memory"))
            {
                return "Memory";
            }
            if (typeName.Contains("Exercise2_CPU") || fullName.Contains("Exercise2_CPU"))
            {
                return "CPU";
            }
            if (typeName.Contains("Exercise3_Tradeoff") || fullName.Contains("Exercise3_Tradeoff"))
            {
                return "Tradeoff";
            }

            // カテゴリ属性からも判定
            foreach (var category in test.Categories)
            {
                if (category == "Exercise1_Memory") return "Memory";
                if (category == "Exercise2_CPU") return "CPU";
                if (category == "Exercise3_Tradeoff") return "Tradeoff";
            }

            return null;
        }
    }
}
