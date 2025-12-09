using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using PerformanceTraining.Core;
using EnemyClass = PerformanceTraining.Enemy.Enemy;

namespace PerformanceTraining.Exercises.Memory
{
    /// <summary>
    /// GCアロケーション削減の課題
    /// </summary>
    public class ZeroAllocation_Exercise : MonoBehaviour
    {
        // ========================================================
        // Step 1: オブジェクトプール
        // ========================================================

        private GameObject _pooledPrefab;
        private Transform _poolParent;

        // TODO: 実装してください

        /// <summary>
        /// プールを初期化する
        /// </summary>
        public void InitializePool(GameObject prefab, int initialSize)
        {
            _pooledPrefab = prefab;

            var poolObject = new GameObject("EnemyPool");
            _poolParent = poolObject.transform;

            // TODO: 実装してください
        }

        /// <summary>
        /// プールから敵を取得する
        /// </summary>
        public EnemyClass GetFromPool()
        {
            // TODO: この実装を最適化してください
            var obj = Instantiate(_pooledPrefab, _poolParent);
            obj.SetActive(true);
            return obj.GetComponent<EnemyClass>();
        }

        /// <summary>
        /// 敵をプールに返却する
        /// </summary>
        public void ReturnToPool(EnemyClass enemy)
        {
            // TODO: この実装を最適化してください
            Destroy(enemy.gameObject);
        }


        // ========================================================
        // Step 2: 文字列キャッシュ
        // ========================================================

        // TODO: 実装してください

        /// <summary>
        /// ステータステキストを構築する
        /// </summary>
        public string BuildStatusText(int enemyCount, int killCount)
        {
            // TODO: この実装を最適化してください
            return "Enemies: " + enemyCount.ToString() + " | Kills: " + killCount.ToString();
        }


        // ========================================================
        // Step 3: デリゲートキャッシュ
        // ========================================================

        // TODO: 実装してください

        /// <summary>
        /// キャッシュされた更新アクションを取得する
        /// </summary>
        public Action<EnemyClass> GetCachedUpdateAction()
        {
            // TODO: この実装を最適化してください
            return new Action<EnemyClass>((enemy) =>
            {
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.UpdateCooldown(Time.deltaTime);
                }
            });
        }


        // ========================================================
        // Step 4: コレクション再利用
        // ========================================================

        // TODO: 実装してください

        /// <summary>
        /// 再利用可能なリストを取得する
        /// </summary>
        public List<EnemyClass> GetReusableList()
        {
            // TODO: この実装を最適化してください
            return new List<EnemyClass>();
        }


        // ========================================================
        // 初期化
        // ========================================================

        private void Awake()
        {
            // TODO: 必要なフィールドを初期化
        }
    }
}
