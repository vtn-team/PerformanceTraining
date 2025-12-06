using UnityEngine;
using System.Collections.Generic;
using System;

namespace PerformanceTraining.Core
{
    /// <summary>
    /// 全キャラクターを管理するマネージャー
    /// バトルロイヤルの進行状態を監視
    /// </summary>
    public class CharacterManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterSpawner _spawner;

        [Header("Game State")]
        [SerializeField] private int _aliveCount;
        [SerializeField] private int _totalSpawned;
        [SerializeField] private int _totalKills;

        private List<Character> _allCharacters = new List<Character>();
        private List<Character> _aliveCharacters = new List<Character>();

        // イベント
        public event Action<Character> OnCharacterSpawned;
        public event Action<Character> OnCharacterDied;
        public event Action<Character> OnBattleRoyaleWinner;

        // プロパティ
        public int AliveCount => _aliveCount;
        public int TotalSpawned => _totalSpawned;
        public int TotalKills => _totalKills;
        public IReadOnlyList<Character> AllCharacters => _allCharacters;
        public IReadOnlyList<Character> AliveCharacters => _aliveCharacters;

        // 統計情報文字列（毎フレーム更新）
        private string _statsString;
        public string StatsString => _statsString;

        private void Awake()
        {
            if (_spawner == null)
            {
                _spawner = GetComponentInChildren<CharacterSpawner>();
            }
        }

        private void Update()
        {
            // TODO: パフォーマンス課題 - 毎フレーム文字列結合によるGC Alloc
            // 最適化: StringBuilderを使用、または更新頻度を下げる
            BuildStatsString();
        }

        /// <summary>
        /// 統計情報文字列を構築（毎フレーム）
        /// ボトルネック: 文字列結合による GC Alloc
        /// </summary>
        private void BuildStatsString()
        {
            // ボトルネック: 複数回の文字列結合と数値のToString()
            _statsString = "=== Battle Royale Stats ===\n";
            _statsString = _statsString + "Alive: " + _aliveCount.ToString() + " / " + _totalSpawned.ToString() + "\n";
            _statsString = _statsString + "Kills: " + _totalKills.ToString() + "\n";
            _statsString = _statsString + "Time: " + Time.time.ToString("F2") + "s\n";

            // タイプ別の生存数を追加（さらにGC Allocを増やす）
            _statsString = _statsString + "--- By Type ---\n";
            foreach (CharacterType type in Enum.GetValues(typeof(CharacterType)))
            {
                int count = 0;
                foreach (var character in _aliveCharacters)
                {
                    if (character != null && character.IsAlive && character.Type == type)
                    {
                        count++;
                    }
                }
                _statsString = _statsString + type.ToString() + ": " + count.ToString() + "\n";
            }
        }

        /// <summary>
        /// ゲームを初期化
        /// </summary>
        public void Initialize()
        {
            ClearAllCharacters();
            _totalSpawned = 0;
            _totalKills = 0;
            _aliveCount = 0;
        }

        /// <summary>
        /// 初期キャラクターをスポーン
        /// </summary>
        public void SpawnInitialCharacters(int count)
        {
            if (_spawner == null)
            {
                Debug.LogError("CharacterManager: Spawner is not assigned!");
                return;
            }

            var characters = _spawner.SpawnCharacters(count);
            foreach (var character in characters)
            {
                RegisterCharacter(character);
            }

            Debug.Log($"CharacterManager: Spawned {count} characters");
        }

        /// <summary>
        /// 追加でキャラクターをスポーン
        /// </summary>
        public void SpawnAdditionalCharacters(int count)
        {
            if (_spawner == null) return;

            int maxAllowed = GameConstants.MAX_ENEMY_COUNT - _aliveCount;
            int actualCount = Mathf.Min(count, maxAllowed);

            if (actualCount <= 0) return;

            var characters = _spawner.SpawnCharacters(actualCount);
            foreach (var character in characters)
            {
                RegisterCharacter(character);
            }
        }

        private void RegisterCharacter(Character character)
        {
            if (character == null) return;

            _allCharacters.Add(character);
            _aliveCharacters.Add(character);
            _totalSpawned++;
            _aliveCount++;

            // 死亡イベントを購読
            character.OnDeath += HandleCharacterDeath;
            character.OnKill += HandleKill;

            OnCharacterSpawned?.Invoke(character);
        }

        private void HandleCharacterDeath(Character character)
        {
            _aliveCharacters.Remove(character);
            _aliveCount--;

            OnCharacterDied?.Invoke(character);

            // 勝者チェック
            if (_aliveCount == 1 && _aliveCharacters.Count == 1)
            {
                var winner = _aliveCharacters[0];
                Debug.Log($"Battle Royale Winner: {winner.CharacterName} ({winner.Type})");
                OnBattleRoyaleWinner?.Invoke(winner);
            }
            else if (_aliveCount <= 0)
            {
                Debug.Log("Battle Royale: No survivors!");
            }
        }

        private void HandleKill(Character killer, Character victim)
        {
            _totalKills++;
        }

        /// <summary>
        /// 全キャラクターをクリア
        /// </summary>
        public void ClearAllCharacters()
        {
            foreach (var character in _allCharacters)
            {
                if (character != null)
                {
                    character.OnDeath -= HandleCharacterDeath;
                    character.OnKill -= HandleKill;
                }
            }

            _allCharacters.Clear();
            _aliveCharacters.Clear();

            if (_spawner != null)
            {
                _spawner.DespawnAllCharacters();
            }

            _aliveCount = 0;
        }

        /// <summary>
        /// 指定範囲内の全キャラクターにダメージを与える
        /// </summary>
        public int DealAreaDamage(Vector3 center, float radius, float damage)
        {
            int hitCount = 0;
            float radiusSqr = radius * radius;

            // TODO: パフォーマンス課題 - 全キャラクターを走査している
            // 最適化: 空間分割を使用して範囲内のキャラクターのみ検索
            foreach (var character in _aliveCharacters)
            {
                if (character == null || !character.IsAlive) continue;

                float distSqr = (character.transform.position - center).sqrMagnitude;
                if (distSqr <= radiusSqr)
                {
                    character.TakeDamage(damage, null);
                    hitCount++;
                }
            }

            return hitCount;
        }

        /// <summary>
        /// 指定キャラクターにバフを付与
        /// </summary>
        public void ApplyBuffToCharacter(Character character, float multiplier, float duration)
        {
            if (character != null && character.IsAlive)
            {
                character.ApplyBuff(multiplier, duration);
            }
        }

        /// <summary>
        /// ランダムなキャラクターにバフを付与
        /// </summary>
        public Character ApplyBuffToRandomCharacter(float multiplier, float duration)
        {
            if (_aliveCharacters.Count == 0) return null;

            var character = _aliveCharacters[UnityEngine.Random.Range(0, _aliveCharacters.Count)];
            character.ApplyBuff(multiplier, duration);
            return character;
        }

        /// <summary>
        /// 最も近いキャラクターを取得
        /// </summary>
        public Character GetNearestCharacter(Vector3 position, float maxDistance = float.MaxValue)
        {
            Character nearest = null;
            float nearestDistSqr = maxDistance * maxDistance;

            foreach (var character in _aliveCharacters)
            {
                if (character == null || !character.IsAlive) continue;

                float distSqr = (character.transform.position - position).sqrMagnitude;
                if (distSqr < nearestDistSqr)
                {
                    nearestDistSqr = distSqr;
                    nearest = character;
                }
            }

            return nearest;
        }

        /// <summary>
        /// 指定範囲内のキャラクターを取得
        /// </summary>
        public List<Character> GetCharactersInRadius(Vector3 center, float radius)
        {
            var result = new List<Character>();
            float radiusSqr = radius * radius;

            foreach (var character in _aliveCharacters)
            {
                if (character == null || !character.IsAlive) continue;

                float distSqr = (character.transform.position - center).sqrMagnitude;
                if (distSqr <= radiusSqr)
                {
                    result.Add(character);
                }
            }

            return result;
        }

        /// <summary>
        /// 生存キャラクターの統計を取得
        /// </summary>
        public Dictionary<CharacterType, int> GetAliveCountByType()
        {
            var counts = new Dictionary<CharacterType, int>();
            foreach (CharacterType type in Enum.GetValues(typeof(CharacterType)))
            {
                counts[type] = 0;
            }

            foreach (var character in _aliveCharacters)
            {
                if (character != null && character.IsAlive)
                {
                    counts[character.Type]++;
                }
            }

            return counts;
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            // デバッグ表示
            GUILayout.BeginArea(new Rect(10, 10, 200, 100));
            GUILayout.Label($"Alive: {_aliveCount}");
            GUILayout.Label($"Total Spawned: {_totalSpawned}");
            GUILayout.Label($"Total Kills: {_totalKills}");
            GUILayout.EndArea();
        }
#endif
    }
}
