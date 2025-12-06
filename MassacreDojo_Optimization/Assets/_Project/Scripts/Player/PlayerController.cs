using UnityEngine;
using PerformanceTraining.Core;
using PerformanceTraining.Enemy;

namespace PerformanceTraining.Player
{
    /// <summary>
    /// プレイヤーの移動、攻撃、回避を制御するコントローラー
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform attackPoint;
        [SerializeField] private Transform modelTransform;

        [Header("設定（上書き可能）")]
        [SerializeField] private float moveSpeed = GameConstants.PLAYER_MOVE_SPEED;
        [SerializeField] private float attackRange = GameConstants.PLAYER_ATTACK_RANGE;
        [SerializeField] private float attackCooldown = GameConstants.PLAYER_ATTACK_COOLDOWN;
        [SerializeField] private float dodgeSpeed = GameConstants.PLAYER_DODGE_SPEED;
        [SerializeField] private float dodgeDuration = GameConstants.PLAYER_DODGE_DURATION;
        [SerializeField] private float dodgeCooldown = GameConstants.PLAYER_DODGE_COOLDOWN;
        [SerializeField] private int attackDamage = GameConstants.PLAYER_ATTACK_DAMAGE;

        [Header("状態")]
        [SerializeField] private bool isDodging = false;
        [SerializeField] private bool canDodge = true;
        [SerializeField] private bool canAttack = true;

        // 内部変数
        private Vector3 moveDirection;
        private Vector3 dodgeDirection;
        private float dodgeTimer;
        private float attackCooldownTimer;
        private float dodgeCooldownTimer;
        private Camera mainCamera;

        // 攻撃アニメーション用
        private float attackAnimTimer;
        private const float ATTACK_ANIM_DURATION = 0.2f;

        private void Awake()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (attackPoint == null)
            {
                // 攻撃判定ポイントを自動生成
                var attackObj = new GameObject("AttackPoint");
                attackObj.transform.SetParent(transform);
                attackObj.transform.localPosition = Vector3.forward * 1.5f;
                attackPoint = attackObj.transform;
            }

            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning)
                return;

            HandleMovement();
            HandleRotation();
            HandleDodge();
            HandleAttack();
            UpdateTimers();
            UpdateAttackAnimation();
        }

        /// <summary>
        /// 移動処理
        /// </summary>
        private void HandleMovement()
        {
            if (isDodging) return;

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            // カメラ基準の移動方向を計算
            Vector3 forward = mainCamera.transform.forward;
            Vector3 right = mainCamera.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            moveDirection = (forward * vertical + right * horizontal).normalized;

            if (moveDirection.magnitude > 0.1f)
            {
                Vector3 motion = moveDirection * moveSpeed * Time.deltaTime;
                characterController.Move(motion);

                // フィールド範囲内に制限
                ClampPosition();
            }
        }

        /// <summary>
        /// 回転処理（マウス方向を向く）
        /// </summary>
        private void HandleRotation()
        {
            if (isDodging) return;

            // マウス位置へのレイキャストで地面との交点を取得
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, transform.position);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 targetPoint = ray.GetPoint(distance);
                Vector3 lookDirection = targetPoint - transform.position;
                lookDirection.y = 0f;

                if (lookDirection.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(lookDirection);
                }
            }
        }

        /// <summary>
        /// 回避処理
        /// </summary>
        private void HandleDodge()
        {
            // 回避中の処理
            if (isDodging)
            {
                Vector3 motion = dodgeDirection * dodgeSpeed * Time.deltaTime;
                characterController.Move(motion);
                ClampPosition();

                dodgeTimer -= Time.deltaTime;
                if (dodgeTimer <= 0f)
                {
                    isDodging = false;
                }
                return;
            }

            // 回避入力
            if (Input.GetKeyDown(KeyCode.Space) && canDodge)
            {
                StartDodge();
            }
        }

        /// <summary>
        /// 回避を開始
        /// </summary>
        private void StartDodge()
        {
            isDodging = true;
            canDodge = false;
            dodgeTimer = dodgeDuration;
            dodgeCooldownTimer = dodgeCooldown;

            // 移動方向に回避、入力がなければ後方に回避
            if (moveDirection.sqrMagnitude > 0.1f)
            {
                dodgeDirection = moveDirection;
            }
            else
            {
                dodgeDirection = -transform.forward;
            }
        }

        /// <summary>
        /// 攻撃処理
        /// </summary>
        private void HandleAttack()
        {
            if (isDodging) return;

            // 左クリックで攻撃
            if (Input.GetMouseButtonDown(0) && canAttack)
            {
                PerformAttack();
            }
        }

        /// <summary>
        /// 攻撃を実行
        /// </summary>
        private void PerformAttack()
        {
            canAttack = false;
            attackCooldownTimer = attackCooldown;
            attackAnimTimer = ATTACK_ANIM_DURATION;

            // 攻撃範囲内の敵にダメージを与える
            Vector3 attackPosition = attackPoint.position;
            EnemySystem enemySystem = FindAnyObjectByType<EnemySystem>();

            if (enemySystem != null)
            {
                int hits = enemySystem.DamageEnemiesInRange(attackPosition, attackRange, attackDamage);
                if (hits > 0)
                {
                    // ヒットエフェクトなど（必要に応じて追加）
                }
            }
        }

        /// <summary>
        /// タイマー更新
        /// </summary>
        private void UpdateTimers()
        {
            // 攻撃クールダウン
            if (!canAttack)
            {
                attackCooldownTimer -= Time.deltaTime;
                if (attackCooldownTimer <= 0f)
                {
                    canAttack = true;
                }
            }

            // 回避クールダウン
            if (!canDodge && !isDodging)
            {
                dodgeCooldownTimer -= Time.deltaTime;
                if (dodgeCooldownTimer <= 0f)
                {
                    canDodge = true;
                }
            }
        }

        /// <summary>
        /// 攻撃アニメーション更新（簡易的なスケールアニメーション）
        /// </summary>
        private void UpdateAttackAnimation()
        {
            if (attackAnimTimer > 0f)
            {
                attackAnimTimer -= Time.deltaTime;
                float t = attackAnimTimer / ATTACK_ANIM_DURATION;

                if (modelTransform != null)
                {
                    // 攻撃時に前方に伸びるアニメーション
                    float scaleZ = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
                    modelTransform.localScale = new Vector3(1f, 1f, scaleZ);
                }
            }
            else if (modelTransform != null)
            {
                modelTransform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// フィールド範囲内に位置を制限
        /// </summary>
        private void ClampPosition()
        {
            Vector3 pos = transform.position;
            float limit = GameConstants.FIELD_HALF_SIZE - GameConstants.SPAWN_MARGIN;

            pos.x = Mathf.Clamp(pos.x, -limit, limit);
            pos.z = Mathf.Clamp(pos.z, -limit, limit);

            transform.position = pos;
        }

        /// <summary>
        /// ダメージを受ける
        /// </summary>
        public void TakeDamage(int damage)
        {
            // 回避中は無敵
            if (isDodging) return;

            // ダメージ処理（必要に応じて実装）
            Debug.Log($"Player took {damage} damage!");
        }

        private void OnDrawGizmosSelected()
        {
            // 攻撃範囲を可視化
            if (attackPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(attackPoint.position, attackRange);
            }
        }
    }
}
