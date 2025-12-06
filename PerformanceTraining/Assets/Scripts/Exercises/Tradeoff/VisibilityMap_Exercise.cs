using UnityEngine;
using PerformanceTraining.Core;

namespace PerformanceTraining.Exercises.Tradeoff
{
    /// <summary>
    /// 【課題3-D: 可視性マップ】
    ///
    /// 目標: メモリを消費してRaycast計算を削減する
    ///
    /// トレードオフ:
    /// - メモリ使用量: グリッドサイズ²（例: 50×50 = 約2.5KB）
    /// - CPU削減効果: 10-100倍高速化（Raycast完全不要）
    /// - 代償: 空間の離散化による精度低下
    ///
    /// 確認方法:
    /// - Raycast呼び出し回数を比較
    ///
    /// 注意:
    /// - 静的障害物のみに有効
    /// - 動的障害物がある場合は定期的な再計算が必要
    /// </summary>
    public class VisibilityMap_Exercise : MonoBehaviour
    {
        // ========================================================
        // 可視性マップ
        // ========================================================

        // TODO: 可視性マップを宣言
        // 方法1: 2次元配列 bool[,]
        // 方法2: 1次元配列 bool[]（キャッシュ効率が良い）


        [Header("設定")]
        [SerializeField] private int _gridSize = GameConstants.VISIBILITY_GRID_SIZE;
        [SerializeField] private LayerMask _obstacleLayer;
        [SerializeField] private float _rayHeight = 1f;

        private float _cellSize;
        private bool _isInitialized = false;


        // ========================================================
        // 初期化
        // ========================================================

        public void Initialize()
        {
            if (_isInitialized) return;

            _cellSize = GameConstants.FIELD_SIZE / _gridSize;

            // TODO: 可視性マップを初期化
            // 全セルペアについてRaycastで可視性を計算

            Debug.Log($"VisibilityMap initialized: {_gridSize}x{_gridSize} grid");
            _isInitialized = true;
        }

        private void Awake()
        {
            // 注: 初期化は重いので、必要なタイミングで呼び出す
        }


        // ========================================================
        // 座標変換
        // ========================================================

        /// <summary>
        /// ワールド座標からセルインデックスを計算する
        /// </summary>
        public void WorldToCell(Vector3 worldPos, out int x, out int z)
        {
            x = Mathf.Clamp(
                Mathf.FloorToInt((worldPos.x + GameConstants.FIELD_HALF_SIZE) / _cellSize),
                0, _gridSize - 1
            );
            z = Mathf.Clamp(
                Mathf.FloorToInt((worldPos.z + GameConstants.FIELD_HALF_SIZE) / _cellSize),
                0, _gridSize - 1
            );
        }

        /// <summary>
        /// セルインデックスからワールド座標を計算する
        /// </summary>
        public Vector3 CellToWorld(int x, int z)
        {
            float worldX = x * _cellSize - GameConstants.FIELD_HALF_SIZE + _cellSize / 2f;
            float worldZ = z * _cellSize - GameConstants.FIELD_HALF_SIZE + _cellSize / 2f;
            return new Vector3(worldX, _rayHeight, worldZ);
        }


        // ========================================================
        // 可視性判定
        // ========================================================

        /// <summary>
        /// 2点間の可視性を取得する
        /// </summary>
        public bool IsVisible(Vector3 from, Vector3 to)
        {
            // 現在の実装（問題あり）: 毎回Raycast
            // TODO: マップから値を取得する
            return CheckVisibilityRaycast(from, to);
        }

        /// <summary>
        /// Raycastで可視性をチェック（初期化時に使用）
        /// </summary>
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


        // ========================================================
        // 再計算
        // ========================================================

        public void Recalculate()
        {
            _isInitialized = false;
            Initialize();
        }


        // ========================================================
        // デバッグ
        // ========================================================

        public int GetMemoryUsageBytes()
        {
            int totalCells = _gridSize * _gridSize;
            return totalCells * totalCells / 8; // bit単位
        }

        public void LogStats()
        {
            int memBytes = GetMemoryUsageBytes();
            Debug.Log($"VisibilityMap - Grid: {_gridSize}x{_gridSize}, Memory: {memBytes / 1024f:F2} KB");
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
