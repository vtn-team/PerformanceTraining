using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using MassacreDojo.Core;
using EnemyClass = MassacreDojo.Enemy.Enemy;

namespace MassacreDojo.Exercises.Memory
{
    /// <summary>
    /// 【課題1: ゼロアロケーション】
    ///
    /// 目標: Update内でのGCアロケーションをゼロにする
    ///
    /// 確認方法:
    /// - Profiler > CPU > GC Alloc 列を確認
    /// - 目標: 50+ KB/frame → 1KB以下/frame
    ///
    /// このクラスには4つのStepがあります。
    /// 各Stepのメソッドを実装してください。
    /// </summary>
    public class ZeroAllocation_Exercise : MonoBehaviour
    {
        // ========================================================
        // Step 1: オブジェクトプール
        // ========================================================
        // 問題: Instantiate/Destroyは重いGCアロケーションを発生させる
        // 解決: オブジェクトを再利用するプールを実装する

        private GameObject _pooledPrefab;
        private Transform _poolParent;

        // TODO: プール用のデータ構造を宣言


        /// <summary>
        /// プールを初期化する
        /// </summary>
        public void InitializePool(GameObject prefab, int initialSize)
        {
            _pooledPrefab = prefab;

            var poolObject = new GameObject("EnemyPool");
            _poolParent = poolObject.transform;

            // TODO: 初期オブジェクトを生成してプールに追加
        }

        /// <summary>
        /// プールから敵を取得する
        /// </summary>
        public EnemyClass GetFromPool()
        {
            // 現在の実装（問題あり）: 毎回Instantiate
            var obj = Instantiate(_pooledPrefab, _poolParent);
            obj.SetActive(true);
            return obj.GetComponent<EnemyClass>();
        }

        /// <summary>
        /// 敵をプールに返却する
        /// </summary>
        public void ReturnToPool(EnemyClass enemy)
        {
            // 現在の実装（問題あり）: 毎回Destroy
            Destroy(enemy.gameObject);
        }


        // ========================================================
        // Step 2: 文字列キャッシュ
        // ========================================================
        // 問題: 文字列結合（+ 演算子）は毎回新しい文字列を生成する
        // 解決: StringBuilderを再利用する

        // TODO: StringBuilderをフィールドで宣言


        /// <summary>
        /// ステータステキストを構築する
        /// </summary>
        public string BuildStatusText(int enemyCount, int killCount)
        {
            // 現在の実装（問題あり）: 毎回新しい文字列を生成
            return "Enemies: " + enemyCount.ToString() + " | Kills: " + killCount.ToString();
        }


        // ========================================================
        // Step 3: デリゲートキャッシュ
        // ========================================================
        // 問題: ラムダ式は毎回新しいデリゲートオブジェクトを生成する
        // 解決: デリゲートをフィールドでキャッシュする

        // TODO: Action<EnemyClass>をフィールドで宣言


        /// <summary>
        /// キャッシュされた更新アクションを取得する
        /// </summary>
        public Action<EnemyClass> GetCachedUpdateAction()
        {
            // 現在の実装（問題あり）: 毎回新しいActionを生成
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
        // 問題: new List<T>()は毎回ヒープメモリを確保する
        // 解決: リストをフィールドで保持し、Clear()で再利用する

        // TODO: List<EnemyClass>をフィールドで宣言


        /// <summary>
        /// 再利用可能なリストを取得する
        /// </summary>
        public List<EnemyClass> GetReusableList()
        {
            // 現在の実装（問題あり）: 毎回新しいListを生成
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
