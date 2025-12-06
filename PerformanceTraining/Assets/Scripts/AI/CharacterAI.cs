using UnityEngine;
using PerformanceTraining.Core;
using PerformanceTraining.AI.BehaviorTree;
using PerformanceTraining.AI.BehaviorTree.Nodes;

namespace PerformanceTraining.AI
{
    /// <summary>
    /// キャラクターのAI制御
    /// ビヘイビアツリーを使用してAIの行動を決定する
    /// </summary>
    public class CharacterAI : BehaviorTreeBase
    {
        [Header("AI Parameters")]
        [SerializeField] private float _searchRadius = 20f;
        [SerializeField] private float _fleeHealthThreshold = 0.25f;
        [SerializeField] private float _safeDistance = 15f;

        [Header("Behavior Settings")]
        [SerializeField] private bool _canFlee = true;
        [SerializeField] private bool _preferLowHealthTargets = false;

        /// <summary>
        /// ビヘイビアツリーの構築
        ///
        /// ツリー構造:
        /// Root (Selector)
        /// ├─ 反応ブランチ (Sequence): 攻撃された時の反応（最優先）
        /// │   ├─ HasReaction
        /// │   └─ Selector
        /// │       ├─ 逃亡 (Sequence): IsFleeReaction -> FleeFromAttacker
        /// │       └─ 反撃 (Sequence): IsCounterAttackReaction -> CounterAttack
        /// │
        /// ├─ 逃走ブランチ (Sequence): HP低下時に逃げる
        /// │   ├─ IsLowHealth
        /// │   └─ Flee
        /// │
        /// ├─ 戦闘ブランチ (Sequence): ターゲットがいれば攻撃
        /// │   ├─ HasTarget
        /// │   └─ Selector
        /// │       ├─ Attack (攻撃範囲内なら攻撃)
        /// │       └─ Chase (範囲外なら追跡)
        /// │
        /// └─ 索敵ブランチ (Sequence): ターゲットを探す
        ///     ├─ Search
        ///     └─ Chase/Wander
        /// </summary>
        protected override Node BuildTree()
        {
            // ルートノード（セレクター）
            var root = new Selector("Root");

            // 0. 反応ブランチ（攻撃された時の反応 - 最優先）
            var reactionSequence = new Sequence("ReactionBranch");
            reactionSequence.AddChild(new HasReactionNode());

            var reactionSelector = new Selector("ReactionAction");

            // 逃亡反応
            var fleeReactionSequence = new Sequence("FleeReaction");
            fleeReactionSequence.AddChild(new IsFleeReactionNode());
            fleeReactionSequence.AddChild(new FleeFromAttackerNode(_safeDistance));
            reactionSelector.AddChild(fleeReactionSequence);

            // 反撃反応
            var counterSequence = new Sequence("CounterAttackReaction");
            counterSequence.AddChild(new IsCounterAttackReactionNode());
            counterSequence.AddChild(new CounterAttackNode());
            reactionSelector.AddChild(counterSequence);

            reactionSequence.AddChild(reactionSelector);
            root.AddChild(reactionSequence);

            // 1. 逃走ブランチ（HP低下時）
            if (_canFlee)
            {
                var fleeSequence = new Sequence("FleeBranch");
                fleeSequence.AddChild(new IsLowHealthNode(_fleeHealthThreshold));
                fleeSequence.AddChild(new FleeNode(_safeDistance, _fleeHealthThreshold));
                root.AddChild(fleeSequence);
            }

            // 2. 戦闘ブランチ
            var combatSequence = new Sequence("CombatBranch");
            combatSequence.AddChild(new HasTargetNode());

            var combatSelector = new Selector("CombatAction");
            combatSelector.AddChild(new AttackNode());
            combatSelector.AddChild(new ChaseNode(_owner?.Stats.attackRange ?? 2f));

            combatSequence.AddChild(combatSelector);
            root.AddChild(combatSequence);

            // 3. 索敵ブランチ
            var searchSequence = new Sequence("SearchBranch");
            var searchNode = new SearchNode(_searchRadius);
            searchSequence.AddChild(searchNode);

            var searchSelector = new Selector("SearchAction");
            searchSelector.AddChild(new ChaseNode(_owner?.Stats.attackRange ?? 2f));
            searchSelector.AddChild(new WanderNode(10f));

            searchSequence.AddChild(searchSelector);
            root.AddChild(searchSequence);

            // 4. フォールバック（徘徊）
            root.AddChild(new WanderNode(10f));

            return root;
        }

        /// <summary>
        /// パラメータをインスペクタから設定可能にする
        /// </summary>
        public void SetParameters(float searchRadius, float fleeThreshold, bool canFlee)
        {
            _searchRadius = searchRadius;
            _fleeHealthThreshold = fleeThreshold;
            _canFlee = canFlee;
        }

        /// <summary>
        /// キャラクタータイプに基づいてAIパラメータを調整
        /// </summary>
        protected override void Start()
        {
            // オーナーのタイプに応じてパラメータ調整
            if (_owner != null)
            {
                AdjustParametersForType(_owner.Type);
            }

            base.Start();
        }

        private void AdjustParametersForType(CharacterType type)
        {
            switch (type)
            {
                case CharacterType.Warrior:
                    _canFlee = false; // 戦士は逃げない
                    _searchRadius = 18f;
                    break;

                case CharacterType.Assassin:
                    _preferLowHealthTargets = true; // 弱った敵を狙う
                    _searchRadius = 25f;
                    _fleeHealthThreshold = 0.2f;
                    break;

                case CharacterType.Tank:
                    _canFlee = false;
                    _searchRadius = 15f;
                    break;

                case CharacterType.Mage:
                    _searchRadius = 30f;
                    _fleeHealthThreshold = 0.35f; // 早めに逃げる
                    _safeDistance = 20f;
                    break;

                case CharacterType.Ranger:
                    _searchRadius = 35f;
                    _safeDistance = 15f;
                    break;

                case CharacterType.Berserker:
                    _canFlee = false;
                    _searchRadius = 22f;
                    _updateInterval = 0.05f; // より頻繁に更新
                    break;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// デバッグ用のGizmo表示
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (_owner == null) return;

            // 索敵範囲
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _searchRadius);

            // 攻撃範囲
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _owner.Stats.attackRange);

            // ターゲットへのライン
            var target = GetBlackboardData<Character>(KEY_TARGET);
            if (target != null && target.IsAlive)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, target.transform.position);
            }
        }
#endif
    }
}
