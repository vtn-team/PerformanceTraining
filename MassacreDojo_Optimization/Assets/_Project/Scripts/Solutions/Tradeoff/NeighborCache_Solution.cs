using System.Collections.Generic;
using UnityEngine;
using MassacreDojo.Core;
using EnemyClass = MassacreDojo.Enemy.Enemy;
using EnemySystem = MassacreDojo.Enemy.EnemySystem;

namespace MassacreDojo.Solutions.Tradeoff
{
    /// <summary>
    /// 【解答】課題3-A: 近傍キャッシュ
    ///
    /// このファイルは教員用の解答です。
    /// 学生には見せないでください。
    /// </summary>
    public class NeighborCache_Solution : MonoBehaviour
    {
        // ========================================================
        // キャッシュデータ構造【解答】
        // ========================================================

        private struct CacheEntry
        {
            public List<EnemyClass> Neighbors;
            public int LastUpdateFrame;
        }

        // 【解答】キャッシュ用のDictionary
        private Dictionary<EnemyClass, CacheEntry> _cache;

        [Header("キャッシュ設定")]
        [SerializeField] private int _cacheLifetimeFrames = 10;
        [SerializeField] private float _neighborRadius = 5f;

        [Header("デバッグ")]
        [SerializeField] private int _cacheHitCount;
        [SerializeField] private int _cacheMissCount;

        private int _currentFrame;

        // 【解答】計算用の再利用リスト
        private List<EnemyClass> _tempList;


        // ========================================================
        // 初期化【解答】
        // ========================================================

        private void Awake()
        {
            // 【解答】キャッシュと一時リストを初期化
            _cache = new Dictionary<EnemyClass, CacheEntry>();
            _tempList = new List<EnemyClass>(50);
        }


        // ========================================================
        // メインメソッド【解答】
        // ========================================================

        public List<EnemyClass> GetNeighbors(EnemyClass enemy)
        {
            _currentFrame = Time.frameCount;

            // 【解答】キャッシュを使った近傍取得
            if (_cache.TryGetValue(enemy, out CacheEntry entry))
            {
                // キャッシュが有効期間内かチェック
                if ((_currentFrame - entry.LastUpdateFrame) < _cacheLifetimeFrames)
                {
                    // キャッシュヒット
                    _cacheHitCount++;
                    return entry.Neighbors;
                }
            }

            // キャッシュミス - 再計算
            _cacheMissCount++;
            return UpdateCache(enemy);
        }


        // ========================================================
        // キャッシュ更新【解答】
        // ========================================================

        private List<EnemyClass> UpdateCache(EnemyClass enemy)
        {
            // 【解答】近傍を計算
            List<EnemyClass> neighbors = CalculateNeighbors(enemy);

            // 【解答】キャッシュに保存（リストのコピーを作成）
            var entry = new CacheEntry
            {
                Neighbors = new List<EnemyClass>(neighbors),
                LastUpdateFrame = _currentFrame
            };
            _cache[enemy] = entry;

            return entry.Neighbors;
        }


        // ========================================================
        // 近傍計算【解答】
        // ========================================================

        private List<EnemyClass> CalculateNeighbors(EnemyClass enemy)
        {
            // 【解答】再利用リストを使用
            if (_tempList == null)
                _tempList = new List<EnemyClass>(50);
            _tempList.Clear();

            var enemySystem = FindObjectOfType<EnemySystem>();
            if (enemySystem == null) return _tempList;

            Vector3 myPos = enemy.transform.position;
            float radiusSqr = _neighborRadius * _neighborRadius;

            foreach (var other in enemySystem.ActiveEnemies)
            {
                if (other == null || other == enemy || !other.IsAlive)
                    continue;

                float distSqr = (other.transform.position - myPos).sqrMagnitude;
                if (distSqr <= radiusSqr)
                {
                    _tempList.Add(other);
                }
            }

            return _tempList;
        }


        // ========================================================
        // キャッシュ管理【解答】
        // ========================================================

        public void InvalidateCache(EnemyClass enemy)
        {
            // 【解答】指定した敵のキャッシュを削除
            _cache.Remove(enemy);
        }

        public void ClearAllCache()
        {
            // 【解答】全キャッシュをクリア
            _cache.Clear();
            _cacheHitCount = 0;
            _cacheMissCount = 0;
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
            return _cache?.Count ?? 0;
        }

        public int GetEstimatedMemoryUsage()
        {
            return GetCacheSize() * 212;
        }

        public void LogStats()
        {
            Debug.Log($"NeighborCache Stats:");
            Debug.Log($"  Cache Size: {GetCacheSize()} entries");
            Debug.Log($"  Hit Rate: {GetHitRate() * 100:F1}%");
            Debug.Log($"  Hits: {_cacheHitCount}, Misses: {_cacheMissCount}");
            Debug.Log($"  Estimated Memory: {GetEstimatedMemoryUsage() / 1024f:F1} KB");
        }
    }
}
