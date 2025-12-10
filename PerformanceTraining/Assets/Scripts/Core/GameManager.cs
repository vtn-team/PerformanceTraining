using UnityEngine;
using UnityEngine.UI;
using PerformanceTraining.UI;

namespace PerformanceTraining.Core
{
    /// <summary>
    /// ゲーム全体の状態を管理するシングルトンクラス
    /// バトルロイヤル形式のゲーム進行を制御
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("設定")]
        [SerializeField] private LearningSettings learningSettings;

        [Header("参照")]
        [SerializeField] private CharacterManager characterManager;
        [SerializeField] private PerformanceMonitor performanceMonitor;

        [Header("ゲーム状態")]
        [SerializeField] private bool isGameRunning = false;
        [SerializeField] private Character winner;

        // プロパティ
        public LearningSettings Settings => learningSettings;
        public CharacterManager CharacterManager => characterManager;
        public bool IsGameRunning => isGameRunning;
        public int AliveCount => characterManager != null ? characterManager.AliveCount : 0;
        public int TotalKills => characterManager != null ? characterManager.TotalKills : 0;
        public Character Winner => winner;

        // ===== 後方互換性用プロパティ（既存のEnemyシステム用） =====
        public int CurrentEnemyCount => AliveCount;
        public int KillCount => TotalKills;

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
            // CharacterManagerのイベントを購読
            if (characterManager != null)
            {
                characterManager.OnBattleRoyaleWinner += HandleWinner;
            }

            // CharacterUI用のCanvasを確実に作成
            EnsureCharacterUICanvasExists();

            StartGame();
        }

        /// <summary>
        /// CharacterUI用のScreen Space Canvasを作成する
        /// </summary>
        private void EnsureCharacterUICanvasExists()
        {
            // 既存のCanvasを探す
            var existingCanvas = GameObject.Find("CharacterUICanvas");
            if (existingCanvas != null) return;

            // Canvasを作成
            var canvasObj = new GameObject("CharacterUICanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            // CanvasScaler設定（1920x1080基準、Height優先）
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            canvasObj.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasObj);

            Debug.Log("CharacterUICanvas created by GameManager");
        }

        /// <summary>
        /// ゲームを開始する
        /// </summary>
        public void StartGame()
        {
            isGameRunning = true;
            winner = null;

            // キャラクターマネージャーの初期化
            if (characterManager != null)
            {
                characterManager.Initialize();
                characterManager.SpawnInitialCharacters(GameConstants.INITIAL_ENEMY_COUNT);
            }

            Debug.Log($"Battle Royale Started! Initial characters: {GameConstants.INITIAL_ENEMY_COUNT}");
        }

        private void HandleWinner(Character winningCharacter)
        {
            winner = winningCharacter;
            isGameRunning = false;
            Debug.Log($"Battle Royale Ended! Winner: {winningCharacter.CharacterName}");
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
            if (characterManager != null)
            {
                characterManager.ClearAllCharacters();
            }
            StartGame();
        }

        /// <summary>
        /// キャラクターを追加スポーンする
        /// </summary>
        public void SpawnMoreCharacters(int count)
        {
            if (characterManager != null)
            {
                characterManager.SpawnAdditionalCharacters(count);
            }
        }

        /// <summary>
        /// 範囲攻撃を実行（プレイヤー介入）
        /// </summary>
        public int PerformAreaAttack(Vector3 center, float radius, float damage)
        {
            if (characterManager == null) return 0;
            return characterManager.DealAreaDamage(center, radius, damage);
        }

        /// <summary>
        /// ランダムなキャラクターにバフを付与（プレイヤー介入）
        /// </summary>
        public Character BuffRandomCharacter(float multiplier, float duration)
        {
            if (characterManager == null) return null;
            return characterManager.ApplyBuffToRandomCharacter(multiplier, duration);
        }

        #region Backward Compatibility (for existing Enemy system)

        /// <summary>
        /// プレイヤー位置を取得（バトルロイヤルではプレイヤーなし）
        /// </summary>
        public Vector3 GetPlayerPosition()
        {
            // バトルロイヤルモードではプレイヤーがいないため、中央を返す
            return Vector3.zero;
        }

        /// <summary>
        /// 敵スポーン時の通知（後方互換性用）
        /// </summary>
        public void OnEnemySpawned()
        {
            // CharacterManagerが管理するため、ここでは何もしない
        }

        /// <summary>
        /// 敵撃破時の通知（後方互換性用）
        /// </summary>
        public void OnEnemyKilled()
        {
            // CharacterManagerが管理するため、ここでは何もしない
        }

        #endregion

        private void OnDestroy()
        {
            if (characterManager != null)
            {
                characterManager.OnBattleRoyaleWinner -= HandleWinner;
            }

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
            // F1: キャラクター50体追加
            if (Input.GetKeyDown(KeyCode.F1))
            {
                SpawnMoreCharacters(GameConstants.ENEMY_SPAWN_BATCH_SIZE);
                Debug.Log($"Spawned {GameConstants.ENEMY_SPAWN_BATCH_SIZE} characters. Total: {AliveCount}");
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
            if (Input.GetKeyDown(KeyCode.F4) && learningSettings != null)
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

            // F5: ランダムバフ
            if (Input.GetKeyDown(KeyCode.F5))
            {
                var buffed = BuffRandomCharacter(2f, 10f);
                if (buffed != null)
                {
                    Debug.Log($"Buffed: {buffed.CharacterName}");
                }
            }

            // F6: 中央に範囲攻撃
            if (Input.GetKeyDown(KeyCode.F6))
            {
                int hits = PerformAreaAttack(Vector3.zero, 20f, 50f);
                Debug.Log($"Area Attack hit {hits} characters");
            }
        }

        #endregion
    }
}
