using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using MassacreDojo.Core;
using MassacreDojo.Enemy;

namespace MassacreDojo.UI
{
    /// <summary>
    /// パフォーマンス計測値を画面に表示するUI
    /// 学生がProfilerと併用して最適化の効果を確認するためのツール
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        [Header("表示設定")]
        [SerializeField] private bool showMonitor = true;
        [SerializeField] private bool showDetailedInfo = false;
        [SerializeField] private bool showOptimizationStatus = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F5;
        [SerializeField] private KeyCode detailToggleKey = KeyCode.F6;

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
            enemySystem = FindObjectOfType<EnemySystem>();
            aiManager = FindObjectOfType<EnemyAIManager>();
            settings = GameManager.Instance?.Settings;
        }

        private void Update()
        {
            // トグルキー
            if (Input.GetKeyDown(toggleKey))
            {
                showMonitor = !showMonitor;
            }
            if (Input.GetKeyDown(detailToggleKey))
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

            float lineHeight = 18f;
            float padding = 10f;
            int lineCount = 0;

            // ライン数をカウント
            lineCount += 2; // ヘッダー
            lineCount += 5; // 基本情報
            if (showDetailedInfo) lineCount += 4;
            if (showOptimizationStatus) lineCount += 12;
            lineCount += 2; // キー説明

            float height = lineCount * lineHeight + padding * 2;
            Rect boxRect = new Rect(position.x, position.y, size.x, height);

            GUI.Box(boxRect, "", boxStyle);

            float y = position.y + padding;
            float x = position.x + padding;
            float labelWidth = size.x - padding * 2;

            // ヘッダー
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), "Performance Monitor", headerStyle);
            y += lineHeight;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), "━━━━━━━━━━━━━━━━━", labelStyle);
            y += lineHeight;

            // FPS
            bool fpsGood = lastFps >= 60;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                $"FPS: {lastFps:F1}", fpsGood ? goodStyle : badStyle);
            y += lineHeight;

            // Frame Time
            bool frameTimeGood = frameTime < 16.67f;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                $"Frame Time: {frameTime:F2} ms", frameTimeGood ? goodStyle : badStyle);
            y += lineHeight;

            // GC Alloc (推定)
            bool gcGood = gcAllocThisFrame < 10f; // 10KB/s以下
            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                $"GC Alloc: ~{gcAllocThisFrame:F1} KB/s", gcGood ? goodStyle : badStyle);
            y += lineHeight;

            // Enemy Count
            int enemyCount = GameManager.Instance?.CurrentEnemyCount ?? 0;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                $"Enemies: {enemyCount}", labelStyle);
            y += lineHeight;

            // Kill Count
            int killCount = GameManager.Instance?.KillCount ?? 0;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                $"Kills: {killCount}", labelStyle);
            y += lineHeight;

            // 詳細情報
            if (showDetailedInfo)
            {
                GUI.Label(new Rect(x, y, labelWidth, lineHeight), "── Detail ──", labelStyle);
                y += lineHeight;

                GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                    $"Total Memory: {totalMemory / 1024 / 1024} MB", labelStyle);
                y += lineHeight;

                GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                    $"Reserved: {usedMemory / 1024 / 1024} MB", labelStyle);
                y += lineHeight;

                float aiTime = aiManager?.GetLastUpdateTimeMs() ?? 0;
                GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                    $"AI Update: {aiTime:F2} ms", labelStyle);
                y += lineHeight;
            }

            // 最適化ステータス
            if (showOptimizationStatus && settings != null)
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

            // キー説明
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), "━━━━━━━━━━━━━━━━━", labelStyle);
            y += lineHeight;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                $"[{toggleKey}] Toggle | [{detailToggleKey}] Detail | [F4] All Opt", labelStyle);
        }

        private void DrawOptStatus(ref float y, float x, float labelWidth, float lineHeight, string name, bool enabled)
        {
            string status = enabled ? "[ON] " : "[OFF]";
            GUIStyle style = enabled ? goodStyle : badStyle;
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), $"  {status} {name}", style);
            y += lineHeight;
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
