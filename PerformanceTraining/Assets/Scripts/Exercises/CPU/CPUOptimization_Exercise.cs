using System.Collections.Generic;
using UnityEngine;
using PerformanceTraining.Core;

namespace PerformanceTraining.Exercises.CPU
{
    /// <summary>
    /// 【課題2: CPU計算最適化】
    ///
    /// ■ 実装項目: 適切な探索ロジックとせよ
    ///
    /// 修正箇所は2つ：
    ///
    /// ① 空間分割による近傍検索の実装
    ///    - UpdateSpatialGrid(): グリッドにキャラクターを登録
    ///    - GetCellIndex(): 座標からセルインデックスを計算
    ///    - GetNearbyCharacters(): 周辺9セルからキャラクターを取得
    ///    - ExecuteAttackSequence内でGetAllCharactersをGetNearbyCharactersに置き換え
    ///
    /// ② 処理順序の最適化
    ///    - ExecuteAttackSequence内の処理ブロック呼び出し順序を並び替え
    ///    - 軽いフィルタを先に、重い経路探索を後に
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

        // ================================================================
        // 修正箇所①: 空間分割用のデータ構造
        // ================================================================
        // TODO: グリッドを実装してください
        // private Dictionary<int, List<Character>> _spatialGrid;
        // private List<Character> _nearbyResult;

        private float _cellSize = GameConstants.CELL_SIZE;
        private int _gridWidth = GameConstants.GRID_SIZE;

        private void Awake()
        {
            _characterManager = FindObjectOfType<CharacterManager>();
            // TODO: 空間グリッドを初期化
        }

        /// <summary>
        /// 空間グリッドを更新する
        /// 全キャラクターをグリッドに登録する
        /// </summary>
        public void UpdateSpatialGrid()
        {
            // TODO: 実装してください
            // 1. グリッドをクリア（各セルのリストをClear）
            // 2. 各キャラクターの位置からセルインデックスを計算
            // 3. 該当セルにキャラクターを追加
        }

        /// <summary>
        /// 座標からセルインデックスを取得する
        /// </summary>
        public int GetCellIndex(Vector3 position)
        {
            // TODO: 実装してください
            // ワールド座標を1次元のセルインデックスに変換
            // フィールド範囲: -FIELD_HALF_SIZE ～ +FIELD_HALF_SIZE
            // ヒント: x = (position.x + FIELD_HALF_SIZE) / cellSize
            //        z = (position.z + FIELD_HALF_SIZE) / cellSize
            //        index = z * gridWidth + x
            return 0;
        }

        /// <summary>
        /// 指定位置周辺のキャラクターを取得する（O(1)平均）
        /// </summary>
        public List<Character> GetNearbyCharacters(Vector3 position, Character excludeCharacter)
        {
            // TODO: 実装してください
            // 周辺9セル（3x3）からキャラクターを取得
            // 中心セルと周囲8セルをループして、各セルのキャラクターをリストに追加
            return new List<Character>();
        }

        // ================================================================
        // 修正箇所②: 処理順序の最適化
        // ================================================================

        /// <summary>
        /// 攻撃シーケンスを実行する
        ///
        /// 【課題】この関数内の処理順序を最適化してください
        /// 現在は理論上最悪の順序になっています
        /// </summary>
        public void ExecuteAttackSequence(Character attacker)
        {
            if (attacker == null || !attacker.IsAlive) return;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // ============================================================
            // 【最悪効率の呼び出し順序 - 並び替えてください】
            // ============================================================

            // TODO: 修正箇所①完了後は GetAllCharacters を GetNearbyCharacters に置き換え

            // Step A: まず全キャラクターを取得（非効率）
            List<Character> candidates = GetAllCharacters(attacker);

            // Step B: 全員に対して重い経路探索を実行（最も重い処理を最初に = 最悪）
            candidates = SortByPathfindingDistance(candidates, attacker);

            // Step C: HP条件でフィルタ（経路探索後 = 無駄）
            candidates = FilterByHP(candidates, _minTargetHP, _maxTargetHP);

            // Step D: 距離条件でフィルタ（一番軽い処理を最後に = 最悪）
            candidates = FilterByDistance(candidates, attacker, _maxAttackDistance);

            // Step E: 先頭を攻撃
            AttackFirst(candidates, attacker);

            // ============================================================

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
