# Toku Tactics - Godot Integration Handoff

**Date**: 2026-03-31
**Status**: Presentation layer complete, builds successfully, ready for first playtest

## What's Complete

### ✅ Game Logic Layer (93 files)
- **65 source files** + **28 test suites** = **569 passing tests**
- All pure C#, no Godot dependencies
- Complete vertical slice: Rangers, Enemies, Forms, Combat, Phase Management, Bond system
- See `CLAUDE.md` for full architecture documentation

### ✅ Godot Project Setup
- **Godot 4.6.1** with .NET SDK
- **TokuTactics.csproj** - Excludes test files from Godot build
- **Main scene**: `BattleScene.tscn`
- **Build status**: ✅ 0 errors, 0 warnings

### ✅ Presentation Layer (Isometric)
Created 4 presentation files in `Presentation/` directory:

1. **BattleController.cs** (`Presentation/Battle/BattleController.cs`)
   - Main coordinator - owns `MissionContext`
   - Subscribes to game events (turn start/end, phase changes, win/loss)
   - Handles player input via ActionMenu
   - Executes combat via `CombatResolver`
   - **Lines**: 280

2. **GridView.cs** (`Presentation/Battle/GridView.cs`)
   - **Isometric projection** - diamond-shaped tiles
   - Tile dimensions: 64×32 pixels (2:1 ratio)
   - Formula: `x = (col - row) * 32`, `y = (col + row) * 16`
   - Renders Rangers as colored diamonds (R/B/Y/G/P)
   - Renders Enemies as gray diamonds (E)
   - Handles highlighting for current unit & targets
   - **Lines**: 290

3. **TurnDisplay.cs** (`Presentation/UI/TurnDisplay.cs`)
   - Shows current phase (Player Phase / Enemy Phase / Idle)
   - Shows active unit name
   - Victory/Defeat overlays
   - **Lines**: 80

4. **ActionMenu.cs** (`Presentation/UI/ActionMenu.cs`)
   - Bottom-right action menu
   - Dynamic buttons: "Basic Attack", "Weapon Attack", "End Turn"
   - Calls `BattleController.OnActionSelected(action)`
   - **Lines**: 75

### ✅ Scene Structure
**BattleScene.tscn** hierarchy:
```
BattleScene (Node)
  └─ BattleController (Node) [BattleController.cs]
      ├─ GridView (Node2D) [GridView.cs]
      └─ UI (Control)
          ├─ TurnDisplay (Control) [TurnDisplay.cs]
          └─ ActionMenu (Control) [ActionMenu.cs]
```

## API Fixes Applied

All presentation layer code now correctly uses the game logic APIs:

- **MissionContext**: Uses `RangerLookup`/`EnemyLookup` dictionaries (not `Rangers`/`Enemies` directly)
- **PhaseManager**: Uses `PhaseState` enum property (not `CurrentPhase` string)
- **PhaseManager**: Uses `ActiveUnit` property (not `GetCurrentUnit()` method)
- **EventBus.Subscribe**: Includes `EventPriority.Presentation` parameter
- **FormData**: Uses `WeaponA`/`WeaponB` properties (not `Weapons` collection)
- **WeaponData**: Uses `BasePower` property (not `AttackPower`)
- **CombatResolver**: Passes `WeaponData.StatusEffect` (StatusEffectTemplate), not whole WeaponData

## How to Run

### In Godot Editor
1. Open Godot 4.6.1 (.NET build)
2. Open project: `/Users/bmetzger/code/TokuTactics/project.godot`
3. Press **F5** (or click Play button) to run BattleScene

### Expected Behavior
- **Isometric grid** renders with 120 diamond tiles (12×10 map)
- **5 Rangers** on left side (colored: Red, Blue, Yellow, Green, Pink)
- **4 Enemies** on right side (gray diamonds)
- **Turn Display** (top-left): "Phase: Player Phase" + active unit
- **Action Menu** (bottom-right): 3 buttons when Ranger's turn

### Game Flow
1. **Round Start** → Tick cooldowns, status effects
2. **Player Phase** → Rangers act in SPD order
   - Click "Basic Attack" → auto-attacks first alive enemy
   - Click "Weapon Attack" → uses form's WeaponA if available
   - Click "End Turn" → advances to next Ranger
3. **Enemy Phase** → Enemies auto-skip (AI placeholder logs "Skipping enemy...")
4. **Repeat** until victory (all enemies dead) or defeat (Ranger dies unmorphed)

## Current Limitations (Intentional)

These are **vertical slice gaps** documented in `CLAUDE.md`:

1. **No player targeting** - attacks always hit first alive enemy
2. **No movement system** - units stay in spawn positions
3. **No AI execution** - enemies skip their turns
4. **No loadout selection UI** - Rangers use base form only
5. **No form switching UI** - morphing system not exposed yet
6. **Single phase only** - processes Ground phase, no Mecha phase transition
7. **No animations** - instant combat resolution
8. **No sound/music** - pure logic + basic visuals

## Console Output to Verify

When you run the scene, you should see:
```
=== Battle Controller Starting ===
Initializing mission...
✓ Mission initialized: Frozen Outpost
  - Grid: 12x10
  - Rangers: 5
  - Enemies: 4
=== Starting Battle ===
Created 120 isometric tiles
Created 9 unit views
Turn started: ranger_red
Phase changed: Idle → Player Phase
```

Click "Basic Attack":
```
Action selected: Basic Attack
Entering targeting mode (weapon: False)
Executing attack: ranger_red → enemy_putty_1 (weapon: False)
  → Damage: [number], Target died: [true/false]
```

## What's Next

### Immediate (for playable vertical slice):
1. **Click-to-target** - Replace auto-target with grid cell click detection
2. **Movement UI** - Show movement range, click to move
3. **Loadout selection** - UI to pick forms within budget on first morph
4. **Form switching** - Button to change forms (triggers cooldown)
5. **Basic AI** - Enemies move toward Rangers and attack

### Polish:
- Tween animations for movement/attacks
- Damage numbers floating above units
- Health bars above units
- Phase transition banners
- Sound effects for attacks/hits/phase changes

### Future Features (post-vertical slice):
- Behavior tree AI execution
- Bond tier assist visual effects
- Status effect icons above units
- Mecha phase (grid transformation)
- Victory screen with rewards
- Save/load between episodes

## Project Files

### Godot Project
- **project.godot** - Main scene: `res://BattleScene.tscn`
- **TokuTactics.csproj** - .NET 8.0, Godot.NET.Sdk/4.6.1
- **BattleScene.tscn** - Main battle scene

### Documentation
- **CLAUDE.md** - Full architecture, world bible, content authoring guide
- **toku-tactics-world-bible.md** - Story, setting, characters, terminology
- **HANDOFF.md** - This document

### Code Structure
```
Scripts/
  ├── Core/           # Grid, Combat, Events, Health, Stats, Types
  ├── Data/Content/   # All content catalogs (Forms, Rangers, Enemies, Episodes)
  ├── Entities/       # Rangers, Enemies, Forms, Weapons, Zords
  └── Systems/        # Combat, Phase, AI, Assist, Gimmick, FormPool, LoadoutController

Presentation/
  ├── Battle/         # BattleController, GridView
  └── UI/             # TurnDisplay, ActionMenu

Tests/
  └── [28 test suites, 569 tests - all passing]
```

## Known Issues

**None** - Project builds cleanly, all tests pass

## Commands

### Build
```bash
cd /Users/bmetzger/code/TokuTactics
dotnet build TokuTactics.csproj
```

### Run Tests
```bash
dotnet run --project TestRunner.csproj
```

### Open in Godot
```bash
/Applications/Godot.app/Contents/MacOS/Godot --path . --editor
```

## Notes

- All 569 tests still pass after Godot integration
- Game logic layer is **completely decoupled** from Godot
- Presentation layer is ~725 lines total (very lean)
- Isometric projection works correctly (tested with checkerboard pattern)
- MissionContext.Create() builds entire dependency graph in one call
- Episode data: "Frozen Outpost" (12×10 map, 5 Rangers, 4 Enemies)

---

**Ready for first playtest** - Just press F5 in Godot and click buttons to see combat loop
