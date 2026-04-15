using System;
using System.Collections.Generic;
using TokuTactics.Commands.Loadout;
using TokuTactics.Data.Content;
using TokuTactics.Systems.FormManagement;
using TokuTactics.Systems.LoadoutSelection;

namespace TokuTactics.Tests.Commands.Loadout
{
    public static class ExecuteLoadoutSubmissionTests
    {
        public static void Run()
        {
            Test_ValidSubmission_EquipsForms();
            Test_OverBudget_DoesNotEquip();
            Test_InvalidForm_DoesNotEquip();
            Test_UsesInjectedValidator();
            Console.WriteLine("ExecuteLoadoutSubmissionTests: All passed");
        }

        private static void Test_ValidSubmission_EquipsForms()
        {
            var pool = MakePool();
            var result = ExecuteLoadoutSubmission.Execute(
                new List<string> { "form_blaze" }, pool);

            Assert(result == LoadoutResult.Accepted, "Valid submission should be accepted");
            Assert(pool.EquippedCount == 1, $"Should have 1 equipped, got {pool.EquippedCount}");
        }

        private static void Test_OverBudget_DoesNotEquip()
        {
            var pool = MakePool(budget: 1);
            var result = ExecuteLoadoutSubmission.Execute(
                new List<string> { "form_blaze", "form_torrent" }, pool);

            Assert(result == LoadoutResult.OverBudget, "Over budget should be rejected");
            Assert(pool.EquippedCount == 0, "Should not equip any forms");
        }

        private static void Test_InvalidForm_DoesNotEquip()
        {
            var pool = MakePool();
            var result = ExecuteLoadoutSubmission.Execute(
                new List<string> { "form_unknown" }, pool);

            Assert(result == LoadoutResult.InvalidForm, "Invalid form should be rejected");
            Assert(pool.EquippedCount == 0, "Should not equip any forms");
        }

        private static void Test_UsesInjectedValidator()
        {
            var pool = MakePool();
            bool validatorCalled = false;

            ExecuteLoadoutSubmission.Execute(
                new List<string> { "form_blaze" }, pool,
                validateSubmission: (ids, locked, budget, registered, baseId) =>
                {
                    validatorCalled = true;
                    return LoadoutResult.Accepted;
                });

            Assert(validatorCalled, "Should call injected validator");
        }

        private static FormPool MakePool(int budget = 3)
        {
            var pool = new FormPool("form_base", budget);
            pool.RegisterForm(FormCatalog.BlazeForm());
            pool.RegisterForm(FormCatalog.TorrentForm());
            return pool;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
