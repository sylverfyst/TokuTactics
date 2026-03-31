using TokuTactics.Core.Grid;
using TokuTactics.Core.Stats;
using TokuTactics.Core.StatusEffect.Triggers;
using TokuTactics.Core.StatusEffect.Behaviors;
using TokuTactics.Entities.Rangers;
using TokuTactics.Entities.Weapons;

namespace TokuTactics.Data.Content.PersonalAbilities
{
    /// <summary>
    /// Push an enemy 2 tiles away from the Ranger.
    /// Repositioning tool for the unmorphed scouting phase.
    /// Range 1 (must be adjacent). Scales push distance with MAG.
    /// 
    /// The combat resolver applies Bresenham cardinal stepping to resolve
    /// the actual destination — this ability just declares the distance and direction.
    /// </summary>
    public class ScoutPush : IPersonalAbility
    {
        public string Id => "ability_scout_push";
        public string Name => "Scout Push";
        public string Description => "Shove an adjacent enemy back, creating distance.";
        public int Range => 1;
        public AbilityTargetType TargetType => AbilityTargetType.Enemy;

        /// <summary>Base push distance in tiles.</summary>
        public int BasePushDistance { get; } = 2;

        /// <summary>MAG threshold for bonus push distance.</summary>
        public float MagBonusThreshold { get; } = 10f;

        public bool CanExecute(AbilityContext context)
        {
            if (context.Target == null) return false;
            return context.SourcePosition.ManhattanDistance(context.TargetPosition) <= Range;
        }

        public AbilityOutput GetOutput(AbilityContext context)
        {
            // MAG scaling: high MAG pushes 1 extra tile
            int pushDistance = BasePushDistance + (context.SourceMag >= MagBonusThreshold ? 1 : 0);

            return new AbilityOutput
            {
                DisplaceTargetDistance = pushDistance,
                DisplaceTargetPush = true
            };
        }
    }

    /// <summary>
    /// Boost the DEF of all adjacent allies for 2 turns.
    /// Support ability for the unmorphed phase — helps the team survive
    /// before morphing. Scales buff strength with MAG.
    /// </summary>
    public class Rally : IPersonalAbility
    {
        public string Id => "ability_rally";
        public string Name => "Rally";
        public string Description => "Inspire adjacent allies, boosting their defense.";
        public int Range => 1;
        public AbilityTargetType TargetType => AbilityTargetType.Area;

        /// <summary>Base DEF bonus.</summary>
        public float BaseDefBonus { get; } = 3f;

        public bool CanExecute(AbilityContext context)
        {
            // Always usable if there are adjacent allies
            return context.AdjacentAllyIds != null && context.AdjacentAllyIds.Count > 0;
        }

        public AbilityOutput GetOutput(AbilityContext context)
        {
            // MAG scaling: bonus scales up
            float defBonus = BaseDefBonus + context.SourceMag * 0.2f;

            return new AbilityOutput
            {
                StatusEffect = new StatusEffectTemplate(
                    "eff_rally_def",
                    new TurnStartTrigger(),
                    new StatModifierBehavior(StatType.DEF, defBonus),
                    baseDuration: 2)
            };
        }
    }
}
