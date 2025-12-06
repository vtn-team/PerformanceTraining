using UnityEngine;
using PerformanceTraining.Core;

namespace StudentExercises.Tradeoff
{
    /// <summary>
    /// 【課題3-A: 三角関数ルックアップテーブル（LUT）】
    ///
    /// 目標: メモリを消費してCPU計算を削減する
    ///
    /// トレードオフ:
    /// - メモリ: +2.8KB（360エントリ × 4バイト × 2テーブル）
    /// - CPU: 2-5倍高速化
    ///
    /// 使用場面:
    /// - 敵の包囲行動（プレイヤーを囲む位置計算）
    /// - 待機モーション（ボビング、揺れ）
    /// - 旋回移動（弧を描いて接近）
    ///
    /// TODO: Sin/Cosの値を事前計算してテーブルに格納してください
    /// </summary>
    public class TrigLUT_Exercise : MonoBehaviour
    {
        // ========================================================
        // ルックアップテーブル
        // ========================================================

        // TODO: ここにSin/Cosのテーブルを宣言してください
        // private float[] _sinTable;
        // private float[] _cosTable;

        private int _tableSize = GameConstants.TRIG_LUT_SIZE;
        private bool _isInitialized = false;


        /// <summary>
        /// テーブルを初期化する
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // TODO: Sin/Cosテーブルを事前計算してください
            // ヒント:
            // 1. _sinTable = new float[_tableSize];
            // 2. _cosTable = new float[_tableSize];
            // 3. for (int i = 0; i < _tableSize; i++)
            //    {
            //        float rad = i * Mathf.Deg2Rad;
            //        _sinTable[i] = Mathf.Sin(rad);
            //        _cosTable[i] = Mathf.Cos(rad);
            //    }

            _isInitialized = true;
        }

        private void Awake()
        {
            Initialize();
        }


        /// <summary>
        /// 角度からテーブルインデックスを計算する
        /// </summary>
        /// <param name="angleDegrees">角度（度）</param>
        /// <returns>テーブルインデックス（0〜359）</returns>
        public int AngleToIndex(float angleDegrees)
        {
            // TODO: 角度をインデックスに変換してください
            // ヒント:
            // 1. 角度を0〜360の範囲に正規化
            // 2. 負の角度も正しく処理する（例: -90 → 270）
            // 3. 360は0として扱う

            // 仮実装 - これを置き換えてください
            int index = Mathf.RoundToInt(angleDegrees) % 360;
            if (index < 0) index += 360;
            return index;
        }


        /// <summary>
        /// Sin値を取得する（LUT使用）
        /// </summary>
        /// <param name="angleDegrees">角度（度）</param>
        /// <returns>Sin値</returns>
        public float Sin(float angleDegrees)
        {
            // TODO: テーブルからSin値を取得してください
            // ヒント:
            // 1. AngleToIndex() でインデックスを取得
            // 2. _sinTable[index] を返す

            // 仮実装（直接計算 - 問題あり）- これを置き換えてください
            return Mathf.Sin(angleDegrees * Mathf.Deg2Rad);
        }


        /// <summary>
        /// Cos値を取得する（LUT使用）
        /// </summary>
        /// <param name="angleDegrees">角度（度）</param>
        /// <returns>Cos値</returns>
        public float Cos(float angleDegrees)
        {
            // TODO: テーブルからCos値を取得してください
            // ヒント:
            // 1. AngleToIndex() でインデックスを取得
            // 2. _cosTable[index] を返す

            // 仮実装（直接計算 - 問題あり）- これを置き換えてください
            return Mathf.Cos(angleDegrees * Mathf.Deg2Rad);
        }


        /// <summary>
        /// Sin/Cos値を同時に取得する（さらに効率的）
        /// </summary>
        /// <param name="angleDegrees">角度（度）</param>
        /// <param name="sin">出力: Sin値</param>
        /// <param name="cos">出力: Cos値</param>
        public void SinCos(float angleDegrees, out float sin, out float cos)
        {
            // TODO: 一度のインデックス計算で両方の値を取得してください
            // ヒント:
            // 1. インデックスを1回だけ計算
            // 2. _sinTable と _cosTable から取得

            // 仮実装（直接計算 - 問題あり）- これを置き換えてください
            float rad = angleDegrees * Mathf.Deg2Rad;
            sin = Mathf.Sin(rad);
            cos = Mathf.Cos(rad);
        }


        // ========================================================
        // 発展課題: 線形補間によるLUT
        // ========================================================
        // より高精度が必要な場合、隣接する2つのエントリを
        // 線形補間することで精度を上げられます

        /// <summary>
        /// 線形補間を使用してSin値を取得する（発展課題）
        /// </summary>
        /// <param name="angleDegrees">角度（度）</param>
        /// <returns>補間されたSin値</returns>
        public float SinLerp(float angleDegrees)
        {
            // TODO（発展）: 線形補間でより高精度なSin値を取得
            // ヒント:
            // 1. 角度の小数部分を取得
            // 2. 前後のインデックスの値を取得
            // 3. Mathf.Lerp で補間

            return Sin(angleDegrees);
        }


        // ========================================================
        // デバッグ・計測用
        // ========================================================

        /// <summary>
        /// テーブルのメモリ使用量を計算する（バイト）
        /// </summary>
        /// <returns>メモリ使用量</returns>
        public int GetMemoryUsageBytes()
        {
            // float配列2つ × テーブルサイズ × 4バイト
            return _tableSize * sizeof(float) * 2;
        }

        /// <summary>
        /// 計算精度をテストする
        /// </summary>
        public void TestAccuracy()
        {
            float maxError = 0f;

            for (int i = 0; i < 360; i++)
            {
                float lutSin = Sin(i);
                float realSin = Mathf.Sin(i * Mathf.Deg2Rad);
                float error = Mathf.Abs(lutSin - realSin);
                maxError = Mathf.Max(maxError, error);
            }

            Debug.Log($"TrigLUT Max Error: {maxError}");
            Debug.Log($"TrigLUT Memory: {GetMemoryUsageBytes()} bytes ({GetMemoryUsageBytes() / 1024f:F2} KB)");
        }
    }
}
