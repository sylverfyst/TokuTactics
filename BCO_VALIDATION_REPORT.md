# BCO Pattern Validation Report

**Date**: 2026-03-31
**Validator**: Agent D
**Target**: DamageCalculator → BCO Refactor

---

## Summary

- **Bricks Validated**: 6
- **Commands Validated**: 1
- **Test Suites Validated**: 7
- **Overall Status**: ❌ **REJECTED**

**Critical Issues**: 42 compilation errors prevent code from building. Multiple BCO pattern violations and implementation errors detected.

---

## Build Status

**Compilation Result**: ❌ FAILED (42 errors)

The code does not compile and cannot be executed. Critical issues must be fixed before validation can be completed.

### Major Compilation Errors

1. **ResolveDamageRoll.cs line 56**: Incorrect argument count to `ApplyTypeMatchup.Execute()`
   - Command passes `constants` as single parameter
   - Brick expects 4 individual float parameters: `strongMultiplier, weakMultiplier, doubleStrongMultiplier, doubleWeakMultiplier`

2. **ApplyTypeMatchupTests.cs**: Type mismatch across all tests (12+ errors)
   - Tests pass `ElementalType` as second parameter
   - Brick expects `DualType attackerDualType`

3. **ApplyTypeMatchupTests.cs**: Field name mismatch
   - Tests access `result.Result` property
   - Brick returns struct with `Matchup` property (not `Result`)

4. **ResolveDamageRollTests.cs**: Incorrect command signature
   - Tests attempt to inject bricks as function parameters
   - Actual command has no such injection mechanism (uses static brick calls directly)

---

## Brick Validation

### 1. CalculateBaseDamage

**Status**: ✅ PASS

- [x] **Test 1 - One Operation**: "Calculates base damage from attacker strength vs defender defense and action power" - clean single sentence
- [x] **Test 2 - One Failure Mode**: Only direct dependents break (commands calling this brick)
- [x] **Test 3 - One Reason to Change**: Only if damage formula changes
- [x] **Test 4 - Self-Describing Signature**: `Execute(attackerStr, defenderDef, actionPower) → float` - fully self-describing
- [x] **Test 5 - Independently Testable**: Zero mocks needed (pure math)
- [x] Does NOT call other bricks
- [x] Has XML doc comments
- [x] In Index.cs
- [ ] Has corresponding test file (**exists but contains compilation errors**)

**Naming**: Uses `Calculate*` verb (valid brick verb per BCO spec)

---

### 2. ApplyTypeMatchup

**Status**: ⚠️ PARTIAL PASS (brick implementation correct, tests broken)

- [x] **Test 1 - One Operation**: "Applies type effectiveness multiplier to damage based on type chart matchup"
- [x] **Test 2 - One Failure Mode**: Only direct dependents break
- [x] **Test 3 - One Reason to Change**: Only if type matchup calculation rules change
- [x] **Test 4 - Self-Describing Signature**: Clear parameters and return type
- [x] **Test 5 - Independently Testable**: One mock (TypeChart)
- [x] Does NOT call other bricks
- [x] Has XML doc comments
- [x] In Index.cs
- [ ] Has corresponding test file (**exists but has type mismatches - tests expect `ElementalType`, brick expects `DualType`**)

**Issues**:
- Tests use wrong parameter type (`ElementalType` instead of `DualType`)
- Tests access wrong field name (`result.Result` instead of `result.Matchup`)

**Naming**: Uses `Apply*` verb (valid brick verb per BCO spec)

---

### 3. CalculateSameTypeBonus

**Status**: ✅ PASS

- [x] **Test 1 - One Operation**: "Applies same-type attack bonus (STAB) multiplier to damage"
- [x] **Test 2 - One Failure Mode**: Only direct dependents break
- [x] **Test 3 - One Reason to Change**: Only if STAB bonus formula changes
- [x] **Test 4 - Self-Describing Signature**: `Execute(damage, sameTypeBonus) → float` - perfectly clear
- [x] **Test 5 - Independently Testable**: Zero mocks (pure math)
- [x] Does NOT call other bricks
- [x] Has XML doc comments
- [x] In Index.cs
- [x] Has corresponding test file (appears correct, would compile if other errors fixed)

**Naming**: Uses `Calculate*` verb (valid brick verb per BCO spec)

---

### 4. ApplyComboScaling

**Status**: ✅ PASS

- [x] **Test 1 - One Operation**: "Applies combo multiplier to damage for chained attacks"
- [x] **Test 2 - One Failure Mode**: Only direct dependents break
- [x] **Test 3 - One Reason to Change**: Only if combo scaling formula changes
- [x] **Test 4 - Self-Describing Signature**: `Execute(damage, comboMultiplier) → float` - fully self-describing
- [x] **Test 5 - Independently Testable**: Zero mocks (pure math)
- [x] Does NOT call other bricks
- [x] Has XML doc comments
- [x] In Index.cs
- [x] Has corresponding test file (appears correct, would compile if other errors fixed)

**Naming**: Uses `Apply*` verb (valid brick verb per BCO spec)

---

### 5. RollCrit

**Status**: ✅ PASS

- [x] **Test 1 - One Operation**: "Determines if an attack is a critical hit based on attacker luck stat"
- [x] **Test 2 - One Failure Mode**: Only direct dependents break
- [x] **Test 3 - One Reason to Change**: Only if crit chance formula changes
- [x] **Test 4 - Self-Describing Signature**: `Execute(attackerLck, baseCritChance, lckCritScale, rng) → bool` - clear intent
- [x] **Test 5 - Independently Testable**: One mock (Random RNG)
- [x] Does NOT call other bricks
- [x] Has XML doc comments
- [x] In Index.cs
- [x] Has corresponding test file (appears correct, would compile if other errors fixed)

**Naming**: Uses `Roll*` verb (valid brick verb per BCO spec)

---

### 6. RollDodge

**Status**: ✅ PASS

- [x] **Test 1 - One Operation**: "Determines if an attack is dodged based on defender luck stat"
- [x] **Test 2 - One Failure Mode**: Only direct dependents break
- [x] **Test 3 - One Reason to Change**: Only if dodge chance formula changes
- [x] **Test 4 - Self-Describing Signature**: `Execute(defenderLck, baseDodgeChance, lckDodgeScale, rng) → bool` - clear intent
- [x] **Test 5 - Independently Testable**: One mock (Random RNG)
- [x] Does NOT call other bricks
- [x] Has XML doc comments
- [x] In Index.cs
- [x] Has corresponding test file (correct, includes DeterministicRandom mock)

**Naming**: Uses `Roll*` verb (valid brick verb per BCO spec)

---

## Command Validation

### ResolveDamageRoll

**Status**: ❌ FAIL

- [x] **Test 1 - One Intention**: "Resolves a complete damage roll for one attack" - clean single intention
- [x] **Test 2 - No State**: No instance variables, no mutable state (pure orchestration)
- [x] **Test 3 - Dependencies Injected**: TypeChart, Random, TunableConstants passed as parameters ✅
- [ ] **Test 4 - Testable with Mock Bricks**: ❌ **FAILS** - Command uses direct static brick calls, cannot be mocked as currently implemented
- [x] Has co-located contract file (ResolveDamageRollParams.cs)
- [x] In Index.cs (documented in comments)
- [ ] Has corresponding test file (**exists but assumes different signature with brick injection**)

**Critical Issues**:

1. **Line 56 compilation error**: Passes `constants` to `ApplyTypeMatchup.Execute()` but brick expects 4 individual float parameters
   ```csharp
   // WRONG (current code):
   var matchup = ApplyTypeMatchup.Execute(
       baseDamage, p.AttackType, p.DefenderType, p.DefenderDualType,
       typeChart, constants); // ← ERROR: missing 3 parameters

   // CORRECT (should be):
   var matchup = ApplyTypeMatchup.Execute(
       baseDamage, p.AttackType, p.DefenderType, p.DefenderDualType,
       typeChart,
       constants.StrongMultiplier,
       constants.WeakMultiplier,
       constants.DoubleStrongMultiplier,
       constants.DoubleWeakMultiplier);
   ```

2. **Testability violation**: Tests expect brick functions to be injectable:
   ```csharp
   // Test expects this signature:
   ResolveDamageRoll.Execute(params, typeChart, rng, constants,
       mockRollDodge, mockCalculateBase, ...) // ← These parameters don't exist

   // Actual signature:
   ResolveDamageRoll.Execute(params, typeChart, rng, constants)
   ```

   The command cannot be tested with mock bricks because bricks are called via static methods, not injected dependencies.

**Naming**: Uses `Resolve*` verb (valid command verb per BCO spec)

---

## Test Coverage Validation

- [x] All 6 bricks have test files
- [x] Command has test file
- [ ] ❌ Tests are runnable: **NO - 42 compilation errors**
- [ ] ❌ Tests are comprehensive: **CANNOT VERIFY** - code doesn't build

**Test Files Created**:
1. CalculateBaseDamageTests.cs ✅
2. ApplyTypeMatchupTests.cs ⚠️ (type mismatches)
3. CalculateSameTypeBonusTests.cs ✅
4. ApplyComboScalingTests.cs ✅
5. RollCritTests.cs ✅
6. RollDodgeTests.cs ✅
7. ResolveDamageRollTests.cs ❌ (signature mismatch)

---

## Prohibited Patterns Check

### ✅ Passed Checks:
- [x] No brick calls another brick (verified via grep)
- [x] No state in commands (ResolveDamageRoll is stateless)
- [x] Direct imports only through namespaces (using statements correct)

### ⚠️ Naming Checks:
- [x] All bricks use valid brick verbs: `Calculate*`, `Apply*`, `Roll*`
- [x] Command uses valid command verb: `Resolve*`

### Issues Found:
- Parameter contract follows BCO pattern (ResolveDamageRollParams is co-located) ✅
- TunableConstants is co-located with command ✅

---

## Additional Observations

### Good Practices Observed:
1. **Excellent brick atomicity**: Each brick truly does one thing
2. **Good naming**: All verbs follow BCO conventions precisely
3. **XML documentation**: Comprehensive doc comments on all units
4. **Test structure**: Tests follow consistent pattern (Arrange/Act/Assert)
5. **Contract files**: Proper use of frozen parameter contracts

### Critical Architecture Violations:

**Brick signature mismatch between Agent A and Agent B**:
- Agent A created `ApplyTypeMatchup` expecting `DualType attackerDualType`
- Agent B's command passes `p.AttackType` (an `ElementalType`)
- This suggests coordination failure between agents

**Test implementation mismatch**:
- Agent C created tests assuming dependency injection of bricks
- Agent B implemented command with static brick calls
- These approaches are incompatible

---

## Root Cause Analysis

The failures stem from **lack of coordination between agents**:

1. **Agent A** built bricks in isolation (mostly correct)
2. **Agent B** built command without verifying brick signatures (compilation error)
3. **Agent C** built tests assuming a different command architecture (dependency injection)

**BCO Pattern Compliance**: The brick *design* largely follows BCO rules, but the *integration* between layers is broken.

---

## Recommendations

### Must Fix (Blocking):

1. **Fix ResolveDamageRoll.cs line 56**:
   - Replace `constants` with individual parameters: `constants.StrongMultiplier, constants.WeakMultiplier, constants.DoubleStrongMultiplier, constants.DoubleWeakMultiplier`

2. **Fix ApplyTypeMatchup brick signature OR command usage**:
   - **Option A**: Change brick to accept `ElementalType` instead of `DualType`
   - **Option B**: Change command to convert `ElementalType` to `DualType` before calling brick
   - **Recommendation**: Option A - the brick should match the game's actual usage

3. **Fix all ApplyTypeMatchupTests**:
   - Change test parameter types from `ElementalType` to `DualType` (or update brick per recommendation above)
   - Change all `result.Result` to `result.Matchup`

4. **Rebuild ResolveDamageRollTests OR modify command**:
   - **Option A**: Remove dependency injection tests, test command as-is with real bricks
   - **Option B**: Refactor command to accept brick functions as optional parameters
   - **Recommendation**: Option A for simplicity, but Option B is more aligned with BCO Test 4

### Should Fix (BCO Compliance):

5. **Make ResolveDamageRoll testable with mock bricks**:
   - Current implementation violates BCO Command Test 4
   - Consider adding optional brick function parameters with defaults to static brick calls
   - This allows tests to inject mocks while production code uses real bricks

### Nice to Have:

6. **Add TestRunner integration**: Ensure new test suites are registered in TestRunner.cs
7. **Verify brick verb consistency**: Confirm `Roll*` vs `Calculate*` verb usage matches semantic intent

---

## Approval Decision

### ❌ **REJECTED** - Critical violations must be fixed before merge

**Blocking Issues**:
- 42 compilation errors
- Code does not build
- Tests cannot run
- Command violates BCO testability requirement

**Approval Criteria**:
1. ✅ All code compiles with zero errors
2. ✅ All tests run and pass
3. ✅ Command is testable with mock bricks (BCO Command Test 4)
4. ✅ No prohibited patterns present

**Current Status**: 1 of 4 criteria met

---

## Next Steps for Agents A, B, C

1. **Agent B** must fix ResolveDamageRoll.cs compilation error immediately
2. **Agent A** and **Agent C** must align on ApplyTypeMatchup signature (ElementalType vs DualType)
3. **Agent C** must fix test compilation errors
4. **Agent B** should refactor command for testability OR **Agent C** should rewrite tests to match current architecture
5. All agents should verify their work compiles before committing

Once these issues are resolved, re-run validation with:
```bash
cd /Users/bmetzger/code/TokuTactics/.worktrees/review-bco
dotnet build TestRunner.csproj
dotnet run --project TestRunner.csproj
```

---

**Validator Notes**: The brick design quality is quite good - atomicity, naming, and documentation are excellent. The failures are primarily integration errors (signature mismatches) and architectural misalignment (testability). These are fixable without redesigning the bricks themselves.
