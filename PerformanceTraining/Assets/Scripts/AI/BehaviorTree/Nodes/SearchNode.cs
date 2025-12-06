using UnityEngine;
using PerformanceTraining.Core;
using System.Collections.Generic;

namespace PerformanceTraining.AI.BehaviorTree.Nodes
{
    /// <summary>
    /// 索敵ノード
    /// 周囲の敵を探し、最も近い敵をターゲットとして設定する
    /// </summary>
    [System.Serializable]
    public class SearchNode : Node
    {
        [SerializeField] private float _searchRadius = 20f;
        [SerializeField] private bool _preferLowHealth = false;
        [SerializeField] private bool _preferSameType = false;

        public SearchNode() : base("Search") { }

        public SearchNode(float searchRadius) : base("Search")
        {
            _searchRadius = searchRadius;
        }

        public override NodeState Evaluate()
        {
            var owner = GetData<Character>(BehaviorTreeBase.KEY_OWNER);
            if (owner == null || !owner.IsAlive)
            {
                _state = NodeState.Failure;
                return _state;
            }

            // 既にターゲットがいて、生きている場合は成功
            var currentTarget = GetData<Character>(BehaviorTreeBase.KEY_TARGET);
            if (currentTarget != null && currentTarget.IsAlive)
            {
                float distToTarget = Vector3.Distance(owner.transform.position, currentTarget.transform.position);
                if (distToTarget <= _searchRadius)
                {
                    SetData(BehaviorTreeBase.KEY_TARGET_POSITION, currentTarget.transform.position);
                    _state = NodeState.Success;
                    return _state;
                }
            }

            // 新しいターゲットを探す
            Character bestTarget = FindBestTarget(owner);

            if (bestTarget != null)
            {
                SetData(BehaviorTreeBase.KEY_TARGET, bestTarget);
                SetData(BehaviorTreeBase.KEY_TARGET_POSITION, bestTarget.transform.position);
                owner.SetTarget(bestTarget);
                owner.SetState(CharacterState.Chasing);

                _state = NodeState.Success;
                return _state;
            }

            // ターゲットが見つからない
            SetData(BehaviorTreeBase.KEY_TARGET, null);
            owner.SetTarget(null);
            owner.SetState(CharacterState.Searching);

            _state = NodeState.Failure;
            return _state;
        }

        private Character FindBestTarget(Character owner)
        {
            // TODO: パフォーマンス課題 - これは非効率な実装
            // 最適化: 空間分割（Grid）を使用して近傍のみ検索
            var allCharacters = UnityEngine.Object.FindObjectsByType<Character>(FindObjectsSortMode.None);

            Character bestTarget = null;
            float bestScore = float.MaxValue;

            foreach (var character in allCharacters)
            {
                // 自分自身はスキップ
                if (character == owner) continue;

                // 死んでいるキャラクターはスキップ
                if (!character.IsAlive) continue;

                float distance = Vector3.Distance(owner.transform.position, character.transform.position);

                // 範囲外はスキップ
                if (distance > _searchRadius) continue;

                // スコア計算（低いほど優先）
                float score = distance;

                // 低HP優先
                if (_preferLowHealth)
                {
                    score *= character.HealthPercent;
                }

                // 同タイプ優先（バトルロイヤルの戦略として）
                if (_preferSameType && character.Type == owner.Type)
                {
                    score *= 0.8f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = character;
                }
            }

            return bestTarget;
        }
    }
}
