using UnityEngine;
using System.Collections.Generic;
using System;

#pragma warning disable 0414 // 課題用フィールド: 学生が実装時に使用

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

        // 統計情報文字列
        private string _statsString;
        public string StatsString => _statsString;

        // 行動ログ
        private readonly ActionLogger _actionLogger = new ActionLogger();
        public string ActionLogString => _actionLogger.LogString;

        private void Awake()
        {
            if (_spawner == null)
            {
                _spawner = GetComponentInChildren<CharacterSpawner>();
            }
        }

        private void Update()
        {
            BuildStatsString();

            // 行動ログの更新
            _actionLogger.UpdateIfDirty();
        }

        /// <summary>
        /// 統計情報文字列を構築
        /// </summary>
        private void BuildStatsString()
        {
            _statsString = "=== Battle Royale Stats ===\n";
            _statsString = _statsString + "Alive: " + _aliveCount.ToString() + " / " + _totalSpawned.ToString() + "\n";
            _statsString = _statsString + "Kills: " + _totalKills.ToString() + "\n";
            _statsString = _statsString + "Time: " + Time.time.ToString("F2") + "s\n";

            // タイプ別の生存数を追加
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

            // 行動ログに死亡を記録
            _actionLogger.LogDeath(character);

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

            // 行動ログにキルを記録
            if (killer != null && victim != null)
            {
                _actionLogger.LogKill(killer, victim);
            }
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

        // 空間分割用のデータ構造
        private Dictionary<int, List<Character>> _spatialGrid = new Dictionary<int, List<Character>>();
        private List<Character> _nearbyResult = new List<Character>();

        private float _cellSize = GameConstants.CELL_SIZE;
        private int _gridWidth = GameConstants.GRID_SIZE;

        /// <summary>
        /// 座標からセルインデックスを計算する
        /// </summary>
        public int GetCellIndex(Vector3 position)
        {
            // TODO: ワールド座標を1次元のセルインデックスに変換する処理を実装してください
            return 0;
        }

        /// <summary>
        /// 空間グリッドを更新する
        /// </summary>
        public void UpdateSpatialGrid()
        {
            // TODO: 全キャラクターをグリッドに登録する処理を実装してください
        }

        /// <summary>
        /// 指定位置周辺のキャラクターを取得する（最適化版）
        /// </summary>
        public List<Character> GetNearbyCharactersOptimized(Vector3 position, Character excludeCharacter)
        {
            // TODO: 空間分割を使って近傍のキャラクターのみを返すよう実装してください
            // 現在は非最適化版: 全キャラクターを返す
            _nearbyResult.Clear();
            foreach (var c in _aliveCharacters)
            {
                if (c != null && c != excludeCharacter && c.IsAlive)
                {
                    _nearbyResult.Add(c);
                }
            }
            return _nearbyResult;
        }

        [SerializeField] private float _maxAttackDistance = 20f;
        [SerializeField] private float _minTargetHP = 10f;
        [SerializeField] private float _maxTargetHP = 100f;
        private float _lastExecutionTimeMs;
        private int _lastProcessedCount;

        /// <summary>
        /// 攻撃対象を検索する
        /// </summary>
        public Character FindBestAttackTarget(Character attacker)
        {
            if (attacker == null || !attacker.IsAlive) return null;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // TODO: この処理を最適化してください
            // ここから
            List<Character> candidates = new List<Character>();
            foreach (var c in _aliveCharacters)
            {
                if (c != null && c != attacker && c.IsAlive)
                {
                    candidates.Add(c);
                }
            }

            candidates = SortByPathfindingDistance(candidates, attacker);
            candidates = FilterByHP(candidates, _minTargetHP, _maxTargetHP);
            candidates = FilterByDistance(candidates, attacker, _maxAttackDistance);
            // ここまでの処理を並び替えましょう

            stopwatch.Stop();
            _lastExecutionTimeMs = (float)stopwatch.Elapsed.TotalMilliseconds;
            _lastProcessedCount = candidates.Count;

            return candidates.Count > 0 ? candidates[0] : null;
        }

        /// <summary>
        /// 距離条件でフィルタする（軽い処理）
        /// </summary>
        private List<Character> FilterByDistance(List<Character> characters, Character attacker, float maxDistance)
        {
            if (attacker == null) return characters;

            var result = new List<Character>();
            Vector3 attackerPos = attacker.transform.position;
            float maxDistSqr = maxDistance * maxDistance;

            foreach (var c in characters)
            {
                if (c == null) continue;
                float distSqr = (c.transform.position - attackerPos).sqrMagnitude;
                if (distSqr <= maxDistSqr)
                {
                    result.Add(c);
                }
            }
            return result;
        }

        /// <summary>
        /// HP条件でフィルタする（軽い処理）
        /// </summary>
        private List<Character> FilterByHP(List<Character> characters, float minHP, float maxHP)
        {
            var result = new List<Character>();
            foreach (var c in characters)
            {
                if (c == null) continue;
                float hp = c.Stats.currentHealth;
                if (hp >= minHP && hp <= maxHP)
                {
                    result.Add(c);
                }
            }
            return result;
        }

        /// <summary>
        /// 経路探索して到達距離順にソートする（非常に重い処理）
        /// </summary>
        private List<Character> SortByPathfindingDistance(List<Character> characters, Character attacker)
        {
            if (attacker == null || characters.Count == 0) return characters;

            var withDistance = new List<(Character character, float distance)>();
            foreach (var c in characters)
            {
                if (c == null) continue;
                float pathDistance = CalculatePathDistance(attacker.transform.position, c.transform.position);
                withDistance.Add((c, pathDistance));
            }

            withDistance.Sort((a, b) => a.distance.CompareTo(b.distance));

            var result = new List<Character>();
            foreach (var item in withDistance)
            {
                result.Add(item.character);
            }
            return result;
        }

        private float CalculatePathDistance(Vector3 start, Vector3 end)
        {
            // 重い経路探索をシミュレート
            float directDistance = Vector3.Distance(start, end);
            int gridResolution = 20;
            float totalCost = 0f;
            Vector3 current = start;
            Vector3 direction = (end - start).normalized;
            float stepSize = directDistance / gridResolution;

            for (int i = 0; i < gridResolution; i++)
            {
                Vector3 next = current + direction * stepSize;
                float obstacleCost = SimulateObstacleCheck(current, next);
                totalCost += stepSize + obstacleCost;
                current = next;
            }
            return totalCost;
        }

        private float SimulateObstacleCheck(Vector3 from, Vector3 to)
        {
            float cost = 0f;
            for (int i = 0; i < 100; i++)
            {
                float angle = Mathf.Atan2(to.z - from.z, to.x - from.x);
                float dist = Mathf.Sqrt((to.x - from.x) * (to.x - from.x) + (to.z - from.z) * (to.z - from.z));
                cost += Mathf.Sin(angle) * Mathf.Cos(angle) * 0.001f;
                cost += dist * 0.0001f;
            }
            return Mathf.Abs(cost);
        }

        /// <summary>
        /// 最後の検索実行時間を取得（テスト用）
        /// </summary>
        public float GetLastExecutionTimeMs() => _lastExecutionTimeMs;
        public int GetLastProcessedCount() => _lastProcessedCount;

#if UNITY_EDITOR
        private void OnGUI()
        {
            // デバッグ表示（左上）
            GUILayout.BeginArea(new Rect(10, 10, 200, 100));
            GUILayout.Label($"Alive: {_aliveCount}");
            GUILayout.Label($"Total Spawned: {_totalSpawned}");
            GUILayout.Label($"Total Kills: {_totalKills}");
            GUILayout.EndArea();

            // 行動ログ表示（右上）
            GUILayout.BeginArea(new Rect(Screen.width - 360, 10, 350, 250));
            GUI.Box(new Rect(0, 0, 350, 250), "");
            GUILayout.Label(ActionLogString, GUILayout.MaxWidth(340));
            GUILayout.EndArea();
        }
#endif
    }
}
