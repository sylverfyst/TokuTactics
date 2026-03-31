using TokuTactics.Core.Types;
using TokuTactics.Core.Stats;

namespace TokuTactics.Core.Combat
{
    /// <summary>
    /// Anything that can be targeted in combat: Rangers, enemies, the heart, terrain.
    /// </summary>
    public interface ICombatTarget
    {
        string Id { get; }
        ElementalType Type { get; }
        StatBlock Stats { get; }
        bool IsAlive { get; }
    }

    /// <summary>
    /// Anything that can perform a combat action: Rangers (morphed/unmorphed), enemies, Megazord.
    /// </summary>
    public interface ICombatActor : ICombatTarget
    {
        DualType DualType { get; }
        float ComboScaleMultiplier { get; }
    }

    /// <summary>
    /// A single combat action that can be executed.
    /// Implementations: BasicAttack, WeaponAttack, PersonalAbility, FormSwitch,
    /// ZordAbility, MegazordUltimate.
    /// </summary>
    public interface ICombatAction
    {
        string ActionId { get; }
        string DisplayName { get; }

        /// <summary>Range in tiles. 0 = self, 1 = adjacent, 2+ = ranged.</summary>
        int Range { get; }

        /// <summary>Whether this action can be performed given current state.</summary>
        bool CanExecute(ICombatActor actor);

        /// <summary>Execute the action and return the result.</summary>
        ActionResult Execute(ICombatActor actor, ICombatTarget target);
    }

    /// <summary>
    /// Result of any combat action. Consumed by the UI and game state systems.
    /// </summary>
    public class ActionResult
    {
        public bool Success { get; set; }
        public float DamageDealt { get; set; }
        public float HealingDone { get; set; }
        public bool WasCritical { get; set; }
        public bool WasDodged { get; set; }
        public MatchupResult TypeMatchup { get; set; }
        public string StatusEffectApplied { get; set; }
        public string Description { get; set; }

        public static ActionResult Miss() => new ActionResult
        {
            Success = false,
            WasDodged = true,
            Description = "Attack missed"
        };
    }
}
