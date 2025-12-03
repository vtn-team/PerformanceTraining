using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using MassacreDojo.Core;

#if EXERCISES_DEPLOYED
using StudentExercises.Memory;
#else
using MassacreDojo.Exercises.Memory;
#endif

namespace MassacreDojo.Enemy
{
    /// <summary>
    /// 敵の生成・管理を行うシステム
    /// 【課題1: メモリ最適化】問題を含むコード
    /// - Step 1: オブジェクトプール → Instantiate/Destroy乱発
    /// - Step 2: 文字列結合 → 毎フレーム文字列生成
    /// - Step 3: デリゲートキャッシュ → 毎回new Action
    /// - Step 4: コレクション再利用 → 毎回new List
    /// </summary>
    public class EnemySystem : MonoBehaviour
    {
        [Header("設定")]
        [SerializeField] private GameObject enemyPrefab;

        [Header("デバッグ")]
        [SerializeField] private int activeEnemyCount;
        [SerializeField] private string statusText;

        // 敵リスト
        private List<Enemy> activeEnemies = new List<Enemy>();

        // ===== 課題1の問題コード =====
        // これらは学生が最適化する対象

        // Step 3用: 毎フレーム新しいデリゲートを生成（問題あり）
        private Action<Enemy> onEnemyUpdateAction;

        // 学習用の参照
        private LearningSettings settings;
        private ZeroAllocation_Exercise memoryExercise;

        public List<Enemy> ActiveEnemies => activeEnemies;
        public int ActiveEnemyCount => activeEnemies.Count;

        private void Awake()
        {
            settings = GameManager.Instance?.Settings;

            // Exerciseクラスのインスタンスを取得または作成
            memoryExercise = GetComponent<ZeroAllocation_Exercise>();
            if (memoryExercise == null)
            {
                memoryExercise = gameObject.AddComponent<ZeroAllocation_Exercise>();
            }
        }

        /// <summary>
        /// システム初期化
        /// </summary>
        public void Initialize()
        {
            activeEnemies.Clear();

            // 敵プレハブがなければ自動生成
            if (enemyPrefab == null)
            {
                CreateDefaultEnemyPrefab();
            }

            // オブジェクトプールの初期化（最適化有効時）
            if (settings != null && settings.useObjectPool)
            {
                memoryExercise.InitializePool(enemyPrefab, GameConstants.OBJECT_POOL_INITIAL_SIZE);
            }
        }

        /// <summary>
        /// デフォルトの敵プレハブを作成
        /// </summary>
        private void CreateDefaultEnemyPrefab()
        {
            enemyPrefab = new GameObject("EnemyPrefab");
            enemyPrefab.SetActive(false);

            // Capsule形状
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(enemyPrefab.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

            // マテリアル設定
            var renderer = visual.GetComponent<Renderer>();
            renderer.material.color = Color.red;

            // Enemyコンポーネント追加
            var enemy = enemyPrefab.AddComponent<Enemy>();

            // Collider調整
            var capsuleCollider = visual.GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
            {
                // メインオブジェクトに移動
                var mainCollider = enemyPrefab.AddComponent<CapsuleCollider>();
                mainCollider.height = 2f;
                mainCollider.radius = 0.4f;
                Destroy(capsuleCollider);
            }

            DontDestroyOnLoad(enemyPrefab);
        }

        /// <summary>
        /// 敵をスポーンする
        /// </summary>
        public void SpawnEnemies(int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy();
            }
        }

        /// <summary>
        /// 指定位置に敵を1体スポーン
        /// </summary>
        public void SpawnEnemy(Vector3 position)
        {
            SpawnEnemyInternal(position);
        }

        /// <summary>
        /// 敵を1体スポーン（ランダム位置）
        /// 【問題コード】Instantiate/Destroyを毎回実行
        /// </summary>
        private void SpawnEnemy()
        {
            SpawnEnemyInternal(GetRandomSpawnPosition());
        }

        /// <summary>
        /// 内部スポーン処理
        /// </summary>
        private void SpawnEnemyInternal(Vector3 spawnPos)
        {
            Enemy enemy;

            // ===== Step 1: オブジェクトプール =====
            if (settings != null && settings.useObjectPool)
            {
                // 最適化版: プールから取得
                enemy = memoryExercise.GetFromPool();
            }
            else
            {
                // 問題版: 毎回Instantiate（GCアロケーション発生）
                GameObject obj = Instantiate(enemyPrefab);
                obj.SetActive(true);
                enemy = obj.GetComponent<Enemy>();
            }

            if (enemy != null)
            {
                enemy.transform.position = spawnPos;
                enemy.Initialize(activeEnemies.Count % GameConstants.AI_UPDATE_GROUPS);

                activeEnemies.Add(enemy);
                GameManager.Instance?.OnEnemySpawned();
            }
        }

        /// <summary>
        /// 敵を返却/破棄
        /// </summary>
        public void ReturnEnemy(Enemy enemy)
        {
            if (enemy == null) return;

            activeEnemies.Remove(enemy);

            // ===== Step 1: オブジェクトプール =====
            if (settings != null && settings.useObjectPool)
            {
                // 最適化版: プールに返却
                memoryExercise.ReturnToPool(enemy);
            }
            else
            {
                // 問題版: 毎回Destroy（GCアロケーション発生）
                Destroy(enemy.gameObject);
            }
        }

        /// <summary>
        /// 全敵を破棄
        /// </summary>
        public void DespawnAllEnemies()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = activeEnemies[i];
                if (enemy != null)
                {
                    if (settings != null && settings.useObjectPool)
                    {
                        memoryExercise.ReturnToPool(enemy);
                    }
                    else
                    {
                        Destroy(enemy.gameObject);
                    }
                }
            }
            activeEnemies.Clear();
        }

        /// <summary>
        /// ランダムなスポーン位置を取得
        /// </summary>
        private Vector3 GetRandomSpawnPosition()
        {
            float limit = GameConstants.FIELD_HALF_SIZE - GameConstants.SPAWN_MARGIN;
            return new Vector3(
                UnityEngine.Random.Range(-limit, limit),
                0f,
                UnityEngine.Random.Range(-limit, limit)
            );
        }

        /// <summary>
        /// 範囲内の敵にダメージを与える
        /// 【問題コード】毎回new Listを生成
        /// </summary>
        public int DamageEnemiesInRange(Vector3 position, float range, int damage)
        {
            int hitCount = 0;

            // ===== Step 4: コレクション再利用 =====
            List<Enemy> enemiesInRange;

            if (settings != null && settings.useCollectionReuse)
            {
                // 最適化版: 再利用リストを使用
                enemiesInRange = memoryExercise.GetReusableList();
            }
            else
            {
                // 問題版: 毎回新しいListを生成（GCアロケーション発生）
                enemiesInRange = new List<Enemy>();
            }

            // ===== Step 3: 距離計算（sqrMagnitude vs Distance）=====
            float rangeSqr = range * range;

            foreach (var enemy in activeEnemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;

                float dist;
                if (settings != null && settings.useSqrMagnitude)
                {
                    // 最適化版: sqrMagnitude使用
                    dist = (enemy.transform.position - position).sqrMagnitude;
                    if (dist <= rangeSqr)
                    {
                        enemiesInRange.Add(enemy);
                    }
                }
                else
                {
                    // 問題版: Vector3.Distance使用（平方根計算が重い）
                    dist = Vector3.Distance(enemy.transform.position, position);
                    if (dist <= range)
                    {
                        enemiesInRange.Add(enemy);
                    }
                }
            }

            foreach (var enemy in enemiesInRange)
            {
                enemy.TakeDamage(damage);
                hitCount++;
            }

            return hitCount;
        }

        /// <summary>
        /// ステータステキストを更新
        /// 【問題コード】文字列結合でGCアロケーション発生
        /// </summary>
        private void Update()
        {
            UpdateStatusText();
            UpdateEnemiesWithDelegate();
        }

        /// <summary>
        /// ステータステキスト更新
        /// 【問題コード】毎フレーム文字列結合
        /// </summary>
        private void UpdateStatusText()
        {
            activeEnemyCount = activeEnemies.Count;

            // ===== Step 2: 文字列結合 =====
            if (settings != null && settings.useStringBuilder)
            {
                // 最適化版: StringBuilder使用
                statusText = memoryExercise.BuildStatusText(activeEnemyCount, GameManager.Instance?.KillCount ?? 0);
            }
            else
            {
                // 問題版: 文字列結合（毎フレームGCアロケーション発生）
                statusText = "Enemies: " + activeEnemyCount.ToString() + " | Kills: " + (GameManager.Instance?.KillCount ?? 0).ToString();
            }
        }

        /// <summary>
        /// デリゲートを使った敵更新
        /// 【問題コード】毎フレーム新しいActionを生成
        /// </summary>
        private void UpdateEnemiesWithDelegate()
        {
            // ===== Step 3: デリゲートキャッシュ =====
            Action<Enemy> updateAction;

            if (settings != null && settings.useDelegateCache)
            {
                // 最適化版: キャッシュされたデリゲートを使用
                updateAction = memoryExercise.GetCachedUpdateAction();
            }
            else
            {
                // 問題版: 毎フレーム新しいActionを生成（GCアロケーション発生）
                updateAction = new Action<Enemy>((enemy) =>
                {
                    if (enemy != null && enemy.IsAlive)
                    {
                        enemy.UpdateCooldown(Time.deltaTime);
                    }
                });
            }

            foreach (var enemy in activeEnemies)
            {
                updateAction?.Invoke(enemy);
            }
        }

        public string GetStatusText() => statusText;

        private void OnDrawGizmos()
        {
            // フィールド範囲を表示
            Gizmos.color = Color.green;
            float size = GameConstants.FIELD_SIZE;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size, 0.1f, size));
        }
    }
}
