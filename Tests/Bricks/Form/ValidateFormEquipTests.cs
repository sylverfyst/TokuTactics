using System;
using System.Collections.Generic;
using TokuTactics.Bricks.Form;

namespace TokuTactics.Tests.Bricks.Form
{
    public static class ValidateFormEquipTests
    {
        public static void Run()
        {
            Test_BaseForm_AlwaysTrue();
            Test_LockedLoadout_ReturnsFalse();
            Test_UnknownForm_ReturnsFalse();
            Test_OverBudget_ReturnsFalse();
            Test_ValidEquip_ReturnsTrue();
            Console.WriteLine("ValidateFormEquipTests: All passed");
        }

        private static void Test_BaseForm_AlwaysTrue()
        {
            Assert(ValidateFormEquip.Execute("form_base", "form_base", true, new HashSet<string>(), 99, 3) == true,
                "Base form should always be equippable");
        }

        private static void Test_LockedLoadout_ReturnsFalse()
        {
            var registered = new HashSet<string> { "form_blaze" };
            Assert(ValidateFormEquip.Execute("form_blaze", "form_base", true, registered, 0, 3) == false,
                "Locked loadout should prevent equip");
        }

        private static void Test_UnknownForm_ReturnsFalse()
        {
            Assert(ValidateFormEquip.Execute("form_unknown", "form_base", false, new HashSet<string>(), 0, 3) == false,
                "Unknown form should prevent equip");
        }

        private static void Test_OverBudget_ReturnsFalse()
        {
            var registered = new HashSet<string> { "form_blaze" };
            Assert(ValidateFormEquip.Execute("form_blaze", "form_base", false, registered, 3, 3) == false,
                "Over budget should prevent equip");
        }

        private static void Test_ValidEquip_ReturnsTrue()
        {
            var registered = new HashSet<string> { "form_blaze" };
            Assert(ValidateFormEquip.Execute("form_blaze", "form_base", false, registered, 1, 3) == true,
                "Valid equip should succeed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
