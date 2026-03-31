# TokuTactics BCO Refactoring Plan

**Goal**: Refactor TokuTactics game logic to follow Brick/Command/Orchestrator pattern for improved testability, maintainability, and clarity.

**Method**: Multi-Agent Validation using git worktrees

---

## Phase 1: DamageCalculator → Bricks + Command Pattern

**Current**: `DamageCalculator.cs` (single class, ~150 lines, mixes calculation with RNG state)

**Target BCO Structure**:

```
Scripts/
  Bricks/
    Combat/
      calculateBaseDamage.cs          # Pure: STR vs DEF + power
      applyTypeMatchup.cs             # Pure: type chart lookup + multiplier
      calculateSameTypeBonus.cs       # Pure: check type match, apply bonus
      rollCrit.cs                     # Pure: LCK + base chance → bool
      rollDodge.cs                    # Pure: LCK + base chance → bool
      applyComboScaling.cs            # Pure: apply combo multiplier
      index.cs                        # Re-exports all combat bricks

  Commands/
    Combat/
      resolveDamageRoll.cs            # Composes all damage bricks
      resolveDamageRoll.types.cs      # DamageRollParams contract
      index.cs                        # Re-exports all combat commands

  Systems/
    CombatResolution/
      CombatResolver.cs               # Orchestrator: calls resolveDamageRoll
```

### Brick Signatures (C# Translation from BCO Pattern)

```csharp
// Bricks/Combat/calculateBaseDamage.cs
namespace TokuTactics.Bricks.Combat
{
    public static class CalculateBaseDamage
    {
        public static float Execute(float attackerStr, float defenderDef, float actionPower)
        {
            return (attackerStr / defenderDef) * actionPower;
        }
    }
}

// Bricks/Combat/rollCrit.cs
public static class RollCrit
{
    public static bool Execute(float attackerLck, float baseCritChance, float lckScale, Random rng)
    {
        float critChance = baseCritChance + (attackerLck * lckScale);
        return rng.NextDouble() < critChance;
    }
}

// Commands/Combat/resolveDamageRoll.cs
public class ResolveDamageRollParams
{
    public float AttackerStr { get; init; }
    public float AttackerLck { get; init; }
    public float DefenderDef { get; init; }
    public float DefenderLck { get; init; }
    public float ActionPower { get; init; }
    public ElementalType AttackType { get; init; }
    public ElementalType DefenderType { get; init; }
    public DualType? DefenderDualType { get; init; }
    public float ComboMultiplier { get; init; }
    public bool HasSameTypeBonus { get; init; }
}

public static class ResolveDamageRoll
{
    public static DamageResult Execute(
        ResolveDamageRollParams p,
        TypeChart typeChart,
        Random rng,
        TunableConstants constants)
    {
        var result = new DamageResult();

        // Brick: Roll dodge
        if (RollDodge.Execute(p.DefenderLck, constants.BaseDodge, constants.LckDodgeScale, rng))
        {
            result.WasDodged = true;
            return result;
        }

        // Brick: Calculate base damage
        float baseDamage = CalculateBaseDamage.Execute(p.AttackerStr, p.DefenderDef, p.ActionPower);

        // Brick: Apply type matchup
        var matchup = ApplyTypeMatchup.Execute(
            baseDamage, p.AttackType, p.DefenderType, p.DefenderDualType,
            typeChart, constants);
        result.Matchup = matchup.Result;
        result.TypeMultiplier = matchup.Multiplier;

        // Brick: Apply same-type bonus
        float damage = matchup.Damage;
        if (p.HasSameTypeBonus)
        {
            damage = CalculateSameTypeBonus.Execute(damage, constants.SameTypeBonus);
            result.HadSameTypeBonus = true;
        }

        // Brick: Apply combo scaling
        damage = ApplyComboScaling.Execute(damage, p.ComboMultiplier);
        result.ComboMultiplier = p.ComboMultiplier;

        // Brick: Roll crit
        if (RollCrit.Execute(p.AttackerLck, constants.BaseCrit, constants.LckCritScale, rng))
        {
            damage *= constants.CritMultiplier;
            result.WasCrit = true;
        }

        result.FinalDamage = (int)Math.Max(1, Math.Round(damage));
        return result;
    }
}
```

---

## Multi-Agent Validation Workflow

### Step 1: Create Parallel Worktrees

```bash
cd /Users/bmetzger/code/TokuTactics

# Agent A: Builds the Bricks
git worktree add .worktrees/bricks-combat worktree-bricks-combat

# Agent B: Builds the Command
git worktree add .worktrees/command-damage worktree-command-damage

# Agent C: Writes Tests
git worktree add .worktrees/tests-damage worktree-tests-damage

# Agent D: Reviews Against BCO Rules
git worktree add .worktrees/review-bco worktree-review-bco
```

### Step 2: Agent Tasks

**Agent A (Bricks)**:
- Create `Scripts/Bricks/Combat/` directory
- Implement all 6 damage bricks:
  - `calculateBaseDamage.cs`
  - `applyTypeMatchup.cs`
  - `calculateSameTypeBonus.cs`
  - `rollCrit.cs`
  - `rollDodge.cs`
  - `applyComboScaling.cs`
- Create `Scripts/Bricks/Combat/index.cs` (exports all)
- Each brick must pass the 5 Brick Tests from AGENT_RULES

**Agent B (Command)**:
- Create `Scripts/Commands/Combat/` directory
- Implement `resolveDamageRoll.cs`
- Create `resolveDamageRoll.types.cs` (params contract)
- Create `Scripts/Commands/Combat/index.cs`
- Command must pass the 4 Command Tests from AGENT_RULES

**Agent C (Tests)**:
- Create `Tests/Bricks/Combat/` directory
- Write unit tests for each brick (happy path + edge cases)
- Create `Tests/Commands/Combat/` directory
- Write command test that mocks bricks and verifies orchestration
- All tests must pass

**Agent D (Review)**:
- Read all Agent A/B/C outputs
- Check against BCO_PATTERN.md completion checklist:
  - [ ] Bricks pass all 5 tests
  - [ ] Command passes all 4 tests
  - [ ] No prohibited patterns
  - [ ] All units in index files
  - [ ] Co-located contract file exists
  - [ ] Tests exist and cover requirements
  - [ ] Naming follows verb conventions
- Document any violations

### Step 3: Validation Convergence

- **Overlap**: All agents refer to same BCO_PATTERN.md and AGENT_RULES.md
- **Divergence**: Each agent explores independently (different implementation approaches)
- **Convergence**: Agent D validates all work against single source of truth
- **Merge**: Once Agent D approves, merge all worktrees to main

### Step 4: Integration

**Agent E (Integration)**:
- Update `CombatResolver.cs` to call `ResolveDamageRoll` command instead of inline logic
- Remove old `DamageCalculator` class
- Run full test suite (569 tests must still pass)
- Verify no regressions

---

## Phase 2: BattleGrid Pathfinding → Bricks + Commands

**Current**: `BattleGrid.cs` lines 200-400 (Dijkstra implementation inline)

**Target**:
```
Bricks/Grid/
  getCardinalNeighbors.cs
  calculateMovementCost.cs
  isPositionWalkable.cs

Commands/Grid/
  resolveMovementRange.cs        # Dijkstra algorithm as command
  resolvePath.cs                 # A* pathfinding as command
```

Same multi-agent workflow:
- Agent A: Grid bricks
- Agent B: Pathfinding commands
- Agent C: Tests
- Agent D: BCO validation

---

## Phase 3+: Full System Refactor

Continue pattern through:
- Phase Management (advance turn, check win/loss)
- Combat Resolution (apply damage, apply status, check death)
- Form Management (switch form, apply cooldown)
- Assist Resolution (find eligible assisters, resolve assists)

---

## Why Multi-Agent Validation for TokuTactics?

1. **Speed**: 4 agents working in parallel vs 1 sequential agent = ~4x faster
2. **Quality**: Agent D validates against BCO rules before any merge
3. **Safety**: Each worktree is isolated - breaking tests doesn't block others
4. **Learning**: Agents can try different approaches, see what works best
5. **Testing**: Agent C builds test suite in parallel with implementation

---

## Expected Outcomes

**Before BCO Refactor**:
- Large monolithic classes (652 lines)
- Mixed concerns (calculation + state + coordination)
- Hard to test individual behaviors
- Difficult to extend without breaking

**After BCO Refactor**:
- Atomic bricks (5-20 lines each)
- Single-responsibility units
- 100% test coverage of pure logic
- Commands compose bricks declaratively
- Systems coordinate commands
- Clear extension points via co-located contracts

**Cost**: More files, more structure
**Benefit**: Clarity, testability, maintainability, AI collaboration efficiency
