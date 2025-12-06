using System.Collections.Generic;
using UnityEngine;
using PerformanceTraining.Core;

namespace PerformanceTraining.AI.BehaviorTree
{
    /// <summary>
    /// ビヘイビアツリーのMonoBehaviour基底クラス
    /// インスペクタから調整可能
    /// </summary>
    public abstract class BehaviorTreeBase : MonoBehaviour
    {
        [Header("Behavior Tree Settings")]
        [SerializeField] protected bool _isActive = true;
        [SerializeField] protected float _updateInterval = 0.1f;

        [Header("Debug")]
        [SerializeField] protected bool _showDebugInfo = false;
        [SerializeField] protected string _currentNodeName;
        [SerializeField] protected NodeState _currentState;

        // デバッグ用AIログ文字列（毎フレーム更新）
        private string _aiDebugLog;
        public string AIDebugLog => _aiDebugLog;

        protected Node _rootNode;
        protected Dictionary<string, object> _blackboard = new Dictionary<string, object>();
        protected Character _owner;
        protected float _updateTimer;

        // ブラックボードキー定数
        public const string KEY_OWNER = "Owner";
        public const string KEY_TARGET = "Target";
        public const string KEY_TARGET_POSITION = "TargetPosition";
        public const string KEY_NEAREST_ENEMY = "NearestEnemy";
        public const string KEY_ENEMIES_IN_RANGE = "EnemiesInRange";
        public const string KEY_HEALTH_PERCENT = "HealthPercent";
        public const string KEY_IS_LOW_HEALTH = "IsLowHealth";

        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        public Character Owner => _owner;

        protected virtual void Awake()
        {
            _owner = GetComponent<Character>();
        }

        protected virtual void Start()
        {
            // ブラックボード初期化
            InitializeBlackboard();

            // ツリー構築
            _rootNode = BuildTree();

            if (_rootNode != null)
            {
                _rootNode.SetBlackboard(_blackboard);
            }
        }

        protected virtual void Update()
        {
            if (!_isActive || _rootNode == null) return;
            if (_owner != null && !_owner.IsAlive) return;

            // TODO: パフォーマンス課題 - 毎フレーム文字列結合によるGC Alloc
            // 最適化: StringBuilderを使用して再利用する
            BuildAIDebugLog();

            _updateTimer += Time.deltaTime;
            if (_updateTimer >= _updateInterval)
            {
                _updateTimer = 0f;

                // ブラックボード更新
                UpdateBlackboard();

                // ツリー評価
                _currentState = _rootNode.Evaluate();

                if (_showDebugInfo)
                {
                    UpdateDebugInfo();
                }
            }
        }

        /// <summary>
        /// AIデバッグログを構築（毎フレーム）
        /// ボトルネック: 文字列結合による GC Alloc
        /// </summary>
        private void BuildAIDebugLog()
        {
            if (_owner == null) return;

            // ボトルネック: 補間文字列と + 演算子の混在（大量のGC Alloc）
            string ownerName = _owner.CharacterName;
            string ownerState = _owner.State.ToString();
            float hp = _owner.HealthPercent * 100f;

            // 毎フレーム新しい文字列を生成
            _aiDebugLog = $"[AI] {ownerName}";
            _aiDebugLog = _aiDebugLog + " | State: " + ownerState;
            _aiDebugLog = _aiDebugLog + " | HP: " + hp.ToString("F1") + "%";
            _aiDebugLog = _aiDebugLog + " | Node: " + _currentNodeName;
            _aiDebugLog = _aiDebugLog + " | Time: " + Time.time.ToString("F2");

            // ターゲット情報も追加
            var target = GetBlackboardData<Character>(KEY_TARGET);
            if (target != null && target.IsAlive)
            {
                float dist = Vector3.Distance(_owner.transform.position, target.transform.position);
                _aiDebugLog = _aiDebugLog + " | Target: " + target.CharacterName + " (dist: " + dist.ToString("F1") + ")";
            }
        }

        /// <summary>
        /// ブラックボードの初期化
        /// </summary>
        protected virtual void InitializeBlackboard()
        {
            _blackboard[KEY_OWNER] = _owner;
            _blackboard[KEY_TARGET] = null;
            _blackboard[KEY_TARGET_POSITION] = Vector3.zero;
            _blackboard[KEY_NEAREST_ENEMY] = null;
            _blackboard[KEY_ENEMIES_IN_RANGE] = new List<Character>();
            _blackboard[KEY_HEALTH_PERCENT] = 1f;
            _blackboard[KEY_IS_LOW_HEALTH] = false;
        }

        /// <summary>
        /// ブラックボードの更新（毎評価時）
        /// </summary>
        protected virtual void UpdateBlackboard()
        {
            if (_owner != null)
            {
                _blackboard[KEY_HEALTH_PERCENT] = _owner.HealthPercent;
                _blackboard[KEY_IS_LOW_HEALTH] = _owner.HealthPercent < 0.3f;
            }
        }

        /// <summary>
        /// ビヘイビアツリーの構築（派生クラスで実装）
        /// </summary>
        protected abstract Node BuildTree();

        /// <summary>
        /// デバッグ情報の更新
        /// </summary>
        protected virtual void UpdateDebugInfo()
        {
            _currentNodeName = GetActiveNodeName(_rootNode);
        }

        private string GetActiveNodeName(Node node)
        {
            if (node == null) return "None";
            if (node.State == NodeState.Running)
            {
                foreach (var child in node.Children)
                {
                    if (child.State == NodeState.Running)
                    {
                        return GetActiveNodeName(child);
                    }
                }
                return node.Name;
            }
            return node.Name;
        }

        /// <summary>
        /// ツリーをリセット
        /// </summary>
        public void ResetTree()
        {
            _rootNode?.Reset();
        }

        /// <summary>
        /// ブラックボードからデータを取得
        /// </summary>
        public T GetBlackboardData<T>(string key)
        {
            if (_blackboard.TryGetValue(key, out object value))
            {
                return (T)value;
            }
            return default;
        }

        /// <summary>
        /// ブラックボードにデータを設定
        /// </summary>
        public void SetBlackboardData(string key, object value)
        {
            _blackboard[key] = value;
        }
    }
}
