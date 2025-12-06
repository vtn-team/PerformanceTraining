using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using PerformanceTraining.Core;
using EnemyClass = PerformanceTraining.Enemy.Enemy;

namespace PerformanceTraining.Solutions.Memory
{
    /// <summary>
    /// 【解答】課題1: ゼロアロケーション
    ///
    /// このファイルは教員用の解答です。
    /// 学生には見せないでください。
    /// </summary>
    public class ZeroAllocation_Solution : MonoBehaviour
    {
        // ========================================================
        // Step 1: オブジェクトプール【解答】
        // ========================================================

        private GameObject _pooledPrefab;
        private Transform _poolParent;

        // 【解答】Stack<EnemyClass>でプール管理
        private Stack<EnemyClass> _enemyPool;


        public void InitializePool(GameObject prefab, int initialSize)
        {
            _pooledPrefab = prefab;

            var poolObject = new GameObject("EnemyPool");
            _poolParent = poolObject.transform;

            // 【解答】Stackを初期化
            _enemyPool = new Stack<EnemyClass>(initialSize);

            // 【解答】初期オブジェクトを生成してプールに追加
            for (int i = 0; i < initialSize; i++)
            {
                var obj = Instantiate(_pooledPrefab, _poolParent);
                obj.SetActive(false);
                var enemy = obj.GetComponent<EnemyClass>();
                _enemyPool.Push(enemy);
            }
        }

        public EnemyClass GetFromPool()
        {
            // 【解答】プールから取得
            if (_enemyPool != null && _enemyPool.Count > 0)
            {
                var enemy = _enemyPool.Pop();
                enemy.gameObject.SetActive(true);
                return enemy;
            }
            else
            {
                // プールが空の場合は新規生成
                var obj = Instantiate(_pooledPrefab, _poolParent);
                obj.SetActive(true);
                return obj.GetComponent<EnemyClass>();
            }
        }

        public void ReturnToPool(EnemyClass enemy)
        {
            // 【解答】プールに返却
            if (enemy != null && _enemyPool != null)
            {
                enemy.gameObject.SetActive(false);
                _enemyPool.Push(enemy);
            }
        }


        // ========================================================
        // Step 2: 文字列キャッシュ【解答】
        // ========================================================

        // 【解答】StringBuilderをフィールドで保持
        private StringBuilder _statusBuilder;


        public string BuildStatusText(int enemyCount, int killCount)
        {
            // 【解答】StringBuilderで文字列を構築
            if (_statusBuilder == null)
            {
                _statusBuilder = new StringBuilder(64);
            }

            _statusBuilder.Clear();
            _statusBuilder.Append("Enemies: ");
            _statusBuilder.Append(enemyCount);
            _statusBuilder.Append(" | Kills: ");
            _statusBuilder.Append(killCount);

            return _statusBuilder.ToString();
        }


        // ========================================================
        // Step 3: デリゲートキャッシュ【解答】
        // ========================================================

        // 【解答】Action<EnemyClass>をフィールドでキャッシュ
        private Action<EnemyClass> _cachedUpdateAction;


        public Action<EnemyClass> GetCachedUpdateAction()
        {
            // 【解答】キャッシュされたActionを返す
            if (_cachedUpdateAction == null)
            {
                _cachedUpdateAction = (enemy) =>
                {
                    if (enemy != null && enemy.IsAlive)
                    {
                        enemy.UpdateCooldown(Time.deltaTime);
                    }
                };
            }

            return _cachedUpdateAction;
        }


        // ========================================================
        // Step 4: コレクション再利用【解答】
        // ========================================================

        // 【解答】List<EnemyClass>をフィールドで保持
        private List<EnemyClass> _reusableEnemyList;


        public List<EnemyClass> GetReusableList()
        {
            // 【解答】再利用可能なリストを返す
            if (_reusableEnemyList == null)
            {
                _reusableEnemyList = new List<EnemyClass>(100);
            }

            _reusableEnemyList.Clear();
            return _reusableEnemyList;
        }


        // ========================================================
        // 初期化【解答】
        // ========================================================

        private void Awake()
        {
            // 【解答】全ての初期化を一度に行う
            _statusBuilder = new StringBuilder(64);
            _reusableEnemyList = new List<EnemyClass>(100);
            _cachedUpdateAction = (enemy) =>
            {
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.UpdateCooldown(Time.deltaTime);
                }
            };
        }
    }
}
