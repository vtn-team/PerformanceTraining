using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MassacreDojo.DOTS.Components;

namespace MassacreDojo.DOTS.Systems
{
    /// <summary>
    /// 敵AIを処理するシステム
    /// 空間分割 + 更新分散 + sqrMagnitude を使用した最適化
    /// </summary>
    [BurstCompile]
    [UpdateAfter(typeof(SpatialHashSystem))]
    public partial struct EnemyAISystem : ISystem
    {
        private int _frameCount;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSettings>();
            state.RequireForUpdate<PlayerPosition>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _frameCount++;

            var settings = SystemAPI.GetSingleton<GameSettings>();
            var playerPos = SystemAPI.GetSingleton<PlayerPosition>().Value;

            float attackRangeSqr = settings.EnemyAttackRange * settings.EnemyAttackRange;
            float detectionRangeSqr = settings.EnemyDetectionRange * settings.EnemyDetectionRange;

            // 並列処理でAI更新
            new UpdateAIJob
            {
                FrameCount = _frameCount,
                PlayerPosition = playerPos,
                AttackRangeSqr = attackRangeSqr,
                DetectionRangeSqr = detectionRangeSqr,
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }
    }

    /// <summary>
    /// AI更新ジョブ
    /// </summary>
    [BurstCompile]
    public partial struct UpdateAIJob : IJobEntity
    {
        public int FrameCount;
        public float3 PlayerPosition;
        public float AttackRangeSqr;
        public float DetectionRangeSqr;
        public float DeltaTime;

        public void Execute(
            ref EnemyAI ai,
            ref EnemyMovement movement,
            ref EnemyStats stats,
            in LocalTransform transform)
        {
            if (!stats.IsAlive)
            {
                ai.State = EnemyAIState.Dead;
                movement.MoveDirection = float3.zero;
                return;
            }

            // 更新分散: 10グループに分けて処理
            // 重い処理は自分のグループのフレームのみ
            bool shouldUpdateHeavy = (FrameCount % 10) == ai.UpdateGroup;

            // クールダウン更新（全フレーム）
            if (stats.CurrentCooldown > 0f)
            {
                stats.CurrentCooldown -= DeltaTime;
            }

            if (!shouldUpdateHeavy)
            {
                // 軽い処理のみ: 前フレームの方向を継続
                return;
            }

            // 重い処理: AI判断
            float3 toPlayer = PlayerPosition - transform.Position;

            // sqrMagnitudeで距離計算（平方根を避ける）
            float distSqr = math.lengthsq(toPlayer);

            if (distSqr < AttackRangeSqr)
            {
                // 攻撃範囲内
                ai.State = EnemyAIState.Attack;
                movement.MoveDirection = float3.zero;

                if (stats.CurrentCooldown <= 0f)
                {
                    // 攻撃実行
                    stats.CurrentCooldown = stats.AttackCooldown;
                    // 実際のダメージ処理は別システムで
                }
            }
            else if (distSqr < DetectionRangeSqr)
            {
                // 追跡範囲内
                ai.State = EnemyAIState.Chase;
                movement.MoveDirection = math.normalizesafe(toPlayer);
                movement.TargetPosition = PlayerPosition;
            }
            else
            {
                // 範囲外: 待機（ランダム徘徊は簡略化のため省略）
                ai.State = EnemyAIState.Idle;
                movement.MoveDirection = float3.zero;
            }
        }
    }
}
