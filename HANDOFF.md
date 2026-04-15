# Toku Tactics — Handoff

**Date**: 2026-04-15
**Status**: Godot integration working, PhaseManager BCO refactor complete, full player phase loop verified

## Current State

### Playable Prototype
The game runs in Godot 4.6.1. Isometric battle grid (12x10), 5 Rangers vs 7 enemies. Full round loop works:
- Player phase: 5 rangers cycle through turns by SPD (Yellow → Green → Blue → Red → Pink)
- Each ranger can move (click to select, click tile to preview, click again to confirm)
- End turn with Space/Enter
- Enemy phase: all 7 enemies auto-skip (no AI yet)
- Round transitions: cooldowns tick, status effects process, combos reset, win/loss checked
- Loop repeats indefinitely

### Build Status
- **Godot build**: 0 errors, 0 warnings
- **Test suites**: 45 passed, 0 failed
- **Total files**: 65 source + 28 test suites + BCO bricks/commands/tests

### Debug Tooling
Godot MCP bridge (`DebugBridge.cs`) runs as Autoload on `localhost:9880` in debug builds. Provides:
- `screenshot` — viewport capture (PNG)
- `godot_logs` — tails Godot's log file for GD.Print, errors, warnings
- `scene_tree` / `inspect_node` — runtime node inspection
- `input_key` / `input_mouse` / `input_action` — simulate player input
- `call_method` — invoke methods on any node
- `wait_frames` — pause between actions

MCP server config: `.mcp.json` at project root. Node.js server at `~/code/godot_mcp/index.js`.

## BCO Refactor Status

Architecture pattern documented in `~/code/BCO_PATTERN.md`. Every new unit needs: implementation + test + index entry.

### Done

**PhaseManagement** — Fully refactored
- 6 bricks: `ValidateMissionActive`, `CheckRangerDefeat`, `CheckVictoryCondition`, `ApplyEffectOutputToHealth`, `GetTargetHealthPool`, `CheckFormDeath`
- 5 commands: `ExecutePhaseTransition`, `ExecuteRoundStart`, `ResolveWinLoss`, `ProcessRoundStatusEffects`, `InitializeMission`
- PhaseManager is a thin orchestrator: owns state, calls commands, publishes events from results

### Partially Done

**CombatResolution** (569 lines) — `ResolveDamageRoll` command + 6 combat bricks extracted. `CombatResolver` itself still inlines assist resolution, damage application, death checks, gimmick processing, and event publishing.

**Movement** — `ExecuteMovement` command + 4 movement bricks (`ValidateMovementRange`, `CheckActionBudget`, `ExecuteGridMove`, `ConsumeMoveBudget`). Range calculation still lives in `BattleGrid`.

### Not Started

| System | Lines | Complexity | Notes |
|---|---|---|---|
| **CombatResolver** | 569 | High | Orchestrates full attack flow — biggest refactor target |
| **GimmickResolver** | 397 | High | Spatial translation of gimmick outputs |
| **FormPool** | 334 | Medium | Budget, cooldowns, exclusivity, loadout lock |
| **MissionContext** | 331 | Medium | Dependency graph builder — may already be an orchestrator |
| **LoadoutController** | 299 | Medium | First-morph gate, scouting intel, loadout validation |
| **AssistResolver** | 288 | Medium | Finds eligible allies, calculates assist damage |
| **SaveManager** | 273 | Low | Serialization, 10 slots, restore point |
| **BondTracker** | 157 | Low | Bond tier tracking, assist eligibility |
| **ActionBudget** | 123 | Low | Per-unit action economy (move/act/form switch) |
| **TurnOrder** | 106 | Low | Unit sequencing within a phase |

## Priority: BCO Refactor Order

Complete the BCO migration before adding new features. The investment pays off through testability, discoverability, and preventing monolithic code from growing further.

### Phase 1: High-Touch Systems
1. **CombatResolver** — Most complex, most frequently extended. Extract damage application, death processing, assist orchestration, gimmick triggering into bricks/commands.
2. **Movement** — Finish the partial refactor. Extract range calculation from BattleGrid.

### Phase 2: Medium Systems
3. **AssistResolver** — Tightly coupled with CombatResolver. Refactor alongside or immediately after.
4. **GimmickResolver** — Spatial logic (displacement, AoE) should be bricks. Resolver becomes thin.
5. **FormPool** — Cooldown management, budget enforcement, exclusivity checks are natural bricks.
6. **LoadoutController** — Validation logic, morph gate checks, loadout locking.

### Phase 3: Lower Complexity
7. **MissionContext** — Evaluate whether it's already an orchestrator. May just need index entry.
8. **BondTracker** — Small, mostly state tracking. Quick refactor.
9. **ActionBudget** — Already fairly atomic. May just need brick extraction for budget checks.
10. **TurnOrder** — Small, well-scoped. Quick refactor.
11. **SaveManager** — Serialization bricks, save/load commands.

## What's Next After BCO

### Gameplay (in priority order)
1. **Click-to-target attacks** — Select enemy to attack (replaces auto-target)
2. **Basic enemy AI** — Enemies move toward Rangers and attack (swap the `AutoSkipEnemy` processor in `ExecutePhaseTransition`)
3. **Loadout selection UI** — Pick forms within budget on first morph
4. **Form switching** — In-combat form change with cooldown
5. **Health bars** — HP display above units

### Polish
- Tween animations for movement/attacks
- Damage numbers floating above units
- Phase transition banners
- Sound effects

## How to Run

### Game
```bash
cd ~/code/TokuTactics
dotnet build TokuTactics.csproj
/Applications/Godot_mono.app/Contents/MacOS/Godot --path .
```

### Tests
```bash
dotnet run --project TestRunner.csproj
```

### MCP Debug Bridge
```bash
claude mcp add godot node ~/code/godot_mcp/index.js
```
Game must be running with DebugBridge Autoload active (`localhost:9880`).

## File Structure

```
Scripts/
├── Bricks/
│   ├── Combat/        # 6 bricks (damage, dodge, crit, type matchup, STAB, combo)
│   ├── Movement/      # 4 bricks (validate range, check budget, execute move, consume budget)
│   ├── Phase/         # 4 bricks (validate active, check defeat, check victory, apply effect)
│   └── Round/         # 2 bricks (get health pool, check form death)
├── Commands/
│   ├── Combat/        # ResolveDamageRoll, ExecuteAttack
│   ├── Movement/      # ExecuteMovement
│   └── Phase/         # ExecutePhaseTransition, ExecuteRoundStart, ResolveWinLoss,
│                      # ProcessRoundStatusEffects, InitializeMission
├── Core/              # Grid, Combat, Events, Health, Stats, StatusEffect, Types
├── Data/Content/      # ContentRegistry, Catalogs (Forms, Rangers, Enemies, Episodes, Maps)
├── Debug/             # DebugBridge.cs (Godot MCP server)
├── Entities/          # Rangers, Enemies, Forms, Weapons, Zords
└── Systems/           # Orchestrators: PhaseManager, CombatResolver, FormPool, etc.

Scenes/
├── Battle/
│   ├── BattleScene.tscn/.gd    # Main scene, camera controls, input handling
│   ├── BattleGridVisual.tscn/.gd  # Isometric tilemap, unit sprites, highlights
│   └── BattleController.cs     # C#↔GDScript bridge, orchestrates game flow

Tests/                 # 45 test suites, mirrors Scripts/ structure
```

## Key Architecture Decisions

- **BCO pattern** (`~/code/BCO_PATTERN.md`): Bricks (pure atomic ops) → Commands (composed intentions) → Orchestrators (state + events). Every unit needs implementation + test + index.
- **Events are orchestrator-only**: Commands return declarative results. Orchestrators publish events.
- **Godot MCP for verification**: After code changes, use screenshot/logs/scene_tree to self-verify instead of asking the user.
- **Public API frozen on refactor**: When converting a system to BCO, the orchestrator's public API stays identical. Callers don't change.

## Repository

**Remote**: git@github.com:sylverfyst/TokuTactics.git
**Branch**: main
**Latest commit**: `4f80889` — Fix three bugs in PhaseManager found during review
