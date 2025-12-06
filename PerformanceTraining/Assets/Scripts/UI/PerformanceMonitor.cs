using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using PerformanceTraining.Core;
using PerformanceTraining.Enemy;

namespace PerformanceTraining.UI
{
    /// <summary>
    /// パフォーマンス計測値を画面に表示するUI
    /// 学生がProfilerと併用して最適化の効果を確認するためのツール
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        public enum MeasurementMode
        {
            All,        // 全項目表示
            Memory,     // メモリ関連を強調
            CPU         // CPU関連を強調
        }

        [Header("表示設定")]
        [SerializeField] private bool showMonitor = true;
        [SerializeField] private MeasurementMode measurementMode = MeasurementMode.All;
        [SerializeField] private bool showDetailedInfo = false;
        [SerializeField] private bool showOptimizationStatus = false;
        [SerializeField] private KeyCode toggleKey = KeyCode.F5;
        [SerializeField] private KeyCode modeToggleKey = KeyCode.F6;

        [Header("位置・サイズ")]
        [SerializeField] private Vector2 position = new Vector2(10, 10);
        [SerializeField] private Vector2 size = new Vector2(280, 0); // 高さは自動

        [Header("更新間隔")]
        [SerializeField] private float updateInterval = 0.5f;

        // 計測値
        private float fps;
        private float frameTime;
        private float gcAllocThisFrame;
        private long totalMemory;
        private long usedMemory;
        private int drawCalls;
        private int triangles;

        // FPS計算用
        private int frameCount;
        private float fpsTimer;
        private float lastFps;

        // GC計測用
        private long lastTotalMemory;

        // 参照
        private EnemySystem enemySystem;
        private EnemyAIManager aiManager;
        private LearningSettings settings;

        // UI用
        private GUIStyle boxStyle;
        private GUIStyle labelStyle;
        private GUIStyle headerStyle;
        private GUIStyle goodStyle;
        private GUIStyle badStyle;
        private StringBuilder sb;
        private bool stylesInitialized = false;

        private void Awake()
        {
            sb = new StringBuilder(512);
        }

        private void Start()
        {
            enemySystem = FindAnyObjectByType<EnemySystem>();
            aiManager = FindAnyObjectByType<EnemyAIManager>();
            settings = GameManager.Instance?.Settings;
        }

        private void Update()
        {
            // トグルキー
            if (Input.GetKeyDown(toggleKey))
            {
                showMonitor = !showMonitor;
            }
            if (Input.GetKeyDown(modeToggleKey))
            {
                // モード切り替え
                measurementMode = (MeasurementMode)(((int)measurementMode + 1) % 3);
            }
            if (Input.GetKeyDown(KeyCode.F7))
            {
                showDetailedInfo = !showDetailedInfo;
            }

            // FPS計算
            frameCount++;
            fpsTimer += Time.unscaledDeltaTime;

            if (fpsTimer >= updateInterval)
            {
                lastFps = frameCount / fpsTimer;
                frameCount = 0;
                fpsTimer = 0f;

                // メモリ情報更新
                UpdateMemoryInfo();
            }

            // フレームタイム
            frameTime = Time.unscaledDeltaTime * 1000f;
        }

        private void UpdateMemoryInfo()
        {
            totalMemory = Profiler.GetTotalAllocatedMemoryLong();
            usedMemory = Profiler.GetTotalReservedMemoryLong();

            // GCアロケーション推定（簡易）
            if (lastTotalMemory > 0)
            {
                long diff = totalMemory - lastTotalMemory;
                gcAllocThisFrame = diff > 0 ? diff / 1024f / updateInterval : 0; // KB/s
            }
            lastTotalMemory = totalMemory;
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTexture(2, 2, new Color(0, 0, 0, 0.8f));

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 12;
            labelStyle.normal.textColor = Color.white;

            headerStyle = new GUIStyle(labelStyle);
            headerStyle.fontSize = 14;
            headerStyle.fontStyle = FontStyle.Bold;

            goodStyle = new GUIStyle(labelStyle);
            goodStyle.normal.textColor = Color.green;

            badStyle = new GUIStyle(labelStyle);
            badStyle.normal.textColor = Color.red;

            stylesInitialized = true;
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            if (!showMonitor) return;
            if (settings != null && !settings.showPerformanceMonitor) return;

            InitStyles();

            float lineHeight = 20f;
            float padding = 10f;

            // モードに応じた表示内容を決定
            int lineCount = CalculateLineCount();
            float height = lineCount * lineHeight + padding * 2;
            Rect boxRect = new Rect(position.x, position.y, size.x, height);

            GUI.Box(boxRect, "", boxStyle);

            float y = position.y + padding;
            float x = position.x + padding;
            float labelWidth = size.x - padding * 2;

            // ヘッダー（モード表示）
            string modeText = measurementMode switch
            {
                MeasurementMode.Memory => "Memory Mode",
                MeasurementMode.CPU => "CPU Mode",
                _ => "All Metrics"
            };
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Performance Monitor [{modeText}]", headerStyle);
            y += lineHeight;

            // 区切り線
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), "━━━━━━━━━━━━━━━━━━━", labelStyle);
            y += lineHeight;

            // 敵の数（常に表示）
            int enemyCount = GameManager.Instance?.CurrentEnemyCount ?? 0;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"Enemies: {enemyCount}", labelStyle);
            y += lineHeight;

            // モード別の表示
            if (measurementMode == MeasurementMode.Memory || measurementMode == MeasurementMode.All)
            {
                DrawMemoryMetrics(ref y, x, labelWidth, lineHeight);
            }

            if (measurementMode == MeasurementMode.CPU || measurementMode == MeasurementMode.All)
            {
                DrawCPUMetrics(ref y, x, labelWidth, lineHeight);
            }

            // 詳細情報
            if (showDetailedInfo)
            {
                DrawDetailedInfo(ref y, x, labelWidth, lineHeight);
            }

            // 最適化ステータス
            if (showOptimizationStatus && settings != null)
            {
                DrawOptimizationStatus(ref y, x, labelWidth, lineHeight);
            }

            // キー説明
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), "━━━━━━━━━━━━━━━━━━━", labelStyle);
            y += lineHeight;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                $"[{toggleKey}] Toggle | [{modeToggleKey}] Mode | [F7] Detail", labelStyle);
        }

        private int CalculateLineCount()
        {
            int count = 4; // ヘッダー + 区切り + 敵数 + キー説明x2

            if (measurementMode == MeasurementMode.Memory || measurementMode == MeasurementMode.All)
                count += 4; // メモリ関連
            if (measurementMode == MeasurementMode.CPU || measurementMode == MeasurementMode.All)
                count += 4; // CPU関連
            if (showDetailedInfo)
                count += 4;
            if (showOptimizationStatus && settings != null)
                count += 10;

            return count;
        }

        private void DrawMemoryMetrics(ref float y, float x, float labelWidth, float lineHeight)
        {
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), "── Memory ──", labelStyle);
            y += lineHeight;

            // GC Alloc (推定) - 重要指標
            bool gcGood = gcAllocThisFrame < 10f;
            string gcStatus = gcGood ? "GOOD" : "要改善";
            GUIStyle gcStyle = gcGood ? goodStyle : badStyle;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                $"GC Alloc: {gcAllocThisFrame:F1} KB/s [{gcStatus}]", gcStyle);
            y += lineHeight;

            // 総メモリ
            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                $"Total Memory: {totalMemory / 1024 / 1024} MB", labelStyle);
            y += lineHeight;
        }

        private void DrawCPUMetrics(ref float y, float x, float labelWidth, float lineHeight)
        {
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), "── CPU ──", labelStyle);
            y += lineHeight;

            // FPS - 重要指標
            bool fpsGood = lastFps >= 60;
            string fpsStatus = fpsGood ? "GOOD" : "要改善";
            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                $"FPS: {lastFps:F1} [{fpsStatus}]", fpsGood ? goodStyle : badStyle);
            y += lineHeight;

            // Frame Time - 重要指標
            bool frameTimeGood = frameTime < 16.67f;
            string ftStatus = frameTimeGood ? "GOOD" : "要改善";
            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                $"Frame Time: {frameTime:F2} ms [{ftStatus}]", frameTimeGood ? goodStyle : badStyle);
            y += lineHeight;

            // AI更新時間
            float aiTime = aiManager?.GetLastUpdateTimeMs() ?? 0;
            bool aiGood = aiTime < 5f;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                $"AI Update: {aiTime:F2} ms", aiGood ? goodStyle : badStyle);
            y += lineHeight;
        }

        private void DrawDetailedInfo(ref float y, float x, float labelWidth, float lineHeight)
        {
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), "── Detail ──", labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                $"Reserved Memory: {usedMemory / 1024 / 1024} MB", labelStyle);
            y += lineHeight;

            int killCount = GameManager.Instance?.KillCount ?? 0;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                $"Kills: {killCount}", labelStyle);
            y += lineHeight;
        }

        private void DrawOptimizationStatus(ref float y, float x, float labelWidth, float lineHeight)
        {
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), "── Optimizations ──", labelStyle);
            y += lineHeight;

            // メモリ最適化
            DrawOptStatus(ref y, x, labelWidth, lineHeight, "Object Pool", settings.useObjectPool);
            DrawOptStatus(ref y, x, labelWidth, lineHeight, "StringBuilder", settings.useStringBuilder);
            DrawOptStatus(ref y, x, labelWidth, lineHeight, "Delegate Cache", settings.useDelegateCache);
            DrawOptStatus(ref y, x, labelWidth, lineHeight, "Collection Reuse", settings.useCollectionReuse);

            // CPU最適化
            DrawOptStatus(ref y, x, labelWidth, lineHeight, "Spatial Partition", settings.useSpatialPartition);
            DrawOptStatus(ref y, x, labelWidth, lineHeight, "Staggered Update", settings.useStaggeredUpdate);
            DrawOptStatus(ref y, x, labelWidth, lineHeight, "SqrMagnitude", settings.useSqrMagnitude);

            // トレードオフ
            DrawOptStatus(ref y, x, labelWidth, lineHeight, "Trig LUT", settings.useTrigLUT);
            DrawOptStatus(ref y, x, labelWidth, lineHeight, "Visibility Map", settings.useVisibilityMap);
        }

        private void DrawOptStatus(ref float y, float x, float labelWidth, float lineHeight, string name, bool enabled)
        {
            string status = enabled ? "[ON] " : "[OFF]";
            GUIStyle style = enabled ? goodStyle : badStyle;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"  {status} {name}", style);
            y += lineHeight;
        }

        /// <summary>
        /// 計測モードを設定する
        /// </summary>
        public void SetMeasurementMode(MeasurementMode mode)
        {
            measurementMode = mode;
        }

        /// <summary>
        /// 計測モードを設定する（文字列版）
        /// </summary>
        public void SetMeasurementMode(string modeString)
        {
            measurementMode = modeString switch
            {
                "Memory" => MeasurementMode.Memory,
                "CPU" => MeasurementMode.CPU,
                _ => MeasurementMode.All
            };
        }

        /// <summary>
        /// 現在の計測値をログ出力する
        /// </summary>
        public void LogCurrentStats()
        {
            sb.Clear();
            sb.AppendLine("=== Performance Stats ===");
            sb.AppendLine($"FPS: {lastFps:F1}");
            sb.AppendLine($"Frame Time: {frameTime:F2} ms");
            sb.AppendLine($"GC Alloc: ~{gcAllocThisFrame:F1} KB/s");
            sb.AppendLine($"Enemies: {GameManager.Instance?.CurrentEnemyCount ?? 0}");
            sb.AppendLine($"Kills: {GameManager.Instance?.KillCount ?? 0}");
            sb.AppendLine($"Memory: {totalMemory / 1024 / 1024} MB");
            Debug.Log(sb.ToString());
        }
    }
}
