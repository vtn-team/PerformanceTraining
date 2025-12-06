using UnityEngine;
using System;

namespace PerformanceTraining.Core
{
    /// <summary>
    /// キャラクターの種類
    /// </summary>
    public enum CharacterType
    {
        Warrior,    // 戦士 - HP高、攻撃力中
        Assassin,   // 暗殺者 - HP低、攻撃力高、速度高
        Tank,       // タンク - HP最高、攻撃力低、速度低
        Mage,       // 魔法使い - HP低、攻撃力高、範囲攻撃
        Ranger,     // レンジャー - HP中、攻撃力中、射程長
        Berserker   // バーサーカー - HP中、攻撃力変動、速度高
    }

    /// <summary>
    /// キャラクターの状態
    /// </summary>
    public enum CharacterState
    {
        Idle,
        Searching,
        Chasing,
        Attacking,
        Fleeing,
        CounterAttacking,
        Dead
    }

    /// <summary>
    /// 攻撃された時の反応タイプ
    /// </summary>
    public enum AttackReactionType
    {
        None,           // 無視
        Flee,           // 逃亡
        CounterAttack   // 反撃
    }

    /// <summary>
    /// キャラクターのパラメータ
    /// </summary>
    [Serializable]
    public struct CharacterStats
    {
        public float maxHealth;
        public float currentHealth;
        public float attackPower;
        public float defense;
        public float moveSpeed;
        public float attackRange;
        public float detectionRange;
        public float attackCooldown;

        public static CharacterStats CreateRandom(CharacterType type)
        {
            var stats = GetBaseStats(type);

            // ランダム変動（±20%）
            float variation = 0.2f;
            stats.maxHealth *= UnityEngine.Random.Range(1f - variation, 1f + variation);
            stats.attackPower *= UnityEngine.Random.Range(1f - variation, 1f + variation);
            stats.defense *= UnityEngine.Random.Range(1f - variation, 1f + variation);
            stats.moveSpeed *= UnityEngine.Random.Range(1f - variation, 1f + variation);

            stats.currentHealth = stats.maxHealth;
            return stats;
        }

        private static CharacterStats GetBaseStats(CharacterType type)
        {
            return type switch
            {
                CharacterType.Warrior => new CharacterStats
                {
                    maxHealth = 150f,
                    attackPower = 20f,
                    defense = 10f,
                    moveSpeed = 4f,
                    attackRange = 2f,
                    detectionRange = 15f,
                    attackCooldown = 1.2f
                },
                CharacterType.Assassin => new CharacterStats
                {
                    maxHealth = 80f,
                    attackPower = 35f,
                    defense = 3f,
                    moveSpeed = 7f,
                    attackRange = 1.5f,
                    detectionRange = 20f,
                    attackCooldown = 0.8f
                },
                CharacterType.Tank => new CharacterStats
                {
                    maxHealth = 250f,
                    attackPower = 12f,
                    defense = 20f,
                    moveSpeed = 2.5f,
                    attackRange = 2f,
                    detectionRange = 12f,
                    attackCooldown = 2f
                },
                CharacterType.Mage => new CharacterStats
                {
                    maxHealth = 70f,
                    attackPower = 40f,
                    defense = 2f,
                    moveSpeed = 3.5f,
                    attackRange = 8f,
                    detectionRange = 25f,
                    attackCooldown = 1.5f
                },
                CharacterType.Ranger => new CharacterStats
                {
                    maxHealth = 100f,
                    attackPower = 25f,
                    defense = 5f,
                    moveSpeed = 5f,
                    attackRange = 12f,
                    detectionRange = 30f,
                    attackCooldown = 1f
                },
                CharacterType.Berserker => new CharacterStats
                {
                    maxHealth = 120f,
                    attackPower = 30f,
                    defense = 5f,
                    moveSpeed = 6f,
                    attackRange = 2.5f,
                    detectionRange = 18f,
                    attackCooldown = 0.6f
                },
                _ => new CharacterStats
                {
                    maxHealth = 100f,
                    attackPower = 15f,
                    defense = 5f,
                    moveSpeed = 4f,
                    attackRange = 2f,
                    detectionRange = 15f,
                    attackCooldown = 1f
                }
            };
        }
    }

    /// <summary>
    /// キャラクタークラス
    /// バトルロイヤルに参加する各キャラクターを表す
    /// </summary>
    public class Character : MonoBehaviour
    {
        // ===== 識別情報 =====
        [Header("Identity")]
        [SerializeField] private int _id;
        [SerializeField] private string _characterName;
        [SerializeField] private CharacterType _type;

        // ===== 性格 =====
        [Header("Personality")]
        [SerializeField, Range(0f, 1f)] private float _aggressiveness = 0.5f; // 攻撃性（高いほど反撃しやすい）

        // ===== パラメータ =====
        [Header("Stats")]
        [SerializeField] private CharacterStats _stats;

        // ===== 状態 =====
        [Header("State")]
        [SerializeField] private CharacterState _state = CharacterState.Idle;
        [SerializeField] private float _attackCooldownTimer;

        // ===== ターゲット =====
        [Header("Target")]
        [SerializeField] private Character _currentTarget;

        // ===== 攻撃反応 =====
        [Header("Attack Reaction")]
        [SerializeField] private Character _lastAttacker;
        [SerializeField] private AttackReactionType _pendingReaction = AttackReactionType.None;
        [SerializeField] private float _fleeSpeedMultiplier = 1.2f;
        [SerializeField] private float _reactionCooldown = 0f;
        private const float REACTION_COOLDOWN_TIME = 0.5f;

        // ===== バフ =====
        [Header("Buffs")]
        [SerializeField] private float _buffMultiplier = 1f;
        [SerializeField] private float _buffDuration;

        // ===== 攻撃エフェクト =====
        [Header("Attack Effect")]
        [SerializeField] private GameObject _attackEffectPrefab;
        private static GameObject _sharedAttackEffectPrefab;

        // ===== イベント =====
        public event Action<Character> OnDeath;
        public event Action<Character, float> OnDamaged;
        public event Action<Character, Character> OnKill;

        // ===== プロパティ =====
        public int Id => _id;
        public string CharacterName => _characterName;
        public CharacterType Type => _type;
        public CharacterStats Stats => _stats;
        public CharacterState State => _state;
        public Character CurrentTarget => _currentTarget;
        public Character LastAttacker => _lastAttacker;
        public AttackReactionType PendingReaction => _pendingReaction;
        public bool IsAlive => _state != CharacterState.Dead && _stats.currentHealth > 0;
        public float HealthPercent => _stats.currentHealth / _stats.maxHealth;
        public float EffectiveAttackPower => _stats.attackPower * _buffMultiplier;
        public float EffectiveMoveSpeed => _stats.moveSpeed * _buffMultiplier;
        public float FleeSpeed => _stats.moveSpeed * _fleeSpeedMultiplier; // 逃亡時は1.2倍速
        public bool HasPendingReaction => _pendingReaction != AttackReactionType.None && _lastAttacker != null && _lastAttacker.IsAlive;
        public float Aggressiveness => _aggressiveness;

        // ===== 名前生成用 =====
        private static readonly string[] FirstNames = {
            "アルファ", "ベータ", "ガンマ", "デルタ", "イプシロン",
            "ゼータ", "イータ", "シータ", "イオタ", "カッパ",
            "ラムダ", "ミュー", "ニュー", "クシー", "オミクロン",
            "パイ", "ロー", "シグマ", "タウ", "ウプシロン"
        };

        private static readonly string[] LastNames = {
            "レッド", "ブルー", "グリーン", "イエロー", "パープル",
            "オレンジ", "ピンク", "ブラック", "ホワイト", "シルバー",
            "ゴールド", "クリムゾン", "アズール", "エメラルド", "アンバー"
        };

        // ===== 初期化 =====

        /// <summary>
        /// キャラクターを初期化する
        /// </summary>
        public void Initialize(int id)
        {
            _id = id;
            _type = (CharacterType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(CharacterType)).Length);
            _characterName = GenerateRandomName();
            _stats = CharacterStats.CreateRandom(_type);
            _aggressiveness = GetBaseAggressiveness(_type) + UnityEngine.Random.Range(-0.2f, 0.2f);
            _aggressiveness = Mathf.Clamp01(_aggressiveness);
            _state = CharacterState.Idle;
            _attackCooldownTimer = 0f;
            _currentTarget = null;
            _lastAttacker = null;
            _pendingReaction = AttackReactionType.None;
            _buffMultiplier = 1f;
            _buffDuration = 0f;

            gameObject.name = $"Character_{_id}_{_characterName}";
        }

        /// <summary>
        /// 指定タイプでキャラクターを初期化
        /// </summary>
        public void Initialize(int id, CharacterType type)
        {
            _id = id;
            _type = type;
            _characterName = GenerateRandomName();
            _stats = CharacterStats.CreateRandom(_type);
            _aggressiveness = GetBaseAggressiveness(_type) + UnityEngine.Random.Range(-0.2f, 0.2f);
            _aggressiveness = Mathf.Clamp01(_aggressiveness);
            _state = CharacterState.Idle;
            _attackCooldownTimer = 0f;
            _currentTarget = null;
            _lastAttacker = null;
            _pendingReaction = AttackReactionType.None;
            _buffMultiplier = 1f;
            _buffDuration = 0f;

            gameObject.name = $"Character_{_id}_{_characterName}";
        }

        /// <summary>
        /// タイプ別の基本攻撃性
        /// </summary>
        private float GetBaseAggressiveness(CharacterType type)
        {
            return type switch
            {
                CharacterType.Warrior => 0.7f,      // 戦士は反撃しやすい
                CharacterType.Assassin => 0.5f,     // 暗殺者は状況次第
                CharacterType.Tank => 0.8f,         // タンクは反撃しやすい
                CharacterType.Mage => 0.3f,         // 魔法使いは逃げやすい
                CharacterType.Ranger => 0.4f,       // レンジャーは逃げやすい
                CharacterType.Berserker => 0.95f,   // バーサーカーはほぼ反撃
                _ => 0.5f
            };
        }

        private string GenerateRandomName()
        {
            string firstName = FirstNames[UnityEngine.Random.Range(0, FirstNames.Length)];
            string lastName = LastNames[UnityEngine.Random.Range(0, LastNames.Length)];
            return $"{firstName}・{lastName}";
        }

        // ===== 更新 =====

        // デバッグ用ステータス文字列（毎フレーム更新）
        private string _debugStatus;
        public string DebugStatus => _debugStatus;

        private void Update()
        {
            if (!IsAlive) return;

            // クールダウン更新
            if (_attackCooldownTimer > 0)
            {
                _attackCooldownTimer -= Time.deltaTime;
            }

            // 反応クールダウン更新
            if (_reactionCooldown > 0)
            {
                _reactionCooldown -= Time.deltaTime;
            }

            // バフ更新
            if (_buffDuration > 0)
            {
                _buffDuration -= Time.deltaTime;
                if (_buffDuration <= 0)
                {
                    _buffMultiplier = 1f;
                }
            }

            // TODO: パフォーマンス課題 - 毎フレーム文字列結合によるGC Alloc
            // 最適化: StringBuilderを使用して再利用する
            UpdateDebugStatus();
        }

        /// <summary>
        /// デバッグ用ステータス文字列を更新（毎フレーム）
        /// ボトルネック: 文字列結合による GC Alloc
        /// </summary>
        private void UpdateDebugStatus()
        {
            // ボトルネック: + 演算子による文字列結合（毎回新しい文字列を生成）
            _debugStatus = "[" + _id + "] " + _characterName + " | ";
            _debugStatus = _debugStatus + "HP: " + _stats.currentHealth.ToString("F1") + "/" + _stats.maxHealth.ToString("F1") + " | ";
            _debugStatus = _debugStatus + "State: " + _state.ToString() + " | ";
            _debugStatus = _debugStatus + "Pos: (" + transform.position.x.ToString("F2") + ", " + transform.position.z.ToString("F2") + ")";

            if (_currentTarget != null)
            {
                _debugStatus = _debugStatus + " | Target: " + _currentTarget.CharacterName;
            }
        }

        // ===== 状態操作 =====

        public void SetState(CharacterState newState)
        {
            if (_state == CharacterState.Dead) return;
            _state = newState;
        }

        public void SetTarget(Character target)
        {
            _currentTarget = target;
        }

        // ===== 戦闘 =====

        /// <summary>
        /// 攻撃可能かどうか
        /// </summary>
        public bool CanAttack()
        {
            return IsAlive && _attackCooldownTimer <= 0;
        }

        /// <summary>
        /// ターゲットを攻撃
        /// </summary>
        public void Attack(Character target)
        {
            if (!CanAttack() || target == null || !target.IsAlive) return;

            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance > _stats.attackRange) return;

            // 攻撃エフェクトを生成（Object Pool最適化対象）
            SpawnAttackEffect(target);

            // ダメージ計算
            float damage = CalculateDamage(target);
            target.TakeDamage(damage, this);

            // クールダウン開始
            _attackCooldownTimer = _stats.attackCooldown;
        }

        /// <summary>
        /// 攻撃エフェクトを生成（非最適化版 - Instantiate/Destroy）
        /// </summary>
        private void SpawnAttackEffect(Character target)
        {
            GameObject prefab = _attackEffectPrefab ?? _sharedAttackEffectPrefab;
            if (prefab == null) return;

            // 攻撃者からターゲットへの中間位置にエフェクトを生成
            Vector3 spawnPos = (transform.position + target.transform.position) / 2f;
            spawnPos.y = 1f; // 地面より少し上

            // エフェクトをInstantiate（ボトルネック：毎回新規生成）
            GameObject effect = Instantiate(prefab, spawnPos, Quaternion.identity);

            // 一定時間後に破棄
            Destroy(effect, 0.5f);
        }

        /// <summary>
        /// 共有攻撃エフェクトプレハブを設定（Spawnerから呼び出し）
        /// </summary>
        public static void SetSharedAttackEffectPrefab(GameObject prefab)
        {
            _sharedAttackEffectPrefab = prefab;
        }

        private float CalculateDamage(Character target)
        {
            float baseDamage = EffectiveAttackPower;
            float defense = target.Stats.defense;

            // ダメージ計算: 攻撃力 - 防御力（最低1ダメージ）
            float damage = Mathf.Max(1f, baseDamage - defense * 0.5f);

            // クリティカル（10%確率で2倍）
            if (UnityEngine.Random.value < 0.1f)
            {
                damage *= 2f;
            }

            return damage;
        }

        /// <summary>
        /// ダメージを受ける
        /// </summary>
        public void TakeDamage(float damage, Character attacker)
        {
            if (!IsAlive) return;

            _stats.currentHealth -= damage;
            OnDamaged?.Invoke(this, damage);

            // 攻撃者が現在のターゲットと違う場合、反応を決定
            if (attacker != null && attacker != _currentTarget && _reactionCooldown <= 0)
            {
                _lastAttacker = attacker;
                DecideReaction();
                _reactionCooldown = REACTION_COOLDOWN_TIME;
            }

            if (_stats.currentHealth <= 0)
            {
                Die(attacker);
            }
        }

        /// <summary>
        /// 攻撃された時の反応を決定（性格に基づく）
        /// </summary>
        private void DecideReaction()
        {
            // HPが低い場合は逃げやすくなる
            float fleeBonus = (1f - HealthPercent) * 0.3f;
            float effectiveAggression = _aggressiveness - fleeBonus;

            if (UnityEngine.Random.value < effectiveAggression)
            {
                _pendingReaction = AttackReactionType.CounterAttack;
            }
            else
            {
                _pendingReaction = AttackReactionType.Flee;
            }
        }

        /// <summary>
        /// 反応をクリア
        /// </summary>
        public void ClearReaction()
        {
            _pendingReaction = AttackReactionType.None;
            _lastAttacker = null;
        }

        /// <summary>
        /// 反撃：攻撃者をターゲットに切り替え
        /// </summary>
        public void CounterAttack()
        {
            if (_lastAttacker != null && _lastAttacker.IsAlive)
            {
                _currentTarget = _lastAttacker;
                _state = CharacterState.CounterAttacking;
            }
            ClearReaction();
        }

        private void Die(Character killer)
        {
            _stats.currentHealth = 0;
            _state = CharacterState.Dead;
            _currentTarget = null;

            OnDeath?.Invoke(this);

            if (killer != null)
            {
                killer.OnKill?.Invoke(killer, this);
            }

            // キャラクターを破棄
            Destroy(gameObject);
        }

        // ===== 移動 =====

        /// <summary>
        /// 指定方向に移動
        /// </summary>
        public void Move(Vector3 direction)
        {
            if (!IsAlive || direction == Vector3.zero) return;

            Vector3 movement = direction.normalized * EffectiveMoveSpeed * Time.deltaTime;
            transform.position += movement;

            // フィールド範囲内にクランプ
            Vector3 pos = transform.position;
            float halfSize = GameConstants.FIELD_HALF_SIZE;
            pos.x = Mathf.Clamp(pos.x, -halfSize, halfSize);
            pos.z = Mathf.Clamp(pos.z, -halfSize, halfSize);
            transform.position = pos;

            // 移動方向を向く
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        /// <summary>
        /// ターゲットに向かって移動
        /// </summary>
        public void MoveTowards(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0;
            Move(direction);
        }

        /// <summary>
        /// ターゲットから逃げる
        /// </summary>
        public void MoveAwayFrom(Vector3 targetPosition)
        {
            Vector3 direction = transform.position - targetPosition;
            direction.y = 0;
            Move(direction);
        }

        /// <summary>
        /// ターゲットから高速で逃げる（1.2倍速）
        /// </summary>
        public void FleeFrom(Vector3 targetPosition)
        {
            if (!IsAlive) return;

            Vector3 direction = transform.position - targetPosition;
            direction.y = 0;
            direction.Normalize();

            Vector3 movement = direction * FleeSpeed * Time.deltaTime;
            transform.position += movement;

            // フィールド範囲内にクランプ
            Vector3 pos = transform.position;
            float halfSize = GameConstants.FIELD_HALF_SIZE;
            pos.x = Mathf.Clamp(pos.x, -halfSize, halfSize);
            pos.z = Mathf.Clamp(pos.z, -halfSize, halfSize);
            transform.position = pos;

            // 逃げる方向を向く
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        // ===== バフ =====

        /// <summary>
        /// バフを適用
        /// </summary>
        public void ApplyBuff(float multiplier, float duration)
        {
            _buffMultiplier = multiplier;
            _buffDuration = duration;
        }

        // ===== リセット =====

        /// <summary>
        /// キャラクターをリセット（プール用）
        /// </summary>
        public void Reset()
        {
            _state = CharacterState.Idle;
            _currentTarget = null;
            _attackCooldownTimer = 0f;
            _buffMultiplier = 1f;
            _buffDuration = 0f;

            // HPを最大に
            _stats.currentHealth = _stats.maxHealth;
        }
    }
}
