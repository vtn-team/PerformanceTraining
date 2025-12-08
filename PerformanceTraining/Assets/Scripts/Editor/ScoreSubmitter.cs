using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceTraining.Editor
{
    /// <summary>
    /// テスト結果をサーバに送信するクラス
    /// </summary>
    public static class ScoreSubmitter
    {
        /// <summary>
        /// スコア送信リクエストのデータ構造（フラット構造）
        /// </summary>
        [Serializable]
        public class ScoreSubmitRequest
        {
            public string udid;
            public string userName;
            public string exerciseId;
            public float score;
            public int testsPassed;
            public int totalTests;
            public float executionTimeMs;
            public long gcAllocBytes;
        }

        /// <summary>
        /// サーバからのレスポンス
        /// </summary>
        [Serializable]
        public class ScoreSubmitResponse
        {
            public string message;
            public string userName;
            public string exerciseId;
            public float score;
            public string error;
            public float existingScore;
            public float submittedScore;
        }

        /// <summary>
        /// スコアを送信する（非同期）
        /// </summary>
        /// <param name="exerciseId">課題ID (Memory, CPU, Tradeoff)</param>
        /// <param name="testsPassed">合格したテスト数</param>
        /// <param name="totalTests">全テスト数</param>
        /// <param name="executionTimeMs">実行時間（ミリ秒）</param>
        /// <param name="gcAllocBytes">GCアロケーション（バイト）</param>
        /// <param name="onComplete">完了時コールバック</param>
        public static async void SubmitScore(
            string exerciseId,
            int testsPassed,
            int totalTests,
            float executionTimeMs = 0,
            long gcAllocBytes = 0,
            Action<bool, string> onComplete = null)
        {
            // 設定確認
            if (!ExerciseUserSettings.HasUserName)
            {
                string msg = "名前が設定されていません。設定タブで名前を入力してください。";
                Debug.LogWarning($"[ScoreSubmitter] {msg}");
                onComplete?.Invoke(false, msg);
                return;
            }

            // スコア計算（合格率 × 100）
            float score = (totalTests > 0) ? (float)testsPassed / totalTests * 100f : 0f;

            // UDIDを取得（デバイス固有のID）
            string udid = SystemInfo.deviceUniqueIdentifier;

            // リクエストデータ作成（フラット構造）
            var request = new ScoreSubmitRequest
            {
                udid = udid,
                userName = ExerciseUserSettings.UserName,
                exerciseId = exerciseId,
                score = score,
                testsPassed = testsPassed,
                totalTests = totalTests,
                executionTimeMs = executionTimeMs,
                gcAllocBytes = gcAllocBytes
            };

            string json = JsonUtility.ToJson(request);
            string url = $"{ExerciseUserSettings.ServerUrl}/submit";

            Debug.Log($"[ScoreSubmitter] スコア送信中... URL: {url}");
            Debug.Log($"[ScoreSubmitter] データ: {json}");

            try
            {
                using (var webRequest = new UnityWebRequest(url, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                    webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    webRequest.SetRequestHeader("Content-Type", "application/json");

                    var operation = webRequest.SendWebRequest();

                    // 非同期で待機
                    while (!operation.isDone)
                    {
                        await Task.Delay(100);
                    }

                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        string responseText = webRequest.downloadHandler.text;
                        var response = JsonUtility.FromJson<ScoreSubmitResponse>(responseText);

                        if (!string.IsNullOrEmpty(response.error))
                        {
                            Debug.LogWarning($"[ScoreSubmitter] サーバエラー: {response.error}");
                            onComplete?.Invoke(false, response.error);
                        }
                        else
                        {
                            Debug.Log($"[ScoreSubmitter] 送信成功: {response.message}");
                            onComplete?.Invoke(true, response.message);
                        }
                    }
                    else
                    {
                        string errorMsg = $"通信エラー: {webRequest.error}";
                        Debug.LogError($"[ScoreSubmitter] {errorMsg}");
                        onComplete?.Invoke(false, errorMsg);
                    }
                }
            }
            catch (Exception e)
            {
                string errorMsg = $"例外発生: {e.Message}";
                Debug.LogError($"[ScoreSubmitter] {errorMsg}");
                onComplete?.Invoke(false, errorMsg);
            }
        }

        /// <summary>
        /// テスト結果からスコアを送信するヘルパーメソッド
        /// </summary>
        public static void SubmitTestResult(string exerciseId, int passed, int total)
        {
            SubmitScore(
                exerciseId,
                passed,
                total,
                onComplete: (success, message) =>
                {
                    if (success)
                    {
                        EditorUtility.DisplayDialog(
                            "スコア送信完了",
                            $"課題: {exerciseId}\n" +
                            $"スコア: {(total > 0 ? (float)passed / total * 100f : 0f):F0}点\n" +
                            $"({passed}/{total} テスト合格)\n\n" +
                            message,
                            "OK");
                    }
                    else
                    {
                        // エラーの場合はログのみ（ダイアログは出さない）
                        Debug.LogWarning($"スコア送信失敗: {message}");
                    }
                });
        }
    }
}
