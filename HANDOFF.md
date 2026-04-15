# Toku Tactics — Handoff

**Date**: 2026-04-15
**Status**: BCO migration complete, ready for gameplay features

## Current State

### Playable Prototype
The game runs in Godot 4.6.1. Isometric battle grid (12x10), 5 Rangers vs 7 enemies. Full round loop works:
- Player phase: 5 rangers cycle through turns by SPD (Yellow → Green → Blue → Red → Pink)
- Each ranger can move (click to select, click tile to preview, click again to confirm)
- End turn with Space/Enter
- Enemy phase: all 7 enemies auto-skip (no AI yet)
- Round transitions: cooldowns tick, status effects process, combos reset, win/loss checked
- **No attacks yet** — click-to-target is the next feature

### Build Status
- **Godot build**: 0 errors, 0 warnings
- **Test suites**: 88 passed, 0 failed
- **BCO compliance**: Full — all rules pass, zero dependency violations

### Debug Tooling
Godot MCP bridge (`DebugBridge.cs`) runs as Autoload on `localhost:9880` in debug builds. Provides:
screenshot, godot_logs, scene_tree, inspect_node, input_key, input_mouse, input_action, call_method, wait_frames.

## BCO Status: Complete

All 10 systems fully BCO-compliant. 45 bricks, 16 commands, 88 test suites.

**Architecture layers:**
```
Types (Core/)     → Pure data shapes, shared contracts
Bricks            → Pure atomic operations on types
Commands          → Compose bricks, dependencies injected
Orchestrators     → Own state, call commands, publish events
```

**Brick domains:** Assist (4), Bond (2), Combat (14), Form (3), Loadout (2), Movement (4), Phase (3), Shared (10), Spatial (3)

**Command domains:** Assist (1), Combat (6), Form (1), Gimmick (1), Loadout (1), Movement (1), Phase (5)

**Types in Core/:** ActionEconomy (ActionBudget, BondState, BondTierChange, TurnEntry, ITurnParticipant), Assist (AssistCandidateState, AssistEffect, AssistResolution), Phase (MissionState, PhaseState), Form (FormAvailability, FormPoolEntry)

## What's Next: Gameplay Features

### Priority 1: Click-to-Target Attacks
**Current state:** Rangers can move but cannot attack. The combat system (CombatResolver, ExecuteAttack command) is fully built and tested but not wired to player input.

**What's needed:**
- After selecting the active ranger and optionally moving, player clicks an enemy to attack
- Show attack range overlay (weapon range from current position)
- Click enemy in range → execute attack via ExecuteAttack command → CombatResolver
- Update UI with damage results, handle death
- Consume action budget, end turn if no actions remain

### Priority 2: Basic Enemy AI
- Swap `AutoSkipEnemy` in `ExecutePhaseTransition` with real logic
- Enemies move toward nearest ranger and attack if in range
- Use existing `FindUnitsInRange` (Bricks/Spatial) for targeting

### Priority 3: Loadout Selection UI
### Priority 4: Form Switching
### Priority 5: Health Bars

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

## File Structure

```
Scripts/
├── Bricks/            # 45 bricks across 9 domains
│   ├── Assist/        # 4 — eligibility, damage multiplier, tier 2/4
│   ├── Bond/          # 2 — scaled XP, tier resolution
│   ├── Combat/        # 14 — damage, dodge, crit, type, status, budget
│   ├── Form/          # 3 — availability, equip validation, cooldown regen
│   ├── Loadout/       # 2 — morph validation, loadout validation
│   ├── Movement/      # 4 — range, budget, grid move, consume
│   ├── Phase/         # 3 — defeat check, victory check, turn order build
│   ├── Shared/        # 10 — mission active, health, form death, budget ops
│   └── Spatial/       # 3 — units in range, displacement, spawn positions
├── Commands/          # 16 commands across 7 domains
│   ├── Assist/        # ResolveAssistEffect
│   ├── Combat/        # ResolveDamageRoll, ExecuteAttack, ApplyWeaponStatus,
│   │                  # ResolveTargetDeath, ResolveReactiveGimmick, ProcessAssist
│   ├── Form/          # ProcessFormPoolTurn
│   ├── Gimmick/       # ResolveGimmickEffects + GimmickResolution types
│   ├── Loadout/       # ExecuteLoadoutSubmission + MorphRequestResult/LoadoutResult
│   ├── Movement/      # ExecuteMovement
│   └── Phase/         # ExecutePhaseTransition, ExecuteRoundStart, ResolveWinLoss,
│                      # ProcessRoundStatusEffects, InitializeMission
├── Core/              # Shared types + engine-independent foundations
│   ├── ActionEconomy/ # ActionBudget, BondState, BondTierChange, TurnEntry
│   ├── Assist/        # AssistCandidateState, AssistEffect, AssistResolution
│   ├── Combat/        # DamageCalculator, ComboScaler, ICombatActor/Target
│   ├── Cooldown/      # CooldownTimer
│   ├── Events/        # EventBus, GameEvents
│   ├── Form/          # FormAvailability, FormPoolEntry
│   ├── Grid/          # BattleGrid, Tile, TerrainConfig, GridPosition
│   ├── Health/        # HealthPool, IHealthPool
│   ├── Phase/         # MissionState, PhaseState
│   ├── Stats/         # StatBlock, StatType
│   ├── StatusEffect/  # StatusEffectTracker, StatusEffectInstance
│   └── Types/         # ElementalType, DualType, TypeChart
├── Data/Content/      # ContentRegistry, Catalogs
├── Debug/             # DebugBridge.cs (Godot MCP)
├── Entities/          # Rangers, Enemies, Forms, Weapons, Zords
└── Systems/           # Orchestrators (thin shells calling commands)

Scenes/Battle/         # BattleScene, BattleGridVisual, BattleController
Tests/                 # 88 test suites mirroring Scripts/
```

## Key Rules

- **BCO pattern** (`~/code/BCO_PATTERN.md`): Types → Bricks → Commands → Orchestrators
- **Every new unit needs:** implementation + test + index entry
- **Check all indexes before creating new bricks** — avoid duplication
- **Never bypass bricks** — always use brick Execute() to mutate types
- **Events are orchestrator-only** — commands return declarative results
- **Godot MCP for verification** — screenshot/logs/scene_tree after changes

## Repository

**Remote**: git@github.com:sylverfyst/TokuTactics.git
**Branch**: main
**Latest commit**: `6b2a054` — Phase 3 BCO complete
