using System;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Entities.Forms;
using TokuTactics.Entities.Rangers;
using TokuTactics.Entities.Weapons;

namespace TokuTactics.Tests.Entities.Rangers
{
    public class RangerTests
    {
        private FormData MakeBaseFormData()
        {
            return new FormData("base_form", "Base", ElementalType.Blaze,
                StatBlock.Create(str: 5, def: 5, spd: 5), StatBlock.Create(str: 1, def: 1, spd: 1),
                80f, 10f, 4, 1, 1.0f, 2,
                new WeaponData("base_a", "A", 1, 1, null),
                new WeaponData("base_b", "B", 1, 1, null));
        }

        private FormData MakeTankFormData()
        {
            return new FormData("form_tank", "Tank", ElementalType.Stone,
                StatBlock.Create(str: 8, def: 15, spd: 3), StatBlock.Create(str: 1, def: 2, spd: 0.5f),
                120f, 15f, 3, 1, 1.2f, 3,
                new WeaponData("tank_a", "Shield", 1, 1, null),
                new WeaponData("tank_b", "Mace", 1.4f, 1, null));
        }

        private FormData MakeSniperFormData()
        {
            return new FormData("form_sniper", "Sniper", ElementalType.Gale,
                StatBlock.Create(str: 12, def: 4, spd: 6), StatBlock.Create(str: 2, def: 0.5f, spd: 1),
                60f, 8f, 5, 4, 1.5f, 4,
                new WeaponData("sniper_a", "Rifle", 1.8f, 4, null),
                new WeaponData("sniper_b", "Pistol", 1.2f, 2, null));
        }

        private Ranger BuildRanger()
        {
            return new Ranger(
                "ranger_red", "Red Blaze",
                ElementalType.Blaze,
                new Proclivity(StatType.STR),
                null, // Personal ability — not needed for these tests
                StatBlock.Create(str: 3, def: 3, spd: 3),
                50f, // Unmorphed health
                MakeBaseFormData());
        }

        // === Construction ===

        public void Constructor_StartsUnmorphed()
        {
            var ranger = BuildRanger();

            Assert(ranger.MorphState == MorphState.Unmorphed, "Should start unmorphed");
            Assert(ranger.CurrentForm == null, "Should have no current form");
            Assert(ranger.IsAlive, "Should be alive");
        }

        public void Constructor_HasBaseFormInstance()
        {
            var ranger = BuildRanger();

            Assert(ranger.BaseForm != null, "Should have base form");
            Assert(ranger.HasFormInstance("base_form"), "Should have base form instance");
        }

        // === Morph State ===

        public void Morph_TransitionsToMorphed()
        {
            var ranger = BuildRanger();

            bool result = ranger.Morph();

            Assert(result, "Should succeed");
            Assert(ranger.MorphState == MorphState.Morphed, "Should be morphed");
            Assert(ranger.CurrentForm == ranger.BaseForm, "Should be in base form");
        }

        public void Morph_WhileAlreadyMorphed_Fails()
        {
            var ranger = BuildRanger();
            ranger.Morph();

            bool result = ranger.Morph();

            Assert(!result, "Should fail when already morphed");
        }

        public void Morph_WhileDemorphed_Fails()
        {
            var ranger = BuildRanger();
            ranger.Morph();
            ranger.TakeDamage(9999f); // Kill form, demorph

            Assert(ranger.MorphState == MorphState.Demorphed, "Should be demorphed");

            bool result = ranger.Morph();

            Assert(!result, "Should fail when demorphed — must use Remorph instead");
        }

        // === Dual Type ===

        public void DualType_Unmorphed_SingleType()
        {
            var ranger = BuildRanger();

            var dt = ranger.DualType;

            Assert(dt.RangerType == ElementalType.Blaze, "Should be Ranger type");
            Assert(dt.FormType == ElementalType.Blaze, "Should be same as Ranger when unmorphed");
            Assert(dt.IsSameType, "Single type should be same-type");
        }

        public void DualType_Morphed_CombinesRangerAndForm()
        {
            var ranger = BuildRanger();
            ranger.Morph();

            // Base form is Blaze, Ranger is Blaze — same type
            var baseDt = ranger.DualType;
            Assert(baseDt.RangerType == ElementalType.Blaze, "Ranger type");
            Assert(baseDt.FormType == ElementalType.Blaze, "Base form type");
            Assert(baseDt.IsSameType, "Same Ranger and base form type");

            // Switch to tank form (Stone type)
            var tankInstance = ranger.GetOrCreateFormInstance(MakeTankFormData());
            ranger.SwitchForm(tankInstance);

            var tankDt = ranger.DualType;
            Assert(tankDt.RangerType == ElementalType.Blaze, "Still Ranger type");
            Assert(tankDt.FormType == ElementalType.Stone, "Tank form type");
            Assert(!tankDt.IsSameType, "Blaze != Stone");
        }

        // === Stats ===

        public void Stats_Unmorphed_ReturnsUnmorphedStats()
        {
            var ranger = BuildRanger();

            Assert(ranger.Stats.Get(StatType.STR) == 3f, "Should use unmorphed stats");
        }

        public void Stats_Morphed_ReturnsFormStats()
        {
            var ranger = BuildRanger();
            ranger.Morph();

            Assert(ranger.Stats.Get(StatType.STR) == 5f, "Should use base form stats");
        }

        // === Form Switching ===

        public void SwitchForm_ReturnsPreviousForm()
        {
            var ranger = BuildRanger();
            ranger.Morph();
            var tankInstance = ranger.GetOrCreateFormInstance(MakeTankFormData());

            var previous = ranger.SwitchForm(tankInstance);

            Assert(previous == ranger.BaseForm, "Should return base form as previous");
            Assert(ranger.CurrentForm == tankInstance, "Should now be in tank form");
        }

        public void SwitchForm_AdvancesComboChain()
        {
            var ranger = BuildRanger();
            ranger.Morph();
            var tankInstance = ranger.GetOrCreateFormInstance(MakeTankFormData());

            Assert(ranger.ComboScaler.ChainCount == 0, "Should start at 0");

            ranger.SwitchForm(tankInstance);

            Assert(ranger.ComboScaler.ChainCount == 1, "Should advance to 1");
        }

        public void SwitchForm_WhileUnmorphed_ReturnsNull()
        {
            var ranger = BuildRanger();
            var tankInstance = ranger.GetOrCreateFormInstance(MakeTankFormData());

            var result = ranger.SwitchForm(tankInstance);

            Assert(result == null, "Should fail when unmorphed");
        }

        public void SwitchForm_ToSameForm_ReturnsNull()
        {
            var ranger = BuildRanger();
            ranger.Morph();

            var result = ranger.SwitchForm(ranger.BaseForm);

            Assert(result == null, "Should fail when switching to same form");
        }

        // === Health and Damage ===

        public void TakeDamage_Morphed_DamagesFormHealth()
        {
            var ranger = BuildRanger();
            ranger.Morph();
            float initialHealth = ranger.CurrentForm.Health.Current;

            var evt = ranger.TakeDamage(20f);

            Assert(ranger.CurrentForm.Health.Current < initialHealth, "Form health should decrease");
            Assert(evt.DamageDealt == 20f, "Should report damage");
            Assert(!evt.FormDied, "Form should survive");
        }

        public void TakeDamage_FormDeath_Demorphs()
        {
            var ranger = BuildRanger();
            ranger.Morph();

            var evt = ranger.TakeDamage(9999f);

            Assert(evt.FormDied, "Form should die");
            Assert(evt.DeadFormId == "base_form", "Should report which form died");
            Assert(ranger.MorphState == MorphState.Demorphed, "Should be demorphed");
            Assert(ranger.CurrentForm == null, "Should have no current form");
        }

        public void TakeDamage_Unmorphed_DamagesUnmorphedHealth()
        {
            var ranger = BuildRanger();

            var evt = ranger.TakeDamage(20f);

            Assert(ranger.UnmorphedHealth.Current == 30f, "Unmorphed health should decrease");
            Assert(!evt.RangerDiedUnmorphed, "Should survive");
        }

        public void TakeDamage_UnmorphedDeath_FlagsMissionLoss()
        {
            var ranger = BuildRanger();

            var evt = ranger.TakeDamage(9999f);

            Assert(evt.RangerDiedUnmorphed, "Should flag mission loss");
            Assert(!ranger.IsAlive, "Ranger should be dead");
        }

        public void TakeDamage_FormDeathThenUnmorphedDeath_BothFlagged()
        {
            var ranger = BuildRanger();
            ranger.Morph();

            // Kill the form
            var evt1 = ranger.TakeDamage(9999f);
            Assert(evt1.FormDied, "Form should die");
            Assert(ranger.MorphState == MorphState.Demorphed, "Should demorph");

            // Now kill unmorphed
            var evt2 = ranger.TakeDamage(9999f);
            Assert(evt2.RangerDiedUnmorphed, "Should flag unmorphed death");
        }

        // === Demorph and Remorph ===

        public void Remorph_AfterDemorph_Works()
        {
            var ranger = BuildRanger();
            ranger.Morph();
            ranger.TakeDamage(9999f); // Kill form, demorph

            Assert(ranger.MorphState == MorphState.Demorphed, "Should be demorphed");

            bool result = ranger.Remorph();

            Assert(result, "Should succeed");
            Assert(ranger.MorphState == MorphState.Morphed, "Should be morphed");
            Assert(ranger.CurrentForm == ranger.BaseForm, "Should be in base form");
        }

        public void Remorph_WhileMorphed_Fails()
        {
            var ranger = BuildRanger();
            ranger.Morph();

            bool result = ranger.Remorph();

            Assert(!result, "Should fail when already morphed");
        }

        // === Form Instance Management ===

        public void GetOrCreateFormInstance_CreatesOnFirstUse()
        {
            var ranger = BuildRanger();

            Assert(!ranger.HasFormInstance("form_tank"), "Should not have tank form yet");

            var instance = ranger.GetOrCreateFormInstance(MakeTankFormData());

            Assert(instance != null, "Should create instance");
            Assert(ranger.HasFormInstance("form_tank"), "Should have tank form now");
        }

        public void GetOrCreateFormInstance_ReturnsSameOnSecondUse()
        {
            var ranger = BuildRanger();
            var tankData = MakeTankFormData();

            var first = ranger.GetOrCreateFormInstance(tankData);
            var second = ranger.GetOrCreateFormInstance(tankData);

            Assert(ReferenceEquals(first, second), "Should return same instance");
        }

        public void RemoveFormInstance_Works()
        {
            var ranger = BuildRanger();
            ranger.GetOrCreateFormInstance(MakeTankFormData());

            bool result = ranger.RemoveFormInstance("form_tank");

            Assert(result, "Should succeed");
            Assert(!ranger.HasFormInstance("form_tank"), "Should be removed");
        }

        public void RemoveFormInstance_BaseForm_Protected()
        {
            var ranger = BuildRanger();

            bool result = ranger.RemoveFormInstance("base_form");

            Assert(!result, "Should not remove base form");
            Assert(ranger.HasFormInstance("base_form"), "Base form should remain");
        }

        // === Turn Management ===

        public void StartTurn_ResetsComboChain()
        {
            var ranger = BuildRanger();
            ranger.Morph();
            var tankInstance = ranger.GetOrCreateFormInstance(MakeTankFormData());
            ranger.SwitchForm(tankInstance);
            Assert(ranger.ComboScaler.ChainCount == 1, "Should have chain from switch");

            ranger.StartTurn();

            Assert(ranger.ComboScaler.ChainCount == 0, "Should reset chain on turn start");
        }

        // === 6th Ranger Flag ===

        public void SixthRanger_FlagIsSet()
        {
            var ranger = new Ranger(
                "ranger_6th", "Shadow Knight",
                ElementalType.Shadow,
                new Proclivity(StatType.MAG),
                null,
                StatBlock.Create(str: 5, def: 5, spd: 5),
                60f,
                MakeBaseFormData(),
                isSixthRanger: true);

            Assert(ranger.IsSixthRanger, "Should be flagged as 6th Ranger");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new RangerTests();
            tests.Constructor_StartsUnmorphed();
            tests.Constructor_HasBaseFormInstance();
            tests.Morph_TransitionsToMorphed();
            tests.Morph_WhileAlreadyMorphed_Fails();
            tests.Morph_WhileDemorphed_Fails();
            tests.DualType_Unmorphed_SingleType();
            tests.DualType_Morphed_CombinesRangerAndForm();
            tests.Stats_Unmorphed_ReturnsUnmorphedStats();
            tests.Stats_Morphed_ReturnsFormStats();
            tests.SwitchForm_ReturnsPreviousForm();
            tests.SwitchForm_AdvancesComboChain();
            tests.SwitchForm_WhileUnmorphed_ReturnsNull();
            tests.SwitchForm_ToSameForm_ReturnsNull();
            tests.TakeDamage_Morphed_DamagesFormHealth();
            tests.TakeDamage_FormDeath_Demorphs();
            tests.TakeDamage_Unmorphed_DamagesUnmorphedHealth();
            tests.TakeDamage_UnmorphedDeath_FlagsMissionLoss();
            tests.TakeDamage_FormDeathThenUnmorphedDeath_BothFlagged();
            tests.Remorph_AfterDemorph_Works();
            tests.Remorph_WhileMorphed_Fails();
            tests.GetOrCreateFormInstance_CreatesOnFirstUse();
            tests.GetOrCreateFormInstance_ReturnsSameOnSecondUse();
            tests.RemoveFormInstance_Works();
            tests.RemoveFormInstance_BaseForm_Protected();
            tests.StartTurn_ResetsComboChain();
            tests.SixthRanger_FlagIsSet();
            System.Console.WriteLine("RangerTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
