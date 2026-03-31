using System.Collections.Generic;
using TokuTactics.Core.Events;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Entities.Forms;
using TokuTactics.Entities.Rangers;
using TokuTactics.Systems.FormManagement;
using TokuTactics.Systems.LoadoutSelection;
using TokuTactics.Data.Content;

namespace TokuTactics.Tests.Systems.LoadoutSelection
{
    public class LoadoutControllerTests
    {
        // === Helpers ===

        private FormPool MakeFormPool(int budget = 3)
        {
            var pool = new FormPool("form_base", budget);
            pool.RegisterForm(FormCatalog.BaseForm());
            pool.RegisterForm(FormCatalog.BlazeForm());
            pool.RegisterForm(FormCatalog.TorrentForm());
            pool.RegisterForm(FormCatalog.FrostForm());
            return pool;
        }

        private LoadoutController MakeController(FormPool pool = null)
        {
            pool ??= MakeFormPool();
            return new LoadoutController(pool, new EventBus());
        }

        private Ranger MakeRanger(string id)
        {
            return new Ranger(id, id, ElementalType.Blaze,
                new Proclivity(StatType.STR), null,
                StatBlock.Create(str: 8, def: 5, spd: 6, mag: 4),
                50f, FormCatalog.BaseForm());
        }

        // === Morph Gate ===

        public void RequestMorph_BeforeLoadout_ReturnsNeedsLoadout()
        {
            var ctrl = MakeController();
            var ranger = MakeRanger("r1");

            var result = ctrl.RequestMorph(ranger);

            Assert(result == MorphRequestResult.NeedsLoadout,
                "First morph should require loadout selection");
            Assert(ctrl.TriggeringRangerId == "r1",
                "Should record who triggered the loadout");
            Assert(ranger.MorphState == MorphState.Unmorphed,
                "Ranger should NOT morph yet");
        }

        public void RequestMorph_AfterLoadout_MorphsDirectly()
        {
            var pool = MakeFormPool();
            var ctrl = MakeController(pool);
            var ranger = MakeRanger("r1");

            // Submit loadout first
            ctrl.SubmitLoadout(new List<string> { "form_blaze" });

            var result = ctrl.RequestMorph(ranger);

            Assert(result == MorphRequestResult.MorphComplete, "Should morph directly");
            Assert(ranger.MorphState == MorphState.Morphed, "Ranger should be morphed");
        }

        public void RequestMorph_AlreadyMorphed_Invalid()
        {
            var pool = MakeFormPool();
            var ctrl = MakeController(pool);
            var ranger = MakeRanger("r1");

            ctrl.SubmitLoadout(new List<string> { "form_blaze" });
            ctrl.RequestMorph(ranger); // First morph

            var result = ctrl.RequestMorph(ranger);

            Assert(result == MorphRequestResult.Invalid,
                "Cannot morph when already morphed");
        }

        public void RequestMorph_DeadRanger_Invalid()
        {
            var ctrl = MakeController();
            var ranger = MakeRanger("r1");
            ranger.UnmorphedHealth.TakeDamage(100);

            var result = ctrl.RequestMorph(ranger);

            Assert(result == MorphRequestResult.Invalid,
                "Cannot morph when dead");
        }

        public void RequestMorph_SecondRanger_AfterLoadout_NoRelock()
        {
            var pool = MakeFormPool();
            var ctrl = MakeController(pool);
            var r1 = MakeRanger("r1");
            var r2 = MakeRanger("r2");

            // First Ranger triggers loadout
            ctrl.RequestMorph(r1);
            ctrl.SubmitLoadout(new List<string> { "form_blaze", "form_torrent" });

            // First Ranger morphs
            var result1 = ctrl.RequestMorph(r1);
            Assert(result1 == MorphRequestResult.MorphComplete, "r1 should morph");

            // Second Ranger morphs — should NOT trigger loadout again
            var result2 = ctrl.RequestMorph(r2);
            Assert(result2 == MorphRequestResult.MorphComplete,
                "r2 should morph directly — loadout already locked");
        }

        // === Loadout Submission ===

        public void SubmitLoadout_WithinBudget_Accepted()
        {
            var pool = MakeFormPool(budget: 3);
            var ctrl = MakeController(pool);

            var result = ctrl.SubmitLoadout(new List<string>
            {
                "form_blaze", "form_torrent"
            });

            Assert(result == LoadoutResult.Accepted, "Should accept within budget");
            Assert(ctrl.IsLoadoutComplete, "Loadout should be locked");
        }

        public void SubmitLoadout_ExactBudget_Accepted()
        {
            var pool = MakeFormPool(budget: 3);
            var ctrl = MakeController(pool);

            var result = ctrl.SubmitLoadout(new List<string>
            {
                "form_blaze", "form_torrent", "form_frost"
            });

            Assert(result == LoadoutResult.Accepted, "Exact budget should be accepted");
        }

        public void SubmitLoadout_OverBudget_Rejected()
        {
            var pool = MakeFormPool(budget: 2);
            var ctrl = MakeController(pool);

            var result = ctrl.SubmitLoadout(new List<string>
            {
                "form_blaze", "form_torrent", "form_frost"
            });

            Assert(result == LoadoutResult.OverBudget, "Should reject over budget");
            Assert(!ctrl.IsLoadoutComplete, "Loadout should NOT be locked");
        }

        public void SubmitLoadout_InvalidFormId_Rejected()
        {
            var ctrl = MakeController();

            var result = ctrl.SubmitLoadout(new List<string>
            {
                "form_blaze", "form_nonexistent"
            });

            Assert(result == LoadoutResult.InvalidForm, "Should reject unknown form");
            Assert(!ctrl.IsLoadoutComplete, "Loadout should NOT be locked");
        }

        public void SubmitLoadout_Empty_Rejected()
        {
            var ctrl = MakeController();

            var result = ctrl.SubmitLoadout(new List<string>());

            Assert(result == LoadoutResult.Empty, "Should reject empty loadout");
        }

        public void SubmitLoadout_AlreadyLocked_Rejected()
        {
            var pool = MakeFormPool();
            var ctrl = MakeController(pool);

            ctrl.SubmitLoadout(new List<string> { "form_blaze" });
            var result = ctrl.SubmitLoadout(new List<string> { "form_torrent" });

            Assert(result == LoadoutResult.AlreadyLocked,
                "Should reject after loadout is locked");
        }

        // === Loadout Screen Data ===

        public void GetLoadoutScreenData_ShowsNonBaseForms()
        {
            var ctrl = MakeController();

            var data = ctrl.GetLoadoutScreenData();

            Assert(data.AvailableForms.Count == 3,
                "Should show 3 non-base forms (Blaze, Torrent, Frost)");
            Assert(!data.AvailableForms.Exists(f => f.FormData.Id == "form_base"),
                "Should not include base form");
        }

        public void GetLoadoutScreenData_ShowsBudget()
        {
            var pool = MakeFormPool(budget: 2);
            var ctrl = MakeController(pool);

            var data = ctrl.GetLoadoutScreenData();

            Assert(data.Budget == 2, "Should show correct budget");
        }

        public void GetLoadoutScreenData_IncludesScoutingIntel()
        {
            var ctrl = MakeController();
            ctrl.RevealEnemyType("wyrm_1", ElementalType.Frost);
            ctrl.ObserveEnemy("putty_1");

            var data = ctrl.GetLoadoutScreenData();

            Assert(data.RevealedEnemyTypes.ContainsKey("wyrm_1"),
                "Should include revealed type");
            Assert(data.RevealedEnemyTypes["wyrm_1"] == ElementalType.Frost,
                "Should show correct type");
            Assert(data.ObservedEnemyIds.Contains("putty_1"),
                "Should include observed enemy");
        }

        // === Scouting Intelligence ===

        public void Scouting_RevealType()
        {
            var ctrl = MakeController();

            ctrl.RevealEnemyType("wyrm_1", ElementalType.Frost);

            Assert(ctrl.Scouting.IsTypeRevealed("wyrm_1"), "Type should be revealed");
            Assert(ctrl.Scouting.GetRevealedType("wyrm_1") == ElementalType.Frost,
                "Should be Frost");
        }

        public void Scouting_ObserveWithoutReveal()
        {
            var ctrl = MakeController();

            ctrl.ObserveEnemy("putty_1");

            Assert(!ctrl.Scouting.IsTypeRevealed("putty_1"),
                "Observed but not revealed");
            Assert(ctrl.Scouting.GetObservedEnemyIds().Contains("putty_1"),
                "Should be in observed set");
        }

        public void Scouting_RevealAlsoObserves()
        {
            var ctrl = MakeController();

            ctrl.RevealEnemyType("wyrm_1", ElementalType.Frost);

            Assert(ctrl.Scouting.GetObservedEnemyIds().Contains("wyrm_1"),
                "Revealing type should also mark as observed");
        }

        public void Scouting_UnknownEnemy_ReturnsNull()
        {
            var ctrl = MakeController();

            Assert(ctrl.Scouting.GetRevealedType("unknown") == null,
                "Unknown enemy should return null type");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var t = new LoadoutControllerTests();

            // Morph gate
            t.RequestMorph_BeforeLoadout_ReturnsNeedsLoadout();
            t.RequestMorph_AfterLoadout_MorphsDirectly();
            t.RequestMorph_AlreadyMorphed_Invalid();
            t.RequestMorph_DeadRanger_Invalid();
            t.RequestMorph_SecondRanger_AfterLoadout_NoRelock();

            // Loadout submission
            t.SubmitLoadout_WithinBudget_Accepted();
            t.SubmitLoadout_ExactBudget_Accepted();
            t.SubmitLoadout_OverBudget_Rejected();
            t.SubmitLoadout_InvalidFormId_Rejected();
            t.SubmitLoadout_Empty_Rejected();
            t.SubmitLoadout_AlreadyLocked_Rejected();

            // Loadout screen data
            t.GetLoadoutScreenData_ShowsNonBaseForms();
            t.GetLoadoutScreenData_ShowsBudget();
            t.GetLoadoutScreenData_IncludesScoutingIntel();

            // Scouting
            t.Scouting_RevealType();
            t.Scouting_ObserveWithoutReveal();
            t.Scouting_RevealAlsoObserves();
            t.Scouting_UnknownEnemy_ReturnsNull();

            System.Console.WriteLine("LoadoutControllerTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
