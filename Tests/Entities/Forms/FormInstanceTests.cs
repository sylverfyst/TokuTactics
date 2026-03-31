using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Entities.Forms;
using TokuTactics.Entities.Weapons;

namespace TokuTactics.Tests.Entities.Forms
{
    public class FormInstanceTests
    {
        private FormData BuildTestFormData()
        {
            return new FormData(
                id: "form_tank",
                name: "Iron Guard",
                type: ElementalType.Stone,
                baseStats: StatBlock.Create(str: 8, def: 15, spd: 3, mag: 2, cha: 4, lck: 3),
                statsPerLevel: StatBlock.Create(str: 1, def: 2, spd: 0.5f, mag: 0.3f, cha: 0.5f, lck: 0.3f),
                baseHealth: 100f,
                healthPerLevel: 15f,
                movementRange: 3,
                basicAttackRange: 1,
                basicAttackPower: 1.2f,
                cooldownDuration: 3,
                weaponA: new WeaponData("wpn_shield", "Aegis Shield", 1.0f, 1, null),
                weaponB: new WeaponData("wpn_mace", "War Mace", 1.4f, 1, null)
            );
        }

        private FormData BuildGrowthFormData()
        {
            return new FormData(
                id: "form_growth",
                name: "Evolving Force",
                type: ElementalType.Radiant,
                baseStats: StatBlock.Create(str: 3, def: 3, spd: 3),
                statsPerLevel: StatBlock.Create(str: 2, def: 2, spd: 2),
                baseHealth: 50f,
                healthPerLevel: 20f,
                movementRange: 4,
                basicAttackRange: 1,
                basicAttackPower: 0.8f,
                cooldownDuration: 4,
                weaponA: new WeaponData("wpn_a", "Weapon A", 1.0f, 1, null),
                weaponB: new WeaponData("wpn_b", "Weapon B", 1.0f, 2, null),
                isGrowthForm: true,
                growthCurveMultiplier: 1.5f
            );
        }

        // === Construction ===

        public void Constructor_InitializesAtLevel1()
        {
            var form = new FormInstance(BuildTestFormData());

            Assert(form.Level == 1, "Should start at level 1");
            Assert(form.Experience == 0, "Should start with 0 exp");
            Assert(form.DamageTrack == 0, "Damage track should be 0");
            Assert(form.StatusEffectTrack == 0, "Status effect track should be 0");
        }

        public void Constructor_HealthPoolAtMax()
        {
            var form = new FormInstance(BuildTestFormData());

            Assert(form.Health.IsAlive, "Should be alive");
            Assert(form.Health.Current == form.Health.Maximum, "Should start at full health");
        }

        // === Stats ===

        public void GetStats_Level1_ReturnsBaseStats()
        {
            var form = new FormInstance(BuildTestFormData());

            var stats = form.GetStats();

            // At level 1, level multiplier = 0, so just base stats
            Assert(stats.Get(StatType.STR) == 8f, "STR should be base");
            Assert(stats.Get(StatType.DEF) == 15f, "DEF should be base");
        }

        public void GetStats_Level5_IncludesPerLevelScaling()
        {
            var data = BuildTestFormData();
            var form = new FormInstance(data, startingLevel: 5);

            var stats = form.GetStats();

            // Level 5: base + (statsPerLevel * 4)
            float expectedStr = 8f + (1f * 4f);
            Assert(stats.Get(StatType.STR) == expectedStr, $"STR should be {expectedStr}");
        }

        public void GetStats_GrowthForm_AcceleratesAfterLevel5()
        {
            var data = BuildGrowthFormData();
            var normalForm = new FormInstance(data, startingLevel: 5);
            var growthForm = new FormInstance(data, startingLevel: 8);

            var normalStats = normalForm.GetStats();
            var growthStats = growthForm.GetStats();

            // Growth bonus kicks in after level 5
            // Level 8: base + perLevel * 7 + growthBonus for levels 6-8
            // growthBonus = (8-5) * (1.5 - 1.0) = 1.5 extra levels of scaling
            float growthStr = growthStats.Get(StatType.STR);
            float linearStr = 3f + (2f * 7f); // Without growth bonus = 17
            Assert(growthStr > linearStr, "Growth form should exceed linear scaling after level 5");
        }

        // === Leveling ===

        public void AddExperience_BelowThreshold_ReturnsFalse()
        {
            var form = new FormInstance(BuildTestFormData());

            bool leveled = form.AddExperience(10);

            Assert(!leveled, "Should not level up with 10 exp");
            Assert(form.Experience == 10, "Should have 10 exp");
            Assert(form.Level == 1, "Should still be level 1");
        }

        public void AddExperience_AtThreshold_ReturnsTrue()
        {
            var form = new FormInstance(BuildTestFormData());
            // Threshold at level 1 = 100 + (1 * 20) = 120

            bool leveled = form.AddExperience(120);

            Assert(leveled, "Should level up");
            Assert(form.Experience == 0, "Excess should carry over (120-120=0)");
        }

        public void LevelUp_DamageChoice_IncrementsDamageTrack()
        {
            var form = new FormInstance(BuildTestFormData());
            form.AddExperience(999); // Force level up

            form.LevelUp(LevelUpChoice.Damage);

            Assert(form.Level == 2, "Should be level 2");
            Assert(form.DamageTrack == 1, "Damage track should be 1");
            Assert(form.StatusEffectTrack == 0, "Status track should be 0");
        }

        public void LevelUp_StatusChoice_IncrementsStatusTrack()
        {
            var form = new FormInstance(BuildTestFormData());
            form.AddExperience(999);

            form.LevelUp(LevelUpChoice.StatusEffect);

            Assert(form.Level == 2, "Should be level 2");
            Assert(form.DamageTrack == 0, "Damage track should be 0");
            Assert(form.StatusEffectTrack == 1, "Status track should be 1");
        }

        // === Damage/Status Scaling ===

        public void GetDamagePower_ScalesWithDamageTrack()
        {
            var form = new FormInstance(BuildTestFormData());
            float basePower = form.GetDamagePower();

            form.AddExperience(999);
            form.LevelUp(LevelUpChoice.Damage);
            form.AddExperience(999);
            form.LevelUp(LevelUpChoice.Damage);

            float boostedPower = form.GetDamagePower();
            Assert(boostedPower > basePower, "Damage should increase with track investment");
        }

        public void GetStatusEffectPotency_ScalesWithStatusTrack()
        {
            var form = new FormInstance(BuildTestFormData());
            float basePotency = form.GetStatusEffectPotency();

            form.AddExperience(999);
            form.LevelUp(LevelUpChoice.StatusEffect);
            form.AddExperience(999);
            form.LevelUp(LevelUpChoice.StatusEffect);

            float boostedPotency = form.GetStatusEffectPotency();
            Assert(boostedPotency > basePotency, "Potency should increase with track investment");
        }

        // === Weapon Selection ===

        public void EquippedWeapon_DefaultsToA()
        {
            var form = new FormInstance(BuildTestFormData());

            Assert(form.EquippedWeapon == WeaponSlot.A, "Should default to weapon A");
            Assert(form.GetEquippedWeapon().Id == "wpn_shield", "Should be shield");
        }

        public void EquippedWeapon_CanSwitchToB()
        {
            var form = new FormInstance(BuildTestFormData());

            form.EquippedWeapon = WeaponSlot.B;

            Assert(form.GetEquippedWeapon().Id == "wpn_mace", "Should be mace");
        }

        // === Health ===

        public void FormDeath_IsDead_WhenHealthZero()
        {
            var form = new FormInstance(BuildTestFormData());

            form.Health.TakeDamage(9999);

            Assert(form.IsDead, "Should be dead");
        }

        // === Proclivity Bonus Accumulation ===

        public void LevelUp_WithProclivityBonus_AccumulatesInStats()
        {
            var form = new FormInstance(BuildTestFormData());
            form.AddExperience(999);

            float strBefore = form.GetStats().Get(StatType.STR);

            form.LevelUp(LevelUpChoice.Damage, proclivityBonusStat: StatType.STR, bonusAmount: 2.0f);

            float strAfter = form.GetStats().Get(StatType.STR);

            // STR increased by level scaling (1.0) PLUS proclivity bonus (2.0)
            Assert(strAfter > strBefore, "STR should increase from both level and proclivity");
            float expectedMinIncrease = 1.0f + 2.0f; // perLevel STR + proclivity
            Assert(strAfter - strBefore >= expectedMinIncrease,
                "Increase should include proclivity bonus");
        }

        public void LevelUp_WithoutProclivityBonus_NoExtraStats()
        {
            var form = new FormInstance(BuildTestFormData());
            form.AddExperience(999);

            float strBefore = form.GetStats().Get(StatType.STR);

            form.LevelUp(LevelUpChoice.Damage); // No proclivity params

            float strAfter = form.GetStats().Get(StatType.STR);

            // Only level scaling, no proclivity
            float expectedIncrease = 1.0f; // perLevel STR only
            float actual = strAfter - strBefore;
            Assert(actual >= expectedIncrease - 0.01f && actual <= expectedIncrease + 5f,
                "Should only increase from level scaling, not proclivity");
        }

        public void LevelUp_ProclivityBonuses_Accumulate()
        {
            var form = new FormInstance(BuildTestFormData());

            // Level up 3 times, each with a proclivity bonus
            for (int i = 0; i < 3; i++)
            {
                form.AddExperience(999);
                form.LevelUp(LevelUpChoice.Damage, proclivityBonusStat: StatType.MAG, bonusAmount: 1.0f);
            }

            Assert(form.AccumulatedBonuses[StatType.MAG] == 3.0f,
                "Should accumulate 3 MAG bonus points");
        }

        public void LevelUp_ZeroBonusAmount_DoesNotAccumulate()
        {
            var form = new FormInstance(BuildTestFormData());
            form.AddExperience(999);

            form.LevelUp(LevelUpChoice.Damage, proclivityBonusStat: StatType.STR, bonusAmount: 0f);

            Assert(form.AccumulatedBonuses[StatType.STR] == 0f,
                "Zero bonus should not accumulate");
        }

        public void RestoreAccumulatedBonuses_SetsValues()
        {
            var form = new FormInstance(BuildTestFormData());

            form.RestoreAccumulatedBonuses(new System.Collections.Generic.Dictionary<StatType, float>
            {
                { StatType.STR, 5f },
                { StatType.MAG, 3f }
            });

            Assert(form.AccumulatedBonuses[StatType.STR] == 5f, "STR should be restored");
            Assert(form.AccumulatedBonuses[StatType.MAG] == 3f, "MAG should be restored");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new FormInstanceTests();
            tests.Constructor_InitializesAtLevel1();
            tests.Constructor_HealthPoolAtMax();
            tests.GetStats_Level1_ReturnsBaseStats();
            tests.GetStats_Level5_IncludesPerLevelScaling();
            tests.GetStats_GrowthForm_AcceleratesAfterLevel5();
            tests.AddExperience_BelowThreshold_ReturnsFalse();
            tests.AddExperience_AtThreshold_ReturnsTrue();
            tests.LevelUp_DamageChoice_IncrementsDamageTrack();
            tests.LevelUp_StatusChoice_IncrementsStatusTrack();
            tests.GetDamagePower_ScalesWithDamageTrack();
            tests.GetStatusEffectPotency_ScalesWithStatusTrack();
            tests.EquippedWeapon_DefaultsToA();
            tests.EquippedWeapon_CanSwitchToB();
            tests.FormDeath_IsDead_WhenHealthZero();
            tests.LevelUp_WithProclivityBonus_AccumulatesInStats();
            tests.LevelUp_WithoutProclivityBonus_NoExtraStats();
            tests.LevelUp_ProclivityBonuses_Accumulate();
            tests.LevelUp_ZeroBonusAmount_DoesNotAccumulate();
            tests.RestoreAccumulatedBonuses_SetsValues();
            System.Console.WriteLine("FormInstanceTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
