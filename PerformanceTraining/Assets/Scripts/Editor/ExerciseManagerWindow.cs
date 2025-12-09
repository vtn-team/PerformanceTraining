using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

namespace PerformanceTraining.Editor
{
    /// <summary>
    /// ユーザー設定を保存するクラス
    /// </summary>
    public static class ExerciseUserSettings
    {
        private const string USER_NAME_KEY = "PerformanceTraining_UserName";
        private const string DEFAULT_SERVER_URL = "https://e7vhxtwu48.execute-api.ap-northeast-1.amazonaws.com/default";

        public static string UserName
        {
            get => EditorPrefs.GetString(USER_NAME_KEY, "");
            set => EditorPrefs.SetString(USER_NAME_KEY, value);
        }

        public static string ServerUrl => DEFAULT_SERVER_URL;

        public static bool HasUserName => !string.IsNullOrEmpty(UserName);
        public static bool HasServerUrl => true;
    }

    /// <summary>
    /// サーバからのスコアデータ（フラット構造）
    /// </summary>
    [System.Serializable]
    public class ScoreData
    {
        public string exerciseId;
        public float score;
        public int testsPassed;
        public int totalTests;
        public float executionTimeMs;
        public long gcAllocBytes;
        public string updatedAt;
    }

    [System.Serializable]
    public class UserScoresResponse
    {
        public string udid;
        public string userName;
        public ScoreData[] scores;
    }

    [System.Serializable]
    public class RankingEntry
    {
        public int rank;
        public string userName;
        public float score;
        public string updatedAt;
    }

    [System.Serializable]
    public class RankingResponse
    {
        public string exerciseId;
        public RankingEntry[] ranking;
    }

    /// <summary>
    /// プロジェクト起動時にExercise Managerを自動で開く
    /// </summary>
    [InitializeOnLoad]
    public static class ExerciseManagerAutoOpen
    {
        private const string AUTO_OPEN_KEY = "PerformanceTraining_ExerciseManager_AutoOpened";

        static ExerciseManagerAutoOpen()
        {
            // エディタ起動後に遅延実行
            EditorApplication.delayCall += OpenWindowOnce;
        }

        private static void OpenWindowOnce()
        {
            // このセッションで既に開いていたらスキップ
            if (SessionState.GetBool(AUTO_OPEN_KEY, false))
                return;

            SessionState.SetBool(AUTO_OPEN_KEY, true);
            ExerciseManagerWindow.ShowWindow();
        }
    }

    /// <summary>
    /// 課題管理ウィンドウ
    /// 各課題の目標・計測・ソースコードへの導線を提供
    /// </summary>
    public class ExerciseManagerWindow : EditorWindow
    {
        // タブタイプ
        private enum TabType
        {
            Settings,   // 設定
            Exercises   // 課題
        }

        // 課題タイプ（3つの大課題）
        private enum ExerciseType
        {
            Memory,     // 課題1: メモリ最適化
            CPU,        // 課題2: CPU最適化
            Tradeoff    // 課題3: トレードオフ
        }

        // Stepの識別子
        private enum StepType
        {
            // Memory Steps
            Memory_ObjectPool,
            Memory_StringBuilder,
            Memory_DelegateCache,
            Memory_CollectionReuse,
            // CPU Steps
            CPU_SpatialPartition,
            CPU_ProcessingOrder,
            // Tradeoff Steps
            Tradeoff_GPUInstancing
        }

        private TabType currentTab = TabType.Exercises;
        private ExerciseType selectedExercise = ExerciseType.Memory;
        private Vector2 scrollPosition;

        // 設定タブ用
        private string userName = "";
        private bool isInitialized = false;

        // サーバスコア表示用
        private UserScoresResponse cachedUserScores;
        private RankingResponse cachedRanking;
        private string selectedRankingExercise = "Memory";
        private bool isFetchingScores = false;
        private string fetchStatusMessage = "";

        // テスト結果保存用（Stepごと）
        private static Dictionary<StepType, bool> stepTestResults = new Dictionary<StepType, bool>();

        // スタイル
        private GUIStyle headerStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle boldLabelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle stepLabelStyle;
        private bool stylesInitialized = false;

        // 課題情報構造体
        private struct ExerciseInfo
        {
            public string Title;
            public string Description;
            public string TargetMetric;
            public string TargetBefore;
            public string TargetAfter;
            public string SourceFile;
            public string MeasurementType;
            public string TestMethodName;
            public StepInfo[] Steps;
            public HintInfo Hints; // 攻略のヒント
        }

        private struct StepInfo
        {
            public StepType Type;
            public string Name;
            public string Description;
        }

        // 攻略のヒント構造体
        private struct HintInfo
        {
            public string ProfilerGuide;      // Profilerの使い方
            public string CheckPoint;         // 確認すべきポイント
            public int FixCount;              // 修正箇所の数
            public string[] TargetFolders;    // 修正対象のフォルダ
        }

        private static readonly ExerciseInfo[] Exercises = new ExerciseInfo[]
        {
            // 課題1: メモリ最適化
            new ExerciseInfo
            {
                Title = "課題1: メモリ最適化（GC Alloc削減）",
                Description = "毎フレーム発生しているGCアロケーションを削減する。\n文字列結合が原因でメモリ確保が発生している箇所を特定し、修正してください。",
                TargetMetric = "GC Alloc (KB/frame)",
                TargetBefore = "50+ KB/frame",
                TargetAfter = "< 1 KB/frame",
                SourceFile = "", // 自分で探す
                MeasurementType = "Memory",
                TestMethodName = "TestZeroAllocation",
                Steps = new StepInfo[] { }, // 実装項目は非表示
                Hints = new HintInfo
                {
                    ProfilerGuide = "Window > Analysis > Profiler を開く\n" +
                                   "CPU Usage モジュールを選択し、Hierarchy ビューで GC Alloc 列をクリックしてソート\n" +
                                   "毎フレーム大量のアロケーションがある行をダブルクリックするとソースコードに飛べる",
                    CheckPoint = "① 文字列結合（+ 演算子、string.Format、$補間文字列）を毎フレーム行っている箇所\n" +
                                "② Instantiate/Destroy を毎回呼んでいる箇所（Object Pool化が必要）",
                    FixCount = 5,
                    TargetFolders = new string[] { "Scripts/Core/", "Scripts/AI/" }
                }
            },
            // 課題2: CPU最適化
            new ExerciseInfo
            {
                Title = "課題2: CPU最適化",
                Description = "適切な探索ロジックとせよ\n\n" +
                              "敵の探索・攻撃処理が非効率な実装になっています。\n" +
                              "空間分割と処理順序の最適化を実装してください。",
                TargetMetric = "Frame Time (ms)",
                TargetBefore = "40+ ms",
                TargetAfter = "< 16 ms (60fps)",
                SourceFile = "", // Profilerで自分で探す
                MeasurementType = "CPU",
                TestMethodName = "TestCPUOptimization",
                Steps = new StepInfo[] { }, // 実装項目は非表示（ヒントで誘導）
                Hints = new HintInfo
                {
                    ProfilerGuide = "Window > Analysis > Profiler を開く\n" +
                                   "CPU Usage モジュールを選択し、Hierarchy ビューで Self 列をクリックしてソート\n" +
                                   "重い処理（赤い部分）をダブルクリックするとソースコードに飛べる",
                    CheckPoint = "① 空間分割: GetCellIndex() と GetNearbyCharactersOptimized() を実装\n" +
                                "   → グリッドを実装し、周辺9セルのみ検索（O(n)→O(1)）\n\n" +
                                "② 処理順序: FindBestAttackTarget() 内の呼び出し順序を並び替える\n" +
                                "   → 軽いフィルタを先に、重い経路探索を最後に実行",
                    FixCount = 2,
                    TargetFolders = new string[] { "Scripts/Core/" }
                }
            },
            // 課題3: トレードオフ（GPU Instancing）
            new ExerciseInfo
            {
                Title = "課題3: トレードオフ（GPU Instancing）",
                Description = "GPU Instancingで描画を最適化せよ\n\n" +
                              "同一メッシュ・マテリアルのキャラクターを一括描画し、\n" +
                              "Draw Callを削減してください。",
                TargetMetric = "Draw Calls (Batches)",
                TargetBefore = "200+ Draw Calls",
                TargetAfter = "1 Draw Call",
                SourceFile = "", // Profilerで自分で探す
                MeasurementType = "GPU",
                TestMethodName = "TestGPUInstancing",
                Steps = new StepInfo[] { }, // 実装項目は非表示（ヒントで誘導）
                Hints = new HintInfo
                {
                    ProfilerGuide = "Game View → Stats で Batches 数を確認\n" +
                                   "Window → Analysis → Frame Debugger で Draw Call を確認\n" +
                                   "Iキーでインスタンシングの ON/OFF を切り替え可能",
                    CheckPoint = "① CollectInstanceData(): 描画データの収集\n" +
                                "   → transform.localToWorldMatrix で変換行列を取得\n" +
                                "   → キャラクタータイプに応じた色を _colors 配列に設定\n\n" +
                                "② RenderInstanced(): 一括描画の実行\n" +
                                "   → MaterialPropertyBlock.SetVectorArray(\"_Color\", _colors)\n" +
                                "   → Graphics.DrawMeshInstanced() で一括描画",
                    FixCount = 2,
                    TargetFolders = new string[] { "Scripts/Exercises/Tradeoff/" }
                }
            }
        };

        [MenuItem("PerformanceTraining/Exercise Manager &e")]
        public static void ShowWindow()
        {
            var window = GetWindow<ExerciseManagerWindow>("Exercise Manager");
            window.minSize = new Vector2(550, 750);
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            // ベースフォントサイズ（1.25倍）
            int baseFontSize = Mathf.RoundToInt(12 * 1.25f); // 15

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = Mathf.RoundToInt(18 * 1.25f), // 22
                alignment = TextAnchor.MiddleCenter
            };

            titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = Mathf.RoundToInt(16 * 1.25f) // 20
            };

            labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = baseFontSize,
                wordWrap = true
            };

            boldLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = baseFontSize
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = baseFontSize
            };

            stepLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = baseFontSize,
                padding = new RectOffset(20, 0, 0, 0)
            };

            stylesInitialized = true;
        }

        private void OnEnable()
        {
            // 設定から名前を読み込む
            userName = ExerciseUserSettings.UserName;

            // 初回起動時（名前未設定）は設定タブを表示
            if (!isInitialized)
            {
                isInitialized = true;
                if (!ExerciseUserSettings.HasUserName)
                {
                    currentTab = TabType.Settings;
                }
            }
        }

        private void OnGUI()
        {
            InitStyles();

            // ヘッダー
            EditorGUILayout.Space(15);
            GUILayout.Label("PerformanceTraining 最適化課題", headerStyle);
            EditorGUILayout.Space(10);

            // タブ切り替え
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Toggle(currentTab == TabType.Exercises, "課題", "Button", GUILayout.Width(100), GUILayout.Height(30)))
            {
                currentTab = TabType.Exercises;
            }
            if (GUILayout.Toggle(currentTab == TabType.Settings, "設定", "Button", GUILayout.Width(100), GUILayout.Height(30)))
            {
                currentTab = TabType.Settings;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            DrawSeparator();

            // タブの内容を描画
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentTab)
            {
                case TabType.Settings:
                    DrawSettingsTab();
                    break;
                case TabType.Exercises:
                    DrawExercisesTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSettingsTab()
        {
            EditorGUILayout.Space(15);

            // 名前未設定の場合は警告を表示
            if (!ExerciseUserSettings.HasUserName)
            {
                EditorGUILayout.HelpBox(
                    "まずはあなたの名前を入力してください。\n" +
                    "この名前はテスト結果の記録に使用されます。",
                    MessageType.Warning);
                EditorGUILayout.Space(10);
            }

            // 名前入力セクション
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("ユーザー設定", boldLabelStyle);
            EditorGUILayout.Space(10);

            // 名前入力
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("名前:", boldLabelStyle, GUILayout.Width(100));
            string newName = EditorGUILayout.TextField(userName, GUILayout.Height(25));
            if (newName != userName)
            {
                userName = newName;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 保存ボタン
            if (GUILayout.Button("設定を保存", buttonStyle, GUILayout.Height(35)))
            {
                if (string.IsNullOrEmpty(userName.Trim()))
                {
                    EditorUtility.DisplayDialog("エラー", "名前を入力してください。", "OK");
                }
                else
                {
                    ExerciseUserSettings.UserName = userName.Trim();
                    EditorUtility.DisplayDialog("保存完了", $"設定を保存しました。", "OK");

                    // 保存後に課題タブへ移動
                    currentTab = TabType.Exercises;
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);

            // 現在の設定を表示
            if (ExerciseUserSettings.HasUserName)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("現在の設定", boldLabelStyle);
                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("登録名:", labelStyle, GUILayout.Width(100));
                GUILayout.Label(ExerciseUserSettings.UserName, boldLabelStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(15);

            // サーバからスコア取得セクション
            DrawServerScoreSection();
        }

        private void DrawServerScoreSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("サーバスコア", boldLabelStyle);
            EditorGUILayout.Space(5);

            // ステータスメッセージ
            if (!string.IsNullOrEmpty(fetchStatusMessage))
            {
                EditorGUILayout.HelpBox(fetchStatusMessage, MessageType.Info);
            }

            // 自分のスコア取得
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("自分のスコアを取得", buttonStyle, GUILayout.Height(30)))
            {
                FetchUserScores();
            }
            EditorGUILayout.EndHorizontal();

            // キャッシュされたスコアを表示
            if (cachedUserScores != null && cachedUserScores.scores != null)
            {
                EditorGUILayout.Space(10);
                GUILayout.Label($"{cachedUserScores.userName} のスコア:", boldLabelStyle);

                foreach (var score in cachedUserScores.scores)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"  {score.exerciseId}:", labelStyle, GUILayout.Width(100));
                    GUILayout.Label($"{score.score:F0} 点", boldLabelStyle, GUILayout.Width(80));
                    GUILayout.Label($"({score.testsPassed}/{score.totalTests} テスト合格)", labelStyle);
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space(15);
            DrawSeparator();
            EditorGUILayout.Space(10);

            // ランキング取得
            GUILayout.Label("ランキング", boldLabelStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("課題:", labelStyle, GUILayout.Width(50));
            string[] exerciseOptions = { "Memory", "CPU", "Tradeoff" };
            int selectedIndex = System.Array.IndexOf(exerciseOptions, selectedRankingExercise);
            if (selectedIndex < 0) selectedIndex = 0;
            selectedIndex = EditorGUILayout.Popup(selectedIndex, exerciseOptions, GUILayout.Width(120));
            selectedRankingExercise = exerciseOptions[selectedIndex];

            if (GUILayout.Button("ランキング取得", buttonStyle, GUILayout.Height(25)))
            {
                FetchRanking(selectedRankingExercise);
            }
            EditorGUILayout.EndHorizontal();

            // キャッシュされたランキングを表示
            if (cachedRanking != null && cachedRanking.ranking != null && cachedRanking.exerciseId == selectedRankingExercise)
            {
                EditorGUILayout.Space(10);
                GUILayout.Label($"{cachedRanking.exerciseId} ランキング:", boldLabelStyle);

                foreach (var entry in cachedRanking.ranking)
                {
                    EditorGUILayout.BeginHorizontal();

                    // 順位による色分け
                    var rankStyle = new GUIStyle(boldLabelStyle);
                    if (entry.rank == 1) rankStyle.normal.textColor = new Color(1f, 0.84f, 0f); // 金
                    else if (entry.rank == 2) rankStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f); // 銀
                    else if (entry.rank == 3) rankStyle.normal.textColor = new Color(0.8f, 0.5f, 0.2f); // 銅

                    GUILayout.Label($"  {entry.rank}位", rankStyle, GUILayout.Width(50));
                    GUILayout.Label(entry.userName, labelStyle, GUILayout.Width(120));
                    GUILayout.Label($"{entry.score:F0} 点", boldLabelStyle);
                    EditorGUILayout.EndHorizontal();
                }

                if (cachedRanking.ranking.Length == 0)
                {
                    GUILayout.Label("  (まだ記録がありません)", labelStyle);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private async void FetchUserScores()
        {
            if (isFetchingScores) return;

            isFetchingScores = true;
            fetchStatusMessage = "スコアを取得中...";
            Repaint();

            try
            {
                // UDIDでスコアを取得
                string udid = UnityEngine.SystemInfo.deviceUniqueIdentifier;
                string url = $"{ExerciseUserSettings.ServerUrl}/scores/{udid}";

                using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
                {
                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await System.Threading.Tasks.Task.Delay(100);
                    }

                    if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        cachedUserScores = JsonUtility.FromJson<UserScoresResponse>(request.downloadHandler.text);
                        fetchStatusMessage = "スコアを取得しました。";
                    }
                    else
                    {
                        fetchStatusMessage = $"エラー: {request.error}";
                    }
                }
            }
            catch (System.Exception e)
            {
                fetchStatusMessage = $"エラー: {e.Message}";
            }
            finally
            {
                isFetchingScores = false;
                Repaint();
            }
        }

        private async void FetchRanking(string exerciseId)
        {
            if (isFetchingScores) return;

            isFetchingScores = true;
            fetchStatusMessage = "ランキングを取得中...";
            Repaint();

            try
            {
                string url = $"{ExerciseUserSettings.ServerUrl}/ranking/{exerciseId}";

                using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
                {
                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await System.Threading.Tasks.Task.Delay(100);
                    }

                    if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        cachedRanking = JsonUtility.FromJson<RankingResponse>(request.downloadHandler.text);
                        fetchStatusMessage = "ランキングを取得しました。";
                    }
                    else
                    {
                        fetchStatusMessage = $"エラー: {request.error}";
                    }
                }
            }
            catch (System.Exception e)
            {
                fetchStatusMessage = $"エラー: {e.Message}";
            }
            finally
            {
                isFetchingScores = false;
                Repaint();
            }
        }

        private void DrawExercisesTab()
        {
            // 名前未設定の場合は警告
            if (!ExerciseUserSettings.HasUserName)
            {
                EditorGUILayout.HelpBox(
                    "設定タブで名前を入力してください。",
                    MessageType.Warning);
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.HelpBox(
                "各課題を選択し、目標値を確認してから実装に取り組んでください。\n" +
                "「テストを実行」ボタンで実装が正しいか確認できます。",
                MessageType.Info);

            EditorGUILayout.Space(15);

            // 課題選択
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("課題を選択:", boldLabelStyle, GUILayout.Width(100));
            selectedExercise = (ExerciseType)EditorGUILayout.EnumPopup(selectedExercise, GUILayout.Height(25));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            DrawSeparator();

            // 選択した課題の詳細
            DrawExerciseDetail(Exercises[(int)selectedExercise]);
        }

        private void DrawExerciseDetail(ExerciseInfo info)
        {
            EditorGUILayout.Space(10);

            // タイトル
            GUILayout.Label(info.Title, titleStyle);
            EditorGUILayout.Space(10);

            // 説明
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("概要", boldLabelStyle);
            GUILayout.Label(info.Description, labelStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(12);

            // 目標値
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("パフォーマンス目標", boldLabelStyle);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("計測項目:", labelStyle, GUILayout.Width(100));
            GUILayout.Label(info.TargetMetric, boldLabelStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("最適化前:", labelStyle, GUILayout.Width(100));
            var redStyle = new GUIStyle(labelStyle) { normal = { textColor = new Color(0.9f, 0.3f, 0.3f) } };
            GUILayout.Label(info.TargetBefore, redStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("目標値:", labelStyle, GUILayout.Width(100));
            var greenStyle = new GUIStyle(labelStyle) { normal = { textColor = new Color(0.3f, 0.8f, 0.3f) } };
            GUILayout.Label(info.TargetAfter, greenStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);

            // 攻略のヒント（課題1などで使用）
            if (!string.IsNullOrEmpty(info.Hints.ProfilerGuide))
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("攻略のヒント", boldLabelStyle);
                EditorGUILayout.Space(5);

                // Profilerの使い方
                GUILayout.Label("【Profilerの使い方】", boldLabelStyle);
                GUILayout.Label(info.Hints.ProfilerGuide, labelStyle);
                EditorGUILayout.Space(8);

                // 確認すべきポイント
                GUILayout.Label("【確認すべきポイント】", boldLabelStyle);
                GUILayout.Label(info.Hints.CheckPoint, labelStyle);
                EditorGUILayout.Space(8);

                // 修正箇所の数
                GUILayout.Label("【修正箇所】", boldLabelStyle);
                GUILayout.Label($"{info.Hints.FixCount} 箇所", labelStyle);
                EditorGUILayout.Space(8);

                // 対象フォルダ
                if (info.Hints.TargetFolders != null && info.Hints.TargetFolders.Length > 0)
                {
                    GUILayout.Label("【対象フォルダ】", boldLabelStyle);
                    foreach (var folder in info.Hints.TargetFolders)
                    {
                        GUILayout.Label($"• {folder}", stepLabelStyle);
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(15);
            }

            // Stepリスト（チェックボックス付き）- Stepsがある場合のみ表示
            if (info.Steps != null && info.Steps.Length > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("実装項目", boldLabelStyle);
                EditorGUILayout.Space(5);

                foreach (var step in info.Steps)
                {
                    EditorGUILayout.BeginHorizontal();

                    // テスト結果のチェックボックス（表示のみ、編集不可）
                    bool passed = stepTestResults.ContainsKey(step.Type) && stepTestResults[step.Type];

                    // チェックマークアイコンで表示（編集不可を明確に）
                    var checkStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = Mathf.RoundToInt(14 * 1.25f),
                        fixedWidth = 25
                    };
                    if (passed)
                    {
                        checkStyle.normal.textColor = new Color(0.3f, 0.8f, 0.3f);
                        GUILayout.Label("✓", checkStyle);
                    }
                    else
                    {
                        checkStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
                        GUILayout.Label("○", checkStyle);
                    }

                    // Step名と説明
                    GUILayout.Label($"{step.Name}: {step.Description}", stepLabelStyle);

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(20);

            // アクションボタン（課題ごとに1セット）
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("アクション", boldLabelStyle);

            EditorGUILayout.Space(8);

            // Profilerを使って問題箇所を見つけてもらう
            EditorGUILayout.HelpBox(
                "Profilerを使って問題のあるソースコードを自分で見つけてください。",
                MessageType.Info);
            EditorGUILayout.Space(8);

            // シーンを再生
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("シーンを再生", buttonStyle, GUILayout.Height(35)))
            {
                PlayScene(info.MeasurementType);
            }
            if (EditorApplication.isPlaying)
            {
                if (GUILayout.Button("停止", buttonStyle, GUILayout.Height(35), GUILayout.Width(70)))
                {
                    EditorApplication.isPlaying = false;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // テストボタン（Unity Test Runner使用）
            var testButtonColor = new Color(0.2f, 0.5f, 0.9f);
            GUI.backgroundColor = testButtonColor;
            if (GUILayout.Button("テストを実行", buttonStyle, GUILayout.Height(40)))
            {
                RunTestsWithTestRunner();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(8);

            // Profilerを開く
            if (GUILayout.Button($"Profiler を開く（{info.MeasurementType}）", buttonStyle, GUILayout.Height(30)))
            {
                OpenProfiler(info.MeasurementType);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);

            // 確認ポイント
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("確認ポイント", boldLabelStyle);
            if (info.MeasurementType == "Memory")
            {
                GUILayout.Label("• Profiler > CPU > GC Alloc 列を確認", labelStyle);
                GUILayout.Label("• 毎フレーム new しているオブジェクトを探す", labelStyle);
            }
            else
            {
                GUILayout.Label("• Profiler > CPU > Frame Time を確認", labelStyle);
                GUILayout.Label("• 重い処理（赤い部分）を特定する", labelStyle);
            }
            EditorGUILayout.Space(5);
            GUILayout.Label("【テストの実行】", boldLabelStyle);
            GUILayout.Label("「テストを実行」ボタンで自動的にテストが開始されます", labelStyle);
            EditorGUILayout.EndVertical();
        }

        private void OpenSourceFile(string relativePath)
        {
            // Tradeoffの場合はディレクトリなので特別処理
            if (relativePath.EndsWith("/"))
            {
                // ディレクトリを開く場合は最初のファイルを開く
                relativePath = "Exercises/Tradeoff/NeighborCache_Exercise.cs";
            }

            string assetPath = $"Assets/Scripts/{relativePath}";

            // MonoScriptとしてロード
            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
                Debug.Log($"ソースファイルを開きました: {assetPath}");
                return;
            }

            // 見つからない場合は絶対パスで試す
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            if (File.Exists(fullPath))
            {
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(fullPath, 1);
                Debug.Log($"外部エディタでファイルを開きました: {fullPath}");
                return;
            }

            // それでも見つからない場合
            Debug.LogError($"ファイルが見つかりません: {assetPath}");
            EditorUtility.DisplayDialog("エラー",
                $"ファイルが見つかりません:\n{assetPath}\n\n" +
                "課題ファイルが正しい場所にあるか確認してください。",
                "OK");
        }

        private void PlayScene(string measurementType)
        {
            // 既に再生中なら何もしない
            if (EditorApplication.isPlaying)
            {
                Debug.Log("シーンは既に再生中です");
                return;
            }

            // メインシーンを探す
            string[] scenePaths = new string[]
            {
                "Assets/Scenes/MainGame.unity",
                "Assets/Scenes/MainGame.unity",
                "Assets/Scenes/Main.unity",
            };

            string sceneToLoad = null;
            foreach (var scenePath in scenePaths)
            {
                string fullScenePath = Path.Combine(Application.dataPath, "..", scenePath);
                if (File.Exists(fullScenePath))
                {
                    sceneToLoad = scenePath;
                    break;
                }
            }

            // シーンが見つからない場合
            if (sceneToLoad == null)
            {
                var currentScene = EditorSceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(currentScene.path))
                {
                    EditorUtility.DisplayDialog("エラー",
                        "再生するシーンが見つかりません。\n\n" +
                        "MainGame.unityシーンを開くか、\n" +
                        "PerformanceTraining > Scene Setup Wizard でシーンを作成してください。",
                        "OK");
                    return;
                }
                // 現在のシーンを使用
                Debug.Log("現在のシーンを再生します");
            }
            else
            {
                // シーンを開く
                var currentScene = EditorSceneManager.GetActiveScene();
                if (currentScene.path != sceneToLoad)
                {
                    if (currentScene.isDirty)
                    {
                        if (!EditorUtility.DisplayDialog("シーンの保存",
                            "現在のシーンに未保存の変更があります。\n保存してから切り替えますか？",
                            "保存して切り替え", "キャンセル"))
                        {
                            return;
                        }
                        EditorSceneManager.SaveOpenScenes();
                    }
                    EditorSceneManager.OpenScene(sceneToLoad);
                }
            }

            // LearningSettings設定
            var settings = Resources.Load<Core.LearningSettings>("LearningSettings");
            if (settings != null)
            {
                settings.showPerformanceMonitor = true;

                // 課題モードを設定（カメラズームに影響）
                switch (selectedExercise)
                {
                    case ExerciseType.Memory:
                        settings.currentExercise = Core.ExerciseMode.Memory;
                        break;
                    case ExerciseType.CPU:
                        settings.currentExercise = Core.ExerciseMode.CPU;
                        break;
                    case ExerciseType.Tradeoff:
                        settings.currentExercise = Core.ExerciseMode.Tradeoff;
                        break;
                }

                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            // 再生開始
            EditorApplication.isPlaying = true;
            Debug.Log($"シーンを再生します（{measurementType}計測モード）");
        }

        private void RunTestsWithTestRunner()
        {
            // Unity Test Runner ウィンドウを開く
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");

            // 選択した課題に対応するカテゴリ名を取得
            string categoryName = GetTestCategoryForExercise(selectedExercise);

            // TestRunnerApiを使用してPlayModeテストを自動実行
            var testRunnerApi = ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();
            var filter = new UnityEditor.TestTools.TestRunner.Api.Filter
            {
                testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode,
                targetPlatform = null,
                categoryNames = new[] { categoryName }  // カテゴリでフィルタリング
            };

            Debug.Log($"テストを実行します... (カテゴリ: {categoryName})");
            testRunnerApi.Execute(new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings(filter));
        }

        /// <summary>
        /// 課題タイプに対応するテストカテゴリ名を取得
        /// </summary>
        private string GetTestCategoryForExercise(ExerciseType exercise)
        {
            switch (exercise)
            {
                case ExerciseType.Memory:
                    return "Exercise1_Memory";
                case ExerciseType.CPU:
                    return "Exercise2_CPU";
                case ExerciseType.Tradeoff:
                    return "Exercise3_Tradeoff";
                default:
                    return "";
            }
        }

        private void OpenProfiler(string measurementType)
        {
            EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler");
            Debug.Log($"Profilerを開きました。{measurementType}関連の項目を確認してください。");
        }

        private void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            var rect = EditorGUILayout.GetControlRect(false, 2);
            rect.height = 2;
            EditorGUI.DrawRect(rect, new Color(0.4f, 0.4f, 0.4f, 1));
            EditorGUILayout.Space(5);
        }
    }
}
