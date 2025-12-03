using Unity.Entities;
using UnityEngine;
using MassacreDojo.DOTS.Components;

namespace MassacreDojo.DOTS.UI
{
    /// <summary>
    /// DOTS版スポーン制御
    /// デバッグ用のキー入力を処理
    /// </summary>
    public class SpawnController_DOTS : MonoBehaviour
    {
        [Header("スポーン設定")]
        [SerializeField] private int spawnBatchSize = 50;

        private EntityManager _entityManager;
        private Entity _spawnerEntity;
        private bool _initialized;

        private void Start()
        {
            TryInitialize();
        }

        private void TryInitialize()
        {
            if (World.DefaultGameObjectInjectionWorld == null)
                return;

            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            // SpawnerDataを持つエンティティを検索
            var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpawnerData>());
            if (query.CalculateEntityCount() > 0)
            {
                var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                _spawnerEntity = entities[0];
                entities.Dispose();
                _initialized = true;
            }
        }

        private void Update()
        {
            if (!_initialized)
            {
                TryInitialize();
                return;
            }

            // F1: 敵追加スポーン
            if (Input.GetKeyDown(KeyCode.F1))
            {
                SpawnEnemies(spawnBatchSize);
                Debug.Log($"Spawning {spawnBatchSize} enemies...");
            }

            // F2: 大量スポーン（500体）
            if (Input.GetKeyDown(KeyCode.F2))
            {
                SpawnEnemies(500);
                Debug.Log("Spawning 500 enemies...");
            }

            // F3: 超大量スポーン（1000体）
            if (Input.GetKeyDown(KeyCode.F3))
            {
                SpawnEnemies(1000);
                Debug.Log("Spawning 1000 enemies...");
            }
        }

        private void SpawnEnemies(int count)
        {
            if (!_entityManager.Exists(_spawnerEntity))
                return;

            var spawner = _entityManager.GetComponentData<SpawnerData>(_spawnerEntity);
            spawner.SpawnCount = count;
            spawner.ShouldSpawn = true;
            _entityManager.SetComponentData(_spawnerEntity, spawner);
        }

        private void OnGUI()
        {
            // 操作説明を表示
            float x = Screen.width - 220;
            float y = 10;
            float lineHeight = 20;

            GUI.Label(new Rect(x, y, 200, lineHeight), "Controls:");
            y += lineHeight;
            GUI.Label(new Rect(x, y, 200, lineHeight), $"  F1: Spawn {spawnBatchSize} enemies");
            y += lineHeight;
            GUI.Label(new Rect(x, y, 200, lineHeight), "  F2: Spawn 500 enemies");
            y += lineHeight;
            GUI.Label(new Rect(x, y, 200, lineHeight), "  F3: Spawn 1000 enemies");
        }
    }
}
