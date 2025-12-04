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
    /// このクラスには4つのStepがあります。
    /// 各Stepの「TODO:」コメントを見つけて、コードを実装してください。
    ///
    /// 確認方法:
    /// - Profiler > CPU > GC Alloc を確認
    /// - Before: 50KB+/frame → After: 1KB以下/frame
    /// </summary>
    public class ZeroAllocation_Exercise : MonoBehaviour
    {
        // ========================================================
        // Step 1: オブジェクトプール
        // ========================================================
        // 目的: Instantiate/Destroyを避け、オブジェクトを再利用する
        //
        // TODO: Stack<Enemy>を使ってプールを実装してください
        // ヒント: Stackはpush/popでLIFO（後入れ先出し）で管理できます

        private GameObject _pooledPrefab;
        private Transform _poolParent;

        // TODO: ここにStack<EnemyClass>を宣言してください
        // private Stack<EnemyClass> _enemyPool;


        /// <summary>
        /// プールを初期化する
        /// </summary>
        /// <param name="prefab">プールするプレハブ</param>
        /// <param name="initialSize">初期プールサイズ</param>
        public void InitializePool(GameObject prefab, int initialSize)
        {
            _pooledPrefab = prefab;

            // プール用の親オブジェクトを作成
            var poolObject = new GameObject("EnemyPool");
            _poolParent = poolObject.transform;

            // TODO: Stackを初期化してください
            // _enemyPool = new Stack<EnemyClass>(initialSize);

            // TODO: 初期オブジェクトを生成してプールに追加してください
            // for (int i = 0; i < initialSize; i++)
            // {
            //     var obj = Instantiate(_pooledPrefab, _poolParent);
            //     obj.SetActive(false);
            //     var enemy = obj.GetComponent<EnemyClass>();
            //     _enemyPool.Push(enemy);
            // }
        }

        /// <summary>
        /// プールから敵を取得する
        /// </summary>
        /// <returns>取得した敵（プールが空の場合は新規生成）</returns>
        public EnemyClass GetFromPool()
        {
            // TODO: プールから敵を取得してください
            // ヒント:
            // 1. プールに敵がいれば Pop() で取得
            // 2. いなければ Instantiate で新規生成
            // 3. SetActive(true) を忘れずに

            // 仮実装（問題あり）- これを置き換えてください
            var obj = Instantiate(_pooledPrefab, _poolParent);
            obj.SetActive(true);
            return obj.GetComponent<EnemyClass>();
        }

        /// <summary>
        /// 敵をプールに返却する
        /// </summary>
        /// <param name="enemy">返却する敵</param>
        public void ReturnToPool(EnemyClass enemy)
        {
            // TODO: 敵をプールに返却してください
            // ヒント:
            // 1. SetActive(false) で非表示に
            // 2. Push() でスタックに追加

            // 仮実装（問題あり）- これを置き換えてください
            Destroy(enemy.gameObject);
        }


        // ========================================================
        // Step 2: 文字列キャッシュ（StringBuilder）
        // ========================================================
        // 目的: 文字列結合によるGCアロケーションを防ぐ
        //
        // TODO: StringBuilderをフィールドで保持し、再利用してください

        // TODO: ここにStringBuilderを宣言してください
        // private StringBuilder _statusBuilder;


        /// <summary>
        /// ステータステキストを構築する
        /// </summary>
        /// <param name="enemyCount">現在の敵数</param>
        /// <param name="killCount">撃破数</param>
        /// <returns>構築されたテキスト</returns>
        public string BuildStatusText(int enemyCount, int killCount)
        {
            // TODO: StringBuilderを使って文字列を構築してください
            // ヒント:
            // 1. _statusBuilder が null なら new StringBuilder(64) で初期化
            // 2. Clear() でリセット
            // 3. Append() で文字列を追加
            // 4. ToString() で結果を返す

            // 仮実装（問題あり）- これを置き換えてください
            return "Enemies: " + enemyCount.ToString() + " | Kills: " + killCount.ToString();
        }


        // ========================================================
        // Step 3: デリゲートキャッシュ
        // ========================================================
        // 目的: 毎フレームnew Action()を防ぐ
        //
        // TODO: Action<Enemy>をフィールドでキャッシュしてください

        // TODO: ここにAction<EnemyClass>を宣言してください
        // private Action<EnemyClass> _cachedUpdateAction;


        /// <summary>
        /// キャッシュされた更新アクションを取得する
        /// </summary>
        /// <returns>キャッシュされたアクション</returns>
        public Action<EnemyClass> GetCachedUpdateAction()
        {
            // TODO: キャッシュされたActionを返してください
            // ヒント:
            // 1. _cachedUpdateAction が null なら初期化
            // 2. 初期化は一度だけ行う（コンストラクタやAwakeでも可）
            // 3. 二度目以降はキャッシュを返す

            // 仮実装（問題あり）- これを置き換えてください
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
        // 目的: 毎回new List<T>()を防ぐ
        //
        // TODO: List<Enemy>をフィールドで保持し、Clear()で再利用してください

        // TODO: ここにList<EnemyClass>を宣言してください
        // private List<EnemyClass> _reusableEnemyList;


        /// <summary>
        /// 再利用可能なリストを取得する
        /// </summary>
        /// <returns>クリア済みのリスト</returns>
        public List<EnemyClass> GetReusableList()
        {
            // TODO: 再利用可能なリストを返してください
            // ヒント:
            // 1. _reusableEnemyList が null なら new List<EnemyClass>(100) で初期化
            // 2. Clear() でリセット
            // 3. 同じインスタンスを返す

            // 仮実装（問題あり）- これを置き換えてください
            return new List<EnemyClass>();
        }


        // ========================================================
        // ヘルパーメソッド
        // ========================================================

        private void Awake()
        {
            // TODO: ここで必要な初期化を行ってください
            // 例:
            // _statusBuilder = new StringBuilder(64);
            // _reusableEnemyList = new List<Enemy>(100);
            // _cachedUpdateAction = (enemy) => { ... };
        }
    }
}
