using System.Collections.Generic;
using UnityEngine;
using PerformanceTraining.Core;

namespace PerformanceTraining.Solutions.CPU
{
    /// <summary>
    /// 【課題2: CPU計算最適化 - 解答】
    ///
    /// このファイルは教員用の解答です。
    /// 学生には見せないでください。
    ///
    /// 修正箇所①: 空間分割 → O(n) → O(1)
    ///   - Dictionary でグリッドを管理
    ///   - GetNearbyCharacters で周辺9セルのみ検索
    ///
    /// 修正箇所②: 処理順序 → 軽いフィルタ先、重い処理後
    ///   【最悪】全取得 → 経路探索 → HPフィルタ → 距離フィルタ → 攻撃
    ///   【最適】近傍取得 → 距離フィルタ → HPフィルタ → 経路探索 → 攻撃
    /// </summary>
    public class CPUOptimization_Solution : MonoBehaviour
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
        // 修正箇所①: 空間分割【解答】
        // ================================================================
        private Dictionary<int, List<Character>> _spatialGrid;
        private List<Character> _nearbyResult;

        private float _cellSize = GameConstants.CELL_SIZE;
        private int _gridWidth = GameConstants.GRID_SIZE;

        private void Awake()
        {
            _characterManager = FindAnyObjectByType<CharacterManager>();

            // 【解答】空間グリッドを初期化
            _spatialGrid = new Dictionary<int, List<Character>>();
            _nearbyResult = new List<Character>(50);
        }

        /// <summary>
        /// 【解答】空間グリッドを更新する
        /// </summary>
        public void UpdateSpatialGrid()
        {
            // 1. 各セルのリストをクリア
            foreach (var cell in _spatialGrid.Values)
            {
                cell.Clear();
            }

            if (_characterManager == null) return;

            // 2. 各キャラクターをセルに追加
            foreach (var character in _characterManager.AliveCharacters)
            {
                if (character == null || !character.IsAlive) continue;

                int cellIndex = GetCellIndex(character.transform.position);

                // セルがなければ作成
                if (!_spatialGrid.TryGetValue(cellIndex, out var cell))
                {
                    cell = new List<Character>(10);
                    _spatialGrid[cellIndex] = cell;
                }

                cell.Add(character);
            }
        }

        /// <summary>
        /// 【解答】座標からセルインデックスを取得する
        /// </summary>
        public int GetCellIndex(Vector3 position)
        {
            // フィールド中心がorigin（0,0）
            int x = Mathf.FloorToInt((position.x + GameConstants.FIELD_HALF_SIZE) / _cellSize);
            int z = Mathf.FloorToInt((position.z + GameConstants.FIELD_HALF_SIZE) / _cellSize);

            // 範囲外をクランプ
            x = Mathf.Clamp(x, 0, _gridWidth - 1);
            z = Mathf.Clamp(z, 0, _gridWidth - 1);

            // 1次元インデックスに変換
            return z * _gridWidth + x;
        }

        /// <summary>
        /// 【解答】指定位置周辺のキャラクターを取得する（O(1)平均）
        /// </summary>
        public List<Character> GetNearbyCharacters(Vector3 position, Character excludeCharacter)
        {
            _nearbyResult.Clear();

            // 中心セルの座標を計算
            int centerX = Mathf.FloorToInt((position.x + GameConstants.FIELD_HALF_SIZE) / _cellSize);
            int centerZ = Mathf.FloorToInt((position.z + GameConstants.FIELD_HALF_SIZE) / _cellSize);

            // 周辺9セル（3x3）をループ
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;

                    // 範囲外チェック
                    if (x < 0 || x >= _gridWidth || z < 0 || z >= _gridWidth)
                        continue;

                    int cellIndex = z * _gridWidth + x;

                    // セルのキャラクターをリストに追加
                    if (_spatialGrid.TryGetValue(cellIndex, out var cell))
                    {
                        foreach (var character in cell)
                        {
                            if (character != null && character != excludeCharacter && character.IsAlive)
                            {
                                _nearbyResult.Add(character);
                            }
                        }
                    }
                }
            }

            return _nearbyResult;
        }

        // ================================================================
        // 修正箇所②: 処理順序の最適化【解答】
        // ================================================================

        /// <summary>
        /// 【解答】攻撃シーケンスを実行する（最適化版）
        /// </summary>
        public void ExecuteAttackSequence(Character attacker)
        {
            if (attacker == null || !attacker.IsAlive) return;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // ============================================================
            // 【最適化された呼び出し順序】
            // ============================================================

            // Step A: 近傍のキャラクターのみ取得（空間分割でO(1)）
            List<Character> candidates = GetNearbyCharacters(attacker.transform.position, attacker);

            // Step B: 距離条件でフィルタ（軽い処理を先に）
            candidates = FilterByDistance(candidates, attacker, _maxAttackDistance);

            // Step C: HP条件でフィルタ（軽い処理）
            candidates = FilterByHP(candidates, _minTargetHP, _maxTargetHP);

            // Step D: 経路探索でソート（重い処理は最後、少数のみ）
            candidates = SortByPathfindingDistance(candidates, attacker);

            // Step E: 先頭を攻撃
            AttackFirst(candidates, attacker);

            // ============================================================

            stopwatch.Stop();
            _lastExecutionTimeMs = (float)stopwatch.Elapsed.TotalMilliseconds;
            _lastProcessedCount = candidates.Count;
        }

        // ================================================================
        // 処理ブロック
        // ================================================================

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

        public void AttackFirst(List<Character> characters, Character attacker)
        {
            if (characters.Count == 0 || attacker == null) return;

            var target = characters[0];
            if (target != null && target.IsAlive)
            {
                attacker.Attack(target);
            }
        }

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

        private void Update()
        {
            // 空間グリッドを毎フレーム更新
            UpdateSpatialGrid();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_characterManager != null && _characterManager.AliveCharacters.Count > 0)
                {
                    var attacker = _characterManager.AliveCharacters[0];
                    ExecuteAttackSequence(attacker);
                    Debug.Log($"[Solution] ExecuteAttackSequence: {_lastExecutionTimeMs:F2}ms, Candidates: {_lastProcessedCount}");
                }
            }
        }

        public float GetLastExecutionTimeMs() => _lastExecutionTimeMs;
        public int GetLastProcessedCount() => _lastProcessedCount;
    }
}
