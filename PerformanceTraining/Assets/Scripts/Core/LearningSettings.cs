using UnityEngine;

namespace PerformanceTraining.Core
{
    /// <summary>
    /// 学習課題の進行状況を管理するScriptableObject
    /// 各最適化機能のON/OFFを切り替えて効果を確認できる
    /// </summary>
    [CreateAssetMenu(fileName = "LearningSettings", menuName = "PerformanceTraining/Learning Settings")]
    public class LearningSettings : ScriptableObject
    {
        [Header("=== 課題1: メモリ最適化 ===")]
        [Tooltip("Step 1: オブジェクトプールを使用する")]
        public bool useObjectPool = false;

        [Tooltip("Step 2: StringBuilderを使用する")]
        public bool useStringBuilder = false;

        [Tooltip("Step 3: デリゲートをキャッシュする")]
        public bool useDelegateCache = false;

        [Tooltip("Step 4: コレクションを再利用する")]
        public bool useCollectionReuse = false;

        [Header("=== 課題2: CPU最適化 ===")]
        [Tooltip("Step 1: 空間分割を使用する")]
        public bool useSpatialPartition = false;

        [Tooltip("Step 2: 更新分散を使用する")]
        public bool useStaggeredUpdate = false;

        [Tooltip("Step 3: sqrMagnitudeを使用する")]
        public bool useSqrMagnitude = false;

        [Header("=== 課題3: トレードオフ ===")]
        [Tooltip("3-A: 近傍キャッシュを使用する")]
        public bool useNeighborCache = false;

        [Tooltip("3-B: AI判断キャッシュを使用する")]
        public bool useDecisionCache = false;

        [Header("=== 課題3: 発展課題 ===")]
        [Tooltip("三角関数LUTを使用する（発展）")]
        public bool useTrigLUT = false;

        [Tooltip("可視性マップを使用する（発展）")]
        public bool useVisibilityMap = false;

        [Header("=== 課題4: グラフィクス ===")]
        [Tooltip("GPU Instancingが有効")]
        public bool gpuInstancingEnabled = false;

        [Tooltip("LODが設定済み")]
        public bool lodConfigured = false;

        [Tooltip("カリング設定済み")]
        public bool cullingConfigured = false;

        [Header("=== デバッグ設定 ===")]
        [Tooltip("パフォーマンスモニターを表示する")]
        public bool showPerformanceMonitor = true;

        [Tooltip("空間分割グリッドをデバッグ表示する")]
        public bool showSpatialGrid = false;

        /// <summary>
        /// 全ての最適化が有効かどうか
        /// </summary>
        public bool AllOptimizationsEnabled =>
            useObjectPool && useStringBuilder && useDelegateCache && useCollectionReuse &&
            useSpatialPartition && useStaggeredUpdate && useSqrMagnitude &&
            useNeighborCache && useDecisionCache;

        /// <summary>
        /// トレードオフ最適化が全て有効かどうか
        /// </summary>
        public bool AllTradeoffOptimizationsEnabled =>
            useNeighborCache && useDecisionCache;

        /// <summary>
        /// メモリ最適化が全て有効かどうか
        /// </summary>
        public bool AllMemoryOptimizationsEnabled =>
            useObjectPool && useStringBuilder && useDelegateCache && useCollectionReuse;

        /// <summary>
        /// CPU最適化が全て有効かどうか
        /// </summary>
        public bool AllCPUOptimizationsEnabled =>
            useSpatialPartition && useStaggeredUpdate && useSqrMagnitude;

        /// <summary>
        /// 設定を初期状態（全てOFF）にリセット
        /// </summary>
        public void ResetToDefault()
        {
            useObjectPool = false;
            useStringBuilder = false;
            useDelegateCache = false;
            useCollectionReuse = false;
            useSpatialPartition = false;
            useStaggeredUpdate = false;
            useSqrMagnitude = false;
            useNeighborCache = false;
            useDecisionCache = false;
            useTrigLUT = false;
            useVisibilityMap = false;
            gpuInstancingEnabled = false;
            lodConfigured = false;
            cullingConfigured = false;
        }

        /// <summary>
        /// 全ての最適化を有効にする
        /// </summary>
        public void EnableAllOptimizations()
        {
            useObjectPool = true;
            useStringBuilder = true;
            useDelegateCache = true;
            useCollectionReuse = true;
            useSpatialPartition = true;
            useStaggeredUpdate = true;
            useSqrMagnitude = true;
            useNeighborCache = true;
            useDecisionCache = true;
            // 発展課題は手動で有効化
            // useTrigLUT = true;
            // useVisibilityMap = true;
        }
    }
}
