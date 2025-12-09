using System.Collections.Generic;
using UnityEngine;
using PerformanceTraining.Core;
using EnemyClass = PerformanceTraining.Enemy.Enemy;
using EnemySystem = PerformanceTraining.Enemy.EnemySystem;

#pragma warning disable 0414 // 課題用フィールド: 学生が実装時に使用

namespace PerformanceTraining.Exercises.Tradeoff
{
    /// <summary>
    /// 【課題3-A: 近傍キャッシュ】
    ///
    /// 目標: メモリを消費してCPU計算を削減する
    ///
    /// トレードオフ:
    /// - メモリ使用量: +約170KB（1000体時）
    /// - CPU削減効果: 5-7倍高速化
    /// - 代償: キャッシュ期間分の位置ズレ
    ///
    /// 確認方法:
    /// - Cache Hit Rate > 80% を目指す
    /// </summary>
    public class NeighborCache_Exercise : MonoBehaviour
    {
        // ========================================================
        // キャッシュデータ構造
        // ========================================================

        private struct CacheEntry
        {
            public List<EnemyClass> Neighbors;
            public int LastUpdateFrame;
        }

        // TODO: キャッシュ用のデータ構造を宣言


        // ========================================================
        // 設定
        // ========================================================

        [Header("キャッシュ設定")]
        [SerializeField] private int _cacheLifetimeFrames = 10;
        [SerializeField] private float _neighborRadius = 5f;

        [Header("デバッグ")]
        [SerializeField] private int _cacheHitCount;
        [SerializeField] private int _cacheMissCount;

        private int _currentFrame;

        // TODO: 計算用の再利用リストを宣言（GC対策）


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
        /// 指定した敵の近傍リストを取得
        /// キャッシュが有効なら再利用、無効なら再計算
        /// </summary>
        public List<EnemyClass> GetNeighbors(EnemyClass enemy)
        {
            _currentFrame = Time.frameCount;

            // 現在の実装（問題あり）: 毎回計算
            // TODO: キャッシュを確認し、有効なら再利用する
            _cacheMissCount++;
            return CalculateNeighbors(enemy);
        }


        // ========================================================
        // キャッシュ更新
        // ========================================================

        private List<EnemyClass> UpdateCache(EnemyClass enemy)
        {
            // TODO: 計算結果をキャッシュに保存して返す
            return CalculateNeighbors(enemy);
        }


        // ========================================================
        // 近傍計算
        // ========================================================

        private List<EnemyClass> CalculateNeighbors(EnemyClass enemy)
        {
            // 現在の実装（問題あり）: 毎回新しいListを生成
            var result = new List<EnemyClass>();

            var enemySystem = FindAnyObjectByType<EnemySystem>();
            if (enemySystem == null) return result;

            Vector3 myPos = enemy.transform.position;
            float radiusSqr = _neighborRadius * _neighborRadius;

            foreach (var other in enemySystem.ActiveEnemies)
            {
                if (other == null || other == enemy || !other.IsAlive)
                    continue;

                float distSqr = (other.transform.position - myPos).sqrMagnitude;
                if (distSqr <= radiusSqr)
                {
                    result.Add(other);
                }
            }

            return result;
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
            // 例: エントリ数 × 近傍リストの平均サイズ × 参照サイズ
            return 0;
        }

        public void LogStats()
        {
            Debug.Log($"NeighborCache - Hit Rate: {GetHitRate() * 100:F1}%, Size: {GetCacheSize()}");
        }
    }
}
