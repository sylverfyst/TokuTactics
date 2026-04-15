using System;
using System.Collections.Generic;
using TokuTactics.Commands.Loadout;

namespace TokuTactics.Tests.Commands.Loadout
{
    public static class ExecuteLoadoutSubmissionTests
    {
        public static void Run()
        {
            Test_ValidSubmission_Accepted();
            Test_OverBudget_Rejected();
            Test_InvalidForm_Rejected();
            Test_UsesInjectedValidator();
            Console.WriteLine("ExecuteLoadoutSubmissionTests: All passed");
        }

        private static void Test_ValidSubmission_Accepted()
        {
            var registered = new HashSet<string> { "form_blaze", "form_torrent" };
            var result = ExecuteLoadoutSubmission.Execute(
                new List<string> { "form_blaze" }, false, 3, registered, "form_base");
            Assert(result == LoadoutResult.Accepted, "Valid submission should be accepted");
        }

        private static void Test_OverBudget_Rejected()
        {
            var registered = new HashSet<string> { "form_blaze", "form_torrent" };
            var result = ExecuteLoadoutSubmission.Execute(
                new List<string> { "form_blaze", "form_torrent" }, false, 1, registered, "form_base");
            Assert(result == LoadoutResult.OverBudget, "Over budget should be rejected");
        }

        private static void Test_InvalidForm_Rejected()
        {
            var registered = new HashSet<string> { "form_blaze" };
            var result = ExecuteLoadoutSubmission.Execute(
                new List<string> { "form_unknown" }, false, 3, registered, "form_base");
            Assert(result == LoadoutResult.InvalidForm, "Invalid form should be rejected");
        }

        private static void Test_UsesInjectedValidator()
        {
            var registered = new HashSet<string> { "form_blaze" };
            bool validatorCalled = false;

            ExecuteLoadoutSubmission.Execute(
                new List<string> { "form_blaze" }, false, 3, registered, "form_base",
                validateSubmission: (ids, locked, budget, reg, baseId) =>
                {
                    validatorCalled = true;
                    return LoadoutResult.Accepted;
                });

            Assert(validatorCalled, "Should call injected validator");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
