using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MassacreDojo.DOTS.Components;

namespace MassacreDojo.DOTS.Systems
{
    /// <summary>
    /// 空間ハッシュを更新するシステム
    /// NativeMultiHashMapを使用した高速な空間分割
    /// </summary>
    [BurstCompile]
    public partial struct SpatialHashSystem : ISystem
    {
        private NativeParallelMultiHashMap<int, Entity> _spatialHash;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSettings>();
            _spatialHash = new NativeParallelMultiHashMap<int, Entity>(10000, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_spatialHash.IsCreated)
                _spatialHash.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var settings = SystemAPI.GetSingleton<GameSettings>();

            // 空間ハッシュをクリア
            _spatialHash.Clear();

            // 全敵のセルインデックスを計算して登録
            foreach (var (transform, cell, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRW<SpatialCell>>()
                .WithAll<EnemyTag>()
                .WithEntityAccess())
            {
                int cellIndex = CalculateCellIndex(
                    transform.ValueRO.Position,
                    settings.FieldHalfSize,
                    settings.CellSize,
                    settings.GridSize
                );

                cell.ValueRW.CellIndex = cellIndex;
                _spatialHash.Add(cellIndex, entity);
            }
        }

        /// <summary>
        /// 座標からセルインデックスを計算
        /// </summary>
        [BurstCompile]
        private static int CalculateCellIndex(float3 position, float fieldHalfSize, float cellSize, int gridSize)
        {
            int x = (int)math.floor((position.x + fieldHalfSize) / cellSize);
            int z = (int)math.floor((position.z + fieldHalfSize) / cellSize);

            x = math.clamp(x, 0, gridSize - 1);
            z = math.clamp(z, 0, gridSize - 1);

            return z * gridSize + x;
        }

        /// <summary>
        /// 指定セルの近傍エンティティを取得
        /// </summary>
        public NativeList<Entity> QueryNearby(int centerCellIndex, int gridSize, Allocator allocator)
        {
            var result = new NativeList<Entity>(allocator);

            int centerX = centerCellIndex % gridSize;
            int centerZ = centerCellIndex / gridSize;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;

                    if (x < 0 || x >= gridSize || z < 0 || z >= gridSize)
                        continue;

                    int cellIndex = z * gridSize + x;

                    if (_spatialHash.TryGetFirstValue(cellIndex, out Entity entity, out var iterator))
                    {
                        do
                        {
                            result.Add(entity);
                        }
                        while (_spatialHash.TryGetNextValue(out entity, ref iterator));
                    }
                }
            }

            return result;
        }
    }
}
