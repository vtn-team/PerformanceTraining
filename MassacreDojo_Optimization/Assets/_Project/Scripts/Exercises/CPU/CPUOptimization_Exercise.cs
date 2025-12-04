using System.Collections.Generic;
using UnityEngine;
using MassacreDojo.Core;
using EnemyClass = MassacreDojo.Enemy.Enemy;

namespace MassacreDojo.Exercises.CPU
{
    /// <summary>
    /// 【課題2: CPU計算キャッシュ】
    ///
    /// 目標: CPU負荷の高い処理を最適化する
    ///
    /// このクラスには3つのStepがあります。
    /// 各Stepの「TODO:」コメントを見つけて、コードを実装してください。
    ///
    /// 確認方法:
    /// - Profiler > CPU > Frame Time を確認
    /// - Before: 40ms+ → After: 15ms以下
    /// </summary>
    public class CPUOptimization_Exercise : MonoBehaviour
    {
        // ========================================================
        // Step 1: 空間分割（Spatial Partitioning）
        // ========================================================
        // 目的: O(n²)の総当たり検索をO(1)に近づける
        //
        // TODO: グリッドベースの空間分割を実装してください

        // TODO: ここにグリッド用のDictionaryを宣言してください
        // private Dictionary<int, List<EnemyClass>> _spatialGrid;

        // グリッドの設定
        private float _cellSize = GameConstants.CELL_SIZE;
        private int _gridWidth = GameConstants.GRID_SIZE;


        /// <summary>
        /// 空間グリッドを更新する
        /// </summary>
        /// <param name="enemies">全敵リスト</param>
        public void UpdateSpatialGrid(List<EnemyClass> enemies)
        {
            // TODO: グリッドを更新してください
            // ヒント:
            // 1. _spatialGrid が null なら初期化
            // 2. 各セルの List を Clear()
            // 3. 各敵の位置からセルインデックスを計算
            // 4. 該当セルの List に敵を追加

            // 仮実装（何もしない）- これを置き換えてください
        }

        /// <summary>
        /// 座標からセルインデックスを取得する
        /// </summary>
        /// <param name="position">ワールド座標</param>
        /// <returns>セルインデックス（1次元化）</returns>
        public int GetCellIndex(Vector3 position)
        {
            // TODO: 座標からセルインデックスを計算してください
            // ヒント:
            // 1. フィールド中心がorigin（0,0）として計算
            // 2. x = (position.x + FIELD_HALF_SIZE) / _cellSize
            // 3. z = (position.z + FIELD_HALF_SIZE) / _cellSize
            // 4. index = z * _gridWidth + x
            // 5. 範囲外はクランプする

            // 仮実装 - これを置き換えてください
            return 0;
        }

        /// <summary>
        /// 指定位置周辺の敵を取得する
        /// </summary>
        /// <param name="position">中心座標</param>
        /// <returns>周辺の敵リスト</returns>
        public List<EnemyClass> QueryNearbyEnemies(Vector3 position)
        {
            // TODO: 周辺9セル（3x3）から敵を取得してください
            // ヒント:
            // 1. 結果用の List を用意（再利用推奨）
            // 2. 中心セルのx,zインデックスを計算
            // 3. x-1〜x+1, z-1〜z+1 の9セルをループ
            // 4. 各セルの敵をリストに追加
            // 5. 範囲外チェックを忘れずに

            // 仮実装（空リストを返す）- これを置き換えてください
            return new List<EnemyClass>();
        }


        // ========================================================
        // Step 2: 更新分散（Staggered Update）
        // ========================================================
        // 目的: 全敵が毎フレーム更新するのを避け、負荷を分散する
        //
        // TODO: グループごとに更新タイミングを分散してください

        /// <summary>
        /// このフレームで更新すべきかどうかを判定する
        /// </summary>
        /// <param name="group">敵のグループ番号（0〜AI_UPDATE_GROUPS-1）</param>
        /// <param name="frameCount">現在のフレーム番号</param>
        /// <returns>更新すべきならtrue</returns>
        public bool ShouldUpdateThisFrame(int group, int frameCount)
        {
            // TODO: グループに応じて更新タイミングを分散してください
            // ヒント:
            // 1. frameCount % AI_UPDATE_GROUPS == group なら更新
            // 2. これにより各グループは10フレームに1回だけ重い処理を実行
            // 3. 例: グループ0はフレーム0,10,20...で更新
            //        グループ1はフレーム1,11,21...で更新

            // 仮実装（毎フレーム更新）- これを置き換えてください
            return true;
        }


        // ========================================================
        // Step 3: 距離計算の最適化
        // ========================================================
        // 目的: 平方根計算を避けて高速化する
        //
        // TODO: sqrMagnitudeを使って距離計算を最適化してください

        /// <summary>
        /// 2点間の距離の2乗を計算する
        /// </summary>
        /// <param name="a">点A</param>
        /// <param name="b">点B</param>
        /// <returns>距離の2乗</returns>
        public float CalculateDistanceSqr(Vector3 a, Vector3 b)
        {
            // TODO: sqrMagnitudeを使って距離の2乗を計算してください
            // ヒント:
            // 1. Vector3.Distance() は内部で平方根（Sqrt）を計算
            // 2. sqrMagnitude は平方根を計算しないので高速
            // 3. 比較時は閾値も2乗する必要がある

            // 仮実装（Distance使用 - 問題あり）- これを置き換えてください
            return Vector3.Distance(a, b);
        }

        /// <summary>
        /// 2点間の距離が指定値以下かどうかを判定する
        /// </summary>
        /// <param name="a">点A</param>
        /// <param name="b">点B</param>
        /// <param name="maxDistance">最大距離</param>
        /// <returns>距離が maxDistance 以下なら true</returns>
        public bool IsWithinDistance(Vector3 a, Vector3 b, float maxDistance)
        {
            // TODO: sqrMagnitudeを使って距離判定してください
            // ヒント:
            // 1. 閾値を2乗して比較
            // 2. (a - b).sqrMagnitude <= maxDistance * maxDistance

            // 仮実装（Distance使用 - 問題あり）- これを置き換えてください
            return Vector3.Distance(a, b) <= maxDistance;
        }


        // ========================================================
        // デバッグ表示
        // ========================================================

        private void OnDrawGizmos()
        {
            // 空間分割グリッドを可視化
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
