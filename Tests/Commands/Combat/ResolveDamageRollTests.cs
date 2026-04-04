using System;
using TokuTactics.Commands.Combat;
using TokuTactics.Core.Types;

namespace TokuTactics.Tests.Commands.Combat
{
    /// <summary>
    /// Tests for ResolveDamageRoll command.
    /// Uses mocks to verify orchestration: correct bricks called in correct order.
    /// </summary>
    public static class ResolveDamageRollTests
    {
        public static void Run()
        {
            Test_DodgeEarlyReturn_NoBricksCalledAfterDodge();
            Test_NotDodged_AllBricksCalledInOrder();
            Test_NotDodged_WithSTAB_CorrectBricksUsed();
            Test_NotDodged_WithoutSTAB_STABBrickSkipped();
            Test_CorrectParametersPassed();
            Console.WriteLine("ResolveDamageRollTests: All passed");
        }

        private static void Test_DodgeEarlyReturn_NoBricksCalledAfterDodge()
        {
            // Arrange
            var tracker = new BrickCallTracker();
            var mockBricks = new MockBricks(tracker);
            mockBricks.RollDodgeMock.ReturnValue = true; // Dodge succeeds

            var ctx = CreateTestContext();

            // Act
            var result = ResolveDamageRoll.Execute(
                ctx.Params, ctx.TypeChart, ctx.Rng, ctx.Constants,
                mockBricks.RollDodge, mockBricks.CalculateBaseDamage,
                mockBricks.ApplyTypeMatchup, mockBricks.CalculateSameTypeBonus,
                mockBricks.ApplyComboScaling, mockBricks.RollCrit);

            // Assert
            Assert.IsTrue(result.WasDodged, "Result should indicate dodge");
            Assert.AreEqual(0, result.FinalDamage, "Dodged attacks deal 0 damage");
            Assert.AreEqual(1, tracker.GetCallCount("RollDodge"), "RollDodge should be called once");
            Assert.AreEqual(0, tracker.GetCallCount("CalculateBaseDamage"), "CalculateBaseDamage should not be called");
            Assert.AreEqual(0, tracker.GetCallCount("ApplyTypeMatchup"), "ApplyTypeMatchup should not be called");
            Assert.AreEqual(0, tracker.GetCallCount("RollCrit"), "RollCrit should not be called");
        }

        private static void Test_NotDodged_AllBricksCalledInOrder()
        {
            // Arrange
            var tracker = new BrickCallTracker();
            var mockBricks = new MockBricks(tracker);
            mockBricks.RollDodgeMock.ReturnValue = false; // No dodge
            mockBricks.CalculateBaseDamageMock.ReturnValue = 50f;
            mockBricks.ApplyTypeMatchupMock.ReturnValue = new TypeMatchupResult
            {
                Damage = 75f,
                Result = MatchupResult.Strong,
                Multiplier = 1.5f
            };
            mockBricks.CalculateSameTypeBonusMock.ReturnValue = 93.75f; // 75 * 1.25
            mockBricks.ApplyComboScalingMock.ReturnValue = 75f; // 93.75 * 0.8
            mockBricks.RollCritMock.ReturnValue = false;

            var ctx = CreateTestContext(hasSTAB: true, comboMultiplier: 0.8f);

            // Act
            var result = ResolveDamageRoll.Execute(
                ctx.Params, ctx.TypeChart, ctx.Rng, ctx.Constants,
                mockBricks.RollDodge, mockBricks.CalculateBaseDamage,
                mockBricks.ApplyTypeMatchup, mockBricks.CalculateSameTypeBonus,
                mockBricks.ApplyComboScaling, mockBricks.RollCrit);

            // Assert: Verify call order
            var callOrder = tracker.GetCallOrder();
            Assert.AreEqual("RollDodge", callOrder[0], "RollDodge should be called first");
            Assert.AreEqual("CalculateBaseDamage", callOrder[1], "CalculateBaseDamage should be second");
            Assert.AreEqual("ApplyTypeMatchup", callOrder[2], "ApplyTypeMatchup should be third");
            Assert.AreEqual("CalculateSameTypeBonus", callOrder[3], "CalculateSameTypeBonus should be fourth");
            Assert.AreEqual("ApplyComboScaling", callOrder[4], "ApplyComboScaling should be fifth");
            Assert.AreEqual("RollCrit", callOrder[5], "RollCrit should be last");

            // Verify result
            Assert.IsFalse(result.WasDodged, "Should not be dodged");
            Assert.IsTrue(result.FinalDamage > 0, "Should deal damage");
        }

        private static void Test_NotDodged_WithSTAB_CorrectBricksUsed()
        {
            // Arrange
            var tracker = new BrickCallTracker();
            var mockBricks = new MockBricks(tracker);
            mockBricks.RollDodgeMock.ReturnValue = false;
            mockBricks.CalculateBaseDamageMock.ReturnValue = 100f;
            mockBricks.ApplyTypeMatchupMock.ReturnValue = new TypeMatchupResult
            {
                Damage = 100f,
                Result = MatchupResult.Neutral,
                Multiplier = 1.0f
            };
            mockBricks.CalculateSameTypeBonusMock.ReturnValue = 125f;
            mockBricks.ApplyComboScalingMock.ReturnValue = 125f;
            mockBricks.RollCritMock.ReturnValue = false;

            var ctx = CreateTestContext(hasSTAB: true);

            // Act
            var result = ResolveDamageRoll.Execute(
                ctx.Params, ctx.TypeChart, ctx.Rng, ctx.Constants,
                mockBricks.RollDodge, mockBricks.CalculateBaseDamage,
                mockBricks.ApplyTypeMatchup, mockBricks.CalculateSameTypeBonus,
                mockBricks.ApplyComboScaling, mockBricks.RollCrit);

            // Assert
            Assert.AreEqual(1, tracker.GetCallCount("CalculateSameTypeBonus"),
                "CalculateSameTypeBonus should be called when STAB is present");
            Assert.IsTrue(result.HadSameTypeBonus, "Result should indicate STAB was applied");
        }

        private static void Test_NotDodged_WithoutSTAB_STABBrickSkipped()
        {
            // Arrange
            var tracker = new BrickCallTracker();
            var mockBricks = new MockBricks(tracker);
            mockBricks.RollDodgeMock.ReturnValue = false;
            mockBricks.CalculateBaseDamageMock.ReturnValue = 100f;
            mockBricks.ApplyTypeMatchupMock.ReturnValue = new TypeMatchupResult
            {
                Damage = 100f,
                Result = MatchupResult.Neutral,
                Multiplier = 1.0f
            };
            mockBricks.ApplyComboScalingMock.ReturnValue = 100f;
            mockBricks.RollCritMock.ReturnValue = false;

            var ctx = CreateTestContext(hasSTAB: false);

            // Act
            var result = ResolveDamageRoll.Execute(
                ctx.Params, ctx.TypeChart, ctx.Rng, ctx.Constants,
                mockBricks.RollDodge, mockBricks.CalculateBaseDamage,
                mockBricks.ApplyTypeMatchup, mockBricks.CalculateSameTypeBonus,
                mockBricks.ApplyComboScaling, mockBricks.RollCrit);

            // Assert
            Assert.AreEqual(0, tracker.GetCallCount("CalculateSameTypeBonus"),
                "CalculateSameTypeBonus should NOT be called when no STAB");
            Assert.IsFalse(result.HadSameTypeBonus, "Result should indicate no STAB");
        }

        private static void Test_CorrectParametersPassed()
        {
            // Arrange
            var tracker = new BrickCallTracker();
            var mockBricks = new MockBricks(tracker);
            mockBricks.RollDodgeMock.ReturnValue = false;
            mockBricks.CalculateBaseDamageMock.ReturnValue = 100f;
            mockBricks.ApplyTypeMatchupMock.ReturnValue = new TypeMatchupResult
            {
                Damage = 150f,
                Result = MatchupResult.Strong,
                Multiplier = 1.5f
            };
            mockBricks.ApplyComboScalingMock.ReturnValue = 120f; // 150 * 0.8
            mockBricks.RollCritMock.ReturnValue = true;

            var ctx = CreateTestContext(
                attackerStr: 20f,
                defenderDef: 10f,
                actionPower: 5f,
                comboMultiplier: 0.8f);

            // Act
            var result = ResolveDamageRoll.Execute(
                ctx.Params, ctx.TypeChart, ctx.Rng, ctx.Constants,
                mockBricks.RollDodge, mockBricks.CalculateBaseDamage,
                mockBricks.ApplyTypeMatchup, mockBricks.CalculateSameTypeBonus,
                mockBricks.ApplyComboScaling, mockBricks.RollCrit);

            // Assert: Verify correct parameters were passed
            var baseDamageCall = mockBricks.CalculateBaseDamageMock.LastCall;
            Assert.AreEqual(20f, baseDamageCall.AttackerStr, "Correct STR passed to CalculateBaseDamage");
            Assert.AreEqual(10f, baseDamageCall.DefenderDef, "Correct DEF passed to CalculateBaseDamage");
            Assert.AreEqual(5f, baseDamageCall.ActionPower, "Correct power passed to CalculateBaseDamage");

            var comboCall = mockBricks.ApplyComboScalingMock.LastCall;
            Assert.AreEqual(0.8f, comboCall.ComboMultiplier, "Correct combo multiplier passed");

            // Verify crit was applied
            Assert.IsTrue(result.WasCrit, "Result should indicate crit");
        }

        // === Test Infrastructure ===

        private static TestContext CreateTestContext(
            float attackerStr = 10f,
            float defenderDef = 5f,
            float actionPower = 1f,
            bool hasSTAB = false,
            float comboMultiplier = 1f)
        {
            var typeChart = new TypeChart();
            typeChart.AddStrength(ElementalType.Blaze, ElementalType.Frost);

            var attackerType = hasSTAB
                ? new DualType(ElementalType.Blaze, ElementalType.Blaze)
                : new DualType(ElementalType.Blaze, ElementalType.Frost);

            var @params = new ResolveDamageRollParams
            {
                AttackerStr = attackerStr,
                AttackerLck = 0f,
                DefenderDef = defenderDef,
                DefenderLck = 0f,
                ActionPower = actionPower,
                AttackType = attackerType.FormType,
                DefenderType = ElementalType.Normal,
                DefenderDualType = null,
                ComboMultiplier = comboMultiplier,
                HasSameTypeBonus = hasSTAB
            };

            var constants = new TunableConstants
            {
                BaseDodge = 0.02f,
                LckDodgeScale = 0.003f,
                BaseCrit = 0.05f,
                LckCritScale = 0.005f,
                CritMultiplier = 1.5f,
                SameTypeBonus = 1.25f,
                StrongMultiplier = 1.5f,
                WeakMultiplier = 0.5f,
                DoubleStrongMultiplier = 2.0f,
                DoubleWeakMultiplier = 0.25f
            };

            return new TestContext
            {
                Params = @params,
                TypeChart = typeChart,
                Rng = new Random(42),
                Constants = constants
            };
        }

        private class TestContext
        {
            public ResolveDamageRollParams Params { get; set; }
            public TypeChart TypeChart { get; set; }
            public Random Rng { get; set; }
            public TunableConstants Constants { get; set; }
        }

        // === Mock Infrastructure ===

        private class BrickCallTracker
        {
            private readonly System.Collections.Generic.List<string> _callOrder = new();
            private readonly System.Collections.Generic.Dictionary<string, int> _callCounts = new();

            public void RecordCall(string brickName)
            {
                _callOrder.Add(brickName);
                if (!_callCounts.ContainsKey(brickName))
                    _callCounts[brickName] = 0;
                _callCounts[brickName]++;
            }

            public int GetCallCount(string brickName)
            {
                return _callCounts.ContainsKey(brickName) ? _callCounts[brickName] : 0;
            }

            public System.Collections.Generic.List<string> GetCallOrder() => _callOrder;
        }

        private class MockBricks
        {
            private readonly BrickCallTracker _tracker;

            public MockRollDodge RollDodgeMock { get; }
            public MockCalculateBaseDamage CalculateBaseDamageMock { get; }
            public MockApplyTypeMatchup ApplyTypeMatchupMock { get; }
            public MockCalculateSameTypeBonus CalculateSameTypeBonusMock { get; }
            public MockApplyComboScaling ApplyComboScalingMock { get; }
            public MockRollCrit RollCritMock { get; }

            public Func<float, float, float, Random, bool> RollDodge { get; }
            public Func<float, float, float, float> CalculateBaseDamage { get; }
            public Func<float, ElementalType, ElementalType, DualType?, TypeChart, float, float, float, float, TypeMatchupResult> ApplyTypeMatchup { get; }
            public Func<float, float, float> CalculateSameTypeBonus { get; }
            public Func<float, float, float> ApplyComboScaling { get; }
            public Func<float, float, float, Random, bool> RollCrit { get; }

            public MockBricks(BrickCallTracker tracker)
            {
                _tracker = tracker;
                RollDodgeMock = new MockRollDodge(tracker);
                CalculateBaseDamageMock = new MockCalculateBaseDamage(tracker);
                ApplyTypeMatchupMock = new MockApplyTypeMatchup(tracker);
                CalculateSameTypeBonusMock = new MockCalculateSameTypeBonus(tracker);
                ApplyComboScalingMock = new MockApplyComboScaling(tracker);
                RollCritMock = new MockRollCrit(tracker);

                RollDodge = RollDodgeMock.Execute;
                CalculateBaseDamage = CalculateBaseDamageMock.Execute;
                ApplyTypeMatchup = ApplyTypeMatchupMock.Execute;
                CalculateSameTypeBonus = CalculateSameTypeBonusMock.Execute;
                ApplyComboScaling = ApplyComboScalingMock.Execute;
                RollCrit = RollCritMock.Execute;
            }
        }

        private class MockRollDodge
        {
            private readonly BrickCallTracker _tracker;
            public bool ReturnValue { get; set; }

            public MockRollDodge(BrickCallTracker tracker) => _tracker = tracker;

            public bool Execute(float defenderLck, float baseDodge, float lckScale, Random rng)
            {
                _tracker.RecordCall("RollDodge");
                return ReturnValue;
            }
        }

        private class MockCalculateBaseDamage
        {
            private readonly BrickCallTracker _tracker;
            public float ReturnValue { get; set; }
            public (float AttackerStr, float DefenderDef, float ActionPower) LastCall { get; private set; }

            public MockCalculateBaseDamage(BrickCallTracker tracker) => _tracker = tracker;

            public float Execute(float attackerStr, float defenderDef, float actionPower)
            {
                _tracker.RecordCall("CalculateBaseDamage");
                LastCall = (attackerStr, defenderDef, actionPower);
                return ReturnValue;
            }
        }

        private class MockApplyTypeMatchup
        {
            private readonly BrickCallTracker _tracker;
            public TypeMatchupResult ReturnValue { get; set; }

            public MockApplyTypeMatchup(BrickCallTracker tracker) => _tracker = tracker;

            public TypeMatchupResult Execute(
                float baseDamage, ElementalType attackType, ElementalType defenderType,
                DualType? defenderDualType, TypeChart chart,
                float strongMult, float weakMult, float doubleStrongMult, float doubleWeakMult)
            {
                _tracker.RecordCall("ApplyTypeMatchup");
                return ReturnValue;
            }
        }

        private class MockCalculateSameTypeBonus
        {
            private readonly BrickCallTracker _tracker;
            public float ReturnValue { get; set; }

            public MockCalculateSameTypeBonus(BrickCallTracker tracker) => _tracker = tracker;

            public float Execute(float damage, float stabMultiplier)
            {
                _tracker.RecordCall("CalculateSameTypeBonus");
                return ReturnValue;
            }
        }

        private class MockApplyComboScaling
        {
            private readonly BrickCallTracker _tracker;
            public float ReturnValue { get; set; }
            public (float Damage, float ComboMultiplier) LastCall { get; private set; }

            public MockApplyComboScaling(BrickCallTracker tracker) => _tracker = tracker;

            public float Execute(float damage, float comboMultiplier)
            {
                _tracker.RecordCall("ApplyComboScaling");
                LastCall = (damage, comboMultiplier);
                return ReturnValue;
            }
        }

        private class MockRollCrit
        {
            private readonly BrickCallTracker _tracker;
            public bool ReturnValue { get; set; }

            public MockRollCrit(BrickCallTracker tracker) => _tracker = tracker;

            public bool Execute(float attackerLck, float baseCrit, float lckScale, Random rng)
            {
                _tracker.RecordCall("RollCrit");
                return ReturnValue;
            }
        }
    }

    // Stub types that will be defined by Agents A & B
    public class ResolveDamageRollParams
    {
        public float AttackerStr { get; set; }
        public float AttackerLck { get; set; }
        public float DefenderDef { get; set; }
        public float DefenderLck { get; set; }
        public float ActionPower { get; set; }
        public ElementalType AttackType { get; set; }
        public ElementalType DefenderType { get; set; }
        public DualType? DefenderDualType { get; set; }
        public float ComboMultiplier { get; set; }
        public bool HasSameTypeBonus { get; set; }
    }

    public class DamageResult
    {
        public int FinalDamage { get; set; }
        public bool WasDodged { get; set; }
        public bool WasCrit { get; set; }
        public bool HadSameTypeBonus { get; set; }
        public MatchupResult Matchup { get; set; }
        public float TypeMultiplier { get; set; }
        public float ComboMultiplier { get; set; }
    }

    public class TypeMatchupResult
    {
        public float Damage { get; set; }
        public MatchupResult Result { get; set; }
        public float Multiplier { get; set; }
    }

    public class TunableConstants
    {
        public float BaseDodge { get; set; }
        public float LckDodgeScale { get; set; }
        public float BaseCrit { get; set; }
        public float LckCritScale { get; set; }
        public float CritMultiplier { get; set; }
        public float SameTypeBonus { get; set; }
        public float StrongMultiplier { get; set; }
        public float WeakMultiplier { get; set; }
        public float DoubleStrongMultiplier { get; set; }
        public float DoubleWeakMultiplier { get; set; }
    }
}
