using UnityEngine;
using PerformanceTraining.Core;

namespace PerformanceTraining.Solutions.Tradeoff
{
    /// <summary>
    /// 【解答】課題3-B: 可視性マップ
    ///
    /// このファイルは教員用の解答です。
    /// 学生には見せないでください。
    /// </summary>
    public class VisibilityMap_Solution : MonoBehaviour
    {
        // ========================================================
        // 可視性マップ【解答】
        // ========================================================

        // 【解答】1次元配列で可視性マップを保持
        // インデックス計算: (fromZ * gridSize + fromX) * gridSize * gridSize + (toZ * gridSize + toX)
        // 対称性を利用して半分に圧縮することも可能だが、ここでは単純な実装
        private bool[] _visibilityMap;

        // 【解答】より効率的なビット配列版
        // private System.Collections.BitArray _visibilityBits;

        [Header("設定")]
        [SerializeField] private int _gridSize = GameConstants.VISIBILITY_GRID_SIZE;
        [SerializeField] private LayerMask _obstacleLayer;
        [SerializeField] private float _rayHeight = 1f;

        private float _cellSize;
        private bool _isInitialized = false;
        private int _totalCells;


        public void Initialize()
        {
            if (_isInitialized) return;

            _cellSize = GameConstants.FIELD_SIZE / _gridSize;
            _totalCells = _gridSize * _gridSize;

            // 【解答】可視性マップを初期化
            // 全セルペアについて可視性を計算
            _visibilityMap = new bool[_totalCells * _totalCells];

            int raycastCount = 0;

            for (int fromX = 0; fromX < _gridSize; fromX++)
            {
                for (int fromZ = 0; fromZ < _gridSize; fromZ++)
                {
                    Vector3 fromPos = CellToWorld(fromX, fromZ);
                    int fromIndex = fromZ * _gridSize + fromX;

                    for (int toX = 0; toX < _gridSize; toX++)
                    {
                        for (int toZ = 0; toZ < _gridSize; toZ++)
                        {
                            Vector3 toPos = CellToWorld(toX, toZ);
                            int toIndex = toZ * _gridSize + toX;

                            // 同じセルは常に可視
                            if (fromIndex == toIndex)
                            {
                                _visibilityMap[fromIndex * _totalCells + toIndex] = true;
                                continue;
                            }

                            // 対称性を利用: 既に計算済みなら結果をコピー
                            if (toIndex < fromIndex)
                            {
                                _visibilityMap[fromIndex * _totalCells + toIndex] =
                                    _visibilityMap[toIndex * _totalCells + fromIndex];
                                continue;
                            }

                            // Raycastで可視性をチェック
                            bool visible = CheckVisibilityRaycast(fromPos, toPos);
                            _visibilityMap[fromIndex * _totalCells + toIndex] = visible;
                            raycastCount++;
                        }
                    }
                }
            }

            Debug.Log($"VisibilityMap initialized:");
            Debug.Log($"  Grid: {_gridSize}x{_gridSize} ({_totalCells} cells)");
            Debug.Log($"  Raycasts: {raycastCount}");
            Debug.Log($"  Memory: {GetMemoryUsageBytes()} bytes ({GetMemoryUsageBytes() / 1024f:F2} KB)");

            _isInitialized = true;
        }


        private void Awake()
        {
            // 初期化は重いので、Startで呼び出すか、明示的に呼び出す
        }


        private void Start()
        {
            // ゲーム開始時に初期化
            Initialize();
        }


        public void WorldToCell(Vector3 worldPos, out int x, out int z)
        {
            // 【解答】ワールド座標をセルインデックスに変換
            x = Mathf.Clamp(
                Mathf.FloorToInt((worldPos.x + GameConstants.FIELD_HALF_SIZE) / _cellSize),
                0, _gridSize - 1
            );
            z = Mathf.Clamp(
                Mathf.FloorToInt((worldPos.z + GameConstants.FIELD_HALF_SIZE) / _cellSize),
                0, _gridSize - 1
            );
        }


        public Vector3 CellToWorld(int x, int z)
        {
            // 【解答】セルインデックスをワールド座標に変換
            float worldX = x * _cellSize - GameConstants.FIELD_HALF_SIZE + _cellSize / 2f;
            float worldZ = z * _cellSize - GameConstants.FIELD_HALF_SIZE + _cellSize / 2f;
            return new Vector3(worldX, _rayHeight, worldZ);
        }


        public bool IsVisible(Vector3 from, Vector3 to)
        {
            // 【解答】可視性マップから値を取得

            if (!_isInitialized)
            {
                // フォールバック: Raycast
                return CheckVisibilityRaycast(from, to);
            }

            // セルインデックスを計算
            WorldToCell(from, out int fromX, out int fromZ);
            WorldToCell(to, out int toX, out int toZ);

            int fromIndex = fromZ * _gridSize + fromX;
            int toIndex = toZ * _gridSize + toX;

            // マップから値を取得
            return _visibilityMap[fromIndex * _totalCells + toIndex];
        }


        private bool CheckVisibilityRaycast(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            float distance = direction.magnitude;

            from.y = _rayHeight;
            to.y = _rayHeight;

            if (Physics.Raycast(from, direction.normalized, distance, _obstacleLayer))
            {
                return false;
            }

            return true;
        }


        public void Recalculate()
        {
            _isInitialized = false;
            Initialize();
        }


        // ========================================================
        // デバッグ・計測用【解答】
        // ========================================================

        public int GetMemoryUsageBytes()
        {
            // bool配列のメモリ使用量
            // 実際には1 boolあたり1バイト使用
            return _totalCells * _totalCells;
        }


        public void LogStats()
        {
            if (!_isInitialized)
            {
                Debug.Log("VisibilityMap not initialized");
                return;
            }

            // 可視セルペアの統計
            int visibleCount = 0;
            for (int i = 0; i < _visibilityMap.Length; i++)
            {
                if (_visibilityMap[i]) visibleCount++;
            }

            float visibleRatio = (float)visibleCount / _visibilityMap.Length * 100f;

            Debug.Log($"VisibilityMap Stats:");
            Debug.Log($"  Grid Size: {_gridSize}x{_gridSize}");
            Debug.Log($"  Cell Size: {_cellSize:F2}m");
            Debug.Log($"  Total Pairs: {_visibilityMap.Length}");
            Debug.Log($"  Visible Pairs: {visibleCount} ({visibleRatio:F1}%)");
            Debug.Log($"  Memory: {GetMemoryUsageBytes()} bytes ({GetMemoryUsageBytes() / 1024f:F2} KB)");
        }


        public void Benchmark(int iterations = 10000)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("VisibilityMap not initialized for benchmark");
                return;
            }

            // ランダムな位置ペアを生成
            Vector3[] fromPositions = new Vector3[iterations];
            Vector3[] toPositions = new Vector3[iterations];

            float limit = GameConstants.FIELD_HALF_SIZE - 1f;
            for (int i = 0; i < iterations; i++)
            {
                fromPositions[i] = new Vector3(
                    Random.Range(-limit, limit),
                    _rayHeight,
                    Random.Range(-limit, limit)
                );
                toPositions[i] = new Vector3(
                    Random.Range(-limit, limit),
                    _rayHeight,
                    Random.Range(-limit, limit)
                );
            }

            bool dummy = false;

            // Raycast ベンチマーク
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                dummy ^= CheckVisibilityRaycast(fromPositions[i], toPositions[i]);
            }
            sw.Stop();
            float raycastTime = sw.ElapsedMilliseconds;

            // Map ベンチマーク
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                dummy ^= IsVisible(fromPositions[i], toPositions[i]);
            }
            sw.Stop();
            float mapTime = sw.ElapsedMilliseconds;

            Debug.Log($"VisibilityMap Benchmark ({iterations} iterations):");
            Debug.Log($"  Raycast: {raycastTime}ms");
            Debug.Log($"  Map:     {mapTime}ms");
            Debug.Log($"  Speedup: {raycastTime / Mathf.Max(mapTime, 0.001f):F2}x");

            if (dummy) Debug.Log(""); // 最適化防止
        }


        private void OnDrawGizmosSelected()
        {
            if (!_isInitialized) return;

            Gizmos.color = new Color(0, 0, 1, 0.2f);

            for (int x = 0; x < _gridSize; x++)
            {
                for (int z = 0; z < _gridSize; z++)
                {
                    Vector3 pos = CellToWorld(x, z);
                    pos.y = 0.1f;
                    Gizmos.DrawWireCube(pos, new Vector3(_cellSize, 0.1f, _cellSize));
                }
            }
        }
    }
}
