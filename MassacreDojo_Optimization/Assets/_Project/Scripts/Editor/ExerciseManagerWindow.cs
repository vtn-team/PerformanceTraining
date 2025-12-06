using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

namespace MassacreDojo.Editor
{
    /// <summary>
    /// 課題管理ウィンドウ
    /// 各課題の目標・計測・ソースコードへの導線を提供
    /// </summary>
    public class ExerciseManagerWindow : EditorWindow
    {
        // 課題タイプ
        private enum ExerciseType
        {
            Memory_ZeroAllocation,
            CPU_SpatialPartition,
            CPU_StaggeredUpdate,
            CPU_SqrMagnitude,
            Tradeoff_NeighborCache,
            Tradeoff_DecisionCache,
            Tradeoff_TrigLUT,
            Tradeoff_VisibilityMap
        }

        private ExerciseType selectedExercise = ExerciseType.Memory_ZeroAllocation;
        private Vector2 scrollPosition;

        // テスト結果保存用
        private static Dictionary<ExerciseType, bool> testResults = new Dictionary<ExerciseType, bool>();

        // スタイル
        private GUIStyle headerStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle boldLabelStyle;
        private GUIStyle buttonStyle;
        private bool stylesInitialized = false;

        // 課題情報構造体
        private struct ExerciseInfo
        {
            public string Title;
            public string Category;
            public string Description;
            public string Goal;
            public string TargetMetric;
            public string TargetBefore;
            public string TargetAfter;
            public string SourceFile;
            public string MeasurementType;
            public string TestMethodName;
        }

        private static readonly ExerciseInfo[] Exercises = new ExerciseInfo[]
        {
            new ExerciseInfo
            {
                Title = "ゼロアロケーション",
                Category = "Memory",
                Description = "Update内でのGCアロケーションをゼロにする",
                Goal = "オブジェクトプール、StringBuilder再利用、デリゲートキャッシュ、コレクション再利用を実装",
                TargetMetric = "GC Alloc (KB/frame)",
                TargetBefore = "50+ KB/frame",
                TargetAfter = "< 1 KB/frame",
                SourceFile = "Exercises/Memory/ZeroAllocation_Exercise.cs",
                MeasurementType = "Memory",
                TestMethodName = "TestZeroAllocation"
            },
            new ExerciseInfo
            {
                Title = "空間分割（Spatial Partitioning）",
                Category = "CPU",
                Description = "O(n²)の総当たり検索をO(1)に近づける",
                Goal = "グリッドベースの空間分割を実装し、近傍検索を高速化",
                TargetMetric = "Frame Time (ms)",
                TargetBefore = "40+ ms",
                TargetAfter = "< 16 ms",
                SourceFile = "Exercises/CPU/CPUOptimization_Exercise.cs",
                MeasurementType = "CPU",
                TestMethodName = "TestSpatialPartition"
            },
            new ExerciseInfo
            {
                Title = "更新分散（Staggered Update）",
                Category = "CPU",
                Description = "全敵が毎フレーム更新するのを避け、負荷を分散",
                Goal = "グループごとに更新タイミングを分散させる",
                TargetMetric = "Frame Time (ms)",
                TargetBefore = "40+ ms",
                TargetAfter = "< 16 ms",
                SourceFile = "Exercises/CPU/CPUOptimization_Exercise.cs",
                MeasurementType = "CPU",
                TestMethodName = "TestStaggeredUpdate"
            },
            new ExerciseInfo
            {
                Title = "距離計算の最適化",
                Category = "CPU",
                Description = "平方根計算を避けて高速化",
                Goal = "sqrMagnitudeを使用して距離判定を最適化",
                TargetMetric = "Frame Time (ms)",
                TargetBefore = "40+ ms",
                TargetAfter = "< 16 ms",
                SourceFile = "Exercises/CPU/CPUOptimization_Exercise.cs",
                MeasurementType = "CPU",
                TestMethodName = "TestSqrMagnitude"
            },
            new ExerciseInfo
            {
                Title = "近傍キャッシュ",
                Category = "Tradeoff",
                Description = "メモリを消費して近傍検索のCPU計算を削減",
                Goal = "各敵の近傍リストをキャッシュし、一定フレーム間再利用",
                TargetMetric = "Cache Hit Rate",
                TargetBefore = "0%",
                TargetAfter = "> 80%",
                SourceFile = "Exercises/Tradeoff/NeighborCache_Exercise.cs",
                MeasurementType = "CPU",
                TestMethodName = "TestNeighborCache"
            },
            new ExerciseInfo
            {
                Title = "AI判断キャッシュ",
                Category = "Tradeoff",
                Description = "メモリを消費してAI判断のCPU計算を削減",
                Goal = "敵のAI判断結果を数フレームキャッシュ",
                TargetMetric = "Cache Hit Rate",
                TargetBefore = "0%",
                TargetAfter = "> 80%",
                SourceFile = "Exercises/Tradeoff/DecisionCache_Exercise.cs",
                MeasurementType = "CPU",
                TestMethodName = "TestDecisionCache"
            },
            new ExerciseInfo
            {
                Title = "三角関数LUT",
                Category = "Tradeoff",
                Description = "メモリを消費してSin/Cos計算を削減",
                Goal = "Sin/Cosの値を事前計算してルックアップテーブルに格納",
                TargetMetric = "Memory vs Speed",
                TargetBefore = "毎回Mathf.Sin/Cos計算",
                TargetAfter = "配列参照のみ（2-5倍高速）",
                SourceFile = "Exercises/Tradeoff/TrigLUT_Exercise.cs",
                MeasurementType = "CPU",
                TestMethodName = "TestTrigLUT"
            },
            new ExerciseInfo
            {
                Title = "可視性マップ",
                Category = "Tradeoff",
                Description = "メモリを消費してRaycast計算を削減",
                Goal = "2Dグリッドで各セルの可視性を事前計算",
                TargetMetric = "Raycast削減",
                TargetBefore = "毎回Raycast",
                TargetAfter = "配列参照のみ（10-100倍高速）",
                SourceFile = "Exercises/Tradeoff/VisibilityMap_Exercise.cs",
                MeasurementType = "CPU",
                TestMethodName = "TestVisibilityMap"
            }
        };

        [MenuItem("MassacreDojo/Exercise Manager &e")]
        public static void ShowWindow()
        {
            var window = GetWindow<ExerciseManagerWindow>("Exercise Manager");
            window.minSize = new Vector2(550, 700);
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

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            // ヘッダー
            EditorGUILayout.Space(15);
            GUILayout.Label("MassacreDojo 最適化課題", headerStyle);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "各課題を選択し、目標値を確認してから実装に取り組んでください。\n" +
                "「テスト」ボタンで実装が正しいか確認できます。",
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
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawExerciseDetail((int)selectedExercise, Exercises[(int)selectedExercise]);
            EditorGUILayout.EndScrollView();
        }

        private void DrawExerciseDetail(int index, ExerciseInfo info)
        {
            EditorGUILayout.Space(10);

            // テスト結果チェックボックス（読み取り専用）
            EditorGUILayout.BeginHorizontal();
            bool passed = testResults.ContainsKey((ExerciseType)index) && testResults[(ExerciseType)index];

            GUI.enabled = false;
            EditorGUILayout.Toggle(passed, GUILayout.Width(20));
            GUI.enabled = true;

            GUILayout.Label($"【{info.Category}】{info.Title}", titleStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 説明
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("概要", boldLabelStyle);
            GUILayout.Label(info.Description, labelStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // 目標
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("実装目標", boldLabelStyle);
            GUILayout.Label(info.Goal, labelStyle);
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

            EditorGUILayout.Space(20);

            // アクションボタン
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("アクション", boldLabelStyle);

            EditorGUILayout.Space(8);

            // ソースコードを開く
            if (GUILayout.Button("ソースコードを開く", buttonStyle, GUILayout.Height(35)))
            {
                OpenSourceFile(info.SourceFile);
            }

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

            // テストボタン
            var testButtonStyle = new GUIStyle(buttonStyle);
            testButtonStyle.normal.textColor = new Color(0.2f, 0.6f, 1f);
            if (GUILayout.Button("テストを実行", testButtonStyle, GUILayout.Height(35)))
            {
                RunTest((ExerciseType)index, info);
            }

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
            EditorGUILayout.EndVertical();
        }

        private void OpenSourceFile(string relativePath)
        {
            // 複数のパスを試す
            string[] pathsToTry = new string[]
            {
                $"Assets/_Project/Scripts/{relativePath}",
                $"Assets/StudentExercises/{relativePath.Replace("Exercises/", "")}",
            };

            foreach (var path in pathsToTry)
            {
                // Application.dataPathを使用して絶対パスを構築
                string absolutePath = Path.Combine(Application.dataPath, "..", path).Replace("\\", "/");

                if (File.Exists(absolutePath))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (asset != null)
                    {
                        AssetDatabase.OpenAsset(asset);
                        Debug.Log($"ソースファイルを開きました: {path}");
                        return;
                    }
                }
            }

            // ファイルが見つからない場合、直接パスで開く
            string defaultPath = $"Assets/_Project/Scripts/{relativePath}";
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", defaultPath));

            if (File.Exists(fullPath))
            {
                // 外部エディタで開く
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(fullPath, 1);
                Debug.Log($"外部エディタでファイルを開きました: {fullPath}");
            }
            else
            {
                Debug.LogError($"ファイルが見つかりません: {defaultPath}");
                EditorUtility.DisplayDialog("エラー",
                    $"ファイルが見つかりません:\n{defaultPath}\n\n" +
                    "課題ファイルが展開されているか確認してください。",
                    "OK");
            }
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
                "Assets/_Project/Scenes/MainGame.unity",
                "Assets/Scenes/MainGame.unity",
                "Assets/_Project/Scenes/Main.unity",
            };

            string sceneToLoad = null;
            foreach (var scenePath in scenePaths)
            {
                if (File.Exists(Path.Combine(Application.dataPath, "..", scenePath)))
                {
                    sceneToLoad = scenePath;
                    break;
                }
            }

            // シーンが見つからない場合は現在のシーンを使用
            if (sceneToLoad == null)
            {
                var currentScene = EditorSceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(currentScene.path))
                {
                    EditorUtility.DisplayDialog("エラー",
                        "再生するシーンがありません。\n\n" +
                        "MassacreDojo > Scene Setup Wizard でシーンを作成してください。",
                        "OK");
                    return;
                }
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
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            // 再生開始
            EditorApplication.isPlaying = true;
            Debug.Log($"シーンを再生します（{measurementType}計測モード）");
        }

        private void RunTest(ExerciseType exerciseType, ExerciseInfo info)
        {
            Debug.Log($"テスト実行: {info.Title}");

            // テスト実行（ExerciseTestRunnerを呼び出す）
            bool result = ExerciseTestRunner.RunSingleTest(info.TestMethodName);

            // 結果を保存
            testResults[exerciseType] = result;

            // 結果表示
            if (result)
            {
                EditorUtility.DisplayDialog("テスト結果",
                    $"【{info.Title}】\n\n✓ テスト成功！\n\n目標を達成しました。",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("テスト結果",
                    $"【{info.Title}】\n\n✗ テスト失敗\n\nコードを確認して再度お試しください。",
                    "OK");
            }

            Repaint();
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
