using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Entities.Forms;
using TokuTactics.Entities.Weapons;
using TokuTactics.Systems.FormManagement;

namespace TokuTactics.Tests.Systems.FormManagement
{
    public class FormPoolTests
    {
        private FormData MakeForm(string id, int cooldown = 3)
        {
            return new FormData(id, id, ElementalType.Blaze,
                StatBlock.Create(), StatBlock.Create(), 100, 10, 4, 1, 1.0f, cooldown,
                new WeaponData($"{id}_a", "A", 1, 1, null),
                new WeaponData($"{id}_b", "B", 1, 1, null));
        }

        private FormPool BuildPool(int budget = 3)
        {
            var pool = new FormPool("base_form", budget);
            pool.RegisterForm(MakeForm("base_form"));
            pool.RegisterForm(MakeForm("form_tank", 3));
            pool.RegisterForm(MakeForm("form_sniper", 4));
            pool.RegisterForm(MakeForm("form_healer", 2));
            pool.RegisterForm(MakeForm("form_scout", 1));
            return pool;
        }

        // === Budget ===

        public void EquipForm_WithinBudget_Succeeds()
        {
            var pool = BuildPool(budget: 3);

            bool result = pool.EquipForm("form_tank");

            Assert(result, "Should succeed within budget");
            Assert(pool.EquippedCount == 1, "Should have 1 equipped");
        }

        public void EquipForm_ExceedsBudget_Fails()
        {
            var pool = BuildPool(budget: 2);
            pool.EquipForm("form_tank");
            pool.EquipForm("form_sniper");

            bool result = pool.EquipForm("form_healer");

            Assert(!result, "Should fail when budget exceeded");
            Assert(pool.EquippedCount == 2, "Should still have 2");
        }

        public void EquipForm_BaseFormAlwaysSucceeds()
        {
            var pool = BuildPool(budget: 0); // Zero budget

            bool result = pool.EquipForm("base_form");

            Assert(result, "Base form should always succeed regardless of budget");
        }

        public void ExpandBudget_IncreasesCapacity()
        {
            var pool = BuildPool(budget: 2);
            pool.EquipForm("form_tank");
            pool.EquipForm("form_sniper");
            Assert(!pool.HasBudgetRemaining, "Should be at capacity");

            pool.ExpandBudget(2);

            Assert(pool.HasBudgetRemaining, "Should have room after expansion");
            Assert(pool.Budget == 4, "Budget should be 4");
        }

        // === Availability ===

        public void CheckAvailability_EquippedAndFree_Available()
        {
            var pool = BuildPool();
            pool.EquipForm("form_tank");

            var result = pool.CheckAvailability("form_tank", "ranger_red");

            Assert(result == FormAvailability.Available, "Should be available");
        }

        public void CheckAvailability_NotEquipped_NotEquipped()
        {
            var pool = BuildPool();
            // Don't equip form_tank

            var result = pool.CheckAvailability("form_tank", "ranger_red");

            Assert(result == FormAvailability.NotEquipped, "Should be NotEquipped");
        }

        public void CheckAvailability_OnCooldown_OnCooldown()
        {
            var pool = BuildPool();
            pool.EquipForm("form_tank");
            pool.OccupyForm("form_tank", "ranger_red");
            pool.VacateForm("form_tank", "ranger_red");

            var result = pool.CheckAvailability("form_tank", "ranger_blue");

            Assert(result == FormAvailability.OnCooldown, "Should be OnCooldown");
        }

        public void CheckAvailability_OccupiedByOther_OccupiedByOther()
        {
            var pool = BuildPool();
            pool.EquipForm("form_tank");
            pool.OccupyForm("form_tank", "ranger_red");

            var result = pool.CheckAvailability("form_tank", "ranger_blue");

            Assert(result == FormAvailability.OccupiedByOther, "Should be OccupiedByOther");
        }

        public void CheckAvailability_OccupiedBySelf_Available()
        {
            var pool = BuildPool();
            pool.EquipForm("form_tank");
            pool.OccupyForm("form_tank", "ranger_red");

            var result = pool.CheckAvailability("form_tank", "ranger_red");

            Assert(result == FormAvailability.Available, "Own form should be available to self");
        }

        public void CheckAvailability_BaseForm_AlwaysAvailable()
        {
            var pool = BuildPool();

            var result = pool.CheckAvailability("base_form", "ranger_red");

            Assert(result == FormAvailability.Available, "Base form always available");
        }

        // === Occupying and Vacating ===

        public void OccupyForm_BlocksOthers()
        {
            var pool = BuildPool();
            pool.EquipForm("form_tank");

            pool.OccupyForm("form_tank", "ranger_red");

            Assert(pool.CheckAvailability("form_tank", "ranger_blue") == FormAvailability.OccupiedByOther,
                "Should block other Rangers");
        }

        public void VacateForm_StartsCooldown()
        {
            var pool = BuildPool();
            pool.EquipForm("form_tank"); // 3 turn cooldown
            pool.OccupyForm("form_tank", "ranger_red");

            int cd = pool.VacateForm("form_tank", "ranger_red");

            Assert(cd == 3, "Should start 3 turn cooldown");
            Assert(pool.CheckAvailability("form_tank", "ranger_red") == FormAvailability.OnCooldown,
                "Should be on cooldown for everyone");
        }

        public void VacateForm_WithMagModifier_ReducesCooldown()
        {
            var pool = BuildPool();
            pool.EquipForm("form_tank"); // 3 turn cooldown
            pool.OccupyForm("form_tank", "ranger_red");

            int cd = pool.VacateForm("form_tank", "ranger_red", magModifier: 1);

            Assert(cd == 2, "Should be reduced to 2 by MAG modifier");
        }

        public void VacateBaseForm_NoCooldown()
        {
            var pool = BuildPool();

            int cd = pool.VacateForm("base_form", "ranger_red");

            Assert(cd == 0, "Base form should not go on cooldown normally");
        }

        // === Cooldown Ticking ===

        public void ProcessTurn_TicksCooldowns()
        {
            var pool = BuildPool();
            pool.EquipForm("form_tank");
            pool.OccupyForm("form_tank", "ranger_red");
            pool.VacateForm("form_tank", "ranger_red"); // 3 turn CD

            pool.ProcessTurn();
            Assert(pool.CheckAvailability("form_tank", "ranger_blue") == FormAvailability.OnCooldown,
                "Should still be on cooldown after 1 tick");

            pool.ProcessTurn();
            pool.ProcessTurn();
            Assert(pool.CheckAvailability("form_tank", "ranger_blue") == FormAvailability.Available,
                "Should be available after 3 ticks");
        }

        // === Base Form Cooldown (Demorph) ===

        public void PutBaseFormOnCooldown_BlocksRemorph()
        {
            var pool = BuildPool();

            pool.PutBaseFormOnCooldown(2);

            Assert(!pool.IsBaseFormAvailable(), "Base should be on cooldown");
        }

        public void BaseFormCooldown_TicksToAvailable()
        {
            var pool = BuildPool();
            pool.PutBaseFormOnCooldown(2);

            pool.ProcessTurn();
            Assert(!pool.IsBaseFormAvailable(), "Should still be on cooldown after 1 tick");

            pool.ProcessTurn();
            Assert(pool.IsBaseFormAvailable(), "Should be available after 2 ticks");
        }

        // === Permadeath ===

        public void PermanentlyRemoveForm_RemovesFromPool()
        {
            var pool = BuildPool();
            pool.EquipForm("form_tank");

            pool.PermanentlyRemoveForm("form_tank");

            Assert(pool.CheckAvailability("form_tank", "ranger_red") == FormAvailability.NotEquipped,
                "Removed form should not be available");
        }

        public void PermanentlyRemoveForm_BaseFormProtected()
        {
            var pool = BuildPool();

            pool.PermanentlyRemoveForm("base_form");

            Assert(pool.CheckAvailability("base_form", "ranger_red") == FormAvailability.Available,
                "Base form cannot be permanently removed");
        }

        // === Clear Loadout ===

        public void ClearLoadout_ResetsEverything()
        {
            var pool = BuildPool();
            pool.EquipForm("form_tank");
            pool.OccupyForm("form_tank", "ranger_red");
            pool.VacateForm("form_tank", "ranger_red");
            pool.LockLoadout();

            pool.ClearLoadout();

            Assert(pool.EquippedCount == 0, "No forms should be equipped");
            Assert(!pool.IsLoadoutLocked, "Loadout should be unlocked after clear");
            Assert(pool.CheckAvailability("form_tank", "ranger_red") == FormAvailability.NotEquipped,
                "Forms should be unequipped after clear");
        }

        // === Loadout Lock ===

        public void LockLoadout_PreventsEquipping()
        {
            var pool = BuildPool();
            pool.EquipForm("form_tank");
            pool.LockLoadout();

            bool result = pool.EquipForm("form_sniper");

            Assert(!result, "Should not equip after loadout is locked");
            Assert(pool.EquippedCount == 1, "Should still have only 1 equipped");
        }

        public void LockLoadout_IsLockedFlag()
        {
            var pool = BuildPool();

            Assert(!pool.IsLoadoutLocked, "Should not be locked initially");

            pool.LockLoadout();

            Assert(pool.IsLoadoutLocked, "Should be locked after LockLoadout");
        }

        public void LockLoadout_BaseFormStillEquippable()
        {
            var pool = BuildPool();
            pool.LockLoadout();

            bool result = pool.EquipForm("base_form");

            Assert(result, "Base form should always succeed even when locked");
        }

        public void LockLoadout_SwitchingStillWorks()
        {
            var pool = BuildPool();
            pool.EquipForm("form_tank");
            pool.LockLoadout();

            // Locking prevents equipping NEW forms, not switching between equipped ones
            bool occupy = pool.OccupyForm("form_tank", "ranger_red");

            Assert(occupy, "Should still be able to occupy equipped forms after lock");
        }

        public void ClearLoadout_UnlocksLoadout()
        {
            var pool = BuildPool();
            pool.LockLoadout();

            pool.ClearLoadout();

            Assert(!pool.IsLoadoutLocked, "Should be unlocked after clear");

            bool result = pool.EquipForm("form_tank");
            Assert(result, "Should be able to equip after clear");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new FormPoolTests();
            tests.EquipForm_WithinBudget_Succeeds();
            tests.EquipForm_ExceedsBudget_Fails();
            tests.EquipForm_BaseFormAlwaysSucceeds();
            tests.ExpandBudget_IncreasesCapacity();
            tests.CheckAvailability_EquippedAndFree_Available();
            tests.CheckAvailability_NotEquipped_NotEquipped();
            tests.CheckAvailability_OnCooldown_OnCooldown();
            tests.CheckAvailability_OccupiedByOther_OccupiedByOther();
            tests.CheckAvailability_OccupiedBySelf_Available();
            tests.CheckAvailability_BaseForm_AlwaysAvailable();
            tests.OccupyForm_BlocksOthers();
            tests.VacateForm_StartsCooldown();
            tests.VacateForm_WithMagModifier_ReducesCooldown();
            tests.VacateBaseForm_NoCooldown();
            tests.ProcessTurn_TicksCooldowns();
            tests.PutBaseFormOnCooldown_BlocksRemorph();
            tests.BaseFormCooldown_TicksToAvailable();
            tests.PermanentlyRemoveForm_RemovesFromPool();
            tests.PermanentlyRemoveForm_BaseFormProtected();
            tests.ClearLoadout_ResetsEverything();
            tests.LockLoadout_PreventsEquipping();
            tests.LockLoadout_IsLockedFlag();
            tests.LockLoadout_BaseFormStillEquippable();
            tests.LockLoadout_SwitchingStillWorks();
            tests.ClearLoadout_UnlocksLoadout();
            System.Console.WriteLine("FormPoolTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
