using UnityEngine;
using PerformanceTraining.Core;

namespace PerformanceTraining.AI.BehaviorTree.Nodes
{
    /// <summary>
    /// 反応があるかチェック（条件ノード）
    /// </summary>
    [System.Serializable]
    public class HasReactionNode : Node
    {
        public HasReactionNode() : base("HasReaction") { }

        public override NodeState Evaluate()
        {
            var owner = GetData<Character>(BehaviorTreeBase.KEY_OWNER);

            if (owner != null && owner.HasPendingReaction)
            {
                _state = NodeState.Success;
                return _state;
            }

            _state = NodeState.Failure;
            return _state;
        }
    }

    /// <summary>
    /// 逃亡反応かチェック（条件ノード）
    /// </summary>
    [System.Serializable]
    public class IsFleeReactionNode : Node
    {
        public IsFleeReactionNode() : base("IsFleeReaction") { }

        public override NodeState Evaluate()
        {
            var owner = GetData<Character>(BehaviorTreeBase.KEY_OWNER);

            if (owner != null && owner.PendingReaction == AttackReactionType.Flee)
            {
                _state = NodeState.Success;
                return _state;
            }

            _state = NodeState.Failure;
            return _state;
        }
    }

    /// <summary>
    /// 反撃反応かチェック（条件ノード）
    /// </summary>
    [System.Serializable]
    public class IsCounterAttackReactionNode : Node
    {
        public IsCounterAttackReactionNode() : base("IsCounterAttackReaction") { }

        public override NodeState Evaluate()
        {
            var owner = GetData<Character>(BehaviorTreeBase.KEY_OWNER);

            if (owner != null && owner.PendingReaction == AttackReactionType.CounterAttack)
            {
                _state = NodeState.Success;
                return _state;
            }

            _state = NodeState.Failure;
            return _state;
        }
    }

    /// <summary>
    /// 攻撃者から逃げるノード（1.2倍速）
    /// </summary>
    [System.Serializable]
    public class FleeFromAttackerNode : Node
    {
        [SerializeField] private float _safeDistance = 15f;

        public FleeFromAttackerNode() : base("FleeFromAttacker") { }

        public FleeFromAttackerNode(float safeDistance) : base("FleeFromAttacker")
        {
            _safeDistance = safeDistance;
        }

        public override NodeState Evaluate()
        {
            var owner = GetData<Character>(BehaviorTreeBase.KEY_OWNER);

            if (owner == null || !owner.IsAlive)
            {
                _state = NodeState.Failure;
                return _state;
            }

            var attacker = owner.LastAttacker;
            if (attacker == null || !attacker.IsAlive)
            {
                owner.ClearReaction();
                _state = NodeState.Failure;
                return _state;
            }

            float distance = Vector3.Distance(owner.transform.position, attacker.transform.position);

            // 安全距離に到達
            if (distance >= _safeDistance)
            {
                owner.ClearReaction();
                owner.SetState(CharacterState.Idle);
                _state = NodeState.Success;
                return _state;
            }

            // 高速で逃走中
            owner.SetState(CharacterState.Fleeing);
            owner.FleeFrom(attacker.transform.position);

            _state = NodeState.Running;
            return _state;
        }
    }

    /// <summary>
    /// 反撃ノード：攻撃者をターゲットに切り替え
    /// </summary>
    [System.Serializable]
    public class CounterAttackNode : Node
    {
        public CounterAttackNode() : base("CounterAttack") { }

        public override NodeState Evaluate()
        {
            var owner = GetData<Character>(BehaviorTreeBase.KEY_OWNER);

            if (owner == null || !owner.IsAlive)
            {
                _state = NodeState.Failure;
                return _state;
            }

            var attacker = owner.LastAttacker;
            if (attacker == null || !attacker.IsAlive)
            {
                owner.ClearReaction();
                _state = NodeState.Failure;
                return _state;
            }

            // 反撃：ターゲットを切り替え
            owner.CounterAttack();

            // ブラックボードも更新
            SetData(BehaviorTreeBase.KEY_TARGET, attacker);
            SetData(BehaviorTreeBase.KEY_TARGET_POSITION, attacker.transform.position);

            _state = NodeState.Success;
            return _state;
        }
    }
}
