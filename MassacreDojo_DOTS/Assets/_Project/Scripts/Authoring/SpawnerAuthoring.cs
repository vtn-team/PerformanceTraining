using Unity.Entities;
using UnityEngine;
using MassacreDojo.DOTS.Components;

namespace MassacreDojo.DOTS.Authoring
{
    /// <summary>
    /// スポナー設定をECS Entityに変換するAuthoring
    /// </summary>
    public class SpawnerAuthoring : MonoBehaviour
    {
        [Header("スポーン設定")]
        public GameObject EnemyPrefab;
        public int InitialSpawnCount = 200;

        [Header("ゲーム設定")]
        public float FieldSize = 100f;
        public float CellSize = 10f;
        public float EnemyMoveSpeed = 3f;
        public float EnemyAttackRange = 2f;
        public float EnemyDetectionRange = 20f;
        public float EnemyAttackCooldown = 1.5f;

        public class Baker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                // スポナーデータ
                AddComponent(entity, new SpawnerData
                {
                    EnemyPrefab = GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic),
                    SpawnCount = authoring.InitialSpawnCount,
                    ShouldSpawn = true
                });

                // ゲーム設定
                AddComponent(entity, new GameSettings
                {
                    FieldSize = authoring.FieldSize,
                    FieldHalfSize = authoring.FieldSize / 2f,
                    CellSize = authoring.CellSize,
                    GridSize = (int)(authoring.FieldSize / authoring.CellSize),
                    EnemyMoveSpeed = authoring.EnemyMoveSpeed,
                    EnemyAttackRange = authoring.EnemyAttackRange,
                    EnemyDetectionRange = authoring.EnemyDetectionRange,
                    EnemyAttackCooldown = authoring.EnemyAttackCooldown
                });

                // 統計情報
                AddComponent(entity, new GameStats
                {
                    EnemyCount = 0,
                    KillCount = 0
                });
            }
        }
    }
}
