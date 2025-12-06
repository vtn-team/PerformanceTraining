using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using UnityEngine.Networking;

namespace PerformanceTraining.Editor
{
    /// <summary>
    /// テスト結果をサーバーに送信するシステム
    /// CRCチェックを含めてデータの整合性を保証
    /// </summary>
    public static class TestResultSubmitter
    {
        // サーバー設定（後で指定可能）
        private static string _serverUrl = "";
        private static string _apiKey = "";

        // EditorPrefsキー
        private const string PREF_SERVER_URL = "PerformanceTraining_ServerUrl";
        private const string PREF_API_KEY = "PerformanceTraining_ApiKey";
        private const string PREF_STUDENT_ID = "PerformanceTraining_StudentId";
        private const string PREF_STUDENT_NAME = "PerformanceTraining_StudentName";

        /// <summary>
        /// テスト結果データ構造
        /// </summary>
        [Serializable]
        public class TestResultData
        {
            public string studentId;
            public string studentName;
            public string timestamp;
            public string unityVersion;
            public TestCategoryResult[] categories;
            public PerformanceMetrics performance;
            public string crc32;
            public string signature;
        }

        [Serializable]
        public class TestCategoryResult
        {
            public string categoryName;
            public TestItemResult[] items;
            public int passCount;
            public int failCount;
            public bool allPassed;
        }

        [Serializable]
        public class TestItemResult
        {
            public string itemName;
            public bool passed;
            public string message;
            public float executionTimeMs;
        }

        [Serializable]
        public class PerformanceMetrics
        {
            public float fps;
            public float cpuTimeMs;
            public float gcAllocKB;
            public int drawCalls;
            public int enemyCount;
        }

        /// <summary>
        /// サーバーURLを設定
        /// </summary>
        public static void SetServerUrl(string url)
        {
            _serverUrl = url;
            EditorPrefs.SetString(PREF_SERVER_URL, url);
        }

        /// <summary>
        /// サーバーURLを取得
        /// </summary>
        public static string GetServerUrl()
        {
            if (string.IsNullOrEmpty(_serverUrl))
            {
                _serverUrl = EditorPrefs.GetString(PREF_SERVER_URL, "");
            }
            return _serverUrl;
        }

        /// <summary>
        /// APIキーを設定
        /// </summary>
        public static void SetApiKey(string key)
        {
            _apiKey = key;
            EditorPrefs.SetString(PREF_API_KEY, key);
        }

        /// <summary>
        /// 学生情報を設定
        /// </summary>
        public static void SetStudentInfo(string studentId, string studentName)
        {
            EditorPrefs.SetString(PREF_STUDENT_ID, studentId);
            EditorPrefs.SetString(PREF_STUDENT_NAME, studentName);
        }

        /// <summary>
        /// 学生IDを取得
        /// </summary>
        public static string GetStudentId()
        {
            return EditorPrefs.GetString(PREF_STUDENT_ID, "");
        }

        /// <summary>
        /// 学生名を取得
        /// </summary>
        public static string GetStudentName()
        {
            return EditorPrefs.GetString(PREF_STUDENT_NAME, "");
        }

        /// <summary>
        /// テスト結果データを生成
        /// </summary>
        public static TestResultData CreateTestResultData(
            Dictionary<string, List<(string name, bool passed, string message)>> testResults,
            float fps, float cpuTime, float gcAlloc, int drawCalls, int enemyCount)
        {
            var data = new TestResultData
            {
                studentId = GetStudentId(),
                studentName = GetStudentName(),
                timestamp = DateTime.UtcNow.ToString("o"),
                unityVersion = Application.unityVersion,
                performance = new PerformanceMetrics
                {
                    fps = fps,
                    cpuTimeMs = cpuTime,
                    gcAllocKB = gcAlloc,
                    drawCalls = drawCalls,
                    enemyCount = enemyCount
                }
            };

            // カテゴリ別結果を構築
            var categories = new List<TestCategoryResult>();
            foreach (var kvp in testResults)
            {
                var categoryResult = new TestCategoryResult
                {
                    categoryName = kvp.Key,
                    passCount = 0,
                    failCount = 0
                };

                var items = new List<TestItemResult>();
                foreach (var (name, passed, message) in kvp.Value)
                {
                    items.Add(new TestItemResult
                    {
                        itemName = name,
                        passed = passed,
                        message = message
                    });

                    if (passed)
                        categoryResult.passCount++;
                    else
                        categoryResult.failCount++;
                }

                categoryResult.items = items.ToArray();
                categoryResult.allPassed = categoryResult.failCount == 0;
                categories.Add(categoryResult);
            }

            data.categories = categories.ToArray();

            // CRC32を計算
            data.crc32 = CalculateCRC32(data);

            // 署名を生成（改ざん防止）
            data.signature = GenerateSignature(data);

            return data;
        }

        /// <summary>
        /// CRC32を計算
        /// </summary>
        public static string CalculateCRC32(TestResultData data)
        {
            // CRC計算用の文字列を構築（crc32とsignatureを除く）
            var sb = new StringBuilder();
            sb.Append(data.studentId);
            sb.Append(data.studentName);
            sb.Append(data.timestamp);
            sb.Append(data.unityVersion);

            if (data.performance != null)
            {
                sb.Append(data.performance.fps.ToString("F2"));
                sb.Append(data.performance.cpuTimeMs.ToString("F2"));
                sb.Append(data.performance.gcAllocKB.ToString("F2"));
                sb.Append(data.performance.drawCalls);
                sb.Append(data.performance.enemyCount);
            }

            if (data.categories != null)
            {
                foreach (var cat in data.categories)
                {
                    sb.Append(cat.categoryName);
                    sb.Append(cat.passCount);
                    sb.Append(cat.failCount);

                    if (cat.items != null)
                    {
                        foreach (var item in cat.items)
                        {
                            sb.Append(item.itemName);
                            // JavaScript uses lowercase "true"/"false" for boolean string conversion
                            sb.Append(item.passed ? "true" : "false");
                        }
                    }
                }
            }

            // CRC32計算
            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            uint crc = ComputeCRC32(bytes);
            return crc.ToString("X8");
        }

        /// <summary>
        /// CRC32を計算（実装）
        /// </summary>
        private static uint ComputeCRC32(byte[] data)
        {
            uint[] table = GenerateCRC32Table();
            uint crc = 0xFFFFFFFF;

            foreach (byte b in data)
            {
                crc = (crc >> 8) ^ table[(crc ^ b) & 0xFF];
            }

            return ~crc;
        }

        /// <summary>
        /// CRC32テーブルを生成
        /// </summary>
        private static uint[] GenerateCRC32Table()
        {
            uint[] table = new uint[256];
            const uint polynomial = 0xEDB88320;

            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) == 1)
                        crc = (crc >> 1) ^ polynomial;
                    else
                        crc >>= 1;
                }
                table[i] = crc;
            }

            return table;
        }

        /// <summary>
        /// 署名を生成（HMAC-SHA256）
        /// </summary>
        private static string GenerateSignature(TestResultData data)
        {
            string apiKey = EditorPrefs.GetString(PREF_API_KEY, "default_key");

            // 署名対象の文字列
            string signatureBase = $"{data.studentId}|{data.timestamp}|{data.crc32}";

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiKey)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureBase));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// 結果をJSONに変換
        /// </summary>
        public static string ToJson(TestResultData data)
        {
            return JsonUtility.ToJson(data, true);
        }

        /// <summary>
        /// 結果をサーバーに送信
        /// </summary>
        public static void SubmitToServer(TestResultData data, Action<bool, string> callback)
        {
            string serverUrl = GetServerUrl();

            if (string.IsNullOrEmpty(serverUrl))
            {
                callback?.Invoke(false, "サーバーURLが設定されていません");
                return;
            }

            string json = ToJson(data);

            // EditorCoroutineを使用して非同期送信
            EditorApplication.delayCall += () =>
            {
                SendRequest(serverUrl, json, callback);
            };
        }

        /// <summary>
        /// HTTP POSTリクエストを送信
        /// </summary>
        private static async void SendRequest(string url, string json, Action<bool, string> callback)
        {
            try
            {
                using (var request = new UnityWebRequest(url, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");

                    string apiKey = EditorPrefs.GetString(PREF_API_KEY, "");
                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        request.SetRequestHeader("X-API-Key", apiKey);
                    }

                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await System.Threading.Tasks.Task.Delay(100);
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        callback?.Invoke(true, request.downloadHandler.text);
                    }
                    else
                    {
                        callback?.Invoke(false, $"Error: {request.error}");
                    }
                }
            }
            catch (Exception e)
            {
                callback?.Invoke(false, $"Exception: {e.Message}");
            }
        }

        /// <summary>
        /// ローカルファイルに保存（バックアップ用）
        /// </summary>
        public static void SaveToLocalFile(TestResultData data)
        {
            string json = ToJson(data);
            string fileName = $"TestResult_{data.studentId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string path = $"Assets/{fileName}";

            System.IO.File.WriteAllText(path, json);
            AssetDatabase.Refresh();

            Debug.Log($"テスト結果を保存しました: {path}");
        }

        /// <summary>
        /// CRC検証
        /// </summary>
        public static bool VerifyCRC(TestResultData data)
        {
            string originalCrc = data.crc32;
            data.crc32 = null;
            data.signature = null;

            string calculatedCrc = CalculateCRC32(data);
            data.crc32 = originalCrc;

            return originalCrc == calculatedCrc;
        }
    }

    /// <summary>
    /// サーバー設定ウィンドウ
    /// </summary>
    public class SubmissionSettingsWindow : EditorWindow
    {
        private string serverUrl;
        private string apiKey;
        private string studentId;
        private string studentName;

        // [MenuItem("PerformanceTraining/Submission Settings")] // 学生用UIはExerciseManagerWindowを使用
        public static void ShowWindow()
        {
            var window = GetWindow<SubmissionSettingsWindow>("Submission Settings");
            window.minSize = new Vector2(400, 250);
        }

        private void OnEnable()
        {
            serverUrl = TestResultSubmitter.GetServerUrl();
            apiKey = EditorPrefs.GetString("PerformanceTraining_ApiKey", "");
            studentId = TestResultSubmitter.GetStudentId();
            studentName = TestResultSubmitter.GetStudentName();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("送信設定", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("サーバー設定", EditorStyles.boldLabel);
            serverUrl = EditorGUILayout.TextField("サーバーURL", serverUrl);
            apiKey = EditorGUILayout.PasswordField("APIキー", apiKey);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("学生情報", EditorStyles.boldLabel);
            studentId = EditorGUILayout.TextField("学生ID", studentId);
            studentName = EditorGUILayout.TextField("氏名", studentName);

            EditorGUILayout.Space(20);

            if (GUILayout.Button("保存", GUILayout.Height(30)))
            {
                TestResultSubmitter.SetServerUrl(serverUrl);
                TestResultSubmitter.SetApiKey(apiKey);
                TestResultSubmitter.SetStudentInfo(studentId, studentName);

                EditorUtility.DisplayDialog("保存完了", "設定を保存しました。", "OK");
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "サーバーURLとAPIキーは教員から提供されます。\n" +
                "学生IDと氏名を正しく入力してください。",
                MessageType.Info);
        }
    }
}
