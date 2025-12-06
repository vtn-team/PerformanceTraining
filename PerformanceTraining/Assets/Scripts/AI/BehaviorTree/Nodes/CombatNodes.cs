using UnityEngine;
using PerformanceTraining.Core;

namespace PerformanceTraining.AI.BehaviorTree.Nodes
{
    /// <summary>
    /// 追跡ノード
    /// ターゲットに向かって移動する
    /// </summary>
    [System.Serializable]
    public class ChaseNode : Node
    {
        [SerializeField] private float _stopDistance = 1.5f;

        public ChaseNode() : base("Chase") { }

        public ChaseNode(float stopDistance) : base("Chase")
        {
            _stopDistance = stopDistance;
        }

        public override NodeState Evaluate()
        {
            var owner = GetData<Character>(BehaviorTreeBase.KEY_OWNER);
            var target = GetData<Character>(BehaviorTreeBase.KEY_TARGET);

            if (owner == null || !owner.IsAlive)
            {
                _state = NodeState.Failure;
                return _state;
            }

            // ターゲットがいない
            if (target == null || !target.IsAlive)
            {
                _state = NodeState.Failure;
                return _state;
            }

            Vector3 targetPosition = target.transform.position;
            float distance = Vector3.Distance(owner.transform.position, targetPosition);

            // 停止距離に到達
            if (distance <= _stopDistance)
            {
                _state = NodeState.Success;
                return _state;
            }

            // 追跡中
            owner.SetState(CharacterState.Chasing);
            owner.MoveTowards(targetPosition);

            // ターゲット位置を更新
            SetData(BehaviorTreeBase.KEY_TARGET_POSITION, targetPosition);

            _state = NodeState.Running;
            return _state;
        }
    }

    /// <summary>
    /// 逃走ノード
    /// ターゲットまたは脅威から逃げる
    /// </summary>
    [System.Serializable]
    public class FleeNode : Node
    {
        [SerializeField] private float _safeDistance = 15f;
        [SerializeField] private float _healthThreshold = 0.3f;

        public FleeNode() : base("Flee") { }

        public FleeNode(float safeDistance, float healthThreshold) : base("Flee")
        {
            _safeDistance = safeDistance;
            _healthThreshold = healthThreshold;
        }

        public override NodeState Evaluate()
        {
            var owner = GetData<Character>(BehaviorTreeBase.KEY_OWNER);
            var target = GetData<Character>(BehaviorTreeBase.KEY_TARGET);

            if (owner == null || !owner.IsAlive)
            {
                _state = NodeState.Failure;
                return _state;
            }

            // 逃げる対象がいない
            if (target == null || !target.IsAlive)
            {
                _state = NodeState.Failure;
                return _state;
            }

            float distance = Vector3.Distance(owner.transform.position, target.transform.position);

            // 安全距離に到達
            if (distance >= _safeDistance)
            {
                owner.SetState(CharacterState.Idle);
                _state = NodeState.Success;
                return _state;
            }

            // 逃走中
            owner.SetState(CharacterState.Fleeing);
            owner.MoveAwayFrom(target.transform.position);

            _state = NodeState.Running;
            return _state;
        }
    }

    /// <summary>
    /// HP低下チェックノード（条件ノード）
    /// HPが閾値以下かどうかをチェック
    /// </summary>
    [System.Serializable]
    public class IsLowHealthNode : Node
    {
        [SerializeField] private float _threshold = 0.3f;

        public IsLowHealthNode() : base("IsLowHealth") { }

        public IsLowHealthNode(float threshold) : base("IsLowHealth")
        {
            _threshold = threshold;
        }

        public override NodeState Evaluate()
        {
            var owner = GetData<Character>(BehaviorTreeBase.KEY_OWNER);

            if (owner == null)
            {
                _state = NodeState.Failure;
                return _state;
            }

            if (owner.HealthPercent <= _threshold)
            {
                _state = NodeState.Success;
                return _state;
            }

            _state = NodeState.Failure;
            return _state;
        }
    }

    /// <summary>
    /// ターゲット存在チェックノード（条件ノード）
    /// </summary>
    [System.Serializable]
    public class HasTargetNode : Node
    {
        public HasTargetNode() : base("HasTarget") { }

        public override NodeState Evaluate()
        {
            var target = GetData<Character>(BehaviorTreeBase.KEY_TARGET);

            if (target != null && target.IsAlive)
            {
                _state = NodeState.Success;
                return _state;
            }

            _state = NodeState.Failure;
            return _state;
        }
    }

    /// <summary>
    /// 待機ノード
    /// 何もせずに待機する
    /// </summary>
    [System.Serializable]
    public class IdleNode : Node
    {
        [SerializeField] private float _waitTime = 1f;
        private float _timer;

        public IdleNode() : base("Idle") { }

        public IdleNode(float waitTime) : base("Idle")
        {
            _waitTime = waitTime;
        }

        public override NodeState Evaluate()
        {
            var owner = GetData<Character>(BehaviorTreeBase.KEY_OWNER);

            if (owner != null)
            {
                owner.SetState(CharacterState.Idle);
            }

            _timer += Time.deltaTime;

            if (_timer >= _waitTime)
            {
                _timer = 0f;
                _state = NodeState.Success;
                return _state;
            }

            _state = NodeState.Running;
            return _state;
        }

        public override void Reset()
        {
            _timer = 0f;
            base.Reset();
        }
    }

    /// <summary>
    /// ランダム移動ノード
    /// ランダムな方向に移動する（索敵中の徘徊）
    /// </summary>
    [System.Serializable]
    public class WanderNode : Node
    {
        [SerializeField] private float _wanderRadius = 10f;
        [SerializeField] private float _changeDirectionInterval = 2f;

        private Vector3 _targetPosition;
        private float _timer;

        public WanderNode() : base("Wander") { }

        public WanderNode(float wanderRadius) : base("Wander")
        {
            _wanderRadius = wanderRadius;
        }

        public override NodeState Evaluate()
        {
            var owner = GetData<Character>(BehaviorTreeBase.KEY_OWNER);

            if (owner == null || !owner.IsAlive)
            {
                _state = NodeState.Failure;
                return _state;
            }

            _timer += Time.deltaTime;

            // 方向変更
            if (_timer >= _changeDirectionInterval || _targetPosition == Vector3.zero)
            {
                _timer = 0f;
                _targetPosition = GetRandomPosition(owner.transform.position);
            }

            // 目標に到達したら新しい目標を設定
            float distance = Vector3.Distance(owner.transform.position, _targetPosition);
            if (distance < 1f)
            {
                _targetPosition = GetRandomPosition(owner.transform.position);
            }

            // 移動
            owner.SetState(CharacterState.Searching);
            owner.MoveTowards(_targetPosition);

            _state = NodeState.Running;
            return _state;
        }

        private Vector3 GetRandomPosition(Vector3 currentPosition)
        {
            Vector2 randomDir = Random.insideUnitCircle * _wanderRadius;
            Vector3 targetPos = currentPosition + new Vector3(randomDir.x, 0, randomDir.y);

            // フィールド範囲内にクランプ
            float halfSize = GameConstants.FIELD_HALF_SIZE;
            targetPos.x = Mathf.Clamp(targetPos.x, -halfSize, halfSize);
            targetPos.z = Mathf.Clamp(targetPos.z, -halfSize, halfSize);

            return targetPos;
        }

        public override void Reset()
        {
            _timer = 0f;
            _targetPosition = Vector3.zero;
            base.Reset();
        }
    }
}
