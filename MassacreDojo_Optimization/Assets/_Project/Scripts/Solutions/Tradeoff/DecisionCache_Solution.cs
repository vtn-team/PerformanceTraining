using System.Collections.Generic;
using UnityEngine;
using MassacreDojo.Core;
using MassacreDojo.Enemy;

namespace MassacreDojo.Solutions.Tradeoff
{
    /// <summary>
    /// 【解答】課題3-B: AI判断キャッシュ
    ///
    /// このファイルは教員用の解答です。
    /// 学生には見せないでください。
    /// </summary>
    public class DecisionCache_Solution : MonoBehaviour
    {
        // ========================================================
        // キャッシュデータ構造【解答】
        // ========================================================

        private struct DecisionEntry
        {
            public EnemyState CachedState;
            public Vector3 CachedTargetPos;
            public Vector3 CachedMoveDirection;
            public int DecisionFrame;
        }

        // 【解答】キャッシュ用のDictionary
        private Dictionary<Enemy, DecisionEntry> _decisions;

        [Header("キャッシュ設定")]
        [SerializeField] private int _decisionLifetimeFrames = 5;

        [Header("AI設定")]
        [SerializeField] private float _attackRange = GameConstants.ENEMY_ATTACK_RANGE;
        [SerializeField] private float _detectionRange = GameConstants.ENEMY_DETECTION_RANGE;

        [Header("デバッグ")]
        [SerializeField] private int _cacheHitCount;
        [SerializeField] private int _cacheMissCount;
        [SerializeField] private int _totalDecisions;

        private int _currentFrame;


        // ========================================================
        // 初期化【解答】
        // ========================================================

        private void Awake()
        {
            // 【解答】キャッシュを初期化
            _decisions = new Dictionary<Enemy, DecisionEntry>();
        }


        // ========================================================
        // メインメソッド【解答】
        // ========================================================

        public EnemyState GetDecision(Enemy enemy, Vector3 playerPos,
            out Vector3 targetPos, out Vector3 moveDirection)
        {
            _currentFrame = Time.frameCount;
            _totalDecisions++;

            // 【解答】キャッシュを使ったAI判断
            if (_decisions.TryGetValue(enemy, out DecisionEntry entry))
            {
                // キャッシュが有効期間内かチェック
                if ((_currentFrame - entry.DecisionFrame) < _decisionLifetimeFrames)
                {
                    // キャッシュヒット
                    _cacheHitCount++;
                    targetPos = entry.CachedTargetPos;
                    moveDirection = entry.CachedMoveDirection;
                    return entry.CachedState;
                }
            }

            // キャッシュミス - 再計算
            _cacheMissCount++;
            EnemyState state = MakeDecision(enemy, playerPos, out targetPos, out moveDirection);
            CacheDecision(enemy, state, targetPos, moveDirection);
            return state;
        }


        // ========================================================
        // AI判断ロジック【解答】
        // ========================================================

        private EnemyState MakeDecision(Enemy enemy, Vector3 playerPos,
            out Vector3 targetPos, out Vector3 moveDirection)
        {
            Vector3 enemyPos = enemy.transform.position;
            Vector3 toPlayer = playerPos - enemyPos;
            float distSqr = toPlayer.sqrMagnitude;

            if (distSqr < _attackRange * _attackRange)
            {
                targetPos = playerPos;
                moveDirection = Vector3.zero;
                return EnemyState.Attack;
            }

            if (distSqr < _detectionRange * _detectionRange)
            {
                targetPos = playerPos;
                moveDirection = toPlayer.normalized;
                return EnemyState.Chase;
            }

            targetPos = enemyPos;
            moveDirection = Vector3.zero;
            return EnemyState.Idle;
        }


        // ========================================================
        // キャッシュ保存【解答】
        // ========================================================

        private void CacheDecision(Enemy enemy, EnemyState state,
            Vector3 targetPos, Vector3 moveDirection)
        {
            // 【解答】判断結果をキャッシュに保存
            var entry = new DecisionEntry
            {
                CachedState = state,
                CachedTargetPos = targetPos,
                CachedMoveDirection = moveDirection,
                DecisionFrame = _currentFrame
            };
            _decisions[enemy] = entry;
        }


        // ========================================================
        // キャッシュ管理【解答】
        // ========================================================

        public void InvalidateCache(Enemy enemy)
        {
            // 【解答】指定した敵のキャッシュを削除
            _decisions.Remove(enemy);
        }

        public void ClearAllCache()
        {
            // 【解答】全キャッシュをクリア
            _decisions.Clear();
            _cacheHitCount = 0;
            _cacheMissCount = 0;
            _totalDecisions = 0;
        }

        public void CleanupDeadEntries()
        {
            // 【解答】死亡した敵のキャッシュを削除
            var deadEnemies = new List<Enemy>();
            foreach (var kvp in _decisions)
            {
                if (kvp.Key == null || !kvp.Key.IsAlive)
                    deadEnemies.Add(kvp.Key);
            }
            foreach (var dead in deadEnemies)
                _decisions.Remove(dead);
        }


        // ========================================================
        // デバッグ【解答】
        // ========================================================

        public float GetHitRate()
        {
            int total = _cacheHitCount + _cacheMissCount;
            if (total == 0) return 0f;
            return (float)_cacheHitCount / total;
        }

        public int GetCacheSize()
        {
            // 【解答】キャッシュのエントリ数を返す
            return _decisions?.Count ?? 0;
        }

        public int GetEstimatedMemoryUsage()
        {
            return GetCacheSize() * 56;
        }

        public void LogStats()
        {
            Debug.Log($"DecisionCache Stats:");
            Debug.Log($"  Cache Size: {GetCacheSize()} entries");
            Debug.Log($"  Hit Rate: {GetHitRate() * 100:F1}%");
            Debug.Log($"  Hits: {_cacheHitCount}, Misses: {_cacheMissCount}");
            Debug.Log($"  Total Decisions: {_totalDecisions}");
            Debug.Log($"  Estimated Memory: {GetEstimatedMemoryUsage() / 1024f:F2} KB");
        }

        public void SetLifetime(int frames)
        {
            _decisionLifetimeFrames = Mathf.Max(1, frames);
        }
    }
}
