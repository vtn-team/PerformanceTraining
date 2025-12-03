using System.Collections.Generic;
using UnityEngine;
using MassacreDojo.Core;

#if EXERCISES_DEPLOYED
using StudentExercises.CPU;
#else
using MassacreDojo.Exercises.CPU;
#endif

namespace MassacreDojo.Enemy
{
    /// <summary>
    /// 敵AIの管理を行うシステム
    /// 【課題2: CPU最適化】問題を含むコード
    /// - Step 1: 空間分割 → O(n²)の総当たり検索
    /// - Step 2: 更新分散 → 全敵が毎フレーム更新
    /// - Step 3: 距離計算 → Vector3.Distance使用
    /// </summary>
    public class EnemyAIManager : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private EnemySystem enemySystem;

        [Header("デバッグ")]
        [SerializeField] private int nearbyEnemyCount;
        [SerializeField] private float lastUpdateTime;

        // 学習用の参照
        private LearningSettings settings;
        private CPUOptimization_Exercise cpuExercise;

        // フレームカウンター（更新分散用）
        private int frameCount;

        private void Awake()
        {
            if (enemySystem == null)
            {
                enemySystem = GetComponent<EnemySystem>();
            }
        }

        private void Start()
        {
            settings = GameManager.Instance?.Settings;

            // Exerciseクラスのインスタンスを取得または作成
            cpuExercise = GetComponent<CPUOptimization_Exercise>();
            if (cpuExercise == null)
            {
                cpuExercise = gameObject.AddComponent<CPUOptimization_Exercise>();
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning)
                return;

            float startTime = Time.realtimeSinceStartup;

            frameCount++;

            UpdateAllEnemiesAI();

            lastUpdateTime = (Time.realtimeSinceStartup - startTime) * 1000f; // ms
        }

        /// <summary>
        /// 全敵のAI更新
        /// </summary>
        private void UpdateAllEnemiesAI()
        {
            if (enemySystem == null) return;

            var enemies = enemySystem.ActiveEnemies;
            Vector3 playerPos = GameManager.Instance.GetPlayerPosition();

            // ===== Step 1: 空間分割でグリッド更新 =====
            if (settings != null && settings.useSpatialPartition)
            {
                cpuExercise.UpdateSpatialGrid(enemies);
            }

            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;

                // ===== Step 2: 更新分散 =====
                if (settings != null && settings.useStaggeredUpdate)
                {
                    // 最適化版: グループごとに更新を分散
                    if (!cpuExercise.ShouldUpdateThisFrame(enemy.UpdateGroup, frameCount))
                    {
                        // このフレームでは重い処理をスキップ
                        // 軽い処理（移動補間など）のみ実行
                        ContinuePreviousMovement(enemy);
                        continue;
                    }
                }

                // AI判断処理
                UpdateEnemyAI(enemy, playerPos, enemies);
            }
        }

        /// <summary>
        /// 個別敵のAI更新
        /// </summary>
        private void UpdateEnemyAI(Enemy enemy, Vector3 playerPos, List<Enemy> allEnemies)
        {
            float distToPlayer = CalculateDistanceToPlayer(enemy, playerPos);

            // 状態判定
            if (distToPlayer < GameConstants.ENEMY_ATTACK_RANGE)
            {
                // 攻撃範囲内
                enemy.SetState(EnemyState.Attack);
                PerformAttackBehavior(enemy);
            }
            else if (distToPlayer < GameConstants.ENEMY_DETECTION_RANGE)
            {
                // 検知範囲内
                enemy.SetState(EnemyState.Chase);
                PerformChaseBehavior(enemy, playerPos, allEnemies);
            }
            else
            {
                // 範囲外
                enemy.SetState(EnemyState.Idle);
                PerformIdleBehavior(enemy);
            }
        }

        /// <summary>
        /// プレイヤーとの距離計算
        /// 【問題コード】Vector3.Distanceで平方根計算
        /// </summary>
        private float CalculateDistanceToPlayer(Enemy enemy, Vector3 playerPos)
        {
            // ===== Step 3: 距離計算 =====
            if (settings != null && settings.useSqrMagnitude)
            {
                // 最適化版: sqrMagnitudeを使用し、比較時も2乗値で比較
                // 注: 戻り値は2乗距離なので、呼び出し側で考慮が必要
                return cpuExercise.CalculateDistanceSqr(enemy.transform.position, playerPos);
            }
            else
            {
                // 問題版: Vector3.Distance（毎回平方根計算）
                return Vector3.Distance(enemy.transform.position, playerPos);
            }
        }

        /// <summary>
        /// 攻撃行動
        /// </summary>
        private void PerformAttackBehavior(Enemy enemy)
        {
            if (enemy.CanAttack())
            {
                enemy.PerformAttack();
                // プレイヤーにダメージ（実装は省略）
            }
        }

        /// <summary>
        /// 追跡行動
        /// 【問題コード】最近接敵の検索がO(n²)
        /// </summary>
        private void PerformChaseBehavior(Enemy enemy, Vector3 playerPos, List<Enemy> allEnemies)
        {
            // 近くにいる敵を取得（密集を避けるため）
            List<Enemy> nearbyEnemies = GetNearbyEnemies(enemy, allEnemies);
            nearbyEnemyCount = nearbyEnemies.Count;

            // 基本的にはプレイヤーに向かう
            Vector3 dirToPlayer = (playerPos - enemy.transform.position).normalized;

            // 他の敵との距離を考慮して移動方向を調整
            Vector3 separationForce = Vector3.zero;
            foreach (var other in nearbyEnemies)
            {
                if (other == enemy) continue;
                Vector3 diff = enemy.transform.position - other.transform.position;
                float dist = diff.magnitude;
                if (dist > 0.1f && dist < 2f)
                {
                    separationForce += diff.normalized / dist;
                }
            }

            Vector3 finalDirection = (dirToPlayer + separationForce * 0.3f).normalized;
            enemy.Move(finalDirection, GameConstants.ENEMY_MOVE_SPEED, Time.deltaTime);
        }

        /// <summary>
        /// 近くの敵を取得
        /// 【問題コード】O(n²)の総当たり検索
        /// </summary>
        private List<Enemy> GetNearbyEnemies(Enemy enemy, List<Enemy> allEnemies)
        {
            // ===== Step 1: 空間分割 =====
            if (settings != null && settings.useSpatialPartition)
            {
                // 最適化版: 空間分割で近傍のみ検索
                return cpuExercise.QueryNearbyEnemies(enemy.transform.position);
            }
            else
            {
                // 問題版: 全敵との総当たり検索（O(n²)）
                List<Enemy> nearby = new List<Enemy>(); // 毎回new List（これも問題）

                foreach (var other in allEnemies)
                {
                    if (other == null || other == enemy || !other.IsAlive) continue;

                    // 毎回Vector3.Distance（さらに問題）
                    float dist = Vector3.Distance(enemy.transform.position, other.transform.position);
                    if (dist < GameConstants.CELL_SIZE)
                    {
                        nearby.Add(other);
                    }
                }

                return nearby;
            }
        }

        /// <summary>
        /// 待機行動
        /// </summary>
        private void PerformIdleBehavior(Enemy enemy)
        {
            // ゆっくり徘徊
            if (UnityEngine.Random.value < 0.01f) // 1%の確率で方向転換
            {
                Vector3 randomDir = new Vector3(
                    UnityEngine.Random.Range(-1f, 1f),
                    0f,
                    UnityEngine.Random.Range(-1f, 1f)
                ).normalized;
                enemy.SetTargetPosition(enemy.transform.position + randomDir * 5f);
            }

            Vector3 dir = (enemy.TargetPosition - enemy.transform.position).normalized;
            if (dir.sqrMagnitude > 0.01f)
            {
                enemy.Move(dir, GameConstants.ENEMY_MOVE_SPEED * 0.3f, Time.deltaTime);
            }
        }

        /// <summary>
        /// 前フレームの移動を継続（更新分散時に使用）
        /// </summary>
        private void ContinuePreviousMovement(Enemy enemy)
        {
            // 直前の状態を継続（補間）
            Vector3 dir = (enemy.TargetPosition - enemy.transform.position).normalized;
            if (dir.sqrMagnitude > 0.01f)
            {
                enemy.Move(dir, GameConstants.ENEMY_MOVE_SPEED, Time.deltaTime);
            }
        }

        /// <summary>
        /// デバッグ用: 最後の更新時間を取得
        /// </summary>
        public float GetLastUpdateTimeMs() => lastUpdateTime;
    }
}
