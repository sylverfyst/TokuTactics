using System;
using System.Collections.Generic;
using TokuTactics.Core.Grid;
using TokuTactics.Core.Types;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Combat;
using TokuTactics.Core.Events;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Enemies.Gimmicks;
using TokuTactics.Entities.Rangers;
using TokuTactics.Entities.Weapons;
using TokuTactics.Systems.ActionEconomy;
using TokuTactics.Systems.AssistResolution;
using TokuTactics.Commands.Combat;
using GimmickResolutionNS = TokuTactics.Systems.GimmickResolution;

namespace TokuTactics.Systems.CombatResolution
{
    /// <summary>
    /// Orchestrates a single attack action from start to finish.
    /// This is the ONE system that mutates game state in response to combat.
    /// 
    /// Flow for a Ranger attacking an enemy:
    /// 1. Resolve assists (AssistResolver → declarative)
    /// 2. Calculate primary damage (ResolveDamageRoll BCO command)
    /// 3. Apply primary damage to target
    /// 4. Apply weapon status effect to target
    /// 5. For each assist: calculate damage, apply damage, apply status, award bond XP
    /// 6. Handle tier 2 form disruption (force assister to base form)
    /// 7. Flag tier 4 action refresh opportunities
    /// 8. Check for target death (enemy death / form death / unmorphed death)
    /// 9. Check for reactive gimmick triggers (OnHit on damaged enemies)
    /// 10. Publish events
    /// 
    /// The resolver returns a CombatResolution describing everything that happened.
    /// The presentation layer reads this to play animations, show damage numbers, etc.
    /// 
    /// For enemy attacks, the flow is simpler: calculate damage, apply, check form/Ranger death.
    /// No assists (enemies don't assist each other in the vertical slice).
    /// </summary>
    public class CombatResolver
    {
        private readonly BattleGrid _grid;
        private readonly TypeChart _typeChart;
        private readonly Random _rng;
        private readonly TunableConstants _constants;
        private readonly AssistResolver _assistResolver;
        private readonly GimmickResolutionNS.GimmickResolver _gimmickResolver;
        private readonly BondTracker _bondTracker;
        private readonly EventBus _eventBus;

        /// <summary>Tunable: how much each point of MAG increases status effect potency.</summary>
        public float StatusPotencyMagScale { get; set; } = 0.01f;

        public CombatResolver(
            BattleGrid grid,
            TypeChart typeChart,
            Random rng,
            TunableConstants constants,
            AssistResolver assistResolver,
            GimmickResolutionNS.GimmickResolver gimmickResolver,
            BondTracker bondTracker,
            EventBus eventBus)
        {
            _grid = grid;
            _typeChart = typeChart;
            _rng = rng;
            _constants = constants;
            _assistResolver = assistResolver;
            _gimmickResolver = gimmickResolver;
            _bondTracker = bondTracker;
            _eventBus = eventBus;
        }

        /// <summary>
        /// Resolve a Ranger attacking a target.
        /// Handles assists, damage, status effects, bond growth, and cascades.
        /// </summary>
        public CombatResult ResolveRangerAttack(
            Ranger attacker,
            ICombatTarget target,
            float actionPower,
            StatusEffectTemplate weaponEffect,
            Dictionary<string, AssistCandidateState> rangerStates)
        {
            var result = new CombatResult { AttackerId = attacker.Id, TargetId = target.Id };

            var attackerPos = _grid.GetUnitPosition(attacker.Id);
            if (!attackerPos.HasValue) return result;

            // === Step 1: Resolve Assists (declarative, before damage) ===
            var assistResolution = _assistResolver.Resolve(
                attacker.Id, attackerPos.Value,
                attacker.ComboScaler.AssistDamageMultiplier,
                rangerStates);

            // === Step 2: Calculate Primary Damage ===
            var damageParams = new ResolveDamageRollParams
            {
                AttackerStr = attacker.Stats.Get(StatType.STR),
                AttackerLck = attacker.Stats.Get(StatType.LCK),
                DefenderDef = target.Stats.Get(StatType.DEF),
                DefenderLck = target.Stats.Get(StatType.LCK),
                ActionPower = actionPower,
                AttackType = attacker.DualType.FormType,
                DefenderType = target.Type,
                DefenderDualType = (target is Ranger rangerTarget) ? rangerTarget.DualType : null,
                ComboMultiplier = attacker.ComboScaler.DamageMultiplier,
                HasSameTypeBonus = attacker.DualType.IsSameType
            };
            var damageResult = ResolveDamageRoll.Execute(damageParams, _typeChart, _rng, _constants);
            result.PrimaryDamage = damageResult;

            // === Step 3: Apply Primary Damage ===
            if (!damageResult.WasDodged)
            {
                ApplyDamageToTarget(target, damageResult.FinalDamage, result);

                // === Step 4: Apply Weapon Status Effect ===
                if (weaponEffect != null)
                {
                    float potency = 1.0f + attacker.Stats.Get(StatType.MAG) * StatusPotencyMagScale;
                    var effectInstance = weaponEffect.CreateInstance(potency);
                    ApplyStatusToTarget(target, effectInstance, result);
                }

                PublishDamageEvent(attacker.Id, target.Id, damageResult);

                // === Step 5: Process Each Assist ===
                // Assists only fire if the primary attack landed.
                // Skip assists against dead targets (primary already killed them).
                foreach (var assist in assistResolution.Assists)
                {
                    if (!IsTargetAlive(target)) break;

                    var assistResult = ProcessAssist(assist, target, result);
                    result.AssistResults.Add(assistResult);
                }
            }

            // === Step 6: Check Target Death ===
            CheckTargetDeath(target, result);

            // === Step 7: Check Reactive Gimmick (OnHit) ===
            if (!damageResult.WasDodged && target is Enemy enemyWithGimmick
                && enemyWithGimmick.IsAlive && !enemyWithGimmick.IsGimmickVoluntary)
            {
                var gimmickContext = enemyWithGimmick.BuildGimmickContext();
                gimmickContext.WasJustHit = true;
                if (enemyWithGimmick.ShouldGimmickActivate(gimmickContext))
                {
                    var gimmickResult = ResolveReactiveGimmick(enemyWithGimmick);
                    if (gimmickResult != null)
                        result.ReactiveGimmick = gimmickResult;
                }
            }

            // === Step 8: Dispatch Events ===
            _eventBus.Dispatch();

            return result;
        }

        /// <summary>
        /// Resolve an enemy attacking a Ranger.
        /// Simpler flow: no assists, just damage + status + death checks.
        /// </summary>
        public CombatResult ResolveEnemyAttack(
            Enemy attacker,
            ICombatTarget target,
            float actionPower,
            StatusEffectTemplate weaponEffect)
        {
            var result = new CombatResult { AttackerId = attacker.Id, TargetId = target.Id };

            // Calculate and apply damage
            var damageParams = new ResolveDamageRollParams
            {
                AttackerStr = attacker.Stats.Get(StatType.STR),
                AttackerLck = 0, // Enemies don't crit in vertical slice
                DefenderDef = target.Stats.Get(StatType.DEF),
                DefenderLck = target.Stats.Get(StatType.LCK),
                ActionPower = actionPower,
                AttackType = ((ICombatTarget)attacker).Type,
                DefenderType = target.Type,
                DefenderDualType = (target is Ranger rangerTarget) ? rangerTarget.DualType : null,
                ComboMultiplier = 1.0f,
                HasSameTypeBonus = false // Enemies don't have dual typing
            };
            var damageResult = ResolveDamageRoll.Execute(damageParams, _typeChart, _rng, _constants);
            result.PrimaryDamage = damageResult;

            if (!damageResult.WasDodged)
            {
                ApplyDamageToTarget(target, damageResult.FinalDamage, result);

                if (weaponEffect != null)
                {
                    float potency = 1.0f + attacker.Stats.Get(StatType.MAG) * StatusPotencyMagScale;
                    var effectInstance = weaponEffect.CreateInstance(potency);
                    ApplyStatusToTarget(target, effectInstance, result);
                }

                PublishDamageEvent(attacker.Id, target.Id, damageResult);
            }

            CheckTargetDeath(target, result);

            _eventBus.Dispatch();

            return result;
        }

        // === Internal: Helpers ===

        private bool IsTargetAlive(ICombatTarget target)
        {
            if (target is Enemy enemy) return enemy.IsAlive;
            if (target is Ranger ranger) return ranger.IsAlive;
            return true;
        }

        // === Internal: Damage ===

        private DamageInput BuildDamageInput(
            ICombatActor attacker, ICombatTarget target,
            float actionPower, float comboMultiplier)
        {
            var input = new DamageInput
            {
                AttackerStr = attacker.Stats.Get(StatType.STR),
                AttackerLck = attacker.Stats.Get(StatType.LCK),
                AttackerDualType = attacker.DualType,
                DefenderDef = target.Stats.Get(StatType.DEF),
                DefenderLck = target.Stats.Get(StatType.LCK),
                DefenderType = target.Type,
                ActionPower = actionPower,
                ComboMultiplier = comboMultiplier
            };

            // Ranger targets have dual typing that affects matchup calculation.
            // Enemies are single-typed — use the standard Resolve path.
            if (target is Ranger rangerTarget)
                input.DefenderDualType = rangerTarget.DualType;

            return input;
        }

        private void ApplyDamageToTarget(ICombatTarget target, int damage, CombatResult result)
        {
            if (target is Enemy enemy)
            {
                var evt = enemy.TakeDamage(damage);
                if (evt.BecameAggressive)
                {
                    result.AggressionTriggered = true;
                    _eventBus.Publish(new AggressionTriggeredEvent
                    {
                        EnemyId = enemy.Id,
                        HealthPercentage = enemy.Health.Percentage
                    });
                }
            }
            else if (target is Ranger ranger)
            {
                if (ranger.MorphState == MorphState.Morphed && ranger.CurrentForm != null)
                {
                    ranger.CurrentForm.Health.TakeDamage(damage);
                }
                else
                {
                    ranger.UnmorphedHealth.TakeDamage(damage);
                }
            }
        }

        private void ApplyStatusToTarget(
            ICombatTarget target,
            Core.StatusEffect.StatusEffectInstance effect,
            CombatResult result)
        {
            if (target is Enemy enemy)
            {
                enemy.StatusEffects.Apply(effect);
                result.StatusEffectsApplied.Add(effect.Id);
            }
            else if (target is Ranger ranger)
            {
                ranger.StatusEffects.Apply(effect);
                result.StatusEffectsApplied.Add(effect.Id);
            }
        }

        // === Internal: Assists ===

        private AssistCombatResult ProcessAssist(
            AssistEffect assist, ICombatTarget target, CombatResult parentResult)
        {
            var assistResult = new AssistCombatResult
            {
                AssisterId = assist.AssisterId,
                BondTier = assist.BondTier,
                IsPairAttack = assist.IsPairAttack
            };

            // Calculate assist damage using the assister's stats
            var assistDamageParams = new ResolveDamageRollParams
            {
                AttackerStr = assist.AssisterStr,
                AttackerLck = 0, // Assists don't crit independently
                DefenderDef = target.Stats.Get(StatType.DEF),
                DefenderLck = target.Stats.Get(StatType.LCK),
                ActionPower = assist.AssisterWeaponPower,
                AttackType = assist.AssisterDualType.FormType,
                DefenderType = target.Type,
                DefenderDualType = (target is Ranger rangerTarget) ? rangerTarget.DualType : null,
                ComboMultiplier = assist.DamageMultiplier,
                HasSameTypeBonus = assist.AssisterDualType.IsSameType
            };

            var assistDamage = ResolveDamageRoll.Execute(assistDamageParams, _typeChart, _rng, _constants);
            assistResult.Damage = assistDamage;

            // Apply assist damage
            if (!assistDamage.WasDodged)
            {
                ApplyDamageToTarget(target, assistDamage.FinalDamage, parentResult);
            }

            // Award bond experience
            var tierChange = _bondTracker.AddAssistExperience(
                assist.AttackerId, assist.AssisterId, assist.ChaMultiplier);

            if (tierChange != null)
            {
                assistResult.BondTierChange = tierChange;
                _eventBus.Publish(new BondTierReachedEvent
                {
                    RangerA = tierChange.RangerA,
                    RangerB = tierChange.RangerB,
                    OldTier = tierChange.OldTier,
                    NewTier = tierChange.NewTier
                });
            }

            // Publish assist event
            _eventBus.Publish(new AssistOccurredEvent
            {
                ActorId = assist.AttackerId,
                AssisterId = assist.AssisterId,
                BondTier = assist.BondTier,
                TriggeredPairAttack = assist.IsPairAttack
            });

            // Tier 2 form disruption
            if (assist.ForceToBaseForm)
            {
                assistResult.FormDisrupted = true;
                assistResult.VacatedFormId = assist.VacatedFormId;

                _eventBus.Publish(new Tier2FormDisruptionEvent
                {
                    AssisterId = assist.AssisterId,
                    DisruptedFormId = assist.VacatedFormId
                });
            }

            // Tier 4 refresh opportunity
            if (assist.CanRefreshPartner)
            {
                assistResult.RefreshAvailable = true;

                _eventBus.Publish(new BondRefreshEvent
                {
                    GiverId = assist.AssisterId,
                    ReceiverId = assist.AttackerId
                });
            }

            return assistResult;
        }

        // === Internal: Death Checks ===

        private void CheckTargetDeath(ICombatTarget target, CombatResult result)
        {
            if (target is Enemy enemy && !enemy.IsAlive)
            {
                result.TargetDied = true;
                _eventBus.Publish(new EnemyDefeatedEvent
                {
                    EnemyId = enemy.Id,
                    EnemyTypeId = enemy.Data.Id,
                    EnemyType = enemy.Data.Type ?? ElementalType.Normal
                });
            }
            else if (target is Ranger ranger)
            {
                if (ranger.MorphState == MorphState.Morphed
                    && ranger.CurrentForm != null
                    && !ranger.CurrentForm.Health.IsAlive)
                {
                    // Form died — demorph
                    var lostForm = ranger.Demorph();
                    result.FormDied = true;
                    result.LostFormId = lostForm?.Data.Id;

                    _eventBus.Publish(new FormDiedEvent
                    {
                        RangerId = ranger.Id,
                        FormId = lostForm?.Data.Id
                    });
                    _eventBus.Publish(new RangerDemorphedEvent
                    {
                        RangerId = ranger.Id,
                        LostFormId = lostForm?.Data.Id
                    });
                }
                else if (ranger.MorphState != MorphState.Morphed
                    && !ranger.UnmorphedHealth.IsAlive)
                {
                    // Unmorphed Ranger died — mission lost
                    result.TargetDied = true;
                    result.MissionLost = true;

                    _eventBus.Publish(new RangerDiedUnmorphedEvent
                    {
                        RangerId = ranger.Id
                    });
                }
            }
        }

        // === Internal: Reactive Gimmick ===

        private GimmickResolutionNS.GimmickResolution ResolveReactiveGimmick(Enemy enemy)
        {
            var ownerPos = _grid.GetUnitPosition(enemy.Id);
            if (!ownerPos.HasValue) return null;

            var context = enemy.BuildGimmickContext();
            context.WasJustHit = true;

            if (!enemy.ShouldGimmickActivate(context)) return null;

            var output = enemy.GetGimmickOutput(context);
            if (output == null || !output.HasEffect) return null;

            // Find all Ranger IDs on the grid for targeting
            var rangerIds = new HashSet<string>();
            foreach (var unitId in _grid.AllUnitIds)
            {
                // In the full game, the combat state would provide a typed lookup.
                // For the vertical slice, we check if the ID starts with "ranger_"
                // This is a placeholder — the combat state layer will replace this.
                if (unitId.StartsWith("ranger_"))
                    rangerIds.Add(unitId);
            }

            var resolution = _gimmickResolver.Resolve(
                ownerPos.Value, output,
                enemy.Data.Gimmick.Behavior.Range,
                rangerIds);

            if (resolution.HasEffects)
                enemy.OnGimmickActivated();

            return resolution;
        }

        // === Internal: Events ===

        private void PublishDamageEvent(string attackerId, string targetId, DamageResult damage)
        {
            _eventBus.Publish(new DamageDealtEvent
            {
                AttackerId = attackerId,
                TargetId = targetId,
                Amount = damage.FinalDamage,
                WasCritical = damage.WasCritical,
                WasDodged = damage.WasDodged,
                TypeMatchup = damage.Matchup,
                HadSameTypeBonus = damage.HadSameTypeBonus,
                ComboMultiplier = damage.ComboMultiplier
            });
        }
    }

    // === Resolution Output ===

    /// <summary>
    /// Complete result of one attack action. Everything that happened,
    /// in the order it happened. The presentation layer reads this to
    /// sequence animations, damage numbers, and dialogue triggers.
    /// </summary>
    public class CombatResult
    {
        /// <summary>Who attacked.</summary>
        public string AttackerId { get; set; }

        /// <summary>Who was attacked.</summary>
        public string TargetId { get; set; }

        /// <summary>Result of the primary damage calculation.</summary>
        public DamageResult PrimaryDamage { get; set; }

        /// <summary>Results of each assist that contributed.</summary>
        public List<AssistCombatResult> AssistResults { get; } = new();

        /// <summary>Status effects applied to the target this action.</summary>
        public List<string> StatusEffectsApplied { get; } = new();

        /// <summary>Whether the target died (enemy killed or unmorphed Ranger died).</summary>
        public bool TargetDied { get; set; }

        /// <summary>Whether a form was destroyed (Ranger target only).</summary>
        public bool FormDied { get; set; }

        /// <summary>ID of the form that was destroyed.</summary>
        public string LostFormId { get; set; }

        /// <summary>Whether the mission is lost (unmorphed Ranger death).</summary>
        public bool MissionLost { get; set; }

        /// <summary>Whether the target enemy crossed its aggression threshold.</summary>
        public bool AggressionTriggered { get; set; }

        /// <summary>Reactive gimmick that fired in response to the attack (OnHit).</summary>
        public GimmickResolutionNS.GimmickResolution ReactiveGimmick { get; set; }

        /// <summary>Total damage dealt across primary + all assists.</summary>
        public int TotalDamage
        {
            get
            {
                int total = PrimaryDamage?.WasDodged == false ? PrimaryDamage.FinalDamage : 0;
                foreach (var assist in AssistResults)
                {
                    if (assist.Damage?.WasDodged == false)
                        total += assist.Damage.FinalDamage;
                }
                return total;
            }
        }
    }

    /// <summary>
    /// Result of a single assist within a combat action.
    /// </summary>
    public class AssistCombatResult
    {
        public string AssisterId { get; set; }
        public int BondTier { get; set; }
        public bool IsPairAttack { get; set; }
        public DamageResult Damage { get; set; }

        /// <summary>Whether a bond tier change occurred from this assist.</summary>
        public BondTierChange BondTierChange { get; set; }

        /// <summary>Whether tier 2 disrupted the assister's form.</summary>
        public bool FormDisrupted { get; set; }

        /// <summary>The form that was vacated by tier 2 disruption.</summary>
        public string VacatedFormId { get; set; }

        /// <summary>Whether tier 4 refresh is available from this assist.</summary>
        public bool RefreshAvailable { get; set; }
    }
}
