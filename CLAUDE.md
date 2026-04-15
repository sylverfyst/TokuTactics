# CLAUDE.md — Toku Tactics Project Context

---
**📋 LATEST SESSION STATUS → See [HANDOFF.md](./HANDOFF.md)**
- Presentation layer complete (isometric grid, battle UI)
- Project builds successfully (0 errors)
- Ready for first playtest in Godot
- Date: 2026-03-31
---

## What This Is

Toku Tactics is a 2.5D isometric tactics game inspired by anniversary-season Super Sentai (Japanese tokusatsu). A team of five heroes (later six) battles through episodic missions using a form-switching system instead of traditional unit recruitment. Players collect new forms (classes), weapons, and zords as progression rewards. Engine: Godot with C#, IDE: Rider. Single player only.

The game's in-world name for the hero team is **Tenshouger** (転装 — shift-armor). The villain faction is the **Seigidan** (正義団 — Righteous Order), who weaponize toxic fandom to awaken an ancient cosmic entity called **Onkyō** (怨響 — Grudge Resonance).

## Project State

The C# game logic layer is complete as a vertical slice. **93 files: 65 source + 28 test suites, 569 total tests.** All code is pure C# with no Godot dependencies — ready to wire into Godot scenes. The namespace is `TokuTactics`.

The next step is Godot integration: creating scenes, wiring the game logic to the presentation layer, and building the first playable episode.

## Repository Structure

```
TokuTactics/
├── Scripts/
│   ├── Core/                    # Engine-independent foundations
│   │   ├── Combat/              # DamageCalculator, ComboScaler, ICombatActor/Target
│   │   ├── Cooldown/            # CooldownTimer, ICooldown
│   │   ├── Events/              # EventBus, GameEvents (35+ event types), IGameEvent
│   │   ├── Grid/                # BattleGrid (Dijkstra, A*, LoS, area shapes), Tile, TerrainConfig
│   │   ├── Health/              # HealthPool, IHealthPool
│   │   ├── Stats/               # StatBlock (immutable), StatType (STR/DEF/SPD/MAG/CHA/LCK)
│   │   ├── StatusEffect/        # Composable trigger+behavior system
│   │   └── Types/               # ElementalType (9 types), DualType, TypeChart
│   ├── Data/Content/            # All game content definitions
│   │   ├── ContentRegistry.cs   # Central lookup: ID → data object
│   │   ├── EpisodeDefinition.cs # Episode structure: phases, spawns, objectives
│   │   ├── EpisodeCatalog.cs    # Concrete episodes (Frozen Outpost)
│   │   ├── EnemyCatalog.cs      # Enemy data (Putty, BlazeGrunt, FrostWyrm, ShadowCommander)
│   │   ├── FormCatalog.cs       # Form data (Base, Blaze, Torrent, Frost)
│   │   ├── MapCatalog.cs        # Map geometry (FrozenOutpost 12x10)
│   │   ├── RangerCatalog.cs     # 5 Ranger definitions
│   │   ├── TypeChartSetup.cs    # Type matchup table
│   │   ├── ZordCatalog.cs       # 5 base zords
│   │   └── PersonalAbilities/   # ScoutPush, Rally
│   ├── Entities/
│   │   ├── Enemies/             # Enemy (runtime), EnemyData (definition), Gimmicks/
│   │   ├── Forms/               # FormData (definition), FormInstance (per-Ranger runtime)
│   │   ├── Rangers/             # Ranger (runtime), Proclivity, Battleizer, MorphState
│   │   ├── Weapons/             # WeaponData, StatusEffectTemplate
│   │   └── Zords/               # ZordData, ZordInstance, Megazord
│   └── Systems/
│       ├── ActionEconomy/       # ActionBudget, BondTracker, TurnOrder
│       ├── AI/                  # BehaviorTree, StructuralNodes, UtilityScorers
│       ├── AssistResolution/    # AssistResolver (bond-tier assist attacks)
│       ├── CombatResolution/    # CombatResolver (orchestrates full attack flow)
│       ├── FormManagement/      # FormPool (budget, cooldowns, exclusivity, loadout lock)
│       ├── GimmickResolution/   # GimmickResolver (spatial translation of gimmick outputs)
│       ├── LoadoutSelection/    # LoadoutController (first-morph gate, scouting intel)
│       ├── MissionSetup/        # MissionContext (single-call mission initialization)
│       ├── PhaseManagement/     # PhaseManager (round/phase/turn state machine)
│       └── SaveLoad/            # SaveData, SaveManager, ISaveStorage, ISaveSerializer
└── Tests/
    ├── TestRunner.cs            # Runs all 28 test suites
    ├── Core/                    # 9 test suites (207 tests)
    ├── Data/                    # 1 test suite (42 tests)
    ├── Entities/                # 6 test suites (128 tests)
    └── Systems/                 # 12 test suites (192 tests)
```

## Architecture Patterns

### Composable Trigger + Behavior

Status effects, enemy gimmicks, and personal abilities all follow the same pattern: a trigger decides WHEN something happens, a behavior decides WHAT happens, and the behavior produces a declarative output. Resolvers consume the output and apply state changes.

```
Data definition (trigger + behavior)
    → Behavior.GetOutput() returns declarative output
    → Resolver translates output to concrete state changes
```

**Never mutate state inside a behavior.** Behaviors describe intent. Resolvers execute it.

### CreateFresh Pattern

Any trigger or behavior that holds mutable state (turn counters, one-shot flags) must implement `CreateFresh()` to return a new instance. This prevents shared mutable state when the same template is used across multiple entities.

### EventBus Convention

Only one GameState-priority subscriber per event type. GameState handlers do all state mutation. Gameplay/Presentation/Audio tiers are read-only and can have multiple subscribers. Debug mode warns on convention violations.

### Displacement Convention

Abilities and gimmicks that move units express displacement as distance + direction (push/pull), NOT raw GridPositions. The resolver applies Bresenham cardinal stepping with wall/edge/occupancy collision checks. This keeps behaviors spatial-logic-free.

### DamageInput Dual-Type Convention

When the defender is a Ranger (has dual type): `DefenderDualType` is set, `TypeChart.ResolveDefensive` is used (checks both innate + form type). When the defender is an enemy (single type): `DefenderDualType` is null, `TypeChart.Resolve` is used.

## Key Game Systems

### MissionContext (the entry point)

`MissionContext.Create(episode, campaignData, registry)` builds the entire runtime dependency graph in one call. The Godot scene only needs this object.

```csharp
var registry = ContentRegistry.CreateVerticalSlice();
var episode = registry.GetEpisode("episode_frozen_outpost");
var ctx = MissionContext.Create(episode, campaign, registry);
ctx.StartMission();
```

MissionContext holds: EventBus, BattleGrid, TypeChart, DamageCalculator, all Rangers, all Enemies, FormPool, BondTracker, AssistResolver, GimmickResolver, CombatResolver, PhaseManager, LoadoutController, ActionBudgets, ContentRegistry, and lookup tables.

Helpers: `BuildAssistStates()` (extracts live Ranger state for CombatResolver), `BeginUnitTurn(id)` (resets ActionBudget), `StartMission()`.

### Combat Flow

```
Player chooses attack
    → ctx.BuildAssistStates()
    → CombatResolver.ResolveRangerAttack(attacker, target, power, weapon, assistStates)
        → AssistResolver finds eligible allies (bond tier, range, morph state)
        → Primary damage calculated (type matchup, stats, combo scaling)
        → If hit landed (!WasDodged):
            → Apply damage to target
            → Apply weapon status effect
            → Process each assist (dead-target check between each)
            → Check target death
            → Check reactive gimmicks (OnHit)
        → Return CombatResult
    → PhaseManager.CheckWinLoss()
```

### Phase Flow

```
PhaseManager.StartRound()           # Tick cooldowns, tick status effects, reset combos, check deaths
PhaseManager.StartPlayerPhase()     # Build turn order from alive Rangers by SPD
PhaseManager.AdvanceTurn()          # Returns next unit
ctx.BeginUnitTurn(unitId)           # Reset ActionBudget
... player acts ...
PhaseManager.EndCurrentTurn()
... repeat until IsPhaseComplete() ...
PhaseManager.EndPhase()
PhaseManager.StartEnemyPhase()      # Same flow for enemies
```

### Morph / Loadout Flow

```
LoadoutController.RequestMorph(ranger)
    → If loadout not locked: returns NeedsLoadout (open UI)
    → Player picks forms within budget
    → LoadoutController.SubmitLoadout(formIds) → validates, equips, locks
    → LoadoutController.RequestMorph(ranger) again → MorphComplete
    → All subsequent Rangers morph directly (loadout locked once per mission)
```

### Form System

- 10 shared forms for core 5, 5 redeemed forms for 6th
- Only one Ranger per non-base form at a time (exclusivity)
- Vacating a form starts team-wide cooldown (modified by MAG)
- Form switch is free, resets full action economy (movement + action)
- Each successive chain action deals reduced damage (ComboScaler)
- Status effect potency stays full through chains
- Separate health pool per form, passive regen during cooldown
- Form death = immediate demorph, NOT mission loss
- Unmorphed death = mission lost

### Enemy Hierarchy

| Tier | Actions | AI | Notes |
|------|---------|-----|-------|
| FootSoldier | 1 | Behavior tree | Basic attack only |
| Monster | 2 (independent) | Behavior tree + gimmick | Basic attack + unique gimmick |
| Lieutenant | 2 (choose 2 of 3) | Utility scoring | Basic + weapon + rotating gimmick |
| DarkRanger | Full mirror | Full utility AI | Uses Ranger entity with dark forms |

### Bond System

4 tiers via assist combat. T1: assist damage bonus. T2: pair attack (pulls assister to base form). T3: replaces T2, no disruption. T4: T3 + action refresh (1/round/character).

### Save System

Full save between episodes (10 slots). Single mid-mission restore point (manual placement, overwrites). `ISaveStorage` / `ISaveSerializer` abstractions — `MemorySaveStorage` for tests, implement `GodotSaveStorage` for production.

## Content Authoring (How to Build a New Episode)

Building a new episode requires NO new systems. Just data:

1. **New enemy types** → Add to `EnemyCatalog.cs`, compose from existing triggers + behaviors
2. **New map** → Add to `MapCatalog.cs` (terrain + elevation + Ranger spawn positions)
3. **New episode** → Add to `EpisodeCatalog.cs` (map ID + enemy spawns + defeat targets + cutscenes)
4. **Register** → Add to `ContentRegistry.CreateVerticalSlice()` (or a future content loader)

Lieutenant variant pattern (same weapon, different gimmick per encounter):
```csharp
EnemyCatalog.ShadowCommanderWithGimmick(
    new GimmickData("shield_phase", "Shield Phase",
        new HealthThresholdGimmickTrigger(0.5f),
        new ShieldGimmickBehavior(duration: 3)));
```

Composable component libraries:
- **5 gimmick triggers** × **7 gimmick behaviors** = 35 monster mechanics
- **4 status triggers** × **5 status behaviors** = 20 status effects
- Weapons compose from power + range + optional status template
- Forms compose from type + stats + 2 weapons + cooldown

## Type System

9 types: Blaze, Torrent, Gale, Stone, Volt, Frost, Shadow, Radiant, Normal. Core triangle: Blaze > Frost > Torrent > Blaze. Normal is purely neutral. Dual typing: Ranger innate + form type. Same-type bonus is high risk/high reward.

## Known Vertical Slice Gaps

These are documented, intentional, and ready to fill when needed:

- **EffectOutput.PreventsAction (stun):** Field exists, behaviors produce it, PhaseManager doesn't consume it yet. Needs an action prevention check in AdvanceTurn.
- **EffectOutput.StatModifiers:** Field exists, not applied. Needs a stat modifier layer.
- **EffectOutput.MovementMultiplier:** Needs movement system integration.
- **Multi-phase episodes:** MissionContext.Create processes only the first Ground phase. Extend with TransitionToPhase() for Ground → Mecha.
- **Delayed spawns:** EnemySpawnEntry.SpawnTurn > 0 is flagged but not processed. Add spawn check to PhaseManager.StartRound().
- **Movement system:** BattleGrid has Dijkstra pathfinding and movement range calculation. No system-level "move unit and consume movement" orchestrator.
- **AI execution:** Behavior tree structure exists. No concrete behavior tree definitions or execution loop.
- **Godot integration:** All code is pure C#. Needs Godot nodes, scenes, input handling, rendering.

## World / Story Context

Full world bible at `toku-tactics-world-bible.md`. Key points:

- **Setting:** Modern Japan, real-world adjacent
- **Heroes:** Five young adult (20-24) public figures powered by genuine fan love
- **Villains:** Seigidan weaponizes toxic fandom (all kinds, not just hero worship) to feed Onkyō
- **Player choice:** Player picks their avatar character. That character becomes leader regardless of color. Team dynamic shifts around whoever leads.
- **Tone:** Sincere homage to toku. Japanese terminology for specialized words (henshin, tenshou, gattai), romanized in dialogue, kanji in art.

### Cast

| Color | Name | Gender | Domain | Heritage |
|-------|------|--------|--------|----------|
| RED | Kai Asano | Male | Actor | Half-Japanese |
| BLUE | Shinobu Fujiwara | Non-binary | Traditional Performer | Japanese lineage |
| YELLOW | Daichi Reyes | Male | Athlete | Filipino-Japanese |
| GREEN | Hana Ito | Female | Streamer/Creator | Japanese |
| PINK | Yoon Haeun | Female | Idol/Musician | Korean |

### Key Terminology

| Concept | Term | Notes |
|---------|------|-------|
| Team | Tenshouger | テンショウガー |
| Individual | Tenshou [Color] | 転装 |
| Transform | Henshin / Tenshou! | First morph / form switches |
| Power-up | Choutenshi (超転士) | Timed super mode |
| Mecha | Gōki (号機) | Universal — any theme |
| Combined | Tenshou-Oh (転装王) | -Oh suffix |
| Combine call | Gattai (合体) | Genre vocabulary |
| Villain faction | Seigidan (正義団) | Righteous Order |
| General | Dōshi (導士) | The Guide |
| Final boss | Onkyō (怨響) | Grudge Resonance |
| Foot soldiers | Nekkyō (熱狂) | Converted civilians, permanent |
| Monster of Week | Kyōshin (狂心) | Crystallized obsession |

## Conventions for This Codebase

- **Namespace:** `TokuTactics.*` — follows directory structure
- **No Godot dependencies in Scripts/ or Tests/.** Everything is pure C#.
- **Declarative outputs:** Behaviors produce data describing intent. Resolvers apply it.
- **CreateFresh() on stateful components.** Always.
- **EventBus:** One GameState subscriber per event type. Multiple read-only subscribers fine.
- **Tests are in-process, no framework.** Assert throws on failure. TestRunner.cs runs all suites.
- **Content is code, not data files.** Catalogs are static methods returning configured objects. This will likely move to data files (JSON/YAML) later, but for now content IS code.
- **Episode = template.** New episodes fill out EpisodeDefinition with content IDs. No new systems.
- **Novel rule (Bryan's writing projects only, not this game):** Never write prose or narrative. Help with architecture, feedback, editing, and style adjustments only.

## Godot MCP — Debug Bridge

The project has a Godot MCP bridge that lets you interact with the running game directly.
DebugBridge runs as an Autoload on http://localhost:9880 in debug builds.

### Available tools (via MCP):

**Observation:**
- `mcp__godot__ping_godot` — check if the game is running
- `mcp__godot__screenshot` — capture the viewport as a PNG image you can see
- `mcp__godot__scene_tree` — get the full node hierarchy (names, types, positions, visibility)
- `mcp__godot__inspect_node` — deep-inspect a node's properties, groups, script, signals
- `mcp__godot__godot_logs` — get console output (GD.Print, errors, warnings, engine messages)

**Interaction:**
- `mcp__godot__input_action` — trigger an Input Map action (e.g. "ui_accept", "attack")
- `mcp__godot__input_key` — simulate a key press (e.g. "SPACE", "ENTER")
- `mcp__godot__input_mouse` — move mouse and/or click at viewport coordinates
- `mcp__godot__call_method` — call any public method on any node by path with arguments
- `mcp__godot__wait_frames` — wait N milliseconds for animations/transitions to complete

### Usage guidelines:
- When debugging visual or runtime issues, use screenshot + scene_tree + godot_logs to verify state directly instead of asking the user to describe what they see.
- For integration testing, use call_method to invoke game logic and screenshot + inspect_node to verify results.
- Always use wait_frames between actions that trigger animations or scene transitions.
- Always check godot_logs after testing to catch warnings or errors.
- Use ping_godot first to confirm the game is running before attempting other tools.
- call_method is more reliable than input simulation for testing game logic — prefer it when possible.

### Setup Status:
- ✅ DebugBridge.cs copied to Scripts/Debug/
- ✅ Registered as Autoload in project.godot
- ✅ .mcp.json configured with absolute node path
- ✅ Node.js 25.9.0 installed via Homebrew
- ✅ MCP server dependencies installed (91 packages)
- ⏳ **Next step:** Restart Claude Code to enable MCP tools

After restarting Claude Code, the Godot MCP tools will be available with the `mcp__godot__` prefix.
