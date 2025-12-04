using System.Collections.Generic;
using UnityEngine;
using MassacreDojo.Core;
using EnemyClass = MassacreDojo.Enemy.Enemy;
using EnemySystem = MassacreDojo.Enemy.EnemySystem;

namespace MassacreDojo.Exercises.Tradeoff
{
    /// <summary>
    /// 【課題3-A: 近傍キャッシュ（Neighbor Cache）】
    ///
    /// 目標: メモリを消費してCPU計算を削減する
    ///
    /// 概要:
    /// 各敵の「近くにいる敵リスト」を一定フレーム間キャッシュし、
    /// 毎フレームの近傍検索を削減します。
    ///
    /// トレードオフ:
    /// - メモリ: 敵1体あたり約170バイト（1000体で約170KB）
    /// - CPU: 5-7倍高速化（キャッシュ有効期間による）
    /// - 精度: キャッシュ期間分の位置ズレが発生
    ///
    /// 使用場面:
    /// - 敵同士の衝突回避（分離行動）
    /// - 群れ行動の計算
    /// - 包囲時の間隔調整
    ///
    /// TODO: キャッシュの実装を完成させてください
    /// </summary>
    public class NeighborCache_Exercise : MonoBehaviour
    {
        // ========================================================
        // キャッシュデータ構造
        // ========================================================

        /// <summary>
        /// キャッシュエントリ
        /// 各敵ごとに、近傍リストと最終更新フレームを保持
        /// </summary>
        private struct CacheEntry
        {
            public List<EnemyClass> Neighbors;
            public int LastUpdateFrame;
        }

        // TODO: ここにキャッシュ用のDictionaryを宣言してください
        // private Dictionary<EnemyClass, CacheEntry> _cache;


        // ========================================================
        // 設定
        // ========================================================

        [Header("キャッシュ設定")]
        [Tooltip("キャッシュの有効期間（フレーム数）")]
        [SerializeField] private int _cacheLifetimeFrames = 10;

        [Tooltip("近傍判定の距離")]
        [SerializeField] private float _neighborRadius = 5f;

        [Header("デバッグ")]
        [SerializeField] private int _cacheHitCount;
        [SerializeField] private int _cacheMissCount;

        // 内部変数
        private int _currentFrame;

        // TODO: 計算用の再利用リストを宣言してください（GC対策）
        // private List<Enemy> _tempList;


        // ========================================================
        // 初期化
        // ========================================================

        private void Awake()
        {
            // TODO: キャッシュと一時リストを初期化してください
            // ヒント:
            // _cache = new Dictionary<Enemy, CacheEntry>();
            // _tempList = new List<Enemy>(50);
        }


        // ========================================================
        // メインメソッド: 近傍の敵を取得
        // ========================================================

        /// <summary>
        /// 指定した敵の近傍にいる敵リストを取得する
        /// キャッシュが有効なら再利用、無効なら再計算
        /// </summary>
        /// <param name="enemy">対象の敵</param>
        /// <returns>近傍の敵リスト</returns>
        public List<EnemyClass> GetNeighbors(EnemyClass enemy)
        {
            _currentFrame = Time.frameCount;

            // TODO: キャッシュを使った近傍取得を実装してください
            // ヒント:
            // 1. _cache.TryGetValue() でキャッシュを確認
            // 2. キャッシュがあり、有効期間内なら entry.Neighbors を返す
            //    有効期間の判定: (_currentFrame - entry.LastUpdateFrame) < _cacheLifetimeFrames
            // 3. キャッシュがないか期限切れなら UpdateCache() を呼ぶ
            // 4. ヒット/ミスのカウントを更新

            // 仮実装（毎回計算 - 問題あり）- これを置き換えてください
            _cacheMissCount++;
            return CalculateNeighbors(enemy);
        }


        // ========================================================
        // キャッシュ更新
        // ========================================================

        /// <summary>
        /// 指定した敵のキャッシュを更新する
        /// </summary>
        /// <param name="enemy">対象の敵</param>
        /// <returns>更新された近傍リスト</returns>
        private List<EnemyClass> UpdateCache(EnemyClass enemy)
        {
            // TODO: キャッシュを更新してください
            // ヒント:
            // 1. CalculateNeighbors() で近傍を計算
            // 2. 新しい CacheEntry を作成
            //    entry.Neighbors = 計算結果のコピー（new List<Enemy>(計算結果)）
            //    entry.LastUpdateFrame = _currentFrame
            // 3. _cache[enemy] = entry で保存
            // 4. entry.Neighbors を返す

            // 仮実装 - これを置き換えてください
            return CalculateNeighbors(enemy);
        }


        // ========================================================
        // 近傍計算（実際の重い処理）
        // ========================================================

        /// <summary>
        /// 近傍の敵を実際に計算する（重い処理）
        /// </summary>
        private List<EnemyClass> CalculateNeighbors(EnemyClass enemy)
        {
            // TODO: _tempList を再利用してください（GC対策）
            // ヒント:
            // if (_tempList == null) _tempList = new List<Enemy>(50);
            // _tempList.Clear();

            var result = new List<EnemyClass>(); // 問題あり - 毎回new

            var enemySystem = FindObjectOfType<EnemySystem>();
            if (enemySystem == null) return result;

            Vector3 myPos = enemy.transform.position;
            float radiusSqr = _neighborRadius * _neighborRadius;

            foreach (var other in enemySystem.ActiveEnemies)
            {
                if (other == null || other == enemy || !other.IsAlive)
                    continue;

                // sqrMagnitude で距離判定（平方根を避ける）
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

        /// <summary>
        /// 特定の敵のキャッシュをクリアする
        /// （敵が死亡した時などに呼ぶ）
        /// </summary>
        public void InvalidateCache(EnemyClass enemy)
        {
            // TODO: 指定した敵のキャッシュを削除してください
            // ヒント: _cache.Remove(enemy);
        }

        /// <summary>
        /// 全キャッシュをクリアする
        /// </summary>
        public void ClearAllCache()
        {
            // TODO: 全キャッシュをクリアしてください
            // ヒント: _cache.Clear();

            _cacheHitCount = 0;
            _cacheMissCount = 0;
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
            // return _cache?.Count ?? 0;
            return 0;
        }

        /// <summary>
        /// メモリ使用量の推定値を取得（バイト）
        /// </summary>
        public int GetEstimatedMemoryUsage()
        {
            // 1エントリあたりの推定サイズ:
            // - Dictionary オーバーヘッド: 約40バイト
            // - List<Enemy> 参照: 8バイト
            // - List内部配列: 約20要素 × 8バイト = 160バイト
            // - LastUpdateFrame: 4バイト
            // 合計: 約212バイト/エントリ

            int entryCount = GetCacheSize();
            return entryCount * 212;
        }

        /// <summary>
        /// 統計情報をログ出力
        /// </summary>
        public void LogStats()
        {
            Debug.Log($"NeighborCache Stats:");
            Debug.Log($"  Cache Size: {GetCacheSize()} entries");
            Debug.Log($"  Hit Rate: {GetHitRate() * 100:F1}%");
            Debug.Log($"  Hits: {_cacheHitCount}, Misses: {_cacheMissCount}");
            Debug.Log($"  Estimated Memory: {GetEstimatedMemoryUsage() / 1024f:F1} KB");
            Debug.Log($"  Lifetime: {_cacheLifetimeFrames} frames");
        }


        private void OnDrawGizmosSelected()
        {
            // 近傍範囲を可視化
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _neighborRadius);
        }
    }
}
