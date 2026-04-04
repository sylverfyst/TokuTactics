# BCO Pattern Validation Report - Iteration 2

**Date**: 2026-04-04
**Validator**: Agent D (Ralph Loop)
**Status**: ITERATION 2 RE-VALIDATION

---

## Ralph Loop Status: CONTINUE - Significant Issues Remain

### Build Status
- **Compilation**: ❌ FAIL
- **Test Execution**: ❌ BLOCKED (cannot run, code doesn't compile)
- **Error Count**: 42 compilation errors (unchanged from iteration 1)

---

## Changes Since Iteration 1

### Agent A (Bricks) - Commit 1427089
**Status**: ⚠️ PARTIAL FIX - Signature fixed, but implementation broken

**Changes Made**:
- Changed `ApplyTypeMatchup.Execute()` signature from 9 parameters to 6 parameters ✅
- Changed attack type parameter from `DualType` to `ElementalType` ✅
- Replaced 4 individual multiplier params with single `TunableConstants` object ✅
- Updated XML documentation ✅

**NEW Critical Error Introduced**:
```
ApplyTypeMatchup.cs(52,45): error CS1503:
Argument 1: cannot convert from 'TokuTactics.Core.Types.ElementalType' to 'TokuTactics.Core.Types.DualType'
```

**Root Cause**: Line 52 calls `typeChart.Resolve(attackType, defenderType)` where both parameters are `ElementalType`, but `TypeChart.Resolve()` requires signature `(DualType, ElementalType)`.

**What Broke**: The fix changed the parameter from `DualType` to `ElementalType` but failed to update the internal logic to handle single-type vs dual-type attackers. The TypeChart API does not have a `Resolve(ElementalType, ElementalType)` overload.

**Available TypeChart Methods**:
- `Resolve(DualType attacker, ElementalType defender)` - for dual-type attackers
- `ResolveDefensive(ElementalType attacker, DualType defender)` - for dual-type defenders
- **MISSING**: `Resolve(ElementalType, ElementalType)` - no such method exists

**Impact**: The brick is now calling a non-existent method overload. This breaks the entire build.

### Agent B (Command) - No Changes
**Status**: ⏸️ NO ACTION TAKEN

No commits detected from Agent B. The command code remains unchanged.

**Command Status**:
- ResolveDamageRoll.cs line 50-56 correctly calls `ApplyTypeMatchup.Execute()` with 6 parameters (matching Agent A's new signature) ✅
- However, this was already correct in iteration 1 - Agent A changed the brick to match the command
- Command still has no brick injection for testability ❌

### Agent C (Tests) - No Changes
**Status**: ⏸️ NO ACTION TAKEN

No commits detected from Agent C. Test code remains unchanged from iteration 1.

**Test Status**:
- ApplyTypeMatchupTests still passes 9 arguments (expects old signature) ❌
- ApplyTypeMatchupTests still accesses `.Result` property instead of `.Matchup` ❌
- ResolveDamageRollTests still expects 10-parameter signature with brick injection ❌
- ResolveDamageRollTests still uses undefined `Assert` helper ❌
- ResolveDamageRollTests still accesses `.WasCrit` property instead of `.WasCritical` ❌

---

## Current Error Breakdown

### Category 1: ApplyTypeMatchup Brick (1 error, BLOCKING ALL)
**File**: `Scripts/Bricks/Combat/ApplyTypeMatchup.cs:52`

```csharp
// Line 52 - BROKEN CODE:
matchup = typeChart.Resolve(attackType, defenderType);
//                          ^^^^^^^^^^  ^^^^^^^^^^^^
//                          ElementalType  ElementalType
//
// ERROR: TypeChart.Resolve expects (DualType, ElementalType)
// but receives (ElementalType, ElementalType)
```

**Error**: `error CS1503: Argument 1: cannot convert from 'ElementalType' to 'DualType'`

**Why This Blocks Everything**: This brick is used by the command, which is used by all systems. Until this compiles, nothing else can be validated.

### Category 2: ApplyTypeMatchupTests (12 errors)
**File**: `Tests/Bricks/Combat/ApplyTypeMatchupTests.cs`

**Error Type 1** (6 occurrences): Wrong parameter count
```csharp
// Tests still expect old 9-parameter signature:
ApplyTypeMatchup.Execute(
    100f,                      // baseDamage ✅
    ElementalType.Blaze,       // attackType (WRONG: test passes this but brick now expects DualType... wait no, brick expects ElementalType now)
    ElementalType.Frost,       // defenderType ✅
    null,                      // defenderDualType ✅
    typeChart,                 // typeChart ✅
    2.0f,                      // strongMultiplier (REMOVED: brick now uses constants)
    0.5f,                      // weakMultiplier (REMOVED)
    2.5f,                      // doubleStrongMultiplier (REMOVED)
    0.25f);                    // doubleWeakMultiplier (REMOVED)
```

**Error**: `error CS1501: No overload for method 'Execute' takes 9 arguments`

**Fix Required**: Remove last 4 float parameters, add `TunableConstants` parameter

**Error Type 2** (6 occurrences): Wrong property name
```csharp
// Tests access wrong property:
Assert.AreEqual(MatchupResult.Strong, result.Result);
//                                            ^^^^^^ WRONG

// Brick returns:
public struct Result {
    public float Damage;
    public MatchupResult Matchup;  // ← Correct property name
    public float Multiplier;
}
```

**Error**: `error CS1061: 'ApplyTypeMatchup.Result' does not contain a definition for 'Result'`

**Fix Required**: Change `result.Result` to `result.Matchup`

### Category 3: ResolveDamageRollTests (29 errors)
**File**: `Tests/Commands/Combat/ResolveDamageRollTests.cs`

**Error Type 1** (4 occurrences): Wrong parameter count
```csharp
// Tests expect brick injection:
var result = ResolveDamageRoll.Execute(
    p, typeChart, rng, constants,
    mockRollDodge,        // ← These 6 parameters don't exist
    mockCalculateBase,
    mockApplyMatchup,
    mockCalcSTAB,
    mockApplyCombo,
    mockRollCrit);

// Actual signature:
public static DamageResult Execute(
    ResolveDamageRollParams p,
    TypeChart typeChart,
    Random rng,
    TunableConstants constants)  // ← Only 4 parameters
```

**Error**: `error CS1501: No overload for method 'Execute' takes 10 arguments`

**Fix Required**: Either (A) remove brick injection from tests, or (B) add optional brick function parameters to command

**Error Type 2** (24 occurrences): Undefined Assert helper
```csharp
Assert.AreEqual(expected, actual);
// ^^^^^^ ERROR: 'Assert' does not exist in the current context
```

**Error**: `error CS0103: The name 'Assert' does not exist in the current context`

**Fix Required**: Agent C needs to define the `Assert` helper class (simple throw-on-mismatch)

**Error Type 3** (1 occurrence): Wrong property name
```csharp
Assert.IsTrue(result.WasCrit);
//                   ^^^^^^^ WRONG

// DamageResult has:
public bool WasCritical;  // ← Correct property name
```

**Error**: `error CS1061: 'DamageResult' does not contain a definition for 'WasCrit'`

**Fix Required**: Change `WasCrit` to `WasCritical`

---

## BCO Compliance Re-Validation

### Brick Checklist (6 bricks)

| Brick | Test 1 | Test 2 | Test 3 | Test 4 | Test 5 | Status |
|-------|--------|--------|--------|--------|--------|--------|
| CalculateBaseDamage | ✅ | ✅ | ✅ | ✅ | ✅ | PASS |
| ApplyTypeMatchup | ✅ | ✅ | ✅ | ⚠️ | ❌ | **FAIL** |
| CalculateSameTypeBonus | ✅ | ✅ | ✅ | ✅ | ✅ | PASS |
| ApplyComboScaling | ✅ | ✅ | ✅ | ✅ | ✅ | PASS |
| RollCrit | ✅ | ✅ | ✅ | ✅ | ✅ | PASS |
| RollDodge | ✅ | ✅ | ✅ | ✅ | ✅ | PASS |

**ApplyTypeMatchup Failures**:
- ⚠️ **Test 4 - Self-Describing Signature**: Signature is now correct (accepts `TunableConstants`), but implementation is broken
- ❌ **Test 5 - Independently Testable**: Brick calls non-existent `TypeChart.Resolve(ElementalType, ElementalType)` method, making it impossible to test or even compile

**Critical Issue**: The signature fix was correct in intent but broke the implementation. The brick must handle three cases:
1. Single-type attacker → Single-type defender (enemy vs enemy) - **BROKEN, no TypeChart method exists**
2. Single-type attacker → Dual-type defender (enemy vs Ranger) - ✅ works via `ResolveDefensive()`
3. Dual-type attacker → Single-type defender (Ranger vs enemy) - **BROKEN, can't accept DualType anymore**

**Diagnosis**: The game needs to support all three cases, but:
- The current signature `ElementalType attackType` can't represent dual-type Rangers attacking
- The TypeChart API doesn't support single-type vs single-type lookups
- This is a **fundamental architecture mismatch**

**Recommended Fix**:
- Keep brick signature with `ElementalType attackType` for most calls
- Add logic to construct a temporary `DualType` when needed, OR
- Modify TypeChart to add `Resolve(ElementalType, ElementalType)` overload

### Command Checklist

| Test | Status | Notes |
|------|--------|-------|
| One Intention | ✅ | Resolves complete damage roll (single intention) |
| No State | ✅ | Pure orchestration, no instance state |
| Dependencies Injected | ✅ | TypeChart, Random, TunableConstants injected |
| **Testable with Mock Bricks** | ❌ | **FAIL** - still uses direct static brick calls |

**ResolveDamageRoll Status**: ⚠️ UNCHANGED

The command wasn't modified in iteration 1. It still fails BCO Test 4 (testability).

**Current Architecture**:
```csharp
// Inside ResolveDamageRoll.Execute():
if (RollDodge.Execute(...)) { ... }           // Direct static call
float baseDamage = CalculateBaseDamage.Execute(...);  // Direct static call
var matchup = ApplyTypeMatchup.Execute(...);  // Direct static call
```

**BCO Violation**: Cannot inject mock bricks for testing the command's orchestration logic.

**Agent C's Tests Assume**: Brick injection via optional function parameters (which don't exist)

**Decision Required**:
- **Option A**: Agent B adds optional brick function parameters (more testable, matches BCO Test 4)
- **Option B**: Agent C rewrites tests to use real bricks (simpler, but less isolated unit testing)

### Test Checklist

| Test Suite | Compiles | Runs | Passes | Notes |
|------------|----------|------|--------|-------|
| CalculateBaseDamageTests | ✅ | ⏸️ | ⏸️ | Would work if other errors fixed |
| ApplyTypeMatchupTests | ❌ | ❌ | ❌ | 12 errors (signature + property) |
| CalculateSameTypeBonusTests | ✅ | ⏸️ | ⏸️ | Would work if other errors fixed |
| ApplyComboScalingTests | ✅ | ⏸️ | ⏸️ | Would work if other errors fixed |
| RollCritTests | ✅ | ⏸️ | ⏸️ | Would work if other errors fixed |
| RollDodgeTests | ✅ | ⏸️ | ⏸️ | Has mock RNG, looks correct |
| ResolveDamageRollTests | ❌ | ❌ | ❌ | 29 errors (signature + Assert + property) |

---

## Prohibited Patterns Check

### ✅ Still Passing:
- No brick calls another brick (verified)
- No state in commands (ResolveDamageRoll is stateless)
- Direct imports via namespaces only

### ⚠️ New Issue:
- **ApplyTypeMatchup now imports Commands namespace**: `using TokuTactics.Commands.Combat;`
  - This is to access `TunableConstants`
  - **Potential violation**: Bricks should not depend on Commands layer
  - **Recommendation**: Move `TunableConstants` to `Core/` or `Bricks/` namespace

---

## Root Cause Analysis - Iteration 1 Outcome

### What Went Wrong:

1. **Agent A made partial fix**: Changed signature correctly but broke implementation
   - Fixed parameter count mismatch ✅
   - Fixed type mismatch for command usage ✅
   - **Failed to update internal logic** ❌
   - Introduced new error: calls non-existent `TypeChart.Resolve(ElementalType, ElementalType)`

2. **Agent B took no action**: Assumed Agent A would fix everything
   - Command was already calling ApplyTypeMatchup correctly
   - No brick injection added for testability

3. **Agent C took no action**: Assumed Agent A and B would fix their code first
   - Tests still use iteration 0 signatures
   - Tests still expect brick injection that doesn't exist

### Coordination Failure:

**The Ralph Loop depends on all agents acting in parallel**. In iteration 1:
- Only 1 of 3 agents took action
- That agent's fix was incomplete
- No inter-agent verification occurred

---

## Remaining Issues for Iteration 2

### Must Fix (Blocking):

1. **ApplyTypeMatchup.cs line 52** - CRITICAL, BLOCKS ALL BUILDS
   ```csharp
   // CURRENT (BROKEN):
   matchup = typeChart.Resolve(attackType, defenderType);

   // OPTION A - Add helper method to wrap single ElementalType in DualType:
   var attackerDual = new DualType(attackType, attackType); // Same type in both slots
   matchup = typeChart.Resolve(attackerDual, defenderType);

   // OPTION B - Add TypeChart.Resolve(ElementalType, ElementalType) overload in Core layer
   // (More invasive, requires modifying TypeChart)
   ```

2. **ApplyTypeMatchupTests** - Update all 6 tests (24 lines):
   - Remove 4 multiplier parameters, add `TunableConstants` parameter
   - Change `result.Result` to `result.Matchup`

3. **ResolveDamageRollTests** - Rewrite tests:
   - **If Agent B adds brick injection**: Update test signatures to match
   - **If Agent B refuses brick injection**: Remove mock brick calls, test with real bricks
   - Define `Assert` helper class (simple equality checker that throws on mismatch)
   - Change `WasCrit` to `WasCritical`

### Should Fix (BCO Compliance):

4. **ResolveDamageRoll testability** - Command Test 4 violation:
   - Add optional brick function parameters for testability
   - Example signature:
   ```csharp
   public static DamageResult Execute(
       ResolveDamageRollParams p,
       TypeChart typeChart,
       Random rng,
       TunableConstants constants,
       Func<int, float, float, Random, bool>? rollDodge = null,
       Func<int, int, int, float>? calculateBaseDamage = null,
       ...)
   {
       rollDodge ??= (lck, base, scale, r) => RollDodge.Execute(lck, base, scale, r);
       // Use injected function or default to real brick
   ```

5. **Move TunableConstants** - Architectural layering violation:
   - Move `TunableConstants` from `Commands.Combat` to `Core.Combat` or `Bricks.Combat`
   - Remove `using TokuTactics.Commands.Combat;` from ApplyTypeMatchup.cs
   - Update all references

### Nice to Have:

6. **TestRunner integration**: Verify new test suites are registered and called
7. **Add integration test**: Test full ResolveDamageRoll with real bricks to verify orchestration

---

## Decision: CONTINUE LOOP

### ❌ Ralph Loop Iteration 2: FAILED

**Criteria**:
- ✅ Build succeeds: **NO (42 errors, same as iteration 1)**
- ✅ Tests pass: **NO (cannot run, code doesn't compile)**
- ✅ BCO compliance: **NO (brick broken, command not testable)**
- ✅ No prohibited patterns: **⚠️ (brick imports command namespace)**

**Status**: **0 of 4 criteria met** (unchanged from iteration 1)

### Why Iteration 1 Failed:

1. **Incomplete fix**: Agent A fixed signature but broke implementation
2. **No coordination**: Agents B and C didn't act, assumed A would fix everything
3. **No verification**: Agent A didn't build/test before committing

### Specific Actions Required for Iteration 2:

**Agent A (Bricks)**:
- [ ] Fix ApplyTypeMatchup.cs line 52 to handle ElementalType × ElementalType case
- [ ] Test with: `dotnet build TestRunner.csproj` (must show <42 errors)
- [ ] Verify brick works in isolation (write quick test if needed)

**Agent B (Command)**:
- [ ] Decide on testability approach (brick injection vs integration tests)
- [ ] If adding injection: Implement optional brick function parameters
- [ ] Coordinate with Agent C on expected test signature

**Agent C (Tests)**:
- [ ] Fix ApplyTypeMatchupTests (update to 6-param signature, fix property access)
- [ ] Define `Assert` helper class
- [ ] Fix ResolveDamageRollTests based on Agent B's decision:
  - If injection added: Update signatures to match
  - If no injection: Rewrite as integration tests with real bricks
- [ ] Test with: `dotnet build TestRunner.csproj` (must show 0 errors)

### Success Criteria for Iteration 2:

When all agents complete their fixes:
```bash
cd /Users/bmetzger/code/TokuTactics/.worktrees/review-bco
dotnet build TestRunner.csproj
# Must show: Build succeeded. 0 Error(s)

dotnet run --project TestRunner.csproj
# Must show: All tests pass
```

Then Agent D will re-validate and approve if:
- ✅ Build succeeds (0 errors)
- ✅ All tests pass
- ✅ All 6 bricks pass BCO design tests
- ✅ Command passes BCO design tests (including testability)
- ✅ No prohibited patterns

---

## Validator Notes

**Iteration 1 Attempt Was**: Partially correct direction, but incomplete execution.

**Agent A's Fix Quality**:
- ✅ Correctly identified the signature mismatch
- ✅ Correctly changed to `TunableConstants`
- ✅ Correctly changed to `ElementalType`
- ❌ Failed to update internal implementation to match
- ❌ Introduced new compilation error
- ❌ Did not verify the fix compiled before committing

**Lesson for Ralph Loop**: All agents must act, and each must verify their changes compile before committing. Agent D cannot validate a broken build.

**Recommended Fix Priority**:
1. Agent A: Fix line 52 (unblocks everything else)
2. Agent C: Fix test signatures and helpers
3. Agent B: Add testability or coordinate with C on integration test approach

**Time to Resolution**: If all agents act in iteration 2, this should reach approval state. The remaining issues are well-defined and mechanical.

---

**End of Iteration 2 Validation Report**
