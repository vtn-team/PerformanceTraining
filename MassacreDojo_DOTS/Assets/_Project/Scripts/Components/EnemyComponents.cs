using Unity.Entities;
using Unity.Mathematics;

namespace MassacreDojo.DOTS.Components
{
    /// <summary>
    /// 敵エンティティを識別するタグ
    /// </summary>
    public struct EnemyTag : IComponentData
    {
    }

    /// <summary>
    /// 敵のステータス
    /// </summary>
    public struct EnemyStats : IComponentData
    {
        public int Health;
        public int MaxHealth;
        public int AttackDamage;
        public float AttackCooldown;
        public float CurrentCooldown;
        public bool IsAlive;
    }

    /// <summary>
    /// 敵の移動データ
    /// </summary>
    public struct EnemyMovement : IComponentData
    {
        public float Speed;
        public float3 TargetPosition;
        public float3 MoveDirection;
    }

    /// <summary>
    /// 敵のAI状態
    /// </summary>
    public struct EnemyAI : IComponentData
    {
        public EnemyAIState State;
        public int UpdateGroup;
    }

    /// <summary>
    /// AI状態の列挙
    /// </summary>
    public enum EnemyAIState : byte
    {
        Idle,
        Chase,
        Attack,
        Surround,
        Dead
    }

    /// <summary>
    /// 空間分割セルのインデックス
    /// </summary>
    public struct SpatialCell : IComponentData
    {
        public int CellIndex;
    }

    /// <summary>
    /// プレイヤー位置（シングルトン）
    /// </summary>
    public struct PlayerPosition : IComponentData
    {
        public float3 Value;
    }

    /// <summary>
    /// ゲーム設定（シングルトン）
    /// </summary>
    public struct GameSettings : IComponentData
    {
        public float FieldSize;
        public float FieldHalfSize;
        public float CellSize;
        public int GridSize;
        public float EnemyMoveSpeed;
        public float EnemyAttackRange;
        public float EnemyDetectionRange;
        public float EnemyAttackCooldown;
    }

    /// <summary>
    /// スポナー設定
    /// </summary>
    public struct SpawnerData : IComponentData
    {
        public Entity EnemyPrefab;
        public int SpawnCount;
        public bool ShouldSpawn;
    }

    /// <summary>
    /// 統計情報（シングルトン）
    /// </summary>
    public struct GameStats : IComponentData
    {
        public int EnemyCount;
        public int KillCount;
    }
}
