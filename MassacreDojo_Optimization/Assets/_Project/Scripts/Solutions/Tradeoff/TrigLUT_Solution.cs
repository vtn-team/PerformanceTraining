using UnityEngine;
using MassacreDojo.Core;

namespace MassacreDojo.Solutions.Tradeoff
{
    /// <summary>
    /// 【解答】課題3-A: 三角関数ルックアップテーブル（LUT）
    ///
    /// このファイルは教員用の解答です。
    /// 学生には見せないでください。
    /// </summary>
    public class TrigLUT_Solution : MonoBehaviour
    {
        // ========================================================
        // ルックアップテーブル【解答】
        // ========================================================

        // 【解答】Sin/Cosテーブル
        private float[] _sinTable;
        private float[] _cosTable;

        private int _tableSize = GameConstants.TRIG_LUT_SIZE;
        private bool _isInitialized = false;


        public void Initialize()
        {
            if (_isInitialized) return;

            // 【解答】テーブルを事前計算
            _sinTable = new float[_tableSize];
            _cosTable = new float[_tableSize];

            for (int i = 0; i < _tableSize; i++)
            {
                float rad = i * Mathf.Deg2Rad;
                _sinTable[i] = Mathf.Sin(rad);
                _cosTable[i] = Mathf.Cos(rad);
            }

            _isInitialized = true;
            Debug.Log($"TrigLUT initialized: {_tableSize} entries, {GetMemoryUsageBytes()} bytes");
        }


        private void Awake()
        {
            Initialize();
        }


        public int AngleToIndex(float angleDegrees)
        {
            // 【解答】角度をインデックスに変換

            // 角度を整数に丸める
            int index = Mathf.RoundToInt(angleDegrees) % _tableSize;

            // 負の値を正に変換
            if (index < 0)
            {
                index += _tableSize;
            }

            return index;
        }


        public float Sin(float angleDegrees)
        {
            // 【解答】テーブルからSin値を取得
            int index = AngleToIndex(angleDegrees);
            return _sinTable[index];
        }


        public float Cos(float angleDegrees)
        {
            // 【解答】テーブルからCos値を取得
            int index = AngleToIndex(angleDegrees);
            return _cosTable[index];
        }


        public void SinCos(float angleDegrees, out float sin, out float cos)
        {
            // 【解答】一度のインデックス計算で両方取得
            int index = AngleToIndex(angleDegrees);
            sin = _sinTable[index];
            cos = _cosTable[index];
        }


        // ========================================================
        // 発展課題: 線形補間【解答】
        // ========================================================

        public float SinLerp(float angleDegrees)
        {
            // 【解答】線形補間でより高精度なSin値を取得

            // 角度を正規化
            float normalizedAngle = angleDegrees % _tableSize;
            if (normalizedAngle < 0) normalizedAngle += _tableSize;

            // 整数部分と小数部分を分離
            int index0 = (int)normalizedAngle;
            int index1 = (index0 + 1) % _tableSize;
            float t = normalizedAngle - index0;

            // 線形補間
            return Mathf.Lerp(_sinTable[index0], _sinTable[index1], t);
        }


        public float CosLerp(float angleDegrees)
        {
            // 【解答】線形補間でより高精度なCos値を取得

            float normalizedAngle = angleDegrees % _tableSize;
            if (normalizedAngle < 0) normalizedAngle += _tableSize;

            int index0 = (int)normalizedAngle;
            int index1 = (index0 + 1) % _tableSize;
            float t = normalizedAngle - index0;

            return Mathf.Lerp(_cosTable[index0], _cosTable[index1], t);
        }


        // ========================================================
        // デバッグ・計測用
        // ========================================================

        public int GetMemoryUsageBytes()
        {
            return _tableSize * sizeof(float) * 2;
        }


        public void TestAccuracy()
        {
            float maxErrorBasic = 0f;
            float maxErrorLerp = 0f;

            for (float angle = 0; angle < 360; angle += 0.1f)
            {
                float realSin = Mathf.Sin(angle * Mathf.Deg2Rad);

                // 基本版の誤差
                float lutSin = Sin(angle);
                float errorBasic = Mathf.Abs(lutSin - realSin);
                maxErrorBasic = Mathf.Max(maxErrorBasic, errorBasic);

                // 補間版の誤差
                float lerpSin = SinLerp(angle);
                float errorLerp = Mathf.Abs(lerpSin - realSin);
                maxErrorLerp = Mathf.Max(maxErrorLerp, errorLerp);
            }

            Debug.Log($"TrigLUT Accuracy Test:");
            Debug.Log($"  Basic Max Error: {maxErrorBasic:F6}");
            Debug.Log($"  Lerp Max Error:  {maxErrorLerp:F6}");
            Debug.Log($"  Memory Usage:    {GetMemoryUsageBytes()} bytes ({GetMemoryUsageBytes() / 1024f:F2} KB)");
        }


        // ベンチマーク用
        public void Benchmark(int iterations = 100000)
        {
            float dummy = 0;

            // ウォームアップ
            for (int i = 0; i < 1000; i++)
            {
                dummy += Mathf.Sin(i * 0.1f);
            }

            // Mathf.Sin ベンチマーク
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                dummy += Mathf.Sin(i * Mathf.Deg2Rad);
            }
            sw.Stop();
            float mathfTime = sw.ElapsedMilliseconds;

            // LUT ベンチマーク
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                dummy += Sin(i);
            }
            sw.Stop();
            float lutTime = sw.ElapsedMilliseconds;

            Debug.Log($"TrigLUT Benchmark ({iterations} iterations):");
            Debug.Log($"  Mathf.Sin: {mathfTime}ms");
            Debug.Log($"  LUT Sin:   {lutTime}ms");
            Debug.Log($"  Speedup:   {mathfTime / lutTime:F2}x");

            // ダミー値を使用（最適化で消されないように）
            if (dummy == float.NaN) Debug.Log("NaN");
        }
    }
}
