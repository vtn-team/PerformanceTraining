using System.Collections.Generic;
using UnityEngine;
using MassacreDojo.Core;
using EnemyClass = MassacreDojo.Enemy.Enemy;
using EnemySystem = MassacreDojo.Enemy.EnemySystem;

namespace MassacreDojo.Solutions.CPU
{
    /// <summary>
    /// 【解答】課題2: CPU計算キャッシュ
    ///
    /// このファイルは教員用の解答です。
    /// 学生には見せないでください。
    /// </summary>
    public class CPUOptimization_Solution : MonoBehaviour
    {
        // ========================================================
        // Step 1: 空間分割【解答】
        // ========================================================

        // 【解答】グリッド用のDictionary
        private Dictionary<int, List<EnemyClass>> _spatialGrid;

        // 【解答】再利用リスト（GetNearby用）
        private List<EnemyClass> _nearbyResult;

        private float _cellSize = GameConstants.CELL_SIZE;
        private int _gridWidth = GameConstants.GRID_SIZE;


        private void Awake()
        {
            // 【解答】事前初期化
            _spatialGrid = new Dictionary<int, List<EnemyClass>>();
            _nearbyResult = new List<EnemyClass>(50);
        }


        public void UpdateSpatialGrid(List<EnemyClass> enemies)
        {
            // 【解答】グリッドを更新

            // 1. 各セルのリストをクリア
            foreach (var cell in _spatialGrid.Values)
            {
                cell.Clear();
            }

            // 2. 各敵をセルに追加
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;

                int cellIndex = GetCellIndex(enemy.transform.position);

                // セルがなければ作成
                if (!_spatialGrid.TryGetValue(cellIndex, out var cell))
                {
                    cell = new List<EnemyClass>(10);
                    _spatialGrid[cellIndex] = cell;
                }

                cell.Add(enemy);
            }
        }


        public int GetCellIndex(Vector3 position)
        {
            // 【解答】座標からセルインデックスを計算

            // フィールド中心がorigin（0,0）
            int x = Mathf.FloorToInt((position.x + GameConstants.FIELD_HALF_SIZE) / _cellSize);
            int z = Mathf.FloorToInt((position.z + GameConstants.FIELD_HALF_SIZE) / _cellSize);

            // 範囲外をクランプ
            x = Mathf.Clamp(x, 0, _gridWidth - 1);
            z = Mathf.Clamp(z, 0, _gridWidth - 1);

            // 1次元インデックスに変換
            return z * _gridWidth + x;
        }


        public List<EnemyClass> QueryNearbyEnemies(Vector3 position)
        {
            // 【解答】周辺9セルから敵を取得

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

                    // セルの敵をリストに追加
                    if (_spatialGrid.TryGetValue(cellIndex, out var cell))
                    {
                        foreach (var enemy in cell)
                        {
                            _nearbyResult.Add(enemy);
                        }
                    }
                }
            }

            return _nearbyResult;
        }


        // ========================================================
        // Step 2: 更新分散【解答】
        // ========================================================

        public bool ShouldUpdateThisFrame(int group, int frameCount)
        {
            // 【解答】グループに応じて更新タイミングを分散
            return frameCount % GameConstants.AI_UPDATE_GROUPS == group;
        }


        // ========================================================
        // Step 3: 距離計算【解答】
        // ========================================================

        public float CalculateDistanceSqr(Vector3 a, Vector3 b)
        {
            // 【解答】sqrMagnitudeを使用
            return (a - b).sqrMagnitude;
        }


        public bool IsWithinDistance(Vector3 a, Vector3 b, float maxDistance)
        {
            // 【解答】sqrMagnitudeで距離判定
            float maxDistSqr = maxDistance * maxDistance;
            return (a - b).sqrMagnitude <= maxDistSqr;
        }


        // ========================================================
        // デバッグ表示
        // ========================================================

        private void OnDrawGizmos()
        {
            if (GameManager.Instance?.Settings?.showSpatialGrid == true && _spatialGrid != null)
            {
                foreach (var kvp in _spatialGrid)
                {
                    int index = kvp.Key;
                    var enemies = kvp.Value;

                    if (enemies.Count == 0) continue;

                    int x = index % _gridWidth;
                    int z = index / _gridWidth;

                    float worldX = x * _cellSize - GameConstants.FIELD_HALF_SIZE + _cellSize / 2f;
                    float worldZ = z * _cellSize - GameConstants.FIELD_HALF_SIZE + _cellSize / 2f;

                    // 敵の数に応じて色を変える
                    float intensity = Mathf.Min(enemies.Count / 10f, 1f);
                    Gizmos.color = new Color(intensity, 1f - intensity, 0, 0.5f);

                    Gizmos.DrawCube(
                        new Vector3(worldX, 0.1f, worldZ),
                        new Vector3(_cellSize * 0.9f, 0.2f, _cellSize * 0.9f)
                    );
                }
            }
        }
    }
}
