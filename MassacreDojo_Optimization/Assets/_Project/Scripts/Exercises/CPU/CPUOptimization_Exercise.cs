using System.Collections.Generic;
using UnityEngine;
using MassacreDojo.Core;
using EnemyClass = MassacreDojo.Enemy.Enemy;

namespace MassacreDojo.Exercises.CPU
{
    /// <summary>
    /// 【課題2: CPU計算最適化】
    ///
    /// 目標: CPU負荷の高い処理を最適化する
    ///
    /// 確認方法:
    /// - Profiler > CPU > Frame Time を確認
    /// - 目標: 40+ ms → 16ms以下（60fps維持）
    ///
    /// このクラスには3つのStepがあります。
    /// 各Stepのメソッドを実装してください。
    /// </summary>
    public class CPUOptimization_Exercise : MonoBehaviour
    {
        // ========================================================
        // Step 1: 空間分割（Spatial Partitioning）
        // ========================================================
        // 問題: 全敵との距離計算はO(n²)の計算量
        // 解決: グリッドで空間を分割し、近傍セルのみ検索する

        // TODO: グリッド用のデータ構造を宣言


        private float _cellSize = GameConstants.CELL_SIZE;
        private int _gridWidth = GameConstants.GRID_SIZE;


        /// <summary>
        /// 空間グリッドを更新する
        /// </summary>
        public void UpdateSpatialGrid(List<EnemyClass> enemies)
        {
            // TODO: 各敵をグリッドに登録する
            // 1. グリッドをクリア
            // 2. 各敵の位置からセルを計算
            // 3. 該当セルに敵を追加
        }

        /// <summary>
        /// 座標からセルインデックスを取得する
        /// </summary>
        public int GetCellIndex(Vector3 position)
        {
            // TODO: ワールド座標を1次元のセルインデックスに変換
            // フィールド範囲: -FIELD_HALF_SIZE ～ +FIELD_HALF_SIZE
            return 0;
        }

        /// <summary>
        /// 指定位置周辺の敵を取得する
        /// </summary>
        public List<EnemyClass> QueryNearbyEnemies(Vector3 position)
        {
            // TODO: 周辺9セル（3x3）から敵を取得
            return new List<EnemyClass>();
        }


        // ========================================================
        // Step 2: 更新分散（Staggered Update）
        // ========================================================
        // 問題: 全敵が毎フレーム重い処理を実行すると負荷が集中する
        // 解決: グループに分けて更新タイミングをずらす

        /// <summary>
        /// このフレームで更新すべきかどうかを判定する
        /// </summary>
        /// <param name="group">敵のグループ番号（0～AI_UPDATE_GROUPS-1）</param>
        /// <param name="frameCount">現在のフレーム番号</param>
        public bool ShouldUpdateThisFrame(int group, int frameCount)
        {
            // 現在の実装（問題あり）: 全員が毎フレーム更新
            // TODO: グループごとに更新フレームを分散させる
            return true;
        }


        // ========================================================
        // Step 3: 距離計算の最適化
        // ========================================================
        // 問題: Vector3.Distance()は内部で平方根（Sqrt）を計算する
        // 解決: sqrMagnitudeを使用して平方根計算を省略する

        /// <summary>
        /// 2点間の距離の2乗を計算する
        /// </summary>
        public float CalculateDistanceSqr(Vector3 a, Vector3 b)
        {
            // 現在の実装（問題あり）: 平方根を計算している
            return Vector3.Distance(a, b);
        }

        /// <summary>
        /// 2点間の距離が指定値以下かどうかを判定する
        /// </summary>
        public bool IsWithinDistance(Vector3 a, Vector3 b, float maxDistance)
        {
            // 現在の実装（問題あり）: 平方根を計算している
            // TODO: 平方根を計算せずに距離判定する
            return Vector3.Distance(a, b) <= maxDistance;
        }


        // ========================================================
        // デバッグ表示
        // ========================================================

        private void OnDrawGizmos()
        {
            if (GameManager.Instance?.Settings?.showSpatialGrid == true)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);

                for (int x = 0; x < _gridWidth; x++)
                {
                    for (int z = 0; z < _gridWidth; z++)
                    {
                        float worldX = x * _cellSize - GameConstants.FIELD_HALF_SIZE + _cellSize / 2f;
                        float worldZ = z * _cellSize - GameConstants.FIELD_HALF_SIZE + _cellSize / 2f;

                        Gizmos.DrawWireCube(
                            new Vector3(worldX, 0, worldZ),
                            new Vector3(_cellSize, 0.1f, _cellSize)
                        );
                    }
                }
            }
        }
    }
}
