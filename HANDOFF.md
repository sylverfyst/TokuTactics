# Toku Tactics - Godot Integration Handoff

**Date**: 2026-04-04
**Status**: Isometric tilemap implemented, BCO refactor complete, ready for C# integration

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

### ✅ BCO Refactor (NEW - 2026-04-04)
Successfully refactored DamageCalculator into Brick/Command/Orchestrator pattern:

**Bricks** (`Scripts/Bricks/Combat/`):
- `CalculateBaseDamage.cs` - Pure STR vs DEF calculation
- `RollDodge.cs` - LCK-based dodge chance
- `RollCrit.cs` - LCK-based crit chance
- `ApplyTypeMatchup.cs` - Type effectiveness multipliers
- `CalculateSameTypeBonus.cs` - STAB bonus
- `ApplyComboScaling.cs` - Combo multiplier

**Command** (`Scripts/Commands/Combat/`):
- `ResolveDamageRoll.cs` - Orchestrates all bricks with dependency injection
- `ResolveDamageRollParams.cs` - Parameter contract

**Integration**:
- CombatResolver now uses ResolveDamageRoll command
- MissionContext updated to construct dependencies
- **Test Results**: 34 test suites pass (569 tests + 6 new BCO tests)
- Zero regressions from refactor

See `BCO_REFACTOR_PLAN.md` for architecture details.

### ✅ Isometric Tilemap (NEW - 2026-04-05)
Implemented professional isometric tileset for battle grid:

**Assets**:
- `Assets/Tilesets/practice_iso_tiles.png` - 14 colored isometric cubes (32×32px)
- `Assets/Tilesets/IsometricTileSet.tres` - TileSet resource
  - Tile shape: Diamond (isometric)
  - Tile size: 32×16 (base size)
  - Margin: 16×16
  - Separation: 16×16
  - Texture origin: (0, -8) per tile

**Scenes**:
- `Scenes/Battle/BattleGridVisual.tscn` - TileMapLayer with isometric grid
- `Scenes/Battle/BattleGridVisual.gd` - Syncs with C# BattleGrid data
  - Maps terrain types to tile colors
  - Highlight system for movement/attack ranges
  - Test pattern: 30×20 checkerboard (600 tiles)
- `Scenes/Battle/BattleScene.tscn` - Main battle scene with camera
- `Scenes/Battle/BattleScene.gd` - Camera controls (WASD, zoom with mouse wheel)

**Display**:
- Viewport: 1920×1080 fullscreen
- Camera zoom: 0.8x default
- Camera centered on grid

**Features**:
- Y-sort enabled for proper depth rendering
- Click detection for tile selection
- Placeholder for C# MissionContext integration
- Ready to replace old GridView presentation code

### ✅ Presentation Layer (Isometric - OLD)
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
- **Isometric grid** renders with 600 diamond tiles (30×20 map)
- **1920×1080 fullscreen viewport**
- **Camera controls**: WASD/Arrow keys to pan, mouse wheel to zoom
- **Note**: C# integration pending - no units rendered yet

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
