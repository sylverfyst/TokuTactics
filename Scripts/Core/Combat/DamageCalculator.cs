using System;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;

namespace TokuTactics.Core.Combat
{
    /// <summary>
    /// Resolves damage for any combat action. This is the single point where
    /// STR, DEF, type matchups, same-type bonus, combo scaling, and LCK-driven
    /// variance all combine into a final damage number.
    /// 
    /// Used by both ground combat and mecha combat (same formula, different units).
    /// </summary>
    public class DamageCalculator
    {
        private readonly TypeChart _typeChart;
        private readonly Random _rng;

        /// <summary>Tunable: base multiplier for Strong matchups.</summary>
        public float StrongMultiplier { get; set; } = 1.5f;

        /// <summary>Tunable: base multiplier for Weak matchups.</summary>
        public float WeakMultiplier { get; set; } = 0.5f;

        /// <summary>Tunable: multiplier for DoubleStrong (both types strong).</summary>
        public float DoubleStrongMultiplier { get; set; } = 2.0f;

        /// <summary>Tunable: multiplier for DoubleWeak (both types weak).</summary>
        public float DoubleWeakMultiplier { get; set; } = 0.25f;

        /// <summary>Tunable: bonus multiplier when Ranger type matches form type.</summary>
        public float SameTypeBonus { get; set; } = 1.25f;

        /// <summary>Tunable: crit damage multiplier.</summary>
        public float CritMultiplier { get; set; } = 1.5f;

        /// <summary>Tunable: base crit chance (0-1) before LCK modification.</summary>
        public float BaseCritChance { get; set; } = 0.05f;

        /// <summary>Tunable: how much each point of LCK adds to crit chance.</summary>
        public float LckCritScale { get; set; } = 0.005f;

        /// <summary>Tunable: base dodge chance (0-1) before LCK modification.</summary>
        public float BaseDodgeChance { get; set; } = 0.02f;

        /// <summary>Tunable: how much each point of LCK adds to dodge chance.</summary>
        public float LckDodgeScale { get; set; } = 0.003f;

        public DamageCalculator(TypeChart typeChart, Random rng = null)
        {
            _typeChart = typeChart;
            _rng = rng ?? new Random();
        }

        /// <summary>
        /// Calculate damage from attacker to target.
        /// This is the core damage formula used by every combat action in the game.
        /// </summary>
        public DamageResult Calculate(DamageInput input)
        {
            var result = new DamageResult();

            // Step 1: Dodge check (defender LCK)
            float dodgeChance = BaseDodgeChance + (input.DefenderLck * LckDodgeScale);
            if (_rng.NextDouble() < dodgeChance)
            {
                result.WasDodged = true;
                result.FinalDamage = 0;
                return result;
            }

            // Steps 2-6: Shared base damage computation
            var baseCalc = ComputeBaseDamage(input);
            float rawDamage = baseCalc.RawDamage;

            result.Matchup = baseCalc.Matchup;
            result.HadSameTypeBonus = baseCalc.HadSameTypeBonus;
            result.ComboMultiplier = input.ComboMultiplier;
            result.BaseDamage = baseCalc.BaseDamage;
            result.TypeMultiplier = baseCalc.TypeMultiplier;

            // Step 7: Crit check (attacker LCK)
            float critChance = BaseCritChance + (input.AttackerLck * LckCritScale);
            if (_rng.NextDouble() < critChance)
            {
                rawDamage *= CritMultiplier;
                result.WasCritical = true;
            }

            // Step 8: Variance (small random range for the damage preview bounds)
            float variance = 0.9f + (float)_rng.NextDouble() * 0.2f; // 0.9 to 1.1
            rawDamage *= variance;

            result.FinalDamage = Math.Max(1, (int)Math.Round(rawDamage));

            return result;
        }

        /// <summary>
        /// Calculate the damage preview range (min/max) for the UI.
        /// Shows upper and lower bounds influenced by LCK (crit ceiling).
        /// </summary>
        public DamagePreview Preview(DamageInput input)
        {
            var baseCalc = ComputeBaseDamage(input);

            float minDamage = baseCalc.RawDamage * 0.9f;
            float maxDamage = baseCalc.RawDamage * 1.1f;

            float critChance = BaseCritChance + (input.AttackerLck * LckCritScale);
            float critCeiling = maxDamage * CritMultiplier;

            return new DamagePreview
            {
                MinDamage = Math.Max(1, (int)Math.Round(minDamage)),
                MaxDamage = Math.Max(1, (int)Math.Round(maxDamage)),
                CritCeiling = Math.Max(1, (int)Math.Round(critCeiling)),
                CritChance = critChance,
                Matchup = baseCalc.Matchup,
                HasSameTypeBonus = baseCalc.HadSameTypeBonus
            };
        }

        /// <summary>
        /// Shared base damage computation used by both Calculate and Preview.
        /// Covers STR vs DEF, action power, type matchup, same-type bonus, and combo scaling.
        /// Does NOT include crit, dodge, or variance — those are Calculate-only.
        /// </summary>
        private BaseDamageCalc ComputeBaseDamage(DamageInput input)
        {
            float baseDamage = Math.Max(1, input.AttackerStr - (input.DefenderDef * 0.5f));
            float rawDamage = baseDamage * input.ActionPower;

            // Type matchup: use ResolveDefensive for dual-typed defenders (Rangers),
            // standard Resolve for single-typed defenders (enemies).
            MatchupResult matchup;
            if (input.DefenderDualType != null)
            {
                // Enemy (single type via DualType.RangerType) → Ranger (dual type)
                matchup = _typeChart.ResolveDefensive(
                    input.AttackerDualType.RangerType, input.DefenderDualType.Value);
            }
            else
            {
                // Ranger (dual type) → Enemy (single type)
                matchup = _typeChart.Resolve(input.AttackerDualType, input.DefenderType);
            }

            float typeMultiplier = GetTypeMultiplier(matchup);
            rawDamage *= typeMultiplier;

            bool hadSameTypeBonus = _typeChart.IsSameTypeBonus(input.AttackerDualType);
            if (hadSameTypeBonus)
                rawDamage *= SameTypeBonus;

            rawDamage *= input.ComboMultiplier;

            return new BaseDamageCalc
            {
                BaseDamage = baseDamage,
                RawDamage = rawDamage,
                Matchup = matchup,
                TypeMultiplier = typeMultiplier,
                HadSameTypeBonus = hadSameTypeBonus
            };
        }

        private struct BaseDamageCalc
        {
            public float BaseDamage;
            public float RawDamage;
            public MatchupResult Matchup;
            public float TypeMultiplier;
            public bool HadSameTypeBonus;
        }

        private float GetTypeMultiplier(MatchupResult matchup)
        {
            return matchup switch
            {
                MatchupResult.DoubleStrong => DoubleStrongMultiplier,
                MatchupResult.Strong => StrongMultiplier,
                MatchupResult.Neutral => 1.0f,
                MatchupResult.Weak => WeakMultiplier,
                MatchupResult.DoubleWeak => DoubleWeakMultiplier,
                _ => 1.0f
            };
        }
    }

    /// <summary>
    /// All inputs needed for a damage calculation.
    /// Collected by the combat system from the actor, target, and game state.
    /// </summary>
    public class DamageInput
    {
        public float AttackerStr { get; set; }
        public float AttackerLck { get; set; }
        public DualType AttackerDualType { get; set; }
        public float DefenderDef { get; set; }
        public float DefenderLck { get; set; }

        /// <summary>Single defender type. Used for single-typed targets (enemies).</summary>
        public ElementalType DefenderType { get; set; }

        /// <summary>
        /// Dual defender type. When set, the calculator uses ResolveDefensive
        /// for the type matchup instead of Resolve. Used for Ranger targets.
        /// </summary>
        public DualType? DefenderDualType { get; set; }

        public float ActionPower { get; set; } = 1.0f;
        public float ComboMultiplier { get; set; } = 1.0f;
    }

    /// <summary>
    /// Output of a damage calculation.
    /// </summary>
    public class DamageResult
    {
        public int FinalDamage { get; set; }
        public float BaseDamage { get; set; }
        public float TypeMultiplier { get; set; }
        public float ComboMultiplier { get; set; }
        public MatchupResult Matchup { get; set; }
        public bool WasCritical { get; set; }
        public bool WasDodged { get; set; }
        public bool HadSameTypeBonus { get; set; }
    }

    /// <summary>
    /// Damage preview for the UI — shows range before committing to an attack.
    /// </summary>
    public class DamagePreview
    {
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public int CritCeiling { get; set; }
        public float CritChance { get; set; }
        public MatchupResult Matchup { get; set; }
        public bool HasSameTypeBonus { get; set; }
    }
}
