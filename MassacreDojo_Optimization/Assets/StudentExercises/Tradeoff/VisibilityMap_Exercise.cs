using UnityEngine;
using PerformanceTraining.Core;

namespace StudentExercises.Tradeoff
{
    /// <summary>
    /// 【課題3-B: 可視性マップ】
    ///
    /// 目標: メモリを消費してRaycast計算を削減する
    ///
    /// トレードオフ:
    /// - メモリ: グリッドサイズ²のbool配列
    ///   例: 50x50 = 2,500 bool = 約2.5KB
    /// - CPU: Raycast完全不要（10-100倍高速）
    /// - 精度: 空間の離散化による誤差
    ///
    /// 使用場面:
    /// - 敵のプレイヤー視認判定
    /// - AI意思決定（見えている敵への反応）
    ///
    /// 注意:
    /// - 静的な障害物のみに有効
    /// - 動的障害物がある場合は定期的な再計算が必要
    ///
    /// TODO: 2Dグリッドで各セルの可視性を事前計算してください
    /// </summary>
    public class VisibilityMap_Exercise : MonoBehaviour
    {
        // ========================================================
        // 可視性マップ
        // ========================================================

        // TODO: ここに可視性マップを宣言してください
        // 方法1: 2次元配列
        // private bool[,] _visibilityMap;
        //
        // 方法2: 1次元配列（よりキャッシュ効率が良い）
        // private bool[] _visibilityMap;

        [Header("設定")]
        [SerializeField] private int _gridSize = GameConstants.VISIBILITY_GRID_SIZE;
        [SerializeField] private LayerMask _obstacleLayer;
        [SerializeField] private float _rayHeight = 1f;

        private float _cellSize;
        private bool _isInitialized = false;


        /// <summary>
        /// 可視性マップを初期化・計算する
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _cellSize = GameConstants.FIELD_SIZE / _gridSize;

            // TODO: 可視性マップを初期化してください
            // ヒント:
            // 1. _visibilityMap = new bool[_gridSize * _gridSize, _gridSize * _gridSize];
            //    または
            //    _visibilityMap = new bool[_gridSize, _gridSize, _gridSize, _gridSize];
            //
            // 2. 全セルペアについてRaycastで可視性を計算
            //    for (int fromX = 0; fromX < _gridSize; fromX++)
            //    for (int fromZ = 0; fromZ < _gridSize; fromZ++)
            //    for (int toX = 0; toX < _gridSize; toX++)
            //    for (int toZ = 0; toZ < _gridSize; toZ++)
            //    {
            //        Vector3 fromPos = CellToWorld(fromX, fromZ);
            //        Vector3 toPos = CellToWorld(toX, toZ);
            //        bool visible = CheckVisibilityRaycast(fromPos, toPos);
            //        // マップに保存
            //    }

            Debug.Log($"VisibilityMap initialized: {_gridSize}x{_gridSize} grid");
            _isInitialized = true;
        }

        private void Awake()
        {
            // 注: 初期化は重いので、必要なタイミングで呼び出す
            // Initialize();
        }


        /// <summary>
        /// ワールド座標からセルインデックスを計算する
        /// </summary>
        /// <param name="worldPos">ワールド座標</param>
        /// <param name="x">出力: X インデックス</param>
        /// <param name="z">出力: Z インデックス</param>
        public void WorldToCell(Vector3 worldPos, out int x, out int z)
        {
            // TODO: ワールド座標をセルインデックスに変換してください
            // ヒント:
            // 1. フィールドの左下を原点として計算
            // 2. x = (worldPos.x + FIELD_HALF_SIZE) / _cellSize
            // 3. 範囲外はクランプ

            // 仮実装 - これを置き換えてください
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
        /// セルインデックスからワールド座標（セル中心）を計算する
        /// </summary>
        /// <param name="x">X インデックス</param>
        /// <param name="z">Z インデックス</param>
        /// <returns>セル中心のワールド座標</returns>
        public Vector3 CellToWorld(int x, int z)
        {
            // TODO: セルインデックスをワールド座標に変換してください
            // ヒント:
            // worldX = x * _cellSize - FIELD_HALF_SIZE + _cellSize / 2

            // 仮実装 - これを置き換えてください
            float worldX = x * _cellSize - GameConstants.FIELD_HALF_SIZE + _cellSize / 2f;
            float worldZ = z * _cellSize - GameConstants.FIELD_HALF_SIZE + _cellSize / 2f;
            return new Vector3(worldX, _rayHeight, worldZ);
        }


        /// <summary>
        /// 2点間の可視性を取得する（マップ参照）
        /// </summary>
        /// <param name="from">始点</param>
        /// <param name="to">終点</param>
        /// <returns>可視ならtrue</returns>
        public bool IsVisible(Vector3 from, Vector3 to)
        {
            // TODO: 可視性マップから値を取得してください
            // ヒント:
            // 1. from, to をセルインデックスに変換
            // 2. マップから値を取得して返す
            // 3. マップが初期化されていない場合はフォールバック（Raycast）

            // 仮実装（Raycast使用 - 問題あり）- これを置き換えてください
            return CheckVisibilityRaycast(from, to);
        }


        /// <summary>
        /// Raycastで可視性をチェックする（初期化時・フォールバック用）
        /// </summary>
        /// <param name="from">始点</param>
        /// <param name="to">終点</param>
        /// <returns>可視ならtrue</returns>
        private bool CheckVisibilityRaycast(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            float distance = direction.magnitude;

            // 高さを調整
            from.y = _rayHeight;
            to.y = _rayHeight;

            // Raycastで障害物をチェック
            if (Physics.Raycast(from, direction.normalized, distance, _obstacleLayer))
            {
                return false; // 障害物に遮られた
            }

            return true; // 可視
        }


        /// <summary>
        /// 可視性マップを再計算する（動的障害物対応）
        /// </summary>
        public void Recalculate()
        {
            _isInitialized = false;
            Initialize();
        }


        // ========================================================
        // デバッグ・計測用
        // ========================================================

        /// <summary>
        /// マップのメモリ使用量を計算する（バイト）
        /// </summary>
        /// <returns>メモリ使用量</returns>
        public int GetMemoryUsageBytes()
        {
            // 簡略化: セルペアごとに1バイト（実際はboolの配列実装による）
            // 完全なマップ: gridSize^4 バイト
            // 対称性を利用: gridSize^4 / 2 バイト
            int totalCells = _gridSize * _gridSize;
            return totalCells * totalCells / 8; // bit単位で保存した場合
        }

        /// <summary>
        /// 統計情報を表示する
        /// </summary>
        public void LogStats()
        {
            int memBytes = GetMemoryUsageBytes();
            Debug.Log($"VisibilityMap Stats:");
            Debug.Log($"  Grid Size: {_gridSize}x{_gridSize}");
            Debug.Log($"  Cell Size: {_cellSize:F2}m");
            Debug.Log($"  Memory (estimated): {memBytes} bytes ({memBytes / 1024f:F2} KB)");
        }


        private void OnDrawGizmosSelected()
        {
            if (!_isInitialized) return;

            // グリッドを可視化
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
