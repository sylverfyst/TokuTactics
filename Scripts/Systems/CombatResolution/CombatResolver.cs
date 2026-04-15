using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Combat;
using TokuTactics.Commands.Combat;
using TokuTactics.Core.Grid;
using TokuTactics.Core.Types;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Combat;
using TokuTactics.Core.Events;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Rangers;
using TokuTactics.Entities.Weapons;
using TokuTactics.Core.StatusEffect;
using TokuTactics.Core.ActionEconomy;
using TokuTactics.Systems.ActionEconomy;
using TokuTactics.Core.Assist;
using TokuTactics.Systems.AssistResolution;
using TokuTactics.Commands.Gimmick;
using GimmickResolver = TokuTactics.Systems.GimmickResolution.GimmickResolver;

namespace TokuTactics.Systems.CombatResolution
{
    /// <summary>
    /// Orchestrator: Coordinates a single attack action from start to finish.
    /// Delegates logic to Commands and Bricks. Publishes events based on results.
    ///
    /// Flow for Ranger attack:
    /// 1. Resolve assists (AssistResolver — declarative)
    /// 2. Calculate primary damage (ResolveDamageRoll command)
    /// 3. Apply damage (ApplyDamageToEnemy/Ranger bricks)
    /// 4. Apply weapon status (ApplyWeaponStatus command)
    /// 5. Process each assist (ProcessAssist command)
    /// 6. Check target death (ResolveTargetDeath command)
    /// 7. Check reactive gimmick (ResolveReactiveGimmick command)
    /// 8. Dispatch events
    /// </summary>
    public class CombatResolver
    {
        private readonly BattleGrid _grid;
        private readonly TypeChart _typeChart;
        private readonly Random _rng;
        private readonly TunableConstants _constants;
        private readonly AssistResolver _assistResolver;
        private readonly GimmickResolver _gimmickResolver;
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
            GimmickResolver gimmickResolver,
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

            // Step 1: Resolve assists (declarative, before damage)
            var assistResolution = _assistResolver.Resolve(
                attacker.Id, attackerPos.Value,
                attacker.ComboScaler.AssistDamageMultiplier,
                rangerStates);

            // Step 2: Calculate primary damage
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

            // Step 3-5: Apply damage, status, assists (only if hit landed)
            if (!damageResult.WasDodged)
            {
                ApplyDamage(target, damageResult.FinalDamage, result);
                ApplyStatus(target, weaponEffect, attacker.Stats.Get(StatType.MAG), result);
                PublishDamageEvent(attacker.Id, target.Id, damageResult);

                // Process each assist (skip dead targets)
                foreach (var assist in assistResolution.Assists)
                {
                    if (!target.IsAlive) break;

                    var assistResult = ProcessAssist.Execute(
                        assist, target, _typeChart, _rng, _constants, _bondTracker);

                    result.AssistResults.Add(assistResult.AssistCombatResult);
                    PublishAssistEvents(assistResult, assist);

                    if (assistResult.AggressionTriggered)
                    {
                        result.AggressionTriggered = true;
                        PublishAggressionEvent(assistResult.AggressionEnemyId, assistResult.AggressionHealthPercentage);
                    }
                }
            }

            // Step 6: Check target death
            ApplyDeathResult(target, result);

            // Step 7: Check reactive gimmick
            var rangerIds = CollectRangerIds();
            var gimmickResult = ResolveReactiveGimmick.Execute(
                target, damageResult.WasDodged, _grid, _gimmickResolver, rangerIds);
            if (gimmickResult != null)
            {
                result.ReactiveGimmick = gimmickResult.Resolution;
                if (gimmickResult.GimmickActivated)
                    ((Enemy)target).OnGimmickActivated();
            }

            // Step 8: Dispatch events
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

            var damageParams = new ResolveDamageRollParams
            {
                AttackerStr = attacker.Stats.Get(StatType.STR),
                AttackerLck = 0,
                DefenderDef = target.Stats.Get(StatType.DEF),
                DefenderLck = target.Stats.Get(StatType.LCK),
                ActionPower = actionPower,
                AttackType = ((ICombatTarget)attacker).Type,
                DefenderType = target.Type,
                DefenderDualType = (target is Ranger rangerTarget) ? rangerTarget.DualType : null,
                ComboMultiplier = 1.0f,
                HasSameTypeBonus = false
            };
            var damageResult = ResolveDamageRoll.Execute(damageParams, _typeChart, _rng, _constants);
            result.PrimaryDamage = damageResult;

            if (!damageResult.WasDodged)
            {
                ApplyDamage(target, damageResult.FinalDamage, result);
                ApplyStatus(target, weaponEffect, attacker.Stats.Get(StatType.MAG), result);
                PublishDamageEvent(attacker.Id, target.Id, damageResult);
            }

            ApplyDeathResult(target, result);

            _eventBus.Dispatch();

            return result;
        }

        // === Private: Thin Helpers (delegation to bricks/commands + event publishing) ===

        /// <summary>Routes damage to the correct brick based on target type. Orchestrator owns this dispatch.</summary>
        private void ApplyDamage(ICombatTarget target, int damage, CombatResult result)
        {
            if (target is Enemy enemy)
            {
                var evt = ApplyDamageToEnemy.Execute(enemy, damage);
                if (evt.BecameAggressive)
                {
                    result.AggressionTriggered = true;
                    PublishAggressionEvent(enemy.Id, enemy.Health.Percentage);
                }
            }
            else if (target is Ranger ranger)
            {
                ApplyDamageToRanger.Execute(ranger, damage);
            }
        }

        private void ApplyStatus(ICombatTarget target, StatusEffectTemplate weaponEffect, float casterMag, CombatResult result)
        {
            if (weaponEffect == null) return;

            StatusEffectTracker tracker = target switch
            {
                Enemy e => e.StatusEffects,
                Ranger r => r.StatusEffects,
                _ => null
            };
            if (tracker == null) return;

            var effectId = ApplyWeaponStatus.Execute(weaponEffect, tracker, casterMag, StatusPotencyMagScale);
            result.StatusEffectsApplied.Add(effectId);
        }

        private void ApplyDeathResult(ICombatTarget target, CombatResult result)
        {
            var deathResult = ResolveTargetDeath.Execute(target);

            if (deathResult.TargetDied)
            {
                result.TargetDied = true;

                if (deathResult.EnemyTypeId != null)
                {
                    _eventBus.Publish(new EnemyDefeatedEvent
                    {
                        EnemyId = deathResult.TargetId,
                        EnemyTypeId = deathResult.EnemyTypeId,
                        EnemyType = deathResult.EnemyType ?? ElementalType.Normal
                    });
                }

                if (deathResult.MissionLost)
                {
                    result.MissionLost = true;
                    _eventBus.Publish(new RangerDiedUnmorphedEvent
                    {
                        RangerId = deathResult.TargetId
                    });
                }
            }

            if (deathResult.FormDied)
            {
                // Perform mutation — demorph the ranger
                var ranger = (Ranger)target;
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
        }

        private void PublishAssistEvents(ProcessAssistResult assistResult, AssistEffect assist)
        {
            _eventBus.Publish(new AssistOccurredEvent
            {
                ActorId = assist.AttackerId,
                AssisterId = assist.AssisterId,
                BondTier = assist.BondTier,
                TriggeredPairAttack = assist.IsPairAttack
            });

            if (assistResult.BondTierChange != null)
            {
                _eventBus.Publish(new BondTierReachedEvent
                {
                    RangerA = assistResult.BondTierChange.RangerA,
                    RangerB = assistResult.BondTierChange.RangerB,
                    OldTier = assistResult.BondTierChange.OldTier,
                    NewTier = assistResult.BondTierChange.NewTier
                });
            }

            if (assistResult.AssistCombatResult.FormDisrupted)
            {
                _eventBus.Publish(new Tier2FormDisruptionEvent
                {
                    AssisterId = assist.AssisterId,
                    DisruptedFormId = assistResult.AssistCombatResult.VacatedFormId
                });
            }

            if (assistResult.AssistCombatResult.RefreshAvailable)
            {
                _eventBus.Publish(new BondRefreshEvent
                {
                    GiverId = assist.AssisterId,
                    ReceiverId = assist.AttackerId
                });
            }
        }

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

        private void PublishAggressionEvent(string enemyId, float healthPercentage)
        {
            _eventBus.Publish(new AggressionTriggeredEvent
            {
                EnemyId = enemyId,
                HealthPercentage = healthPercentage
            });
        }

        private HashSet<string> CollectRangerIds()
        {
            var rangerIds = new HashSet<string>();
            foreach (var unitId in _grid.AllUnitIds)
            {
                if (unitId.StartsWith("ranger_"))
                    rangerIds.Add(unitId);
            }
            return rangerIds;
        }
    }

    // === Resolution Output (unchanged) ===

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
        public Commands.Gimmick.GimmickResolution ReactiveGimmick { get; set; }

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

}
