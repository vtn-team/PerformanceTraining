namespace PerformanceTraining.AI.BehaviorTree
{
    /// <summary>
    /// シーケンスノード（AND条件）
    /// 子ノードを順番に評価し、全て成功した場合のみ成功を返す
    /// 1つでも失敗した場合は失敗を返す
    /// </summary>
    [System.Serializable]
    public class Sequence : Node
    {
        private int _currentChild = 0;

        public Sequence() : base("Sequence") { }
        public Sequence(string name) : base(name) { }

        public override NodeState Evaluate()
        {
            if (_children.Count == 0)
            {
                _state = NodeState.Success;
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

                    case NodeState.Failure:
                        _currentChild = 0;
                        _state = NodeState.Failure;
                        return _state;

                    case NodeState.Success:
                        _currentChild++;
                        break;
                }
            }

            // 全ての子ノードが成功
            _currentChild = 0;
            _state = NodeState.Success;
            return _state;
        }

        public override void Reset()
        {
            _currentChild = 0;
            base.Reset();
        }
    }
}
