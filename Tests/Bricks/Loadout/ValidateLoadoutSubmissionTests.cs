using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Loadout;
using TokuTactics.Systems.LoadoutSelection;

namespace TokuTactics.Tests.Bricks.Loadout
{
    public static class ValidateLoadoutSubmissionTests
    {
        public static void Run()
        {
            Test_Valid_Accepted();
            Test_AlreadyLocked_Rejected();
            Test_Empty_Rejected();
            Test_NullList_Rejected();
            Test_OverBudget_Rejected();
            Test_InvalidForm_Rejected();
            Test_BaseFormSkipped();
            Console.WriteLine("ValidateLoadoutSubmissionTests: All passed");
        }

        private static void Test_Valid_Accepted()
        {
            var registered = new HashSet<string> { "form_blaze", "form_torrent" };
            var result = ValidateLoadoutSubmission.Execute(
                new List<string> { "form_blaze" }, false, 3, registered, "form_base");
            Assert(result == LoadoutResult.Accepted, "Valid submission should be accepted");
        }

        private static void Test_AlreadyLocked_Rejected()
        {
            var result = ValidateLoadoutSubmission.Execute(
                new List<string> { "form_blaze" }, true, 3, new HashSet<string>(), "form_base");
            Assert(result == LoadoutResult.AlreadyLocked, "Locked should be rejected");
        }

        private static void Test_Empty_Rejected()
        {
            var result = ValidateLoadoutSubmission.Execute(
                new List<string>(), false, 3, new HashSet<string>(), "form_base");
            Assert(result == LoadoutResult.Empty, "Empty should be rejected");
        }

        private static void Test_NullList_Rejected()
        {
            var result = ValidateLoadoutSubmission.Execute(
                null, false, 3, new HashSet<string>(), "form_base");
            Assert(result == LoadoutResult.Empty, "Null should be rejected as empty");
        }

        private static void Test_OverBudget_Rejected()
        {
            var registered = new HashSet<string> { "a", "b", "c", "d" };
            var result = ValidateLoadoutSubmission.Execute(
                new List<string> { "a", "b", "c", "d" }, false, 3, registered, "form_base");
            Assert(result == LoadoutResult.OverBudget, "Over budget should be rejected");
        }

        private static void Test_InvalidForm_Rejected()
        {
            var registered = new HashSet<string> { "form_blaze" };
            var result = ValidateLoadoutSubmission.Execute(
                new List<string> { "form_unknown" }, false, 3, registered, "form_base");
            Assert(result == LoadoutResult.InvalidForm, "Unknown form should be rejected");
        }

        private static void Test_BaseFormSkipped()
        {
            var registered = new HashSet<string> { "form_blaze" };
            var result = ValidateLoadoutSubmission.Execute(
                new List<string> { "form_base", "form_blaze" }, false, 3, registered, "form_base");
            Assert(result == LoadoutResult.Accepted, "Base form should be skipped in validation");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
