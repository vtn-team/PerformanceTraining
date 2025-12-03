using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MassacreDojo.DOTS.Components;

namespace MassacreDojo.DOTS.Systems
{
    /// <summary>
    /// 敵の移動を処理するシステム
    /// Job System + Burst Compilerで並列処理
    /// </summary>
    [BurstCompile]
    [UpdateAfter(typeof(EnemyAISystem))]
    public partial struct EnemyMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSettings>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var settings = SystemAPI.GetSingleton<GameSettings>();
            float deltaTime = SystemAPI.Time.DeltaTime;
            float limit = settings.FieldHalfSize - 1f;

            // 並列処理で全敵を移動
            new MoveEnemiesJob
            {
                DeltaTime = deltaTime,
                FieldLimit = limit
            }.ScheduleParallel();
        }
    }

    /// <summary>
    /// 敵移動ジョブ
    /// Burst + 並列処理で高速化
    /// </summary>
    [BurstCompile]
    public partial struct MoveEnemiesJob : IJobEntity
    {
        public float DeltaTime;
        public float FieldLimit;

        public void Execute(
            ref LocalTransform transform,
            in EnemyMovement movement,
            in EnemyStats stats)
        {
            if (!stats.IsAlive)
                return;

            // 移動方向がある場合のみ移動
            if (math.lengthsq(movement.MoveDirection) > 0.01f)
            {
                float3 newPosition = transform.Position + movement.MoveDirection * movement.Speed * DeltaTime;

                // フィールド範囲内に制限
                newPosition.x = math.clamp(newPosition.x, -FieldLimit, FieldLimit);
                newPosition.z = math.clamp(newPosition.z, -FieldLimit, FieldLimit);

                transform.Position = newPosition;

                // 移動方向を向く
                if (math.lengthsq(movement.MoveDirection) > 0.01f)
                {
                    transform.Rotation = quaternion.LookRotationSafe(movement.MoveDirection, math.up());
                }
            }
        }
    }
}
