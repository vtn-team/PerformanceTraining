using UnityEngine;
using PerformanceTraining.Core;

namespace PerformanceTraining.Enemy
{
    /// <summary>
    /// 敵の基本クラス
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        [Header("ステータス")]
        [SerializeField] private int health = 100;
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int attackDamage = 10;

        [Header("状態")]
        [SerializeField] private EnemyState currentState = EnemyState.Idle;
        [SerializeField] private bool isAlive = true;

        [Header("参照")]
        [SerializeField] private Transform visualTransform;

        // 内部変数
        private Vector3 targetPosition;
        private float attackCooldownTimer;
        private int updateGroup; // 更新分散用のグループID

        // プロパティ
        public int Health => health;
        public int MaxHealth => maxHealth;
        public bool IsAlive => isAlive;
        public EnemyState State => currentState;
        public Vector3 TargetPosition => targetPosition;
        public int UpdateGroup => updateGroup;

        public void Initialize(int group)
        {
            health = maxHealth;
            isAlive = true;
            currentState = EnemyState.Idle;
            attackCooldownTimer = 0f;
            updateGroup = group;
        }

        public void SetState(EnemyState state)
        {
            currentState = state;
        }

        public void SetTargetPosition(Vector3 position)
        {
            targetPosition = position;
        }

        /// <summary>
        /// ダメージを受ける
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (!isAlive) return;

            health -= damage;

            // ダメージエフェクト（簡易的な点滅）
            if (visualTransform != null)
            {
                StartCoroutine(DamageFlash());
            }

            if (health <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// ダメージ時の点滅エフェクト
        /// </summary>
        private System.Collections.IEnumerator DamageFlash()
        {
            if (visualTransform != null)
            {
                var renderer = visualTransform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var originalColor = renderer.material.color;
                    renderer.material.color = Color.red;
                    yield return new WaitForSeconds(0.1f);
                    if (renderer != null)
                    {
                        renderer.material.color = originalColor;
                    }
                }
            }
        }

        /// <summary>
        /// 死亡処理
        /// </summary>
        private void Die()
        {
            isAlive = false;
            currentState = EnemyState.Dead;

            // GameManagerに通知
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemyKilled();
            }

            // EnemySystemに返却を通知
            EnemySystem enemySystem = FindAnyObjectByType<EnemySystem>();
            if (enemySystem != null)
            {
                enemySystem.ReturnEnemy(this);
            }
        }

        /// <summary>
        /// 攻撃可能かどうか
        /// </summary>
        public bool CanAttack()
        {
            return attackCooldownTimer <= 0f;
        }

        /// <summary>
        /// 攻撃を実行
        /// </summary>
        public void PerformAttack()
        {
            attackCooldownTimer = GameConstants.ENEMY_ATTACK_COOLDOWN;
        }

        /// <summary>
        /// クールダウン更新
        /// </summary>
        public void UpdateCooldown(float deltaTime)
        {
            if (attackCooldownTimer > 0f)
            {
                attackCooldownTimer -= deltaTime;
            }
        }

        /// <summary>
        /// 移動処理
        /// </summary>
        public void Move(Vector3 direction, float speed, float deltaTime)
        {
            if (!isAlive) return;

            Vector3 newPosition = transform.position + direction * speed * deltaTime;

            // フィールド範囲内に制限
            float limit = GameConstants.FIELD_HALF_SIZE - 1f;
            newPosition.x = Mathf.Clamp(newPosition.x, -limit, limit);
            newPosition.z = Mathf.Clamp(newPosition.z, -limit, limit);

            transform.position = newPosition;

            // 移動方向を向く
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = currentState switch
            {
                EnemyState.Idle => Color.gray,
                EnemyState.Chase => Color.yellow,
                EnemyState.Attack => Color.red,
                EnemyState.Surround => Color.blue,
                _ => Color.white
            };
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }

    /// <summary>
    /// 敵の状態
    /// </summary>
    public enum EnemyState
    {
        Idle,       // 待機
        Chase,      // 追跡
        Attack,     // 攻撃
        Surround,   // 包囲
        Dead        // 死亡
    }
}
