using System.Collections.Generic;
using UnityEngine;
using PerformanceTraining.Core;

#pragma warning disable 0414 // 課題用フィールド: 学生が実装時に使用

namespace PerformanceTraining.Exercises.CPU
{
    /// <summary>
    /// CPU計算最適化の課題
    /// </summary>
    public class CPUOptimization_Exercise : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _maxAttackDistance = 20f;
        [SerializeField] private float _minTargetHP = 10f;
        [SerializeField] private float _maxTargetHP = 100f;

        [Header("Debug")]
        [SerializeField] private int _lastProcessedCount;
        [SerializeField] private float _lastExecutionTimeMs;

        private CharacterManager _characterManager;

        // TODO: 空間分割用のデータ構造を追加してください

        private float _cellSize = GameConstants.CELL_SIZE;
        private int _gridWidth = GameConstants.GRID_SIZE;

        private void Awake()
        {
            _characterManager = FindAnyObjectByType<CharacterManager>();
            // TODO: 空間グリッドを初期化
        }

        /// <summary>
        /// 空間グリッドを更新する
        /// </summary>
        public void UpdateSpatialGrid()
        {
            // TODO: 全キャラクターをグリッドに登録する処理を実装してください
        }

        /// <summary>
        /// 空間グリッドを更新する（Enemy用オーバーロード）
        /// </summary>
        public void UpdateSpatialGrid(List<PerformanceTraining.Enemy.Enemy> enemies)
        {
            // TODO: 実装してください
            // EnemyAIManagerから呼び出される用
        }

        /// <summary>
        /// このフレームで更新すべきかどうかを判定する（更新分散用）
        /// </summary>
        public bool ShouldUpdateThisFrame(int updateGroup, int frameCount)
        {
            // TODO: 実装してください
            // 更新分散: グループごとに異なるフレームで更新
            // 例: グループ0は0,4,8...フレーム、グループ1は1,5,9...フレーム
            return true; // 非最適化版: 常にtrue
        }

        /// <summary>
        /// 平方根を使わない距離計算（二乗距離を返す）
        /// </summary>
        public float CalculateDistanceSqr(Vector3 a, Vector3 b)
        {
            // TODO: 実装してください
            // sqrMagnitudeを使用して平方根計算を回避
            return (a - b).sqrMagnitude;
        }

        /// <summary>
        /// 2点間の距離が指定値以内かどうかを判定する（平方根を使わない）
        /// </summary>
        public bool IsWithinDistance(Vector3 a, Vector3 b, float maxDistance)
        {
            // TODO: 実装してください
            // sqrMagnitudeと距離の2乗を比較して平方根計算を回避
            return (a - b).sqrMagnitude <= maxDistance * maxDistance;
        }

        /// <summary>
        /// 指定位置周辺の敵を取得する（Enemy用）
        /// </summary>
        public List<PerformanceTraining.Enemy.Enemy> QueryNearbyEnemies(Vector3 position)
        {
            // TODO: 実装してください
            // 空間分割を使った近傍検索
            return new List<PerformanceTraining.Enemy.Enemy>();
        }

        /// <summary>
        /// 座標からセルインデックスを取得する
        /// </summary>
        public int GetCellIndex(Vector3 position)
        {
            // TODO: ワールド座標を1次元のセルインデックスに変換してください
            return 0;
        }

        /// <summary>
        /// 指定位置周辺のキャラクターを取得する
        /// </summary>
        public List<Character> GetNearbyCharacters(Vector3 position, Character excludeCharacter)
        {
            // TODO: 空間分割を使って近傍のキャラクターのみを返すよう実装してください
            return new List<Character>();
        }

        /// <summary>
        /// 攻撃シーケンスを実行する
        ///
        /// 【課題】この関数内の処理順序を最適化してください
        /// ヒント: 各処理ブロックの計算量コメントを参考に、効率的な順序を考えてください
        /// </summary>
        public void ExecuteAttackSequence(Character attacker)
        {
            if (attacker == null || !attacker.IsAlive) return;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // TODO: 処理順序を並び替えてください

            List<Character> candidates = GetAllCharacters(attacker);
            candidates = SortByPathfindingDistance(candidates, attacker);
            candidates = FilterByHP(candidates, _minTargetHP, _maxTargetHP);
            candidates = FilterByDistance(candidates, attacker, _maxAttackDistance);
            AttackFirst(candidates, attacker);

            stopwatch.Stop();
            _lastExecutionTimeMs = (float)stopwatch.Elapsed.TotalMilliseconds;
            _lastProcessedCount = candidates.Count;
        }

        // ================================================================
        // 処理ブロック（これらの実装は変更しないでください）
        // ================================================================

        /// <summary>
        /// 【ブロック1】全キャラクターを取得する（非効率版）
        /// 計算量: O(n)
        /// → GetNearbyCharactersに置き換えることでO(1)に改善
        /// </summary>
        public List<Character> GetAllCharacters(Character excludeCharacter)
        {
            if (_characterManager == null) return new List<Character>();

            var result = new List<Character>();
            foreach (var c in _characterManager.AliveCharacters)
            {
                if (c != null && c != excludeCharacter && c.IsAlive)
                {
                    result.Add(c);
                }
            }
            return result;
        }

        /// <summary>
        /// 【ブロック2】距離条件でフィルタする
        /// 計算量: O(n) - 軽い処理
        /// </summary>
        public List<Character> FilterByDistance(List<Character> characters, Character attacker, float maxDistance)
        {
            if (attacker == null) return characters;

            var result = new List<Character>();
            Vector3 attackerPos = attacker.transform.position;
            float maxDistSqr = maxDistance * maxDistance;

            foreach (var c in characters)
            {
                if (c == null) continue;

                float distSqr = (c.transform.position - attackerPos).sqrMagnitude;
                if (distSqr <= maxDistSqr)
                {
                    result.Add(c);
                }
            }
            return result;
        }

        /// <summary>
        /// 【ブロック3】HP条件でフィルタする
        /// 計算量: O(n) - 軽い処理
        /// </summary>
        public List<Character> FilterByHP(List<Character> characters, float minHP, float maxHP)
        {
            var result = new List<Character>();

            foreach (var c in characters)
            {
                if (c == null) continue;

                float hp = c.Stats.currentHealth;
                if (hp >= minHP && hp <= maxHP)
                {
                    result.Add(c);
                }
            }
            return result;
        }

        /// <summary>
        /// 【ブロック4】経路探索して到達距離順にソートする
        /// 計算量: O(n * P) - 非常に重い処理（Pは経路探索の計算量）
        /// </summary>
        public List<Character> SortByPathfindingDistance(List<Character> characters, Character attacker)
        {
            if (attacker == null || characters.Count == 0) return characters;

            var withDistance = new List<(Character character, float distance)>();

            foreach (var c in characters)
            {
                if (c == null) continue;

                float pathDistance = CalculatePathDistance(attacker.transform.position, c.transform.position);
                withDistance.Add((c, pathDistance));
            }

            withDistance.Sort((a, b) => a.distance.CompareTo(b.distance));

            var result = new List<Character>();
            foreach (var item in withDistance)
            {
                result.Add(item.character);
            }
            return result;
        }

        /// <summary>
        /// 【ブロック5】リストの先頭キャラクターを攻撃する
        /// 計算量: O(1)
        /// </summary>
        public void AttackFirst(List<Character> characters, Character attacker)
        {
            if (characters.Count == 0 || attacker == null) return;

            var target = characters[0];
            if (target != null && target.IsAlive)
            {
                attacker.Attack(target);
            }
        }

        // ================================================================
        // 内部処理（変更不要）
        // ================================================================

        private float CalculatePathDistance(Vector3 start, Vector3 end)
        {
            float directDistance = Vector3.Distance(start, end);

            int gridResolution = 20;
            float totalCost = 0f;
            Vector3 current = start;
            Vector3 direction = (end - start).normalized;
            float stepSize = directDistance / gridResolution;

            for (int i = 0; i < gridResolution; i++)
            {
                Vector3 next = current + direction * stepSize;
                float obstacleCost = SimulateObstacleCheck(current, next);
                totalCost += stepSize + obstacleCost;
                current = next;
            }

            return totalCost;
        }

        private float SimulateObstacleCheck(Vector3 from, Vector3 to)
        {
            float cost = 0f;
            for (int i = 0; i < 100; i++)
            {
                float angle = Mathf.Atan2(to.z - from.z, to.x - from.x);
                float dist = Mathf.Sqrt((to.x - from.x) * (to.x - from.x) + (to.z - from.z) * (to.z - from.z));
                cost += Mathf.Sin(angle) * Mathf.Cos(angle) * 0.001f;
                cost += dist * 0.0001f;
            }
            return Mathf.Abs(cost);
        }

        // ================================================================
        // テスト・更新処理
        // ================================================================

        private void Update()
        {
            // 空間グリッドを毎フレーム更新
            UpdateSpatialGrid();

            // Spaceキーでテスト実行
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // 最初の生存キャラクターで攻撃シーケンスをテスト
                if (_characterManager != null && _characterManager.AliveCharacters.Count > 0)
                {
                    var attacker = _characterManager.AliveCharacters[0];
                    ExecuteAttackSequence(attacker);
                    Debug.Log($"ExecuteAttackSequence: {_lastExecutionTimeMs:F2}ms, Candidates: {_lastProcessedCount}");
                }
            }
        }

        public float GetLastExecutionTimeMs() => _lastExecutionTimeMs;
        public int GetLastProcessedCount() => _lastProcessedCount;
    }
}
