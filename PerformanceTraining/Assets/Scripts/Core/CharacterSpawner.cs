using UnityEngine;
using System.Collections.Generic;
using PerformanceTraining.AI;

namespace PerformanceTraining.Core
{
    /// <summary>
    /// キャラクターのスポーンを管理
    /// TODO: パフォーマンス課題 - オブジェクトプールを使用していない
    /// </summary>
    public class CharacterSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private CharacterPrefabList _prefabList;
        [SerializeField] private GameObject _characterPrefab; // フォールバック用
        [SerializeField] private GameObject _attackEffectPrefab;
        [SerializeField] private GameObject _characterUIPrefab; // キャラクターUI用プレハブ

        [Header("Spawn Settings")]
        [SerializeField] private int _initialSpawnCount = 200;
        [SerializeField] private float _spawnHeight = 0f;

        [Header("Type Distribution")]
        [SerializeField] private bool _useEvenDistribution = false;

        private int _nextId = 0;

        private void Awake()
        {
            // 攻撃エフェクトプレハブを共有設定
            if (_attackEffectPrefab != null)
            {
                Character.SetSharedAttackEffectPrefab(_attackEffectPrefab);
            }
        }

        /// <summary>
        /// 複数のキャラクターをスポーン
        /// </summary>
        public List<Character> SpawnCharacters(int count)
        {
            var characters = new List<Character>(count);

            for (int i = 0; i < count; i++)
            {
                var character = SpawnCharacter();
                if (character != null)
                {
                    characters.Add(character);
                }
            }

            return characters;
        }

        /// <summary>
        /// 単体キャラクターをスポーン
        /// </summary>
        public Character SpawnCharacter()
        {
            // タイプを決定
            CharacterType type;
            if (_useEvenDistribution)
            {
                int typeCount = System.Enum.GetValues(typeof(CharacterType)).Length;
                type = (CharacterType)(_nextId % typeCount);
            }
            else
            {
                type = (CharacterType)Random.Range(0, System.Enum.GetValues(typeof(CharacterType)).Length);
            }

            return SpawnCharacter(type);
        }

        /// <summary>
        /// 指定タイプのキャラクターをスポーン
        /// </summary>
        public Character SpawnCharacter(CharacterType type)
        {
            // プレハブを取得（PrefabList優先、なければフォールバック）
            GameObject prefab = GetPrefabForType(type);

            if (prefab == null)
            {
                Debug.LogError("CharacterSpawner: No prefab assigned for type: " + type);
                return null;
            }

            Vector3 spawnPos = GetRandomSpawnPosition();

            // TODO: パフォーマンス課題 - 毎回Instantiateを呼び出している
            // 最適化: オブジェクトプールを使用してInstantiate/Destroyを削減
            GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity, transform);

            var character = obj.GetComponent<Character>();
            if (character == null)
            {
                Debug.LogError("CharacterSpawner: Prefab does not have Character component!");
                Destroy(obj);
                return null;
            }

            character.Initialize(_nextId, type);
            _nextId++;

            // AIコンポーネントを追加（なければ）
            if (obj.GetComponent<CharacterAI>() == null)
            {
                obj.AddComponent<CharacterAI>();
            }

            // CharacterUIを生成
            SpawnCharacterUI(character);

            return character;
        }

        /// <summary>
        /// キャラクター用のUIを生成
        /// </summary>
        private void SpawnCharacterUI(Character character)
        {
            if (_characterUIPrefab == null) return;

            var uiObj = Instantiate(_characterUIPrefab);
            var characterUI = uiObj.GetComponent<CharacterUI>();
            if (characterUI != null)
            {
                characterUI.Initialize(character);
            }
        }

        /// <summary>
        /// タイプに対応するプレハブを取得
        /// </summary>
        private GameObject GetPrefabForType(CharacterType type)
        {
            // PrefabListがあればそちらを優先
            if (_prefabList != null)
            {
                var prefab = _prefabList.GetPrefab(type);
                if (prefab != null) return prefab;
            }

            // フォールバック
            return _characterPrefab;
        }

        /// <summary>
        /// キャラクターを破棄
        /// </summary>
        public void DespawnCharacter(Character character)
        {
            if (character == null) return;

            // TODO: パフォーマンス課題 - 毎回Destroyを呼び出している
            // 最適化: オブジェクトプールに返却
            Destroy(character.gameObject);
        }

        /// <summary>
        /// 全キャラクターを破棄
        /// </summary>
        public void DespawnAllCharacters()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            _nextId = 0;
        }

        private Vector3 GetRandomSpawnPosition()
        {
            float halfSize = GameConstants.FIELD_HALF_SIZE - GameConstants.SPAWN_MARGIN;
            float x = Random.Range(-halfSize, halfSize);
            float z = Random.Range(-halfSize, halfSize);
            return new Vector3(x, _spawnHeight, z);
        }

        /// <summary>
        /// 初期スポーンを実行
        /// </summary>
        public List<Character> PerformInitialSpawn()
        {
            return SpawnCharacters(_initialSpawnCount);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // スポーン範囲を表示
            float halfSize = GameConstants.FIELD_HALF_SIZE;
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(halfSize * 2, 0.1f, halfSize * 2));

            // マージン込みの範囲
            float innerHalf = halfSize - GameConstants.SPAWN_MARGIN;
            Gizmos.color = new Color(0f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(innerHalf * 2, 0.1f, innerHalf * 2));
        }
#endif
    }
}
