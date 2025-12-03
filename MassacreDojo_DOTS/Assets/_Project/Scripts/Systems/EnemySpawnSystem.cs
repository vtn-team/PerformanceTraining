using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MassacreDojo.DOTS.Components;

namespace MassacreDojo.DOTS.Systems
{
    /// <summary>
    /// 敵のスポーンを管理するシステム
    /// Burst Compilerで高速化
    /// </summary>
    [BurstCompile]
    public partial struct EnemySpawnSystem : ISystem
    {
        private Unity.Mathematics.Random _random;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnerData>();
            state.RequireForUpdate<GameSettings>();
            _random = new Unity.Mathematics.Random(12345);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var spawnerEntity = SystemAPI.GetSingletonEntity<SpawnerData>();
            var spawner = SystemAPI.GetComponent<SpawnerData>(spawnerEntity);

            if (!spawner.ShouldSpawn || spawner.SpawnCount <= 0)
                return;

            var settings = SystemAPI.GetSingleton<GameSettings>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            float limit = settings.FieldHalfSize - 5f;
            int groupCounter = 0;

            for (int i = 0; i < spawner.SpawnCount; i++)
            {
                var entity = ecb.Instantiate(spawner.EnemyPrefab);

                // ランダムな位置
                float3 position = new float3(
                    _random.NextFloat(-limit, limit),
                    0f,
                    _random.NextFloat(-limit, limit)
                );

                ecb.SetComponent(entity, LocalTransform.FromPosition(position));

                // ステータス初期化
                ecb.SetComponent(entity, new EnemyStats
                {
                    Health = 100,
                    MaxHealth = 100,
                    AttackDamage = 10,
                    AttackCooldown = settings.EnemyAttackCooldown,
                    CurrentCooldown = 0f,
                    IsAlive = true
                });

                // 移動データ初期化
                ecb.SetComponent(entity, new EnemyMovement
                {
                    Speed = settings.EnemyMoveSpeed,
                    TargetPosition = position,
                    MoveDirection = float3.zero
                });

                // AI初期化（更新分散のためにグループを設定）
                ecb.SetComponent(entity, new EnemyAI
                {
                    State = EnemyAIState.Idle,
                    UpdateGroup = groupCounter % 10
                });

                groupCounter++;
            }

            // スポーンフラグをリセット
            ecb.SetComponent(spawnerEntity, new SpawnerData
            {
                EnemyPrefab = spawner.EnemyPrefab,
                SpawnCount = 0,
                ShouldSpawn = false
            });

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
