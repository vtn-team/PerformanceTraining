using System.Collections.Generic;
using UnityEngine;

namespace PerformanceTraining.AI.BehaviorTree
{
    /// <summary>
    /// ビヘイビアツリーのノード基底クラス
    /// </summary>
    [System.Serializable]
    public abstract class Node
    {
        [SerializeField] protected string _name;
        [SerializeField] protected NodeState _state = NodeState.Failure;

        protected Node _parent;
        protected List<Node> _children = new List<Node>();

        // 共有データ（ブラックボード）
        protected Dictionary<string, object> _blackboard;

        public string Name => _name;
        public NodeState State => _state;
        public Node Parent => _parent;
        public List<Node> Children => _children;

        public Node()
        {
            _name = GetType().Name;
        }

        public Node(string name)
        {
            _name = name;
        }

        /// <summary>
        /// ブラックボードを設定
        /// </summary>
        public void SetBlackboard(Dictionary<string, object> blackboard)
        {
            _blackboard = blackboard;
            foreach (var child in _children)
            {
                child.SetBlackboard(blackboard);
            }
        }

        /// <summary>
        /// ブラックボードからデータを取得
        /// </summary>
        protected T GetData<T>(string key)
        {
            if (_blackboard != null && _blackboard.TryGetValue(key, out object value))
            {
                return (T)value;
            }
            return default;
        }

        /// <summary>
        /// ブラックボードにデータを設定
        /// </summary>
        protected void SetData(string key, object value)
        {
            if (_blackboard != null)
            {
                _blackboard[key] = value;
            }
        }

        /// <summary>
        /// 子ノードを追加
        /// </summary>
        public Node AddChild(Node child)
        {
            child._parent = this;
            child._blackboard = _blackboard;
            _children.Add(child);
            return this;
        }

        /// <summary>
        /// ノードの評価（メインロジック）
        /// </summary>
        public abstract NodeState Evaluate();

        /// <summary>
        /// ノードのリセット
        /// </summary>
        public virtual void Reset()
        {
            _state = NodeState.Failure;
            foreach (var child in _children)
            {
                child.Reset();
            }
        }
    }
}
