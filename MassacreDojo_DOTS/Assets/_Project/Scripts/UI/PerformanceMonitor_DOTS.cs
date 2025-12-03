using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using MassacreDojo.DOTS.Components;

namespace MassacreDojo.DOTS.UI
{
    /// <summary>
    /// DOTS版パフォーマンスモニター
    /// ECSのエンティティ数や各システムの処理時間を表示
    /// </summary>
    public class PerformanceMonitor_DOTS : MonoBehaviour
    {
        [Header("表示設定")]
        [SerializeField] private bool showMonitor = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F5;

        [Header("位置・サイズ")]
        [SerializeField] private Vector2 position = new Vector2(10, 10);
        [SerializeField] private Vector2 size = new Vector2(300, 0);

        // 計測値
        private float fps;
        private float frameTime;
        private int entityCount;
        private long totalMemory;

        // FPS計算用
        private int frameCount;
        private float fpsTimer;
        private float lastFps;
        private float updateInterval = 0.5f;

        // UI用
        private GUIStyle boxStyle;
        private GUIStyle labelStyle;
        private GUIStyle headerStyle;
        private GUIStyle goodStyle;
        private GUIStyle badStyle;
        private bool stylesInitialized;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showMonitor = !showMonitor;
            }

            // FPS計算
            frameCount++;
            fpsTimer += Time.unscaledDeltaTime;

            if (fpsTimer >= updateInterval)
            {
                lastFps = frameCount / fpsTimer;
                frameCount = 0;
                fpsTimer = 0f;

                // エンティティ数を取得
                UpdateEntityCount();

                // メモリ情報
                totalMemory = Profiler.GetTotalAllocatedMemoryLong();
            }

            frameTime = Time.unscaledDeltaTime * 1000f;
        }

        private void UpdateEntityCount()
        {
            if (World.DefaultGameObjectInjectionWorld == null)
                return;

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            // EnemyTagを持つエンティティ数をカウント
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<EnemyTag>());
            entityCount = query.CalculateEntityCount();
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

            InitStyles();

            float lineHeight = 18f;
            float padding = 10f;
            int lineCount = 10;

            float height = lineCount * lineHeight + padding * 2;
            Rect boxRect = new Rect(position.x, position.y, size.x, height);

            GUI.Box(boxRect, "", boxStyle);

            float y = position.y + padding;
            float x = position.x + padding;
            float labelWidth = size.x - padding * 2;

            // ヘッダー
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), "DOTS Performance Monitor", headerStyle);
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

            // Entity Count
            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                $"Entities (Enemies): {entityCount}", labelStyle);
            y += lineHeight;

            // Memory
            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                $"Memory: {totalMemory / 1024 / 1024} MB", labelStyle);
            y += lineHeight;

            // DOTS Info
            GUI.Label(new Rect(x, y, labelWidth, lineHeight), "── DOTS Features ──", labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                "  [ON] Burst Compiler", goodStyle);
            y += lineHeight;

            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                "  [ON] Job System (Parallel)", goodStyle);
            y += lineHeight;

            // キー説明
            GUI.Label(new Rect(x, y, labelWidth, lineHeight),
                $"[{toggleKey}] Toggle Monitor", labelStyle);
        }
    }
}
