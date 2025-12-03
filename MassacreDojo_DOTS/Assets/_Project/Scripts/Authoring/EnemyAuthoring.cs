using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using MassacreDojo.DOTS.Components;

namespace MassacreDojo.DOTS.Authoring
{
    /// <summary>
    /// 敵プレハブをECS Entityに変換するAuthoring
    /// </summary>
    public class EnemyAuthoring : MonoBehaviour
    {
        [Header("ステータス")]
        public int Health = 100;
        public int AttackDamage = 10;
        public float AttackCooldown = 1.5f;

        [Header("移動")]
        public float MoveSpeed = 3f;

        public class Baker : Baker<EnemyAuthoring>
        {
            public override void Bake(EnemyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // タグ
                AddComponent(entity, new EnemyTag());

                // ステータス
                AddComponent(entity, new EnemyStats
                {
                    Health = authoring.Health,
                    MaxHealth = authoring.Health,
                    AttackDamage = authoring.AttackDamage,
                    AttackCooldown = authoring.AttackCooldown,
                    CurrentCooldown = 0f,
                    IsAlive = true
                });

                // 移動
                AddComponent(entity, new EnemyMovement
                {
                    Speed = authoring.MoveSpeed,
                    TargetPosition = float3.zero,
                    MoveDirection = float3.zero
                });

                // AI
                AddComponent(entity, new EnemyAI
                {
                    State = EnemyAIState.Idle,
                    UpdateGroup = 0
                });

                // 空間分割用
                AddComponent(entity, new SpatialCell
                {
                    CellIndex = 0
                });
            }
        }
    }
}
