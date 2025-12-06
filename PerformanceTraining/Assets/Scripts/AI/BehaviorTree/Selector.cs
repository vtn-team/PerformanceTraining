namespace PerformanceTraining.AI.BehaviorTree
{
    /// <summary>
    /// セレクターノード（OR条件）
    /// 子ノードを順番に評価し、最初に成功したノードで成功を返す
    /// 全て失敗した場合のみ失敗を返す
    /// </summary>
    [System.Serializable]
    public class Selector : Node
    {
        private int _currentChild = 0;

        public Selector() : base("Selector") { }
        public Selector(string name) : base(name) { }

        public override NodeState Evaluate()
        {
            if (_children.Count == 0)
            {
                _state = NodeState.Failure;
                return _state;
            }

            while (_currentChild < _children.Count)
            {
                var childState = _children[_currentChild].Evaluate();

                switch (childState)
                {
                    case NodeState.Running:
                        _state = NodeState.Running;
                        return _state;

                    case NodeState.Success:
                        _currentChild = 0;
                        _state = NodeState.Success;
                        return _state;

                    case NodeState.Failure:
                        _currentChild++;
                        break;
                }
            }

            // 全ての子ノードが失敗
            _currentChild = 0;
            _state = NodeState.Failure;
            return _state;
        }

        public override void Reset()
        {
            _currentChild = 0;
            base.Reset();
        }
    }
}
