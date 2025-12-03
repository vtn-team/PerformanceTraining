using UnityEngine;
using MassacreDojo.Enemy;
using MassacreDojo.UI;

namespace MassacreDojo.Core
{
    /// <summary>
    /// ゲーム全体の状態を管理するシングルトンクラス
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("設定")]
        [SerializeField] private LearningSettings learningSettings;

        [Header("参照")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private EnemySystem enemySystem;
        [SerializeField] private PerformanceMonitor performanceMonitor;

        [Header("ゲーム状態")]
        [SerializeField] private int killCount = 0;
        [SerializeField] private int currentEnemyCount = 0;
        [SerializeField] private bool isGameRunning = false;

        // プロパティ
        public LearningSettings Settings => learningSettings;
        public Transform PlayerTransform => playerTransform;
        public int KillCount => killCount;
        public int CurrentEnemyCount => currentEnemyCount;
        public bool IsGameRunning => isGameRunning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // LearningSettingsをResourcesから読み込み
            if (learningSettings == null)
            {
                learningSettings = Resources.Load<LearningSettings>("LearningSettings");
                if (learningSettings == null)
                {
                    Debug.LogWarning("LearningSettings not found. Creating default settings.");
                    learningSettings = ScriptableObject.CreateInstance<LearningSettings>();
                }
            }
        }

        private void Start()
        {
            StartGame();
        }

        /// <summary>
        /// ゲームを開始する
        /// </summary>
        public void StartGame()
        {
            isGameRunning = true;
            killCount = 0;

            // 敵システムの初期化
            if (enemySystem != null)
            {
                enemySystem.Initialize();
                enemySystem.SpawnEnemies(GameConstants.INITIAL_ENEMY_COUNT);
            }

            Debug.Log($"Game Started! Initial enemies: {GameConstants.INITIAL_ENEMY_COUNT}");
        }

        /// <summary>
        /// ゲームを一時停止する
        /// </summary>
        public void PauseGame()
        {
            isGameRunning = false;
            Time.timeScale = 0f;
        }

        /// <summary>
        /// ゲームを再開する
        /// </summary>
        public void ResumeGame()
        {
            isGameRunning = true;
            Time.timeScale = 1f;
        }

        /// <summary>
        /// ゲームをリセットする
        /// </summary>
        public void ResetGame()
        {
            if (enemySystem != null)
            {
                enemySystem.DespawnAllEnemies();
            }
            StartGame();
        }

        /// <summary>
        /// 敵を倒した時に呼ばれる
        /// </summary>
        public void OnEnemyKilled()
        {
            killCount++;
            currentEnemyCount--;
        }

        /// <summary>
        /// 敵がスポーンした時に呼ばれる
        /// </summary>
        public void OnEnemySpawned()
        {
            currentEnemyCount++;
        }

        /// <summary>
        /// 敵を追加スポーンする
        /// </summary>
        public void SpawnMoreEnemies(int count)
        {
            if (enemySystem != null && currentEnemyCount + count <= GameConstants.MAX_ENEMY_COUNT)
            {
                enemySystem.SpawnEnemies(count);
            }
        }

        /// <summary>
        /// 現在のプレイヤー位置を取得
        /// </summary>
        public Vector3 GetPlayerPosition()
        {
            return playerTransform != null ? playerTransform.position : Vector3.zero;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #region Debug Controls

        private void Update()
        {
            HandleDebugInput();
        }

        private void HandleDebugInput()
        {
            // F1: 敵50体追加
            if (Input.GetKeyDown(KeyCode.F1))
            {
                SpawnMoreEnemies(GameConstants.ENEMY_SPAWN_BATCH_SIZE);
                Debug.Log($"Spawned {GameConstants.ENEMY_SPAWN_BATCH_SIZE} enemies. Total: {currentEnemyCount}");
            }

            // F2: リセット
            if (Input.GetKeyDown(KeyCode.F2))
            {
                ResetGame();
                Debug.Log("Game Reset!");
            }

            // F3: 一時停止/再開
            if (Input.GetKeyDown(KeyCode.F3))
            {
                if (isGameRunning)
                {
                    PauseGame();
                    Debug.Log("Game Paused");
                }
                else
                {
                    ResumeGame();
                    Debug.Log("Game Resumed");
                }
            }

            // F4: 全最適化ON/OFF切り替え
            if (Input.GetKeyDown(KeyCode.F4))
            {
                if (learningSettings.AllOptimizationsEnabled)
                {
                    learningSettings.ResetToDefault();
                    Debug.Log("All optimizations DISABLED");
                }
                else
                {
                    learningSettings.EnableAllOptimizations();
                    Debug.Log("All optimizations ENABLED");
                }
            }
        }

        #endregion
    }
}
