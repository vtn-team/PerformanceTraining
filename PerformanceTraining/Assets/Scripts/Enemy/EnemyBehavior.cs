using UnityEngine;
using PerformanceTraining.Core;

#pragma warning disable 0414 // 将来の拡張用フィールド

namespace PerformanceTraining.Enemy
{
    /// <summary>
    /// 敵の詳細行動を制御するクラス
    /// 包囲行動や可視性チェックなどの補助的な行動を担当
    /// </summary>
    public class EnemyBehavior : MonoBehaviour
    {
        [Header("包囲行動設定")]
        [SerializeField] private float surroundRadius = 5f;
        [SerializeField] private float surroundSpeed = 2f;
        [SerializeField] private float bobbingSpeed = 2f;
        [SerializeField] private float bobbingAmount = 0.2f;

        [Header("可視性設定")]
        [SerializeField] private LayerMask visibilityMask;
        [SerializeField] private float visibilityCheckInterval = 0.5f;

        [Header("デバッグ")]
        [SerializeField] private bool canSeePlayer;
        [SerializeField] private float surroundAngle;

        // 内部変数
        private float visibilityTimer;
        private float angleOffset; // 各敵の包囲位置オフセット

        private void Start()
        {
            // 各敵にランダムなオフセットを設定
            angleOffset = Random.Range(0f, 360f);
        }

        /// <summary>
        /// 包囲位置を計算
        /// </summary>
        public Vector3 CalculateSurroundPosition(Vector3 centerPos, float baseAngle, float time)
        {
            // 包囲角度（時間とともに回転）
            float angle = baseAngle + angleOffset + time * 30f; // 30度/秒で回転
            surroundAngle = angle;

            float rad = angle * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);

            // 包囲位置を計算
            Vector3 offset = new Vector3(cos * surroundRadius, 0f, sin * surroundRadius);
            return centerPos + offset;
        }

        /// <summary>
        /// 待機時のボビング（上下揺れ）を計算
        /// </summary>
        public float CalculateBobbingOffset(float time)
        {
            float sin = Mathf.Sin(time * bobbingSpeed * Mathf.PI * 2f);
            return sin * bobbingAmount;
        }

        /// <summary>
        /// プレイヤーが見えるかチェック
        /// </summary>
        public bool CheckPlayerVisibility(Vector3 enemyPos, Vector3 playerPos)
        {
            Vector3 direction = playerPos - enemyPos;
            float distance = direction.magnitude;

            if (Physics.Raycast(enemyPos + Vector3.up, direction.normalized, out RaycastHit hit, distance, visibilityMask))
            {
                // 何かに遮られた
                canSeePlayer = false;
            }
            else
            {
                // 見える
                canSeePlayer = true;
            }

            return canSeePlayer;
        }

        /// <summary>
        /// 旋回しながら接近する動きを計算
        /// </summary>
        public Vector3 CalculateSpiralApproach(Vector3 enemyPos, Vector3 targetPos, float time, float spiralTightness)
        {
            Vector3 toTarget = targetPos - enemyPos;
            float baseAngle = Mathf.Atan2(toTarget.z, toTarget.x) * Mathf.Rad2Deg;
            float spiralAngle = baseAngle + Mathf.Sin(time * 2f) * 30f;

            float rad = spiralAngle * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);

            // 螺旋状に接近する方向を返す
            return new Vector3(cos, 0f, sin).normalized;
        }

        /// <summary>
        /// 複数地点間の可視性をまとめてチェック
        /// </summary>
        public int CountVisibleEnemies(Vector3[] positions, Vector3 viewerPos)
        {
            int count = 0;

            foreach (var pos in positions)
            {
                Vector3 direction = pos - viewerPos;
                if (!Physics.Raycast(viewerPos, direction.normalized, direction.magnitude, visibilityMask))
                {
                    count++;
                }
            }

            return count;
        }

        private void OnDrawGizmosSelected()
        {
            // 包囲範囲を表示
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, surroundRadius);

            // 可視性を表示
            if (canSeePlayer)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.red;
            }
            Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + transform.forward * 5f);
        }
    }
}
