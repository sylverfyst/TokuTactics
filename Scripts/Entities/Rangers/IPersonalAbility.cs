using System.Collections.Generic;
using TokuTactics.Core.Grid;
using TokuTactics.Entities.Weapons;

namespace TokuTactics.Entities.Rangers
{
    /// <summary>
    /// A Ranger's unique personal ability, available only while unmorphed.
    /// These are fixed per Ranger, never change, and cannot be swapped.
    /// They affect the board (repositioning, debuffs, terrain interaction)
    /// rather than dealing direct damage.
    /// 
    /// Follows the same declarative pattern as status effects and gimmicks:
    /// the ability produces an AbilityOutput that the combat resolver consumes.
    /// 
    /// MAG stat scales the ability's effectiveness via the context.
    /// </summary>
    public interface IPersonalAbility
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }

        /// <summary>Range of the ability in tiles.</summary>
        int Range { get; }

        /// <summary>Whether this ability targets enemies, allies, or terrain.</summary>
        AbilityTargetType TargetType { get; }

        /// <summary>
        /// Whether this ability can be used given the current context.
        /// Checked before showing the ability as available in the UI.
        /// </summary>
        bool CanExecute(AbilityContext context);

        /// <summary>
        /// Produce a declarative output describing what this ability does.
        /// The combat resolver reads and applies the output.
        /// </summary>
        AbilityOutput GetOutput(AbilityContext context);
    }

    public enum AbilityTargetType
    {
        Enemy,
        Ally,
        Terrain,
        Self,
        Area
    }

    /// <summary>
    /// Context for personal ability evaluation and execution.
    /// </summary>
    public class AbilityContext
    {
        /// <summary>The Ranger using the ability.</summary>
        public object Source { get; set; }

        /// <summary>The target of the ability (enemy, ally, tile).</summary>
        public object Target { get; set; }

        /// <summary>MAG stat of the source Ranger — scales effectiveness.</summary>
        public float SourceMag { get; set; }

        /// <summary>Grid position of the source.</summary>
        public GridPosition SourcePosition { get; set; }

        /// <summary>Grid position of the target.</summary>
        public GridPosition TargetPosition { get; set; }

        /// <summary>
        /// Adjacent ally IDs for abilities that interact with team positioning.
        /// Populated by the combat resolver from the grid.
        /// </summary>
        public List<string> AdjacentAllyIds { get; set; }
    }

    /// <summary>
    /// Declarative output from a personal ability.
    /// The combat resolver reads this and applies effects to the game state.
    /// Follows the same pattern as EffectOutput and GimmickOutput.
    /// 
    /// Displacement uses distance + push/pull direction, NOT raw positions.
    /// The combat resolver applies Bresenham cardinal stepping with wall/edge/occupancy
    /// checks — the same logic used by gimmick displacement.
    /// </summary>
    public class AbilityOutput
    {
        /// <summary>Distance to push/pull the target (0 = no displacement).</summary>
        public int DisplaceTargetDistance { get; set; }

        /// <summary>Whether target displacement pushes (true) or pulls (false).</summary>
        public bool DisplaceTargetPush { get; set; }

        /// <summary>Distance to move the source Ranger (dash, retreat).</summary>
        public int DisplaceSourceDistance { get; set; }

        /// <summary>Whether source displacement moves away from target (true) or toward (false).</summary>
        public bool DisplaceSourceAway { get; set; }

        /// <summary>Status effect to apply to the target.</summary>
        public StatusEffectTemplate StatusEffect { get; set; }

        /// <summary>Terrain tiles to modify.</summary>
        public List<GridPosition> TerrainTilesToModify { get; set; }

        /// <summary>What terrain type to set modified tiles to.</summary>
        public TerrainType TargetTerrain { get; set; }

        /// <summary>Indirect damage (e.g., pushed into hazard, not a direct attack).</summary>
        public float IndirectDamage { get; set; }

        /// <summary>Healing to apply to the target.</summary>
        public float Healing { get; set; }

        /// <summary>Whether this output has any actual effect.</summary>
        public bool HasEffect => DisplaceTargetDistance > 0 || DisplaceSourceDistance > 0
            || StatusEffect != null || TerrainTilesToModify != null
            || IndirectDamage > 0 || Healing > 0;

        public static AbilityOutput None => new AbilityOutput();
    }
}
