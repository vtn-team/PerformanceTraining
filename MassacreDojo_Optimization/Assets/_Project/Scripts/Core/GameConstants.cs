using UnityEngine;

namespace MassacreDojo.Core
{
    /// <summary>
    /// ゲーム全体の定数を管理するクラス
    /// </summary>
    public static class GameConstants
    {
        // ===== ゲーム設定 =====
        public const int INITIAL_ENEMY_COUNT = 200;
        public const int MAX_ENEMY_COUNT = 1000;
        public const int ENEMY_SPAWN_BATCH_SIZE = 50;

        // ===== フィールド設定 =====
        public const float FIELD_SIZE = 100f;
        public const float FIELD_HALF_SIZE = FIELD_SIZE / 2f;
        public const float SPAWN_MARGIN = 5f;
        public const float SPAWN_MIN_DISTANCE = 10f;
        public const float SPAWN_MAX_DISTANCE = 30f;

        // ===== 空間分割設定 =====
        public const float CELL_SIZE = 10f;
        public const int GRID_SIZE = (int)(FIELD_SIZE / CELL_SIZE);

        // ===== 敵AI設定 =====
        public const float ENEMY_MOVE_SPEED = 3f;
        public const float ENEMY_ATTACK_RANGE = 2f;
        public const float ENEMY_DETECTION_RANGE = 20f;
        public const float ENEMY_ATTACK_COOLDOWN = 1.5f;

        // ===== プレイヤー設定 =====
        public const float PLAYER_MOVE_SPEED = 8f;
        public const float PLAYER_ATTACK_RANGE = 3f;
        public const float PLAYER_ATTACK_COOLDOWN = 0.3f;
        public const float PLAYER_DODGE_SPEED = 15f;
        public const float PLAYER_DODGE_DURATION = 0.3f;
        public const float PLAYER_DODGE_COOLDOWN = 0.5f;
        public const int PLAYER_ATTACK_DAMAGE = 100;

        // ===== パフォーマンス設定 =====
        public const int OBJECT_POOL_INITIAL_SIZE = 500;
        public const int AI_UPDATE_GROUPS = 10; // 更新分散用のグループ数

        // ===== トレードオフ設定 =====
        public const int TRIG_LUT_SIZE = 360; // 三角関数LUTのサイズ
        public const int VISIBILITY_GRID_SIZE = 50; // 可視性マップのグリッドサイズ

        // ===== UI設定 =====
        public const float PERFORMANCE_UPDATE_INTERVAL = 0.5f;
    }
}
