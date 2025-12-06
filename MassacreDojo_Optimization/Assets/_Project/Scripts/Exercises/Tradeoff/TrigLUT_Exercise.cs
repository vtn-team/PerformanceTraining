using UnityEngine;
using MassacreDojo.Core;

namespace MassacreDojo.Exercises.Tradeoff
{
    /// <summary>
    /// 【課題3-C: 三角関数ルックアップテーブル】
    ///
    /// 目標: メモリを消費してSin/Cos計算を削減する
    ///
    /// トレードオフ:
    /// - メモリ使用量: +2.8KB（360エントリ × 4バイト × 2テーブル）
    /// - CPU削減効果: 2-5倍高速化
    /// - 代償: 1度単位の精度（補間なしの場合）
    ///
    /// 確認方法:
    /// - Sin/Cos呼び出し時のCPU時間を比較
    /// </summary>
    public class TrigLUT_Exercise : MonoBehaviour
    {
        // ========================================================
        // ルックアップテーブル
        // ========================================================

        // TODO: Sin/Cosテーブルを宣言


        private int _tableSize = GameConstants.TRIG_LUT_SIZE;
        private bool _isInitialized = false;


        // ========================================================
        // 初期化
        // ========================================================

        public void Initialize()
        {
            if (_isInitialized) return;

            // TODO: Sin/Cosテーブルを事前計算
            // 0度～359度の値を配列に格納

            _isInitialized = true;
        }

        private void Awake()
        {
            Initialize();
        }


        // ========================================================
        // テーブル参照
        // ========================================================

        /// <summary>
        /// 角度からテーブルインデックスを計算する
        /// </summary>
        public int AngleToIndex(float angleDegrees)
        {
            // TODO: 角度を0-359の範囲に正規化してインデックスに変換
            // 負の角度も正しく処理する必要がある
            int index = Mathf.RoundToInt(angleDegrees) % 360;
            if (index < 0) index += 360;
            return index;
        }

        /// <summary>
        /// Sin値を取得する
        /// </summary>
        public float Sin(float angleDegrees)
        {
            // 現在の実装（問題あり）: 毎回計算
            // TODO: テーブルから値を取得
            return Mathf.Sin(angleDegrees * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Cos値を取得する
        /// </summary>
        public float Cos(float angleDegrees)
        {
            // 現在の実装（問題あり）: 毎回計算
            // TODO: テーブルから値を取得
            return Mathf.Cos(angleDegrees * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Sin/Cos値を同時に取得する（効率的）
        /// </summary>
        public void SinCos(float angleDegrees, out float sin, out float cos)
        {
            // 現在の実装（問題あり）: 毎回計算
            // TODO: 1回のインデックス計算で両方取得
            float rad = angleDegrees * Mathf.Deg2Rad;
            sin = Mathf.Sin(rad);
            cos = Mathf.Cos(rad);
        }


        // ========================================================
        // デバッグ
        // ========================================================

        public int GetMemoryUsageBytes()
        {
            return _tableSize * sizeof(float) * 2;
        }

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
            Debug.Log($"TrigLUT - Max Error: {maxError}, Memory: {GetMemoryUsageBytes()} bytes");
        }
    }
}
