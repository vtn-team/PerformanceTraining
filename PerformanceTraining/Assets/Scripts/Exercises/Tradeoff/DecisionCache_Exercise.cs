using System.Collections.Generic;
using UnityEngine;
using PerformanceTraining.Core;
using EnemyClass = PerformanceTraining.Enemy.Enemy;
using EnemyState = PerformanceTraining.Enemy.EnemyState;

#pragma warning disable 0414 // 課題用フィールド: 学生が実装時に使用

namespace PerformanceTraining.Exercises.Tradeoff
{
    /// <summary>
    /// 【課題3-B: AI判断キャッシュ】
    ///
    /// 目標: メモリを消費してAI判断のCPU計算を削減する
    ///
    /// トレードオフ:
    /// - メモリ使用量: +約32KB（1000体時）
    /// - CPU削減効果: AI判断を1/5に削減
    /// - 代償: 最大5フレーム分の反応遅延
    ///
    /// 確認方法:
    /// - Cache Hit Rate > 80% を目指す
    /// </summary>
    public class DecisionCache_Exercise : MonoBehaviour
    {
        // ========================================================
        // キャッシュデータ構造
        // ========================================================

        private struct DecisionEntry
        {
            public EnemyState CachedState;
            public Vector3 CachedTargetPos;
            public Vector3 CachedMoveDirection;
            public int DecisionFrame;
        }

        // TODO: キャッシュ用のデータ構造を宣言


        // ========================================================
        // 設定
        // ========================================================

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
        // 初期化
        // ========================================================

        private void Awake()
        {
            // TODO: キャッシュを初期化
        }


        // ========================================================
        // メインメソッド
        // ========================================================

        /// <summary>
        /// 敵のAI判断を取得
        /// キャッシュが有効なら再利用、無効なら再計算
        /// </summary>
        public EnemyState GetDecision(EnemyClass enemy, Vector3 playerPos,
            out Vector3 targetPos, out Vector3 moveDirection)
        {
            _currentFrame = Time.frameCount;
            _totalDecisions++;

            // 現在の実装（問題あり）: 毎回計算
            // TODO: キャッシュを確認し、有効なら再利用する
            _cacheMissCount++;
            return MakeDecision(enemy, playerPos, out targetPos, out moveDirection);
        }


        // ========================================================
        // AI判断ロジック
        // ========================================================

        private EnemyState MakeDecision(EnemyClass enemy, Vector3 playerPos,
            out Vector3 targetPos, out Vector3 moveDirection)
        {
            Vector3 enemyPos = enemy.transform.position;
            Vector3 toPlayer = playerPos - enemyPos;
            float distSqr = toPlayer.sqrMagnitude;

            // 攻撃範囲内
            if (distSqr < _attackRange * _attackRange)
            {
                targetPos = playerPos;
                moveDirection = Vector3.zero;
                return EnemyState.Attack;
            }

            // 検知範囲内
            if (distSqr < _detectionRange * _detectionRange)
            {
                targetPos = playerPos;
                moveDirection = toPlayer.normalized;
                return EnemyState.Chase;
            }

            // 範囲外
            targetPos = enemyPos;
            moveDirection = Vector3.zero;
            return EnemyState.Idle;
        }


        // ========================================================
        // キャッシュ保存
        // ========================================================

        private void CacheDecision(EnemyClass enemy, EnemyState state,
            Vector3 targetPos, Vector3 moveDirection)
        {
            // TODO: 判断結果をキャッシュに保存
        }


        // ========================================================
        // キャッシュ管理
        // ========================================================

        public void InvalidateCache(EnemyClass enemy)
        {
            // TODO: 指定した敵のキャッシュを削除
        }

        public void ClearAllCache()
        {
            // TODO: 全キャッシュをクリア
            _cacheHitCount = 0;
            _cacheMissCount = 0;
            _totalDecisions = 0;
        }

        public void CleanupDeadEntries()
        {
            // TODO: 死亡した敵のキャッシュを削除
        }


        // ========================================================
        // デバッグ
        // ========================================================

        public float GetHitRate()
        {
            int total = _cacheHitCount + _cacheMissCount;
            if (total == 0) return 0f;
            return (float)_cacheHitCount / total;
        }

        public int GetCacheSize()
        {
            // TODO: キャッシュのエントリ数を返す
            return 0;
        }

        public int GetEstimatedMemoryUsage()
        {
            // TODO: キャッシュの推定メモリ使用量を返す
            // 例: エントリ数 × DecisionEntryのサイズ（約32bytes）
            return 0;
        }

        public void LogStats()
        {
            Debug.Log($"DecisionCache - Hit Rate: {GetHitRate() * 100:F1}%, Size: {GetCacheSize()}");
        }
    }
}
