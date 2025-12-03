using UnityEngine;
using MassacreDojo.Core;
using MassacreDojo.Enemy;

namespace MassacreDojo.UI
{
    /// <summary>
    /// 敵スポーンUIコンポーネント
    /// プレイ中に敵の数を調整するためのUI（IMGUI版）
    /// </summary>
    public class SpawnUI : MonoBehaviour
    {
        [Header("UI Settings")]
        [SerializeField] private bool showUI = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

        private EnemySystem enemySystem;
        private Rect windowRect = new Rect(10, 10, 160, 200);
        private int windowId;

        private void Start()
        {
            enemySystem = FindObjectOfType<EnemySystem>();
            windowId = GetInstanceID();
        }

        private void Update()
        {
            // UIの表示/非表示切り替え
            if (Input.GetKeyDown(toggleKey))
            {
                showUI = !showUI;
            }
        }

        private void OnGUI()
        {
            if (!showUI) return;

            windowRect = GUI.Window(windowId, windowRect, DrawWindow, "Enemy Spawn");
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical();

            // 敵数表示
            int count = enemySystem != null ? enemySystem.ActiveEnemyCount : 0;
            GUILayout.Label($"Enemies: {count}", GetCenteredStyle());

            GUILayout.Space(10);

            // スポーンボタン
            if (GUILayout.Button("+10", GUILayout.Height(30)))
            {
                SpawnEnemies(10);
            }

            if (GUILayout.Button("+100", GUILayout.Height(30)))
            {
                SpawnEnemies(100);
            }

            if (GUILayout.Button("+500", GUILayout.Height(30)))
            {
                SpawnEnemies(500);
            }

            GUILayout.Space(10);

            // クリアボタン
            GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
            if (GUILayout.Button("Clear All", GUILayout.Height(30)))
            {
                ClearAllEnemies();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndVertical();

            // ウィンドウをドラッグ可能に
            GUI.DragWindow();
        }

        private GUIStyle GetCenteredStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 14;
            return style;
        }

        private void SpawnEnemies(int count)
        {
            if (enemySystem == null)
            {
                enemySystem = FindObjectOfType<EnemySystem>();
                if (enemySystem == null) return;
            }

            var player = FindObjectOfType<Player.PlayerController>();
            Vector3 center = player != null ? player.transform.position : Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                // プレイヤーの周囲にランダムスポーン
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(GameConstants.SPAWN_MIN_DISTANCE, GameConstants.SPAWN_MAX_DISTANCE);

                Vector3 spawnPos = center + new Vector3(
                    Mathf.Cos(angle) * distance,
                    0f,
                    Mathf.Sin(angle) * distance
                );

                enemySystem.SpawnEnemy(spawnPos);
            }

            Debug.Log($"Spawned {count} enemies. Total: {enemySystem.ActiveEnemyCount}");
        }

        private void ClearAllEnemies()
        {
            if (enemySystem == null)
            {
                enemySystem = FindObjectOfType<EnemySystem>();
                if (enemySystem == null) return;
            }

            enemySystem.DespawnAllEnemies();
            Debug.Log("Cleared all enemies");
        }
    }
}
