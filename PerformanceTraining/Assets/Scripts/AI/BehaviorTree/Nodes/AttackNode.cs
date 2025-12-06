using UnityEngine;
using PerformanceTraining.Core;

namespace PerformanceTraining.AI.BehaviorTree.Nodes
{
    /// <summary>
    /// 攻撃ノード
    /// ターゲットが攻撃範囲内にいる場合に攻撃を実行する
    /// </summary>
    [System.Serializable]
    public class AttackNode : Node
    {
        public AttackNode() : base("Attack") { }

        public override NodeState Evaluate()
        {
            var owner = GetData<Character>(BehaviorTreeBase.KEY_OWNER);
            var target = GetData<Character>(BehaviorTreeBase.KEY_TARGET);

            if (owner == null || !owner.IsAlive)
            {
                _state = NodeState.Failure;
                return _state;
            }

            // ターゲットがいない、または死んでいる
            if (target == null || !target.IsAlive)
            {
                // ターゲットをクリア
                SetData(BehaviorTreeBase.KEY_TARGET, null);
                owner.SetTarget(null);
                _state = NodeState.Failure;
                return _state;
            }

            float distance = Vector3.Distance(owner.transform.position, target.transform.position);

            // 攻撃範囲内かチェック
            if (distance <= owner.Stats.attackRange)
            {
                // 攻撃可能かチェック（クールダウン）
                if (owner.CanAttack())
                {
                    // 攻撃実行
                    owner.SetState(CharacterState.Attacking);
                    owner.Attack(target);

                    // ターゲットが死亡した場合
                    if (!target.IsAlive)
                    {
                        SetData(BehaviorTreeBase.KEY_TARGET, null);
                        owner.SetTarget(null);
                    }

                    _state = NodeState.Success;
                    return _state;
                }
                else
                {
                    // クールダウン中 - 攻撃待機
                    owner.SetState(CharacterState.Attacking);
                    _state = NodeState.Running;
                    return _state;
                }
            }

            // 攻撃範囲外
            _state = NodeState.Failure;
            return _state;
        }
    }

    /// <summary>
    /// 攻撃範囲チェックノード（条件ノード）
    /// ターゲットが攻撃範囲内にいるかどうかをチェック
    /// </summary>
    [System.Serializable]
    public class IsInAttackRangeNode : Node
    {
        public IsInAttackRangeNode() : base("IsInAttackRange") { }

        public override NodeState Evaluate()
        {
            var owner = GetData<Character>(BehaviorTreeBase.KEY_OWNER);
            var target = GetData<Character>(BehaviorTreeBase.KEY_TARGET);

            if (owner == null || target == null || !target.IsAlive)
            {
                _state = NodeState.Failure;
                return _state;
            }

            float distance = Vector3.Distance(owner.transform.position, target.transform.position);

            if (distance <= owner.Stats.attackRange)
            {
                _state = NodeState.Success;
                return _state;
            }

            _state = NodeState.Failure;
            return _state;
        }
    }
}
