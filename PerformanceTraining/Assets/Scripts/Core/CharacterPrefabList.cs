using UnityEngine;
using System;
using System.Collections.Generic;

namespace PerformanceTraining.Core
{
    /// <summary>
    /// キャラクタータイプとプレハブリストの対応
    /// 各タイプに複数のバリエーションを設定可能
    /// </summary>
    [Serializable]
    public class CharacterPrefabEntry
    {
        public CharacterType type;
        public List<GameObject> prefabs = new List<GameObject>();
    }

    /// <summary>
    /// キャラクタータイプ別のプレハブリスト
    /// ScriptableObjectとしてアセット化可能
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterPrefabList", menuName = "PerformanceTraining/Character Prefab List")]
    public class CharacterPrefabList : ScriptableObject
    {
        [Header("Default Prefab")]
        [Tooltip("タイプ別プレハブが設定されていない場合に使用")]
        [SerializeField] private GameObject _defaultPrefab;

        [Header("Type-Specific Prefabs")]
        [Tooltip("各タイプに複数のプレハブバリエーションを設定可能")]
        [SerializeField] private CharacterPrefabEntry[] _prefabEntries;

        // キャッシュ用（タイプをインデックスとして使用）
        private List<GameObject>[] _prefabCache;
        private bool _isCacheInitialized = false;

        /// <summary>
        /// キャッシュを初期化
        /// </summary>
        private void InitializeCache()
        {
            if (_isCacheInitialized) return;

            int typeCount = Enum.GetValues(typeof(CharacterType)).Length;
            _prefabCache = new List<GameObject>[typeCount];

            // 空のリストで初期化
            for (int i = 0; i < typeCount; i++)
            {
                _prefabCache[i] = new List<GameObject>();
            }

            // エントリからコピー
            if (_prefabEntries != null)
            {
                foreach (var entry in _prefabEntries)
                {
                    if (entry.prefabs != null && entry.prefabs.Count > 0)
                    {
                        _prefabCache[(int)entry.type] = new List<GameObject>(entry.prefabs);
                    }
                }
            }

            _isCacheInitialized = true;
        }

        /// <summary>
        /// 指定タイプのプレハブをランダムに取得
        /// </summary>
        public GameObject GetPrefab(CharacterType type)
        {
            InitializeCache();

            var prefabs = _prefabCache[(int)type];

            // このタイプにプレハブがあればランダムに選択
            if (prefabs != null && prefabs.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, prefabs.Count);
                return prefabs[index];
            }

            // フォールバック
            return _defaultPrefab;
        }

        /// <summary>
        /// 指定タイプの特定インデックスのプレハブを取得
        /// </summary>
        public GameObject GetPrefab(CharacterType type, int variantIndex)
        {
            InitializeCache();

            var prefabs = _prefabCache[(int)type];

            if (prefabs != null && prefabs.Count > 0)
            {
                int index = Mathf.Clamp(variantIndex, 0, prefabs.Count - 1);
                return prefabs[index];
            }

            return _defaultPrefab;
        }

        /// <summary>
        /// 指定タイプのプレハブ数を取得
        /// </summary>
        public int GetPrefabCount(CharacterType type)
        {
            InitializeCache();
            var prefabs = _prefabCache[(int)type];
            return prefabs?.Count ?? 0;
        }

        /// <summary>
        /// 指定タイプの全プレハブを取得
        /// </summary>
        public List<GameObject> GetAllPrefabs(CharacterType type)
        {
            InitializeCache();
            return _prefabCache[(int)type] ?? new List<GameObject>();
        }

        /// <summary>
        /// デフォルトプレハブを取得
        /// </summary>
        public GameObject DefaultPrefab => _defaultPrefab;

        /// <summary>
        /// キャッシュをクリア（エディタでの変更時など）
        /// </summary>
        public void ClearCache()
        {
            _isCacheInitialized = false;
            _prefabCache = null;
        }

        private void OnValidate()
        {
            // エディタで値が変更されたらキャッシュをクリア
            ClearCache();
        }

        /// <summary>
        /// 全タイプのエントリを自動生成（エディタ用）
        /// </summary>
        [ContextMenu("Generate All Type Entries")]
        private void GenerateAllTypeEntries()
        {
            var types = (CharacterType[])Enum.GetValues(typeof(CharacterType));
            _prefabEntries = new CharacterPrefabEntry[types.Length];

            for (int i = 0; i < types.Length; i++)
            {
                _prefabEntries[i] = new CharacterPrefabEntry
                {
                    type = types[i],
                    prefabs = new List<GameObject>()
                };
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
