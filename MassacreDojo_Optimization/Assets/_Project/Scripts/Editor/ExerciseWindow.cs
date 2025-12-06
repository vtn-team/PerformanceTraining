using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using PerformanceTraining.Core;

namespace PerformanceTraining.Editor
{
    /// <summary>
    /// パフォーマンス最適化課題を表示・管理するEditorWindow
    /// </summary>
    public class ExerciseWindow : EditorWindow
    {
        // タブ
        private int selectedTab = 0;
        private readonly string[] tabNames = { "概要", "課題1: メモリ", "課題2: CPU", "課題3: トレードオフ", "課題4: グラフィクス", "テスト" };

        // スクロール位置
        private Vector2 scrollPosition;

        // 進捗チェック状態（EditorPrefsで保存）
        private Dictionary<string, bool> checkStates = new Dictionary<string, bool>();

        // LearningSettings参照
        private LearningSettings settings;

        // 計測値
        private float initialFPS;
        private float initialGCAlloc;
        private float initialCPUTime;
        private int initialDrawCalls;
        private bool hasMeasuredInitial = false;

        // スタイル
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle boxStyle;
        private GUIStyle checkboxStyle;
        private GUIStyle completedStyle;
        private GUIStyle incompleteStyle;
        private bool stylesInitialized = false;

        // [MenuItem("PerformanceTraining/Exercise Window %#e")] // 旧版 - ExerciseManagerWindowを使用
        public static void ShowWindow()
        {
            var window = GetWindow<ExerciseWindow>("Exercise Window");
            window.minSize = new Vector2(500, 600);
        }

        private void OnEnable()
        {
            LoadCheckStates();
            LoadInitialMeasurements();
            FindSettings();
        }

        private void OnDisable()
        {
            SaveCheckStates();
            SaveInitialMeasurements();
        }

        private void FindSettings()
        {
            settings = Resources.Load<LearningSettings>("LearningSettings");
            if (settings == null)
            {
                // Resourcesフォルダ内を検索
                var guids = AssetDatabase.FindAssets("t:LearningSettings");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    settings = AssetDatabase.LoadAssetAtPath<LearningSettings>(path);
                }
            }
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                margin = new RectOffset(0, 0, 10, 10)
            };

            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                margin = new RectOffset(0, 0, 8, 4)
            };

            boxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(0, 0, 5, 5)
            };

            checkboxStyle = new GUIStyle(EditorStyles.toggle)
            {
                margin = new RectOffset(20, 0, 2, 2)
            };

            completedStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) }
            };

            incompleteStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.8f, 0.4f, 0.2f) }
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            // タブ
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            EditorGUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0: DrawOverviewTab(); break;
                case 1: DrawMemoryTab(); break;
                case 2: DrawCPUTab(); break;
                case 3: DrawTradeoffTab(); break;
                case 4: DrawGraphicsTab(); break;
                case 5: DrawTestTab(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        #region Overview Tab

        private void DrawOverviewTab()
        {
            EditorGUILayout.LabelField("パフォーマンス最適化 学習プログラム", headerStyle);

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("目標: 敵500体でFPS 60を達成する", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            // 課題ファイル展開セクション
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("課題ファイル", subHeaderStyle);

            EditorGUILayout.BeginVertical(boxStyle);

            bool isDeployed = ExerciseDeployer.IsDeployed();

            if (!isDeployed)
            {
                EditorGUILayout.HelpBox(
                    "課題を開始するには、まず課題ファイルを展開してください。\n" +
                    "展開後、Assets/StudentExercises フォルダに課題ファイルが作成されます。",
                    MessageType.Info);

                if (GUILayout.Button("課題ファイルを展開", GUILayout.Height(35)))
                {
                    ExerciseDeployer.DeployAllExercises();
                }
            }
            else
            {
                EditorGUILayout.LabelField("課題ファイルは展開済みです", completedStyle);
                EditorGUILayout.LabelField($"展開先: {ExerciseDeployer.GetDeploymentPath()}", EditorStyles.miniLabel);

                EditorGUILayout.Space(5);

                // 展開状態の詳細
                var status = ExerciseDeployer.GetDeploymentStatus();
                foreach (var kvp in status)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(250));
                    EditorGUILayout.LabelField(kvp.Value ? "OK" : "未展開",
                        kvp.Value ? completedStyle : incompleteStyle);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("フォルダを開く", GUILayout.Width(100)))
                {
                    EditorUtility.RevealInFinder(ExerciseDeployer.GetDeploymentPath());
                }
                if (GUILayout.Button("再展開", GUILayout.Width(80)))
                {
                    ExerciseDeployer.DeployAllExercises();
                }
                if (GUILayout.Button("削除", GUILayout.Width(60)))
                {
                    ExerciseDeployer.RemoveDeployedExercises();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            // 初期計測
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("初期計測値", subHeaderStyle);

            EditorGUILayout.BeginVertical(boxStyle);

            if (!hasMeasuredInitial)
            {
                EditorGUILayout.HelpBox("ゲームを実行し、敵200体をスポーンしてから「初期値を記録」を押してください。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"FPS: {initialFPS:F1}");
                EditorGUILayout.LabelField($"GC Alloc: {initialGCAlloc:F1} KB/frame");
                EditorGUILayout.LabelField($"CPU Time: {initialCPUTime:F2} ms");
                EditorGUILayout.LabelField($"Draw Calls: {initialDrawCalls}");
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("初期値を記録", GUILayout.Width(120)))
            {
                RecordInitialMeasurements();
            }
            if (GUILayout.Button("リセット", GUILayout.Width(80)))
            {
                hasMeasuredInitial = false;
                SaveInitialMeasurements();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // 進捗サマリー
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("進捗サマリー", subHeaderStyle);

            EditorGUILayout.BeginVertical(boxStyle);
            DrawProgressBar("課題1: メモリ最適化", CountChecks("ex1_"), 4);
            DrawProgressBar("課題2: CPU最適化", CountChecks("ex2_"), 3);
            DrawProgressBar("課題3: トレードオフ", CountChecks("ex3_"), 2);
            DrawProgressBar("課題4: グラフィクス", CountChecks("ex4_"), 3);
            EditorGUILayout.EndVertical();

            // LearningSettings
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("最適化設定", subHeaderStyle);

            if (settings != null)
            {
                EditorGUILayout.BeginVertical(boxStyle);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("全てOFF", GUILayout.Width(100)))
                {
                    settings.ResetToDefault();
                    EditorUtility.SetDirty(settings);
                }
                if (GUILayout.Button("全てON", GUILayout.Width(100)))
                {
                    settings.EnableAllOptimizations();
                    EditorUtility.SetDirty(settings);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // 各設定のトグル
                EditorGUI.BeginChangeCheck();
                settings.useObjectPool = EditorGUILayout.Toggle("Object Pool", settings.useObjectPool);
                settings.useStringBuilder = EditorGUILayout.Toggle("StringBuilder", settings.useStringBuilder);
                settings.useDelegateCache = EditorGUILayout.Toggle("Delegate Cache", settings.useDelegateCache);
                settings.useCollectionReuse = EditorGUILayout.Toggle("Collection Reuse", settings.useCollectionReuse);
                EditorGUILayout.Space(3);
                settings.useSpatialPartition = EditorGUILayout.Toggle("Spatial Partition", settings.useSpatialPartition);
                settings.useStaggeredUpdate = EditorGUILayout.Toggle("Staggered Update", settings.useStaggeredUpdate);
                settings.useSqrMagnitude = EditorGUILayout.Toggle("SqrMagnitude", settings.useSqrMagnitude);
                EditorGUILayout.Space(3);
                settings.useNeighborCache = EditorGUILayout.Toggle("Neighbor Cache", settings.useNeighborCache);
                settings.useDecisionCache = EditorGUILayout.Toggle("Decision Cache", settings.useDecisionCache);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(settings);
                }

                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("LearningSettingsが見つかりません。Resources フォルダに作成してください。", MessageType.Warning);
                if (GUILayout.Button("LearningSettingsを作成"))
                {
                    CreateLearningSettings();
                }
            }
        }

        private void DrawProgressBar(string label, int completed, int total)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(200));
            var rect = EditorGUILayout.GetControlRect(GUILayout.Width(150));
            EditorGUI.ProgressBar(rect, (float)completed / total, $"{completed}/{total}");
            EditorGUILayout.LabelField(completed == total ? "完了" : "未完了",
                completed == total ? completedStyle : incompleteStyle);
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Memory Tab

        private void DrawMemoryTab()
        {
            EditorGUILayout.LabelField("課題1: ゼロアロケーション（1時間）", headerStyle);

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("目標: Update内でのGCアロケーションをゼロにする", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("ファイル: Scripts/Exercises/Memory/ZeroAllocation_Exercise.cs", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("目標値: GC Alloc 50KB+ → 1KB以下/frame", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            // Step 1
            DrawExerciseStep("Step 1: オブジェクトプール（15分）", "ex1_step1", new string[]
            {
                "Stack<Enemy> でプール管理を実装",
                "GetFromPool() メソッドを完成",
                "ReturnToPool() メソッドを完成"
            }, @"目的: Instantiate/Destroyを避け、オブジェクトを再利用する

ヒント:
- Stack は Push() と Pop() で要素を追加・取得
- Pop前にCountをチェック
- Return時は SetActive(false) を忘れずに");

            // Step 2
            DrawExerciseStep("Step 2: 文字列キャッシュ（15分）", "ex1_step2", new string[]
            {
                "StringBuilder をフィールドで保持",
                "Clear() でリセット",
                "Append() で文字列を追加"
            }, @"目的: 文字列結合によるGCアロケーションを防ぐ

Before (問題あり):
return ""Enemies: "" + count.ToString();

After (最適化):
_sb.Clear();
_sb.Append(""Enemies: "").Append(count);
return _sb.ToString();");

            // Step 3
            DrawExerciseStep("Step 3: デリゲートキャッシュ（15分）", "ex1_step3", new string[]
            {
                "Action<Enemy> をフィールドでキャッシュ",
                "初期化は一度だけ（Awakeまたは初回）",
                "以降はキャッシュを返す"
            }, @"目的: 毎フレームの new Action() を防ぐ

ヒント:
- ラムダ式は毎回新しいオブジェクトを生成
- フィールドに保持して再利用");

            // Step 4
            DrawExerciseStep("Step 4: コレクション再利用（15分）", "ex1_step4", new string[]
            {
                "List<Enemy> をフィールドで保持",
                "初期容量を指定して初期化（例: 100）",
                "使用前に Clear() でリセット"
            }, @"目的: 毎回の new List<T>() を防ぐ

ヒント:
- 初期容量を指定すると再割り当てを減らせる
- Clear() は要素を削除するが配列は保持");

            // 設定トグル
            EditorGUILayout.Space(10);
            DrawSettingsToggle("課題1の設定", new (string, Func<bool>, Action<bool>)[]
            {
                ("Object Pool", () => settings?.useObjectPool ?? false, v => { if(settings) settings.useObjectPool = v; }),
                ("StringBuilder", () => settings?.useStringBuilder ?? false, v => { if(settings) settings.useStringBuilder = v; }),
                ("Delegate Cache", () => settings?.useDelegateCache ?? false, v => { if(settings) settings.useDelegateCache = v; }),
                ("Collection Reuse", () => settings?.useCollectionReuse ?? false, v => { if(settings) settings.useCollectionReuse = v; })
            });
        }

        #endregion

        #region CPU Tab

        private void DrawCPUTab()
        {
            EditorGUILayout.LabelField("課題2: CPU計算キャッシュ（1時間）", headerStyle);

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("目標: CPU負荷の高い処理を最適化する", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("ファイル: Scripts/Exercises/CPU/CPUOptimization_Exercise.cs", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("目標値: CPU Time 40ms+ → 15ms以下", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            // Step 1
            DrawExerciseStep("Step 1: 空間分割（25分）", "ex2_step1", new string[]
            {
                "Dictionary<int, List<Enemy>> でセル管理",
                "GetCellIndex(): 座標→セルインデックス変換",
                "QueryNearbyEnemies(): 周辺9セルから敵を取得"
            }, @"目的: O(n²)の総当たり検索をO(1)に近づける

計算式:
int x = (int)((pos.x + HALF_SIZE) / CELL_SIZE);
int z = (int)((pos.z + HALF_SIZE) / CELL_SIZE);
int index = z * GRID_WIDTH + x;");

            // Step 2
            DrawExerciseStep("Step 2: 更新分散（20分）", "ex2_step2", new string[]
            {
                "frameCount % groupCount == group で更新判定",
                "重い処理のみ分散、軽い処理は毎フレーム"
            }, @"目的: 全敵が毎フレーム更新するのを避け、負荷を分散

例:
// グループ0: フレーム0,10,20...で更新
// グループ1: フレーム1,11,21...で更新
return frameCount % 10 == group;");

            // Step 3
            DrawExerciseStep("Step 3: 距離計算（15分）", "ex2_step3", new string[]
            {
                "Vector3.Distance() → sqrMagnitude に置き換え",
                "比較時は閾値も2乗する"
            }, @"目的: 平方根計算を避けて高速化

Before:
if (Vector3.Distance(a, b) < 5f)

After:
if ((a - b).sqrMagnitude < 25f)  // 5² = 25");

            // 設定トグル
            EditorGUILayout.Space(10);
            DrawSettingsToggle("課題2の設定", new (string, Func<bool>, Action<bool>)[]
            {
                ("Spatial Partition", () => settings?.useSpatialPartition ?? false, v => { if(settings) settings.useSpatialPartition = v; }),
                ("Staggered Update", () => settings?.useStaggeredUpdate ?? false, v => { if(settings) settings.useStaggeredUpdate = v; }),
                ("SqrMagnitude", () => settings?.useSqrMagnitude ?? false, v => { if(settings) settings.useSqrMagnitude = v; })
            });
        }

        #endregion

        #region Tradeoff Tab

        private void DrawTradeoffTab()
        {
            EditorGUILayout.LabelField("課題3: メモリ↔CPUトレードオフ（30分）", headerStyle);

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("目標: メモリとCPUのトレードオフを理解する", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("キャッシュを使ってCPU負荷を削減し、その代償を体感する", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            // 3-A: 近傍キャッシュ
            DrawExerciseStep("3-A: 近傍キャッシュ（15分）", "ex3_neighbor", new string[]
            {
                "Dictionary<Enemy, CacheEntry> でキャッシュ管理",
                "GetNeighbors(): キャッシュ確認→有効なら再利用",
                "UpdateCache(): 近傍を計算してキャッシュに保存"
            }, @"ファイル: Scripts/Exercises/Tradeoff/NeighborCache_Exercise.cs

トレードオフ:
- メモリ: 敵1体あたり約212バイト（1000体で約212KB）
- CPU: 5-7倍高速化（キャッシュ有効期間による）
- 精度: キャッシュ期間分の位置ズレが発生

使用場面: 敵同士の衝突回避、群れ行動、包囲時の間隔調整");

            // 3-B: AI判断キャッシュ
            DrawExerciseStep("3-B: AI判断キャッシュ（15分）", "ex3_decision", new string[]
            {
                "Dictionary<Enemy, DecisionEntry> でキャッシュ管理",
                "GetDecision(): キャッシュ確認→有効なら再利用",
                "CacheDecision(): 判断結果をキャッシュに保存"
            }, @"ファイル: Scripts/Exercises/Tradeoff/DecisionCache_Exercise.cs

トレードオフ:
- メモリ: 敵1体あたり約56バイト（1000体で約56KB）
- CPU: AI判断を1/5に削減（5フレームキャッシュ時）
- 応答性: 最大5フレーム分の反応遅延

使用場面: 敵の状態遷移判定、プレイヤー検知判定、攻撃タイミング判定");

            // 考察課題
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("考察課題", subHeaderStyle);

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Q1: 近傍キャッシュの有効期間を長くすると何が起きるか？", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Q2: AI判断キャッシュの有効期間を長くすると何が起きるか？", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Q3: キャッシュヒット率が低い場合、何を調整すべきか？", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Q4: それぞれのキャッシュを無効化（Invalidate）すべきタイミングは？", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();

            // 設定トグル
            EditorGUILayout.Space(10);
            DrawSettingsToggle("課題3の設定", new (string, Func<bool>, Action<bool>)[]
            {
                ("Neighbor Cache", () => settings?.useNeighborCache ?? false, v => { if(settings) settings.useNeighborCache = v; }),
                ("Decision Cache", () => settings?.useDecisionCache ?? false, v => { if(settings) settings.useDecisionCache = v; })
            });

            // 発展課題（旧トレードオフ）
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("発展課題", subHeaderStyle);

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("以下は時間に余裕がある場合の発展課題です:", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();
            if (settings != null)
            {
                settings.useTrigLUT = EditorGUILayout.Toggle("三角関数LUT（発展）", settings.useTrigLUT);
                settings.useVisibilityMap = EditorGUILayout.Toggle("可視性マップ（発展）", settings.useVisibilityMap);
            }
            if (EditorGUI.EndChangeCheck() && settings != null)
            {
                EditorUtility.SetDirty(settings);
            }
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Graphics Tab

        private void DrawGraphicsTab()
        {
            EditorGUILayout.LabelField("課題4: グラフィクス最適化（30分）", headerStyle);

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("目標: Unity設定変更でグラフィクス最適化", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("参照: Documentation/GraphicsGuide.md", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            // G-1
            DrawExerciseStep("G-1: GPU Instancing（10分）", "ex4_instancing", new string[]
            {
                "敵のMaterialを選択",
                "Enable GPU Instancing にチェック",
                "Frame Debugger で Draw Calls 減少を確認"
            }, "目的: 同じマテリアルを使用するオブジェクトの描画を効率化");

            // G-2
            DrawExerciseStep("G-2: LOD設定（10分）", "ex4_lod", new string[]
            {
                "敵Prefab に LODGroup コンポーネント追加",
                "LOD 0/1/2/Culled の閾値設定",
                "Stats で Triangles 減少を確認"
            }, "目的: 距離に応じてモデルの詳細度を変更");

            // G-3
            DrawExerciseStep("G-3: カリング・オーバードロー（10分）", "ex4_culling", new string[]
            {
                "パーティクル Max Particles を削減",
                "Camera Far Clip Plane を調整",
                "Scene View > Overdraw で確認"
            }, "目的: 不要な描画を削減");

            // 確認用ボタン
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Frame Debugger を開く"))
            {
                EditorApplication.ExecuteMenuItem("Window/Analysis/Frame Debugger");
            }
            if (GUILayout.Button("Profiler を開く"))
            {
                EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler");
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Test Tab

        private void DrawTestTab()
        {
            EditorGUILayout.LabelField("テスト実行", headerStyle);

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("各課題の実装が正しく動作しているかをテストします。", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("テストはPlayモードで実行してください。", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("テストを実行するにはPlayモードに入ってください。", MessageType.Info);

                if (GUILayout.Button("Play Mode を開始", GUILayout.Height(30)))
                {
                    EditorApplication.isPlaying = true;
                }
            }
            else
            {
                if (GUILayout.Button("全テストを実行", GUILayout.Height(40)))
                {
                    ExerciseTestRunner.RunAllTests();
                }

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("個別テスト", subHeaderStyle);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("メモリテスト"))
                {
                    ExerciseTestRunner.RunMemoryTests();
                }
                if (GUILayout.Button("CPUテスト"))
                {
                    ExerciseTestRunner.RunCPUTests();
                }
                if (GUILayout.Button("トレードオフテスト"))
                {
                    ExerciseTestRunner.RunTradeoffTests();
                }
                EditorGUILayout.EndHorizontal();
            }

            // 最終確認セクション
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("最終確認", subHeaderStyle);

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("全最適化適用後の目標値:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("  FPS (500体): 60以上");
            EditorGUILayout.LabelField("  GC Alloc: 1KB以下/frame");
            EditorGUILayout.LabelField("  CPU Time: 15ms以下");
            EditorGUILayout.LabelField("  Draw Calls: 50-100");
            EditorGUILayout.EndVertical();

            // 結果送信セクション
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("結果送信", subHeaderStyle);

            EditorGUILayout.BeginVertical(boxStyle);

            // 学生情報表示
            string studentId = TestResultSubmitter.GetStudentId();
            string studentName = TestResultSubmitter.GetStudentName();
            string serverUrl = TestResultSubmitter.GetServerUrl();

            if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(studentName))
            {
                EditorGUILayout.HelpBox(
                    "結果を送信するには、まず学生情報を設定してください。",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"学生ID: {studentId}");
                EditorGUILayout.LabelField($"氏名: {studentName}");
            }

            if (string.IsNullOrEmpty(serverUrl))
            {
                EditorGUILayout.HelpBox(
                    "サーバーURLが設定されていません。",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"送信先: {serverUrl}", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("送信設定", GUILayout.Width(100)))
            {
                SubmissionSettingsWindow.ShowWindow();
            }

            GUI.enabled = !string.IsNullOrEmpty(studentId) && Application.isPlaying;
            if (GUILayout.Button("結果をサーバーに送信", GUILayout.Width(150)))
            {
                SubmitTestResults();
            }
            GUI.enabled = true;

            if (GUILayout.Button("ローカル保存", GUILayout.Width(100)))
            {
                SaveTestResultsLocally();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// テスト結果をサーバーに送信
        /// </summary>
        private void SubmitTestResults()
        {
            // テスト結果を収集
            var testResults = CollectTestResults();

            // パフォーマンスデータを収集
            float fps = Application.isPlaying ? 1f / Time.unscaledDeltaTime : 0;
            float cpuTime = Application.isPlaying ? Time.unscaledDeltaTime * 1000f : 0;

            var data = TestResultSubmitter.CreateTestResultData(
                testResults,
                fps,
                cpuTime,
                initialGCAlloc,
                initialDrawCalls,
                200 // 敵の数（仮）
            );

            // 送信
            TestResultSubmitter.SubmitToServer(data, (success, message) =>
            {
                if (success)
                {
                    EditorUtility.DisplayDialog("送信完了", "テスト結果をサーバーに送信しました。", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("送信エラー", $"送信に失敗しました:\n{message}", "OK");
                }
            });
        }

        /// <summary>
        /// テスト結果をローカルに保存
        /// </summary>
        private void SaveTestResultsLocally()
        {
            var testResults = CollectTestResults();

            float fps = Application.isPlaying ? 1f / Time.unscaledDeltaTime : 0;
            float cpuTime = Application.isPlaying ? Time.unscaledDeltaTime * 1000f : 0;

            var data = TestResultSubmitter.CreateTestResultData(
                testResults,
                fps,
                cpuTime,
                initialGCAlloc,
                initialDrawCalls,
                200
            );

            TestResultSubmitter.SaveToLocalFile(data);
            EditorUtility.DisplayDialog("保存完了", "テスト結果をローカルに保存しました。", "OK");
        }

        /// <summary>
        /// テスト結果を収集
        /// </summary>
        private Dictionary<string, List<(string name, bool passed, string message)>> CollectTestResults()
        {
            var results = new Dictionary<string, List<(string, bool, string)>>();

            // 課題1: メモリ
            results["Memory"] = new List<(string, bool, string)>
            {
                ("ObjectPool", GetCheckState("ex1_step1_0") && GetCheckState("ex1_step1_1") && GetCheckState("ex1_step1_2"), ""),
                ("StringBuilder", GetCheckState("ex1_step2_0") && GetCheckState("ex1_step2_1") && GetCheckState("ex1_step2_2"), ""),
                ("DelegateCache", GetCheckState("ex1_step3_0") && GetCheckState("ex1_step3_1") && GetCheckState("ex1_step3_2"), ""),
                ("CollectionReuse", GetCheckState("ex1_step4_0") && GetCheckState("ex1_step4_1") && GetCheckState("ex1_step4_2"), "")
            };

            // 課題2: CPU
            results["CPU"] = new List<(string, bool, string)>
            {
                ("SpatialPartition", GetCheckState("ex2_step1_0") && GetCheckState("ex2_step1_1") && GetCheckState("ex2_step1_2"), ""),
                ("StaggeredUpdate", GetCheckState("ex2_step2_0") && GetCheckState("ex2_step2_1"), ""),
                ("SqrMagnitude", GetCheckState("ex2_step3_0") && GetCheckState("ex2_step3_1"), "")
            };

            // 課題3: トレードオフ
            results["Tradeoff"] = new List<(string, bool, string)>
            {
                ("NeighborCache", GetCheckState("ex3_neighbor_0") && GetCheckState("ex3_neighbor_1") && GetCheckState("ex3_neighbor_2"), ""),
                ("DecisionCache", GetCheckState("ex3_decision_0") && GetCheckState("ex3_decision_1") && GetCheckState("ex3_decision_2"), "")
            };

            // 課題4: グラフィクス
            results["Graphics"] = new List<(string, bool, string)>
            {
                ("GPUInstancing", GetCheckState("ex4_instancing_0") && GetCheckState("ex4_instancing_1") && GetCheckState("ex4_instancing_2"), ""),
                ("LOD", GetCheckState("ex4_lod_0") && GetCheckState("ex4_lod_1") && GetCheckState("ex4_lod_2"), ""),
                ("Culling", GetCheckState("ex4_culling_0") && GetCheckState("ex4_culling_1") && GetCheckState("ex4_culling_2"), "")
            };

            return results;
        }

        #endregion

        #region Helper Methods

        private void DrawExerciseStep(string title, string keyPrefix, string[] checkItems, string description)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(title, subHeaderStyle);

            EditorGUILayout.BeginVertical(boxStyle);

            // チェックリスト
            for (int i = 0; i < checkItems.Length; i++)
            {
                string key = $"{keyPrefix}_{i}";
                bool isChecked = GetCheckState(key);
                bool newValue = EditorGUILayout.ToggleLeft(checkItems[i], isChecked);
                if (newValue != isChecked)
                {
                    SetCheckState(key, newValue);
                }
            }

            // 説明
            if (!string.IsNullOrEmpty(description))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSettingsToggle(string label, (string name, Func<bool> getter, Action<bool> setter)[] settings)
        {
            EditorGUILayout.LabelField(label, subHeaderStyle);
            EditorGUILayout.BeginVertical(boxStyle);

            foreach (var (name, getter, setter) in settings)
            {
                EditorGUI.BeginChangeCheck();
                bool newValue = EditorGUILayout.Toggle(name, getter());
                if (EditorGUI.EndChangeCheck())
                {
                    setter(newValue);
                    if (this.settings != null)
                    {
                        EditorUtility.SetDirty(this.settings);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private bool GetCheckState(string key)
        {
            if (!checkStates.ContainsKey(key))
            {
                checkStates[key] = EditorPrefs.GetBool($"PerformanceTraining_{key}", false);
            }
            return checkStates[key];
        }

        private void SetCheckState(string key, bool value)
        {
            checkStates[key] = value;
            EditorPrefs.SetBool($"PerformanceTraining_{key}", value);
        }

        private int CountChecks(string prefix)
        {
            int count = 0;
            foreach (var kvp in checkStates)
            {
                if (kvp.Key.StartsWith(prefix) && kvp.Value)
                {
                    count++;
                }
            }
            return count;
        }

        private void LoadCheckStates()
        {
            // 全てのキーを読み込み
            string[] keys = {
                "ex1_step1_0", "ex1_step1_1", "ex1_step1_2",
                "ex1_step2_0", "ex1_step2_1", "ex1_step2_2",
                "ex1_step3_0", "ex1_step3_1", "ex1_step3_2",
                "ex1_step4_0", "ex1_step4_1", "ex1_step4_2",
                "ex2_step1_0", "ex2_step1_1", "ex2_step1_2",
                "ex2_step2_0", "ex2_step2_1",
                "ex2_step3_0", "ex2_step3_1",
                "ex3_neighbor_0", "ex3_neighbor_1", "ex3_neighbor_2",
                "ex3_decision_0", "ex3_decision_1", "ex3_decision_2",
                "ex4_instancing_0", "ex4_instancing_1", "ex4_instancing_2",
                "ex4_lod_0", "ex4_lod_1", "ex4_lod_2",
                "ex4_culling_0", "ex4_culling_1", "ex4_culling_2"
            };

            foreach (var key in keys)
            {
                checkStates[key] = EditorPrefs.GetBool($"PerformanceTraining_{key}", false);
            }
        }

        private void SaveCheckStates()
        {
            foreach (var kvp in checkStates)
            {
                EditorPrefs.SetBool($"PerformanceTraining_{kvp.Key}", kvp.Value);
            }
        }

        private void LoadInitialMeasurements()
        {
            hasMeasuredInitial = EditorPrefs.GetBool("PerformanceTraining_HasInitial", false);
            initialFPS = EditorPrefs.GetFloat("PerformanceTraining_InitialFPS", 0);
            initialGCAlloc = EditorPrefs.GetFloat("PerformanceTraining_InitialGC", 0);
            initialCPUTime = EditorPrefs.GetFloat("PerformanceTraining_InitialCPU", 0);
            initialDrawCalls = EditorPrefs.GetInt("PerformanceTraining_InitialDC", 0);
        }

        private void SaveInitialMeasurements()
        {
            EditorPrefs.SetBool("PerformanceTraining_HasInitial", hasMeasuredInitial);
            EditorPrefs.SetFloat("PerformanceTraining_InitialFPS", initialFPS);
            EditorPrefs.SetFloat("PerformanceTraining_InitialGC", initialGCAlloc);
            EditorPrefs.SetFloat("PerformanceTraining_InitialCPU", initialCPUTime);
            EditorPrefs.SetInt("PerformanceTraining_InitialDC", initialDrawCalls);
        }

        private void RecordInitialMeasurements()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("エラー", "Playモードで実行してください。", "OK");
                return;
            }

            initialFPS = 1f / Time.unscaledDeltaTime;
            initialCPUTime = Time.unscaledDeltaTime * 1000f;
            initialGCAlloc = 0; // Profiler APIで取得が必要
            initialDrawCalls = UnityEngine.Rendering.OnDemandRendering.effectiveRenderFrameRate; // 代替値
            hasMeasuredInitial = true;

            SaveInitialMeasurements();
            Debug.Log($"初期計測値を記録: FPS={initialFPS:F1}, CPU={initialCPUTime:F2}ms");
        }

        private void CreateLearningSettings()
        {
            // Resourcesフォルダを確認/作成
            string resourcesPath = "Assets/_Project/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                {
                    AssetDatabase.CreateFolder("Assets", "_Project");
                }
                AssetDatabase.CreateFolder("Assets/_Project", "Resources");
            }

            // LearningSettingsを作成
            settings = ScriptableObject.CreateInstance<LearningSettings>();
            AssetDatabase.CreateAsset(settings, $"{resourcesPath}/LearningSettings.asset");
            AssetDatabase.SaveAssets();

            Debug.Log("LearningSettings を作成しました: " + resourcesPath);
        }

        #endregion
    }
}
