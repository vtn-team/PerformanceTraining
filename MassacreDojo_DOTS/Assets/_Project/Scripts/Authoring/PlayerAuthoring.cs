using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using MassacreDojo.DOTS.Components;

namespace MassacreDojo.DOTS.Authoring
{
    /// <summary>
    /// プレイヤー位置をECS Entityとして同期するAuthoring
    /// MonoBehaviour側でプレイヤーを制御し、位置のみECSに同期
    /// </summary>
    public class PlayerAuthoring : MonoBehaviour
    {
        private Entity _playerEntity;
        private EntityManager _entityManager;
        private bool _initialized;

        private void Start()
        {
            // ECSワールドが準備できるまで待機
            if (World.DefaultGameObjectInjectionWorld != null)
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            // プレイヤー位置エンティティを作成
            _playerEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(_playerEntity, new PlayerPosition
            {
                Value = new float3(transform.position.x, transform.position.y, transform.position.z)
            });

            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized)
            {
                if (World.DefaultGameObjectInjectionWorld != null)
                {
                    Initialize();
                }
                return;
            }

            // プレイヤー位置をECSに同期
            if (_entityManager.Exists(_playerEntity))
            {
                _entityManager.SetComponentData(_playerEntity, new PlayerPosition
                {
                    Value = new float3(transform.position.x, transform.position.y, transform.position.z)
                });
            }
        }

        private void OnDestroy()
        {
            if (_initialized && _entityManager != null && _entityManager.Exists(_playerEntity))
            {
                _entityManager.DestroyEntity(_playerEntity);
            }
        }
    }
}
