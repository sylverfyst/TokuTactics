using Godot;
using System.Collections.Generic;
using System.Linq;
using TokuTactics.Data.Content;
using TokuTactics.Systems.MissionSetup;
using TokuTactics.Systems.SaveLoad;
using TokuTactics.Systems.PhaseManagement;
using TokuTactics.Core.Events;
using TokuTactics.Core.Grid;
using TokuTactics.Commands.AI;
using TokuTactics.Commands.Combat;
using TokuTactics.Commands.Movement;
using TokuTactics.Commands.Phase;
using TokuTactics.Core.ActionEconomy;

namespace TokuTactics.Scenes.Battle
{
	/// <summary>
	/// Battle controller that bridges C# game logic (MissionContext) with
	/// GDScript presentation layer (BattleScene.gd, BattleGridVisual.gd).
	///
	/// This is attached to BattleScene as a C# node.
	/// </summary>
	public partial class BattleController : Node
	{
		// === Game Logic ===

		public MissionContext Context { get; private set; }

		// === GDScript References ===

		private Node _battleScene;        // BattleScene.gd root node
		private Node _gridVisual;          // BattleGridVisual.gd

		// === Player Input State ===

		private string _selectedUnitId = null;
		private GridPosition? _selectedUnitPosition = null;
	private Dictionary<GridPosition, int> _currentMovementRange = null;
	private GridPosition? _previewedDestination = null;  // Destination being previewed before move confirmation

		// === Lifecycle ===

		public override void _Ready()
		{
			GD.Print("=== BattleController (C#) Initializing ===");

			// Get GDScript node references
			_battleScene = GetParent();
			_gridVisual = _battleScene.GetNode("BattleGridVisual");

			// Initialize mission
			InitializeMission();

			// Subscribe to events
			Context.EventBus.Subscribe<UnitTurnStartedEvent>(OnTurnStarted, EventPriority.Presentation);
			Context.EventBus.Subscribe<UnitTurnEndedEvent>(OnTurnEnded, EventPriority.Presentation);
			Context.EventBus.Subscribe<PhaseTransitionEvent>(OnPhaseChanged, EventPriority.Presentation);
			Context.EventBus.Subscribe<MissionVictoryEvent>(OnMissionWon, EventPriority.Presentation);
			Context.EventBus.Subscribe<MissionDefeatEvent>(OnMissionLost, EventPriority.Presentation);
			Context.EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt, EventPriority.Presentation);

			// Sync initial state to GDScript grid
			SyncGridToVisual();

			// Start the battle
			Context.StartMission();

		// Start the first round and player phase
		Context.PhaseManager.StartRound();
		Context.PhaseManager.StartPlayerPhase();

		// Start the first unit's turn
		var firstUnit = Context.PhaseManager.AdvanceTurn();
		if (firstUnit != null)
		{
			BeginAndShowUnitTurn(firstUnit.Participant.ParticipantId);
			GD.Print($"=== First Turn Started: {firstUnit.Participant.ParticipantId} ===");
		}

		GD.Print("=== Player Phase Started ===");

			GD.Print("=== BattleController Ready ===");
		}

		// === Initialization ===

		private void InitializeMission()
		{
			GD.Print("Initializing mission from C#...");

			var registry = ContentRegistry.CreateVerticalSlice();
			var episode = registry.GetEpisode("episode_frozen_outpost");
			var campaignData = new CampaignData();

			Context = MissionContext.Create(episode, campaignData, registry);

			GD.Print($"✓ Mission initialized: {episode.Title}");
			GD.Print($"  - Grid: {Context.Grid.Width}x{Context.Grid.Height}");
			GD.Print($"  - Rangers: {Context.Rangers.Count}");
			GD.Print($"  - Enemies: {Context.Enemies.Count}");
		}

		// === Grid Synchronization ===

		/// <summary>
		/// Syncs C# BattleGrid state to GDScript BattleGridVisual.
		/// Calls sync_with_battle_grid(grid_data: Dictionary)
		/// </summary>
		private void SyncGridToVisual()
		{
			// Build grid data dictionary for GDScript
			var gridData = new Godot.Collections.Dictionary
			{
				["Width"] = Context.Grid.Width,
				["Height"] = Context.Grid.Height,
				["Tiles"] = new Godot.Collections.Array()
			};

			var tiles = (Godot.Collections.Array)gridData["Tiles"];

			for (int y = 0; y < Context.Grid.Height; y++)
			{
				for (int x = 0; x < Context.Grid.Width; x++)
				{
					var pos = new GridPosition(x, y);
					var tile = Context.Grid.GetTile(pos);
					var tileData = new Godot.Collections.Dictionary
					{
						["Terrain"] = tile.Terrain.ToString(),
						["Elevation"] = tile.Elevation,
						["Passable"] = Context.Grid.IsTilePassable(pos)
					};
					tiles.Add(tileData);
				}
			}

			// Call GDScript method
			_gridVisual.Call("sync_with_battle_grid", gridData);
			GD.Print($"Synced {tiles.Count} tiles to visual grid");

			// Sync unit positions
			SyncUnitsToVisual();
		}

		/// <summary>
		/// Syncs all unit positions to the visual grid.
		/// Calls spawn_unit(unit_id, grid_pos, unit_type, color)
		/// </summary>
		private void SyncUnitsToVisual()
		{
			// Spawn Rangers
			foreach (var ranger in Context.Rangers.Where(r => r.IsAlive))
			{
				var pos = Context.Grid.GetUnitPosition(ranger.Id);
				if (pos != null)
				{
					var gridPos = new Vector2I(pos.Value.Col, pos.Value.Row);
					var colorName = ExtractColorFromRangerId(ranger.Id);
					_gridVisual.Call("spawn_unit", ranger.Id, gridPos, "ranger", colorName);
					GD.Print($"Spawning {ranger.Id} at C# grid pos Col={pos.Value.Col}, Row={pos.Value.Row}");
				}
			}

			// Spawn Enemies
			foreach (var enemy in Context.Enemies.Where(e => e.IsAlive))
			{
				var pos = Context.Grid.GetUnitPosition(enemy.Id);
				if (pos != null)
				{
					var gridPos = new Vector2I(pos.Value.Col, pos.Value.Row);
					_gridVisual.Call("spawn_unit", enemy.Id, gridPos, "enemy", "gray");
				}
			}

			GD.Print($"Synced {Context.Rangers.Count} Rangers + {Context.Enemies.Count} Enemies to grid");

			// Push initial HP into each unit's health bar
			foreach (var ranger in Context.Rangers.Where(r => r.IsAlive))
			{
				var pool = ranger.ActiveHealthPool;
				_gridVisual.Call("update_health_bar", ranger.Id, pool.Current, pool.Maximum);
			}
			foreach (var enemy in Context.Enemies.Where(e => e.IsAlive))
			{
				_gridVisual.Call("update_health_bar", enemy.Id, enemy.Health.Current, enemy.Health.Maximum);
			}
		}

		/// <summary>
		/// Extract color name from ranger ID (e.g., "ranger_red" -> "red")
		/// </summary>
		private string ExtractColorFromRangerId(string rangerId)
		{
			if (rangerId.StartsWith("ranger_"))
			{
				return rangerId.Substring("ranger_".Length);
			}
			return "red"; // fallback
		}

		// === Event Handlers ===

		private void OnTurnStarted(UnitTurnStartedEvent evt)
		{
			GD.Print($"[C#] Turn started: {evt.UnitId}");

			// Highlight active unit
			_gridVisual.Call("highlight_active_unit", evt.UnitId);

			// Note: UI update is handled by BeginAndShowUnitTurn() which calls
			// BeginUnitTurn first to ensure the ActionBudget is initialized.
		}

		private void OnTurnEnded(UnitTurnEndedEvent evt)
		{
			GD.Print($"[C#] Turn ended: {evt.UnitId}");

			// Clear highlights
			_gridVisual.Call("clear_highlights");
		}

		private void OnPhaseChanged(PhaseTransitionEvent evt)
		{
			GD.Print($"[C#] Phase changed: {evt.FromPhaseId} → {evt.ToPhaseId}");
		}

		private void OnMissionWon(MissionVictoryEvent evt)
		{
			GD.Print("=== MISSION VICTORY ===");
		}

		private void OnMissionLost(MissionDefeatEvent evt)
		{
			GD.Print("=== MISSION DEFEAT ===");
		}

		/// <summary>
		/// Pushes the target's current HP to its health bar after damage lands.
		/// Read-only — pulls fresh values from the already-mutated HealthPool.
		/// </summary>
		private void OnDamageDealt(DamageDealtEvent evt)
		{
			if (evt.WasDodged) return;

			var ranger = Context.Rangers.FirstOrDefault(r => r.Id == evt.TargetId);
			if (ranger != null)
			{
				var pool = ranger.ActiveHealthPool;
				_gridVisual.Call("update_health_bar", ranger.Id, pool.Current, pool.Maximum);
				return;
			}

			var enemy = Context.Enemies.FirstOrDefault(e => e.Id == evt.TargetId);
			if (enemy != null)
			{
				_gridVisual.Call("update_health_bar", enemy.Id, enemy.Health.Current, enemy.Health.Maximum);
			}
		}

		// === Player Input Handling ===

		public void OnTileClicked(int col, int row)
		{
			var clickedPos = new GridPosition(col, row);
			GD.Print($"[C#] Tile clicked: ({col}, {row})");

			// Check if there's a unit at this position
			var tile = Context.Grid.GetTile(clickedPos);
			var unitAtPosition = tile?.OccupantId;

			if (_selectedUnitId == null)
			{
				// No unit selected - try to select one
				if (unitAtPosition != null)
				{
					// Check if it's a Ranger (player unit)
					var ranger = Context.Rangers.FirstOrDefault(r => r.Id == unitAtPosition);
					if (ranger != null && ranger.IsAlive)
					{
						SelectUnit(unitAtPosition, clickedPos);
					}
					else
					{
						GD.Print($"  Cannot select enemy unit: {unitAtPosition}");
					}
				}
			}
			else
			{
				// Unit already selected - try to move or attack
				if (clickedPos == _selectedUnitPosition)
				{
					// Clicked same unit - deselect
					DeselectUnit();
				}
				else if (unitAtPosition != null)
				{
					// Clicked another unit — attack if enemy, switch selection if ranger
					var isEnemy = Context.Enemies.Any(e => e.Id == unitAtPosition && e.IsAlive);
					if (isEnemy)
					{
						TryAttackTarget(unitAtPosition, clickedPos);
					}
					else
					{
						// Clicked a ranger — switch selection
						DeselectUnit();
						var ranger = Context.Rangers.FirstOrDefault(r => r.Id == unitAtPosition);
						if (ranger != null && ranger.IsAlive)
							SelectUnit(unitAtPosition, clickedPos);
					}
				}
				else
				{
				// Clicked empty tile - preview or execute move
				if (_previewedDestination.HasValue && _previewedDestination.Value == clickedPos)
				{
					// Clicking previewed destination again - confirm and execute move
					ExecuteMove(clickedPos);
				}
				else
				{
					// Preview movement to this destination
					PreviewMovement(clickedPos);
				}
				}
			}
		}

		private void SelectUnit(string unitId, GridPosition position)
		{
		// Only allow selecting the unit whose turn it is
		if (Context.PhaseManager.ActiveUnit == null)
		{
			GD.Print($"  Cannot select unit: no active turn");
			return;
		}

		string activeUnitId = Context.PhaseManager.ActiveUnit.Participant.ParticipantId;
		if (unitId != activeUnitId)
		{
			GD.Print($"  Cannot select {unitId}: it's {activeUnitId}'s turn");
			return;
		}

			_selectedUnitId = unitId;
			_selectedUnitPosition = position;

			GD.Print($"[C#] Selected unit: {unitId} at ({position.Col}, {position.Row})");

			// Highlight the selected unit
			_gridVisual.Call("highlight_active_unit", unitId);

			// Show movement range (if can still move)
			if (Context.ActionBudgets.TryGetValue(unitId, out var selectBudget) && selectBudget.CanMove)
				ShowMovementRange(unitId, position);

			// Show attack range (if can act)
			if (selectBudget != null && selectBudget.CanAct)
				ShowAttackRange(unitId, position);
		}

		/// <summary>
		/// Calculate and display movement range for the selected unit.
		/// </summary>
		private void ShowMovementRange(string unitId, GridPosition position)
		{
			// Get the Ranger
			var ranger = Context.Rangers.FirstOrDefault(r => r.Id == unitId);
			if (ranger == null)
			{
				GD.Print($"  Could not find Ranger: {unitId}");
				return;
			}

			// Get movement range from current form (or base form if unmorphed)
			var currentForm = ranger.CurrentForm ?? ranger.BaseForm;
			if (currentForm == null)
			{
				GD.Print($"  Ranger {unitId} has no active form");
				return;
			}

			int movementRange = currentForm.Data.MovementRange;
			GD.Print($"  Movement range: {movementRange}");

			// Build set of ally IDs that can be moved through
			var allyIds = new HashSet<string>(Context.Rangers
				.Where(r => r.IsAlive && r.Id != unitId)
				.Select(r => r.Id));

			// Calculate reachable tiles
			var reachableTiles = Context.Grid.GetMovementRange(position, movementRange, allyIds);

		// Store movement range for validation
		_currentMovementRange = reachableTiles;

			// Convert to Godot Vector2i array for GDScript
			var positions = new Godot.Collections.Array();
			foreach (var pos in reachableTiles.Keys)
			{
				positions.Add(new Vector2I(pos.Col, pos.Row));
			}

			GD.Print($"  Reachable tiles: {positions.Count}");

			// Call GDScript to highlight tiles
			_gridVisual.Call("highlight_tiles", positions, "movement");
		}

		private void DeselectUnit()
		{
			if (_selectedUnitId != null)
			{
				GD.Print($"[C#] Deselected unit: {_selectedUnitId}");
			}

			_selectedUnitId = null;
		_currentMovementRange = null;
		_previewedDestination = null;
			_selectedUnitPosition = null;

			// Clear unit highlights
			_gridVisual.Call("clear_unit_highlight");

		// Clear movement range overlay
		_gridVisual.Call("clear_highlights");
		}

	/// <summary>
	/// Execute a movement action for the selected unit using the BCO pattern.
	/// </summary>
	private void ExecuteMove(GridPosition destination)
	{
		GD.Print($"[C#] ExecuteMove called for {_selectedUnitId} to ({destination.Col}, {destination.Row})");

		// Get action budget
		if (!Context.ActionBudgets.ContainsKey(_selectedUnitId))
		{
			GD.Print($"  No action budget for {_selectedUnitId}");
			return;
		}

		var budget = Context.ActionBudgets[_selectedUnitId];
		GD.Print($"  Budget CanMove: {budget.CanMove}, CanAct: {budget.CanAct}");

		// Debug: check movement range
		if (_currentMovementRange != null)
		{
			GD.Print($"  Movement range has {_currentMovementRange.Count} tiles");
			GD.Print($"  Destination in range: {_currentMovementRange.ContainsKey(destination)}");
		}
		else
		{
			GD.Print($"  WARNING: _currentMovementRange is null!");
		}

		// Execute movement using the Command
		var result = ExecuteMovement.Execute(
			unitId: _selectedUnitId,
			destination: destination,
			movementRange: _currentMovementRange,
			grid: Context.Grid,
			actionBudget: budget
		);

		// Handle result
		if (!result.Success)
		{
			GD.Print($"  Movement failed: {result.FailureReason}");
			return;
		}

		// Movement succeeded
		GD.Print($"[C#] Moved {_selectedUnitId} from ({_selectedUnitPosition.Value.Col}, {_selectedUnitPosition.Value.Row}) to ({destination.Col}, {destination.Row})");

		// Update visual sprite position
		var gridPos = new Vector2I(destination.Col, destination.Row);
		_gridVisual.Call("move_unit", _selectedUnitId, gridPos);

		// Update selected unit position
		_selectedUnitPosition = destination;

		// Clear movement preview
		_previewedDestination = null;
		_gridVisual.Call("clear_movement_preview");

		// Clear movement overlay
		_gridVisual.Call("clear_highlights");
		_currentMovementRange = null;

		// Show attack range if ranger can still act
		if (Context.ActionBudgets.TryGetValue(_selectedUnitId, out var moveBudget) && moveBudget.CanAct)
		{
			ShowAttackRange(_selectedUnitId, destination);
		}

		// Update turn indicator with current budget
		UpdateTurnIndicator();
	}

	/// <summary>
	/// Preview movement to a destination by showing the path and outlining the destination tile.
	/// </summary>
	private void PreviewMovement(GridPosition destination)
	{
		// Validate that destination is in movement range
		if (_currentMovementRange == null || !_currentMovementRange.ContainsKey(destination))
		{
			GD.Print($"  Cannot preview: destination not in movement range");
			return;
		}

		// Get ally IDs for pathfinding (can move through allies)
		var allyIds = new HashSet<string>(Context.Rangers
			.Where(r => r.IsAlive && r.Id != _selectedUnitId)
			.Select(r => r.Id));

		// Find path from current position to destination
		var path = Context.Grid.FindPath(_selectedUnitPosition.Value, destination, 
			_currentMovementRange[destination], allyIds);

		if (path == null || path.Count == 0)
		{
			GD.Print($"  No path found to ({destination.Col}, {destination.Row})");
			return;
		}

		// Store previewed destination
		_previewedDestination = destination;

		// Convert path to Godot Vector2i array
		var pathPositions = new Godot.Collections.Array();
		foreach (var pos in path)
		{
			pathPositions.Add(new Vector2I(pos.Col, pos.Row));
		}

		GD.Print($"[C#] Previewing movement to ({destination.Col}, {destination.Row}), path length: {path.Count}");

		// Call GDScript to show path and outline destination
		_gridVisual.Call("show_movement_preview", pathPositions);
	}

	// === Attack ===

	/// <summary>
	/// Show the attack range for the selected unit from a given position.
	/// </summary>
	private void ShowAttackRange(string unitId, GridPosition position)
	{
		var ranger = Context.Rangers.FirstOrDefault(r => r.Id == unitId);
		if (ranger == null) return;

		var weapon = ranger.CurrentForm?.Data.WeaponA ?? ranger.BaseForm.Data.WeaponA;
		if (weapon == null) return;

		int range = weapon.Range;
		GD.Print($"  Attack range: {range} (weapon: {weapon.Name})");

		// Find enemy targets in attack range using grid neighbors
		var attackTiles = new Godot.Collections.Array();
		var tilesInRange = Context.Grid.GetTilesInRange(position, range);
		foreach (var tilePos in tilesInRange)
		{
			if (tilePos == position) continue;
			var tile = Context.Grid.GetTile(tilePos);
			if (tile?.OccupantId != null)
			{
				var isEnemy = Context.Enemies.Any(e => e.Id == tile.OccupantId && e.IsAlive);
				if (isEnemy)
					attackTiles.Add(new Vector2I(tilePos.Col, tilePos.Row));
			}
		}

		if (attackTiles.Count > 0)
		{
			_gridVisual.Call("highlight_tiles", attackTiles, "attack");
			GD.Print($"  Targets in range: {attackTiles.Count}");
		}
	}

	/// <summary>
	/// Attempt to attack an enemy at the given position.
	/// </summary>
	private void TryAttackTarget(string targetId, GridPosition targetPos)
	{
		GD.Print($"[C#] TryAttackTarget: {_selectedUnitId} → {targetId}");

		var ranger = Context.Rangers.FirstOrDefault(r => r.Id == _selectedUnitId);
		if (ranger == null) return;

		var enemy = Context.Enemies.FirstOrDefault(e => e.Id == targetId);
		if (enemy == null) return;

		if (!Context.ActionBudgets.TryGetValue(_selectedUnitId, out var budget))
		{
			GD.Print("  No action budget");
			return;
		}

		// Get weapon data
		var weapon = ranger.CurrentForm?.Data.WeaponA ?? ranger.BaseForm.Data.WeaponA;
		if (weapon == null)
		{
			GD.Print("  No weapon available");
			return;
		}

		// Execute attack via command
		var result = ExecuteAttack.Execute(
			attackerPos: _selectedUnitPosition.Value,
			targetPos: targetPos,
			weaponRange: weapon.Range,
			actionBudget: budget,
			resolveCombat: () => Context.CombatResolver.ResolveRangerAttack(
				ranger, enemy, weapon.BasePower, weapon.StatusEffect,
				Context.BuildAssistStates())
		);

		if (!result.Success)
		{
			GD.Print($"  Attack failed: {result.FailureReason}");
			// Keep selection intact — don't deselect on failed attack
			return;
		}

		// === Handle combat results ===
		var combat = result.CombatResult;
		GD.Print($"[C#] Attack landed! Damage: {combat.TotalDamage}, Dodged: {combat.PrimaryDamage?.WasDodged}");

		// Handle target death
		if (combat.TargetDied)
		{
			GD.Print($"[C#] Enemy {targetId} defeated!");
			_gridVisual.Call("remove_unit", targetId);

			// Check win condition
			Context.PhaseManager.CheckWinLoss();
		}

		if (combat.FormDied)
			GD.Print($"[C#] Form destroyed: {combat.LostFormId}");

		if (combat.MissionLost)
			GD.Print("[C#] MISSION LOST — unmorphed ranger died");

		// Clear highlights and deselect after successful attack
		DeselectUnit();

		// Update turn indicator with new budget state
		UpdateTurnIndicator();

		// Auto-end turn if no actions remain
		if (budget.IsTurnComplete)
		{
			GD.Print("[C#] No actions remaining — auto-ending turn");
			EndCurrentUnitTurn();
		}
	}

	/// <summary>
	/// Update the turn indicator UI with the active unit's current budget.
	/// </summary>
	private void UpdateTurnIndicator()
	{
		if (Context.PhaseManager.ActiveUnit == null) return;
		string activeId = Context.PhaseManager.ActiveUnit.Participant.ParticipantId;
		if (Context.ActionBudgets.TryGetValue(activeId, out var b))
			_battleScene.CallDeferred("update_turn_indicator", activeId, b.CanMove, b.CanAct);
	}

	/// <summary>
	/// Initialize a unit's turn and update the UI. Uses CallDeferred to ensure
	/// GDScript @onready vars are initialized (parent _ready runs after child _ready).
	/// </summary>
	private void BeginAndShowUnitTurn(string unitId)
	{
		Context.BeginUnitTurn(unitId);

		if (Context.ActionBudgets.TryGetValue(unitId, out var budget))
		{
			// CallDeferred ensures the GDScript turn_indicator @onready var is set
			_battleScene.CallDeferred("update_turn_indicator", unitId, budget.CanMove, budget.CanAct);
			GD.Print($"  UI updated with budget: CanMove={budget.CanMove}, CanAct={budget.CanAct}");
		}
		else
		{
			GD.Print($"  WARNING: No ActionBudget found for {unitId}");
		}
	}

	/// <summary>
	/// End the current unit's turn and advance to the next one.
	/// </summary>
	public void EndCurrentUnitTurn()
	{
		GD.Print("[C#] EndCurrentUnitTurn called");

		// Get the active unit from PhaseManager
		if (Context.PhaseManager.ActiveUnit == null)
		{
			GD.Print("[C#] No active unit, cannot end turn");
			return;
		}

		string activeUnitId = Context.PhaseManager.ActiveUnit.Participant.ParticipantId;
		GD.Print($"[C#] Ending turn for: {activeUnitId}");

		// Clear selection if unit was selected
		if (_selectedUnitId != null)
		{
			DeselectUnit();
		}

		// End current turn
		Context.PhaseManager.EndCurrentTurn();

		// Check if phase is complete
		if (Context.PhaseManager.IsPhaseComplete())
		{
			GD.Print("[C#] Player phase complete!");
			EndPlayerPhaseAndRunEnemies();
			return;
		}

		// Advance to next unit's turn
		var nextUnit = Context.PhaseManager.AdvanceTurn();
		if (nextUnit != null)
		{
			BeginAndShowUnitTurn(nextUnit.Participant.ParticipantId);
			GD.Print($"[C#] Next turn: {nextUnit.Participant.ParticipantId}");
		}
	}

	/// <summary>
	/// Transition from player phase → enemy phase → next round → player phase.
	/// Delegates to ExecutePhaseTransition command; orchestrator handles only UI.
	/// </summary>
	private void EndPlayerPhaseAndRunEnemies()
	{
		var result = ExecutePhaseTransition.Execute(
			Context.PhaseManager,
			beginUnitTurn: unitId => Context.BeginUnitTurn(unitId),
			processEnemyTurn: ProcessEnemyTurn
		);

		// Log phase transition
		GD.Print($"=== Enemy Phase: {result.EnemyTurnsProcessed.Count} enemies processed ===");

		if (result.MissionEnded)
		{
			GD.Print("[C#] Mission ended during phase transition");
			return;
		}

		GD.Print($"=== Round {result.RoundNumber} Started — {result.NextUnitId}'s turn ===");

		// UI update — presentation only
		_battleScene.CallDeferred("update_turn_indicator", result.NextUnitId, true, true);
	}

	// === Enemy AI ===

	/// <summary>
	/// Process a single enemy's turn using the ResolveEnemyTurn command.
	/// Called by ExecutePhaseTransition for each enemy. Routes the move and
	/// attack through the same BCO commands the player uses, so the enemy's
	/// ActionBudget is validated and consumed consistently.
	/// </summary>
	private void ProcessEnemyTurn(string enemyId)
	{
		var enemy = Context.Enemies.FirstOrDefault(e => e.Id == enemyId);
		if (enemy == null || !enemy.IsAlive) return;

		if (!Context.ActionBudgets.TryGetValue(enemyId, out var budget))
		{
			GD.Print($"[C#] Enemy {enemyId}: no action budget");
			return;
		}

		// Collect alive ranger IDs
		var rangerIds = new HashSet<string>(
			Context.Rangers.Where(r => r.IsAlive).Select(r => r.Id));

		// Resolve what the enemy should do
		var decision = ResolveEnemyTurn.Execute(
			Context.Grid, enemyId,
			enemy.Data.MovementRange, enemy.Data.BasicAttackRange,
			rangerIds);

		if (decision.DidNothing)
		{
			GD.Print($"[C#] Enemy {enemyId}: no action available");
			return;
		}

		// Execute move via ExecuteMovement command (consumes ActionBudget.CanMove)
		if (decision.MoveDestination.HasValue)
		{
			var enemyPos = Context.Grid.GetUnitPosition(enemyId);
			if (enemyPos.HasValue)
			{
				var movementRange = Context.Grid.GetMovementRange(
					enemyPos.Value, enemy.Data.MovementRange);

				var moveResult = ExecuteMovement.Execute(
					unitId: enemyId,
					destination: decision.MoveDestination.Value,
					movementRange: movementRange,
					grid: Context.Grid,
					actionBudget: budget);

				if (moveResult.Success)
				{
					var dest = decision.MoveDestination.Value;
					_gridVisual.Call("move_unit", enemyId, new Vector2I(dest.Col, dest.Row));
					GD.Print($"[C#] Enemy {enemyId}: moved to ({dest.Col}, {dest.Row})");
				}
				else
				{
					GD.Print($"[C#] Enemy {enemyId}: move failed — {moveResult.FailureReason}");
				}
			}
		}

		// Execute attack via ExecuteAttack command (consumes ActionBudget.CanAct)
		if (decision.AttackTargetId != null)
		{
			var target = Context.Rangers.FirstOrDefault(r => r.Id == decision.AttackTargetId);
			if (target == null || !target.IsAlive) return;

			var attackerPos = Context.Grid.GetUnitPosition(enemyId);
			var targetPos = Context.Grid.GetUnitPosition(decision.AttackTargetId);
			if (!attackerPos.HasValue || !targetPos.HasValue) return;

			var attackResult = ExecuteAttack.Execute(
				attackerPos: attackerPos.Value,
				targetPos: targetPos.Value,
				weaponRange: enemy.Data.BasicAttackRange,
				actionBudget: budget,
				resolveCombat: () => Context.CombatResolver.ResolveEnemyAttack(
					enemy, target, enemy.Data.BasicAttackPower, null));

			if (!attackResult.Success)
			{
				GD.Print($"[C#] Enemy {enemyId}: attack failed — {attackResult.FailureReason}");
				return;
			}

			var combat = attackResult.CombatResult;
			GD.Print($"[C#] Enemy {enemyId}: attacked {decision.AttackTargetId} for {combat.TotalDamage} damage");

			if (combat.TargetDied)
			{
				GD.Print($"[C#] Ranger {decision.AttackTargetId} defeated!");
				_gridVisual.Call("remove_unit", decision.AttackTargetId);
			}

			if (combat.FormDied)
				GD.Print($"[C#] Form destroyed: {combat.LostFormId}");

			if (combat.MissionLost)
				GD.Print("[C#] MISSION LOST");
		}
	}
}
}
