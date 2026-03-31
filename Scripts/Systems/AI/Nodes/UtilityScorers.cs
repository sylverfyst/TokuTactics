using System.Linq;

namespace TokuTactics.Systems.AI.Nodes
{
    /// <summary>
    /// Scores higher when an unmorphed Ranger is nearby.
    /// Foot soldiers activate this below aggression threshold.
    /// </summary>
    public class TargetUnmorphedScorer : IUtilityScorer
    {
        public string ScorerId => "scorer_target_unmorphed";

        public float Score(AIContext context)
        {
            var enemies = context.GameState.GetVisibleEnemies(context.Self);
            var unmorphed = enemies.Where(e => e.IsUnmorphed).ToList();

            if (unmorphed.Count == 0) return 0;

            // Higher score for closer unmorphed targets
            var closest = unmorphed
                .OrderBy(e => context.GameState.GetDistance(context.Self, e.Entity))
                .First();

            int distance = context.GameState.GetDistance(context.Self, closest.Entity);
            return 100f / (1 + distance); // Closer = higher score
        }
    }

    /// <summary>
    /// Scores higher for targets with low health.
    /// Basic focus-fire behavior.
    /// </summary>
    public class TargetWeakestScorer : IUtilityScorer
    {
        public string ScorerId => "scorer_target_weakest";

        public float Score(AIContext context)
        {
            var enemies = context.GameState.GetVisibleEnemies(context.Self);
            if (enemies.Count == 0) return 0;

            var weakest = enemies.OrderBy(e => e.HealthPercentage).First();
            return (1.0f - weakest.HealthPercentage) * 80f; // Lower health = higher score
        }
    }

    /// <summary>
    /// Scores higher when the entity should retreat (low health, outnumbered).
    /// Used by lieutenants and smart enemies.
    /// </summary>
    public class SelfPreservationScorer : IUtilityScorer
    {
        public string ScorerId => "scorer_self_preservation";

        /// <summary>Health percentage below which self-preservation kicks in.</summary>
        public float HealthThreshold { get; set; } = 0.3f;

        public float Score(AIContext context)
        {
            float health = context.GameState.GetHealthPercentage(context.Self);

            if (health > HealthThreshold)
                return 0;

            // The lower the health, the more we want to retreat
            return (HealthThreshold - health) * 200f;
        }
    }

    /// <summary>
    /// Scores higher when the entity should use aggressive tactics.
    /// Activated below the aggression threshold.
    /// </summary>
    public class AggressiveChargeScorer : IUtilityScorer
    {
        public string ScorerId => "scorer_aggressive_charge";

        public float Score(AIContext context)
        {
            var enemies = context.GameState.GetVisibleEnemies(context.Self);
            if (enemies.Count == 0) return 0;

            // Just charge the nearest target
            var nearest = enemies
                .OrderBy(e => context.GameState.GetDistance(context.Self, e.Entity))
                .First();

            int distance = context.GameState.GetDistance(context.Self, nearest.Entity);
            return 90f / (1 + distance);
        }
    }

    /// <summary>
    /// Scores higher for targeting Rangers who are standing next to bonded partners.
    /// Used by lieutenants and Dark Rangers to disrupt assist pairs.
    /// </summary>
    public class TargetBondedPairScorer : IUtilityScorer
    {
        public string ScorerId => "scorer_target_bonded_pair";

        public float Score(AIContext context)
        {
            var enemies = context.GameState.GetVisibleEnemies(context.Self);

            float bestScore = 0;
            foreach (var enemy in enemies)
            {
                // Check if this enemy has adjacent allies (potential assist pairs)
                var allies = context.GameState.GetAllies(enemy.Entity);
                int adjacentAllies = allies.Count(a =>
                    context.GameState.IsAdjacent(enemy.Entity, a.Entity));

                if (adjacentAllies > 0)
                {
                    float score = 70f + (adjacentAllies * 15f);
                    int distance = context.GameState.GetDistance(context.Self, enemy.Entity);
                    score /= (1 + distance * 0.5f);

                    if (score > bestScore)
                        bestScore = score;
                }
            }

            return bestScore;
        }
    }

    /// <summary>
    /// Aggression threshold check node — activates downstream scorers
    /// when this entity's health is below its type's fixed threshold.
    /// </summary>
    public class AggressionThresholdNode : IBehaviorNode
    {
        public string NodeId { get; }

        /// <summary>Fixed threshold percentage for this enemy type.</summary>
        public float Threshold { get; }

        private readonly IBehaviorNode _aggressiveBranch;
        private readonly IBehaviorNode _normalBranch;

        public AggressionThresholdNode(
            string nodeId,
            float threshold,
            IBehaviorNode aggressiveBranch,
            IBehaviorNode normalBranch)
        {
            NodeId = nodeId;
            Threshold = threshold;
            _aggressiveBranch = aggressiveBranch;
            _normalBranch = normalBranch;
        }

        public NodeStatus Process(AIContext context)
        {
            float health = context.GameState.GetHealthPercentage(context.Self);

            if (health <= Threshold)
                return _aggressiveBranch.Process(context);
            else
                return _normalBranch.Process(context);
        }

        public void Reset()
        {
            _aggressiveBranch.Reset();
            _normalBranch.Reset();
        }
    }
}
