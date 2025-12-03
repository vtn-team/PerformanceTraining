using System.Collections.Generic;
using UnityEngine;
using MassacreDojo.Core;
using MassacreDojo.Enemy;

namespace MassacreDojo.Exercises.Tradeoff
{
    /// <summary>
    /// 【課題3-B: AI判断キャッシュ（Decision Cache）】
    ///
    /// 目標: メモリを消費してCPU計算を削減する
    ///
    /// 概要:
    /// 敵のAI判断結果（追跡/攻撃/待機）を数フレーム保持し、
    /// 毎フレームのAI判断処理を削減します。
    ///
    /// トレードオフ:
    /// - メモリ: 敵1体あたり約32バイト（1000体で約32KB）
    /// - CPU: AI判断を1/5に削減（5フレームキャッシュ時）
    /// - 応答性: 最大5フレーム分の反応遅延
    ///
    /// 使用場面:
    /// - 敵の状態遷移判定
    /// - プレイヤー検知判定
    /// - 攻撃タイミング判定
    ///
    /// TODO: キャッシュの実装を完成させてください
    /// </summary>
    public class DecisionCache_Exercise : MonoBehaviour
    {
        // ========================================================
        // キャッシュデータ構造
        // ========================================================

        /// <summary>
        /// AI判断のキャッシュエントリ
        /// </summary>
        private struct DecisionEntry
        {
            public EnemyState CachedState;      // キャッシュされた状態
            public Vector3 CachedTargetPos;     // キャッシュされた目標位置
            public Vector3 CachedMoveDirection; // キャッシュされた移動方向
            public int DecisionFrame;           // 判断したフレーム
        }

        // TODO: ここにキャッシュ用のDictionaryを宣言してください
        // private Dictionary<Enemy, DecisionEntry> _decisions;


        // ========================================================
        // 設定
        // ========================================================

        [Header("キャッシュ設定")]
        [Tooltip("AI判断の有効期間（フレーム数）")]
        [SerializeField] private int _decisionLifetimeFrames = 5;

        [Header("AI設定")]
        [SerializeField] private float _attackRange = GameConstants.ENEMY_ATTACK_RANGE;
        [SerializeField] private float _detectionRange = GameConstants.ENEMY_DETECTION_RANGE;

        [Header("デバッグ")]
        [SerializeField] private int _cacheHitCount;
        [SerializeField] private int _cacheMissCount;
        [SerializeField] private int _totalDecisions;

        // 内部変数
        private int _currentFrame;


        // ========================================================
        // 初期化
        // ========================================================

        private void Awake()
        {
            // TODO: キャッシュを初期化してください
            // ヒント: _decisions = new Dictionary<Enemy, DecisionEntry>();
        }


        // ========================================================
        // メインメソッド: AI判断を取得
        // ========================================================

        /// <summary>
        /// 敵のAI判断を取得する
        /// キャッシュが有効なら再利用、無効なら再計算
        /// </summary>
        /// <param name="enemy">対象の敵</param>
        /// <param name="playerPos">プレイヤーの位置</param>
        /// <param name="targetPos">出力: 目標位置</param>
        /// <param name="moveDirection">出力: 移動方向</param>
        /// <returns>判断された状態</returns>
        public EnemyState GetDecision(Enemy enemy, Vector3 playerPos,
            out Vector3 targetPos, out Vector3 moveDirection)
        {
            _currentFrame = Time.frameCount;
            _totalDecisions++;

            // TODO: キャッシュを使ったAI判断を実装してください
            // ヒント:
            // 1. _decisions.TryGetValue() でキャッシュを確認
            // 2. キャッシュがあり、有効期間内なら:
            //    - targetPos = entry.CachedTargetPos
            //    - moveDirection = entry.CachedMoveDirection
            //    - _cacheHitCount++
            //    - return entry.CachedState
            // 3. キャッシュがないか期限切れなら:
            //    - MakeDecision() で新規判断
            //    - CacheDecision() でキャッシュに保存
            //    - _cacheMissCount++

            // 仮実装（毎回計算 - 問題あり）- これを置き換えてください
            _cacheMissCount++;
            return MakeDecision(enemy, playerPos, out targetPos, out moveDirection);
        }


        // ========================================================
        // AI判断ロジック（実際の重い処理）
        // ========================================================

        /// <summary>
        /// AI判断を実際に行う（重い処理）
        /// </summary>
        private EnemyState MakeDecision(Enemy enemy, Vector3 playerPos,
            out Vector3 targetPos, out Vector3 moveDirection)
        {
            Vector3 enemyPos = enemy.transform.position;
            Vector3 toPlayer = playerPos - enemyPos;
            float distSqr = toPlayer.sqrMagnitude;

            // 攻撃範囲内
            if (distSqr < _attackRange * _attackRange)
            {
                targetPos = playerPos;
                moveDirection = Vector3.zero; // 攻撃中は移動しない
                return EnemyState.Attack;
            }

            // 検知範囲内
            if (distSqr < _detectionRange * _detectionRange)
            {
                targetPos = playerPos;
                moveDirection = toPlayer.normalized;
                return EnemyState.Chase;
            }

            // 範囲外（待機）
            targetPos = enemyPos;
            moveDirection = Vector3.zero;
            return EnemyState.Idle;
        }


        // ========================================================
        // キャッシュ保存
        // ========================================================

        /// <summary>
        /// AI判断をキャッシュに保存する
        /// </summary>
        private void CacheDecision(Enemy enemy, EnemyState state,
            Vector3 targetPos, Vector3 moveDirection)
        {
            // TODO: 判断結果をキャッシュに保存してください
            // ヒント:
            // var entry = new DecisionEntry
            // {
            //     CachedState = state,
            //     CachedTargetPos = targetPos,
            //     CachedMoveDirection = moveDirection,
            //     DecisionFrame = _currentFrame
            // };
            // _decisions[enemy] = entry;
        }


        // ========================================================
        // キャッシュ管理
        // ========================================================

        /// <summary>
        /// 特定の敵のキャッシュをクリアする
        /// （敵がダメージを受けた時など、即座に再判断が必要な場合）
        /// </summary>
        public void InvalidateCache(Enemy enemy)
        {
            // TODO: 指定した敵のキャッシュを削除してください
            // ヒント: _decisions.Remove(enemy);
        }

        /// <summary>
        /// 全キャッシュをクリアする
        /// </summary>
        public void ClearAllCache()
        {
            // TODO: 全キャッシュをクリアしてください

            _cacheHitCount = 0;
            _cacheMissCount = 0;
            _totalDecisions = 0;
        }

        /// <summary>
        /// 死亡した敵のキャッシュを削除する（定期的に呼ぶ）
        /// </summary>
        public void CleanupDeadEntries()
        {
            // TODO: IsAlive == false の敵をキャッシュから削除してください
            // ヒント:
            // var deadEnemies = new List<Enemy>();
            // foreach (var kvp in _decisions)
            // {
            //     if (kvp.Key == null || !kvp.Key.IsAlive)
            //         deadEnemies.Add(kvp.Key);
            // }
            // foreach (var dead in deadEnemies)
            //     _decisions.Remove(dead);
        }


        // ========================================================
        // デバッグ・計測用
        // ========================================================

        /// <summary>
        /// キャッシュヒット率を取得
        /// </summary>
        public float GetHitRate()
        {
            int total = _cacheHitCount + _cacheMissCount;
            if (total == 0) return 0f;
            return (float)_cacheHitCount / total;
        }

        /// <summary>
        /// 現在のキャッシュエントリ数を取得
        /// </summary>
        public int GetCacheSize()
        {
            // TODO: キャッシュのエントリ数を返してください
            return 0;
        }

        /// <summary>
        /// メモリ使用量の推定値を取得（バイト）
        /// </summary>
        public int GetEstimatedMemoryUsage()
        {
            // 1エントリあたりの推定サイズ:
            // - Dictionary オーバーヘッド: 約24バイト
            // - CachedState (enum): 4バイト
            // - CachedTargetPos (Vector3): 12バイト
            // - CachedMoveDirection (Vector3): 12バイト
            // - DecisionFrame (int): 4バイト
            // 合計: 約56バイト/エントリ

            int entryCount = GetCacheSize();
            return entryCount * 56;
        }

        /// <summary>
        /// 統計情報をログ出力
        /// </summary>
        public void LogStats()
        {
            Debug.Log($"DecisionCache Stats:");
            Debug.Log($"  Cache Size: {GetCacheSize()} entries");
            Debug.Log($"  Hit Rate: {GetHitRate() * 100:F1}%");
            Debug.Log($"  Hits: {_cacheHitCount}, Misses: {_cacheMissCount}");
            Debug.Log($"  Total Decisions: {_totalDecisions}");
            Debug.Log($"  Estimated Memory: {GetEstimatedMemoryUsage() / 1024f:F2} KB");
            Debug.Log($"  Lifetime: {_decisionLifetimeFrames} frames");
        }

        /// <summary>
        /// キャッシュ有効期間を変更する（実験用）
        /// </summary>
        public void SetLifetime(int frames)
        {
            _decisionLifetimeFrames = Mathf.Max(1, frames);
            Debug.Log($"Decision cache lifetime set to {_decisionLifetimeFrames} frames");
        }
    }
}
