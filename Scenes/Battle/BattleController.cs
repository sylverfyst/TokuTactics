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
using TokuTactics.Commands.Loadout;
using TokuTactics.Bricks.Shared;
using TokuTactics.Core.ActionEconomy;
using TokuTactics.Core.Combat;
using TokuTactics.Core.Form;
using TokuTactics.Core.Stats;
using TokuTactics.Core.Types;
using TokuTactics.Entities.Rangers;

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

	// === Target Selection State ===
	private string _selectedTargetId = null;
	private GridPosition? _selectedTargetPos = null;

	// === Undo Move State ===
	private GridPosition? _preMovePosition = null;
	private bool _canUndoMove = false;

	// === Type Reveal State ===
	private HashSet<string> _revealedEnemyDataIds = new HashSet<string>();

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
			var campaignData = new CampaignData { FormBudget = 3 };

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

		private bool _missionEnded;

		private void OnMissionWon(MissionVictoryEvent evt)
		{
			_missionEnded = true;
			GD.Print($"=== MISSION VICTORY === (Round {evt.RoundsElapsed})");
			_battleScene.Call("hide_active_unit_panel");
			_battleScene.Call("hide_enemy_panel");
			_battleScene.Call("show_mission_result", "VICTORY", evt.RoundsElapsed);
		}

		private void OnMissionLost(MissionDefeatEvent evt)
		{
			_missionEnded = true;
			GD.Print($"=== MISSION DEFEAT === ({evt.FallenRangerId} fell, Round {evt.RoundsElapsed})");
			_battleScene.Call("hide_active_unit_panel");
			_battleScene.Call("hide_enemy_panel");
			_battleScene.Call("show_mission_result", "DEFEAT", evt.RoundsElapsed);
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
			if (_missionEnded) return;
			var clickedPos = new GridPosition(col, row);
			GD.Print($"[C#] Tile clicked: ({col}, {row})");

			var tile = Context.Grid.GetTile(clickedPos);
			var unitAtPosition = tile?.OccupantId;

			bool isEnemy = unitAtPosition != null &&
				Context.Enemies.Any(e => e.Id == unitAtPosition && e.IsAlive);
			bool isRanger = unitAtPosition != null &&
				Context.Rangers.Any(r => r.Id == unitAtPosition && r.IsAlive);

			if (_selectedUnitId == null)
			{
				// No ranger selected
				if (isRanger)
					SelectUnit(unitAtPosition, clickedPos);
				else if (isEnemy)
					SelectTarget(unitAtPosition, clickedPos);
			}
			else
			{
				// Ranger is selected
				if (clickedPos == _selectedUnitPosition)
				{
					// Clicked own unit — deselect
					DeselectTarget();
					DeselectUnit();
				}
				else if (isEnemy)
				{
					// Clicked enemy — select as target (show info + weapon buttons)
					SelectTarget(unitAtPosition, clickedPos);
				}
				else if (isRanger)
				{
					// Clicked another ranger — switch selection
					DeselectTarget();
					DeselectUnit();
					SelectUnit(unitAtPosition, clickedPos);
				}
				else
				{
					// Clicked empty tile
					DeselectTarget();

					if (_previewedDestination.HasValue && _previewedDestination.Value == clickedPos)
						ExecuteMove(clickedPos);
					else
						PreviewMovement(clickedPos);
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

		// Save pre-move position for undo
		var preMove = _selectedUnitPosition.Value;

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

		// Movement succeeded — enable undo
		_preMovePosition = preMove;
		_canUndoMove = true;

		GD.Print($"[C#] Moved {_selectedUnitId} from ({preMove.Col}, {preMove.Row}) to ({destination.Col}, {destination.Row})");

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
		PushActiveUnitPanel();
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

		var form = ranger.CurrentForm ?? ranger.BaseForm;
		int rangeA = form.Data.WeaponA?.Range ?? 0;
		int rangeB = form.Data.WeaponB?.Range ?? 0;
		int maxRange = System.Math.Max(rangeA, rangeB);

		GD.Print($"  Attack range: {maxRange} (WeaponA: {form.Data.WeaponA?.Name} r{rangeA}, WeaponB: {form.Data.WeaponB?.Name} r{rangeB})");

		var attackTiles = new Godot.Collections.Array();
		var tilesInRange = Context.Grid.GetTilesInRange(position, maxRange);
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

	// === Target Selection ===

	/// <summary>
	/// Select an enemy as a target, showing their info panel.
	/// If a ranger is selected and can attack, weapon buttons are included.
	/// </summary>
	private void SelectTarget(string targetId, GridPosition pos)
	{
		_selectedTargetId = targetId;
		_selectedTargetPos = pos;

		var enemy = Context.Enemies.FirstOrDefault(e => e.Id == targetId);
		if (enemy == null) return;

		// Build enemy info for GDScript
		string typeName = "???";
		bool typeRevealed = _revealedEnemyDataIds.Contains(enemy.Data.Id);
		if (typeRevealed && enemy.Data.Type.HasValue)
			typeName = enemy.Data.Type.Value.ToString();
		else if (typeRevealed || !enemy.Data.Type.HasValue)
			typeName = "Normal";

		var enemyInfo = new Godot.Collections.Dictionary
		{
			["id"] = enemy.Id,
			["name"] = enemy.Data.Name,
			["tier"] = enemy.Data.Tier.ToString(),
			["type"] = typeName,
			["type_revealed"] = typeRevealed,
			["hp_current"] = (int)enemy.Health.Current,
			["hp_max"] = (int)enemy.Health.Maximum,
		};

		// Check if active ranger can attack this target
		bool canAttack = false;
		if (_selectedUnitId != null && _selectedUnitPosition.HasValue)
		{
			var ranger = Context.Rangers.FirstOrDefault(r => r.Id == _selectedUnitId);
			if (ranger != null && Context.ActionBudgets.TryGetValue(_selectedUnitId, out var b) && b.CanAct)
			{
				var form = ranger.CurrentForm ?? ranger.BaseForm;
				int rangeA = form.Data.WeaponA?.Range ?? 0;
				int rangeB = form.Data.WeaponB?.Range ?? 0;
				int maxRange = System.Math.Max(rangeA, rangeB);
				int distance = _selectedUnitPosition.Value.ManhattanDistance(pos);
				canAttack = distance <= maxRange;

				if (canAttack)
				{
					enemyInfo["can_attack"] = true;
					if (form.Data.WeaponA != null)
					{
						bool weaponAInRange = distance <= form.Data.WeaponA.Range;
						enemyInfo["weapon_a_name"] = form.Data.WeaponA.Name;
						enemyInfo["weapon_a_power"] = form.Data.WeaponA.BasePower;
						enemyInfo["weapon_a_range"] = form.Data.WeaponA.Range;
						enemyInfo["weapon_a_status"] = form.Data.WeaponA.StatusEffect?.EffectId ?? "";
						enemyInfo["weapon_a_in_range"] = weaponAInRange;
					}
					if (form.Data.WeaponB != null)
					{
						bool weaponBInRange = distance <= form.Data.WeaponB.Range;
						enemyInfo["weapon_b_name"] = form.Data.WeaponB.Name;
						enemyInfo["weapon_b_power"] = form.Data.WeaponB.BasePower;
						enemyInfo["weapon_b_range"] = form.Data.WeaponB.Range;
						enemyInfo["weapon_b_status"] = form.Data.WeaponB.StatusEffect?.EffectId ?? "";
						enemyInfo["weapon_b_in_range"] = weaponBInRange;
					}
				}
			}
		}

		if (!canAttack)
			enemyInfo["can_attack"] = false;

		_gridVisual.Call("highlight_active_unit", targetId);
		_battleScene.Call("show_enemy_panel", enemyInfo);
		GD.Print($"[C#] Target selected: {targetId}");
	}

	/// <summary>
	/// Deselect the current target and hide enemy info panel.
	/// </summary>
	private void DeselectTarget()
	{
		if (_selectedTargetId != null)
		{
			_gridVisual.Call("clear_unit_highlight");
			if (_selectedUnitId != null)
				_gridVisual.Call("highlight_active_unit", _selectedUnitId);
		}
		_selectedTargetId = null;
		_selectedTargetPos = null;
		_battleScene.Call("hide_enemy_panel");
	}

	/// <summary>
	/// Attack the selected target with the specified weapon slot.
	/// Called from GDScript when the player clicks a weapon button.
	/// </summary>
	public void AttackWithWeapon(string slot)
	{
		if (_missionEnded || _selectedUnitId == null || _selectedTargetId == null) return;

		var ranger = Context.Rangers.FirstOrDefault(r => r.Id == _selectedUnitId);
		if (ranger == null) return;
		var enemy = Context.Enemies.FirstOrDefault(e => e.Id == _selectedTargetId);
		if (enemy == null) return;

		if (!Context.ActionBudgets.TryGetValue(_selectedUnitId, out var budget) || !budget.CanAct)
			return;

		var form = ranger.CurrentForm ?? ranger.BaseForm;
		var weapon = slot == "B" ? form.Data.WeaponB : form.Data.WeaponA;
		if (weapon == null) return;

		if (!_selectedUnitPosition.HasValue || !_selectedTargetPos.HasValue) return;
		var targetPos = _selectedTargetPos.Value;

		// Execute attack
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
			return;
		}

		// Reveal enemy type on first attack
		if (!_revealedEnemyDataIds.Contains(enemy.Data.Id))
		{
			_revealedEnemyDataIds.Add(enemy.Data.Id);
			GD.Print($"[C#] Enemy type revealed: {enemy.Data.Name} is {enemy.Data.Type?.ToString() ?? "Normal"}");
		}

		// Can't undo move after attacking
		_canUndoMove = false;
		_preMovePosition = null;

		var combat = result.CombatResult;
		GD.Print($"[C#] Attack with {weapon.Name}! Damage: {combat.TotalDamage}");

		// Show effectiveness text
		var matchup = combat.PrimaryDamage?.Matchup;
		if (matchup.HasValue && matchup.Value != MatchupResult.Neutral)
		{
			string effectText = matchup.Value == MatchupResult.Strong ? "Super Effective!" :
							   matchup.Value == MatchupResult.DoubleStrong ? "Super Effective!!" :
							   matchup.Value == MatchupResult.Weak ? "Not Very Effective..." :
							   matchup.Value == MatchupResult.DoubleWeak ? "Not Very Effective..." : "";
			bool isSuperEffective = matchup.Value == MatchupResult.Strong || matchup.Value == MatchupResult.DoubleStrong;
			if (effectText != "")
				_battleScene.Call("show_effectiveness_text", effectText, isSuperEffective);
		}

		// Handle death
		if (combat.TargetDied)
		{
			GD.Print($"[C#] Enemy {_selectedTargetId} defeated!");
			Context.Grid.RemoveUnit(_selectedTargetId);
			_gridVisual.Call("remove_unit", _selectedTargetId);
			Context.PhaseManager.CheckWinLoss();
		}

		if (combat.FormDied)
			GD.Print($"[C#] Form destroyed: {combat.LostFormId}");

		if (combat.MissionLost)
			GD.Print("[C#] MISSION LOST — unmorphed ranger died");

		// Deselect target and unit
		DeselectTarget();
		DeselectUnit();

		UpdateTurnIndicator();
		PushActiveUnitPanel();

		if (budget.IsTurnComplete)
		{
			GD.Print("[C#] No actions remaining — auto-ending turn");
			EndCurrentUnitTurn();
		}
	}

	/// <summary>
	/// Undo the last move. Called from GDScript when the player presses U.
	/// Only available before attacking or switching forms.
	/// </summary>
	public void UndoMove()
	{
		if (_missionEnded || !_canUndoMove || _preMovePosition == null || _selectedUnitId == null) return;

		string unitId = _selectedUnitId;
		var originalPos = _preMovePosition.Value;

		// Undo grid move
		Context.Grid.MoveUnit(unitId, originalPos);

		// Restore movement budget
		if (Context.ActionBudgets.TryGetValue(unitId, out var budget))
			budget.CanMove = true;

		// Update visual
		_gridVisual.Call("move_unit", unitId, new Vector2I(originalPos.Col, originalPos.Row));

		// Clear state
		DeselectTarget();
		DeselectUnit();

		_canUndoMove = false;
		_preMovePosition = null;

		// Re-select at original position
		SelectUnit(unitId, originalPos);

		GD.Print($"[C#] Move undone — {unitId} returned to ({originalPos.Col}, {originalPos.Row})");
		UpdateTurnIndicator();
		PushActiveUnitPanel();
	}

	/// <summary>
	/// Update the turn indicator UI with the active unit's current budget and morph state.
	/// </summary>
	private void UpdateTurnIndicator()
	{
		if (Context.PhaseManager.ActiveUnit == null) return;
		string activeId = Context.PhaseManager.ActiveUnit.Participant.ParticipantId;
		if (!Context.ActionBudgets.TryGetValue(activeId, out var b)) return;

		var ranger = Context.Rangers.FirstOrDefault(r => r.Id == activeId);
		string formName = ranger?.CurrentForm?.Data.Name ?? "";
		bool canMorph = ranger != null && ranger.MorphState == MorphState.Unmorphed && b.CanAct;
		bool canSwitch = ranger != null && ranger.MorphState == MorphState.Morphed && b.CanFormSwitch;

		_battleScene.CallDeferred("update_turn_indicator",
			activeId, b.CanMove, b.CanAct, formName, canMorph, canSwitch, _canUndoMove);
	}

	/// <summary>
	/// Initialize a unit's turn and update the UI. Uses CallDeferred to ensure
	/// GDScript @onready vars are initialized (parent _ready runs after child _ready).
	/// </summary>
	private void BeginAndShowUnitTurn(string unitId)
	{
		// Clear undo state from previous turn
		_canUndoMove = false;
		_preMovePosition = null;

		Context.BeginUnitTurn(unitId);

		if (Context.ActionBudgets.TryGetValue(unitId, out var budget))
		{
			var ranger = Context.Rangers.FirstOrDefault(r => r.Id == unitId);
			string formName = ranger?.CurrentForm?.Data.Name ?? "";
			bool canMorph = ranger != null && ranger.MorphState == MorphState.Unmorphed && budget.CanAct;
			bool canSwitch = ranger != null && ranger.MorphState == MorphState.Morphed && budget.CanFormSwitch;

			_battleScene.CallDeferred("update_turn_indicator",
				unitId, budget.CanMove, budget.CanAct, formName, canMorph, canSwitch, false);
			GD.Print($"  UI updated: CanMove={budget.CanMove}, CanAct={budget.CanAct}, Form={formName}");
		}
		else
		{
			GD.Print($"  WARNING: No ActionBudget found for {unitId}");
		}

		PushActiveUnitPanel();
	}

	/// <summary>
	/// Push the active unit's info to the GDScript active unit panel.
	/// </summary>
	private void PushActiveUnitPanel()
	{
		var activeId = Context.PhaseManager.ActiveUnit?.Participant.ParticipantId;
		if (activeId == null)
		{
			_battleScene.Call("hide_active_unit_panel");
			return;
		}
		var ranger = Context.Rangers.FirstOrDefault(r => r.Id == activeId);
		if (ranger == null)
		{
			_battleScene.Call("hide_active_unit_panel");
			return;
		}

		var form = ranger.CurrentForm ?? ranger.BaseForm;
		var pool = ranger.ActiveHealthPool;
		Context.ActionBudgets.TryGetValue(activeId, out var budget);

		var data = new Godot.Collections.Dictionary
		{
			["id"] = activeId,
			["name"] = ranger.Name,
			["form_name"] = form?.Data.Name ?? "Unmorphed",
			["form_type"] = form?.Data.Type.ToString() ?? "Normal",
			["hp_current"] = (int)pool.Current,
			["hp_max"] = (int)pool.Maximum,
			["can_act"] = budget?.CanAct ?? false,
			["can_move"] = budget?.CanMove ?? false,
			["can_undo"] = _canUndoMove,
		};

		if (form?.Data.WeaponA != null)
		{
			var w = form.Data.WeaponA;
			data["weapon_a_name"] = w.Name;
			data["weapon_a_power"] = w.BasePower;
			data["weapon_a_range"] = w.Range;
			data["weapon_a_status"] = w.StatusEffect?.EffectId ?? "";
		}
		if (form?.Data.WeaponB != null)
		{
			var w = form.Data.WeaponB;
			data["weapon_b_name"] = w.Name;
			data["weapon_b_power"] = w.BasePower;
			data["weapon_b_range"] = w.Range;
			data["weapon_b_status"] = w.StatusEffect?.EffectId ?? "";
		}

		_battleScene.Call("update_active_unit_panel", data);
	}

	/// <summary>
	/// Push current HP from a unit's active health pool to its visual bar.
	/// </summary>
	private void PushUnitHealthBar(string unitId)
	{
		var ranger = Context.Rangers.FirstOrDefault(r => r.Id == unitId);
		if (ranger != null)
		{
			var pool = ranger.ActiveHealthPool;
			_gridVisual.Call("update_health_bar", unitId, pool.Current, pool.Maximum);
			return;
		}
		var enemy = Context.Enemies.FirstOrDefault(e => e.Id == unitId);
		if (enemy != null)
			_gridVisual.Call("update_health_bar", unitId, enemy.Health.Current, enemy.Health.Maximum);
	}

	/// <summary>
	/// End the current unit's turn and advance to the next one.
	/// </summary>
	public void EndCurrentUnitTurn()
	{
		if (_missionEnded) return;
		GD.Print("[C#] EndCurrentUnitTurn called");

		// Clear undo and target state
		_canUndoMove = false;
		_preMovePosition = null;
		DeselectTarget();
		_battleScene.Call("hide_active_unit_panel");

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

		// UI update — presentation only, include morph state
		var nextRanger = Context.Rangers.FirstOrDefault(r => r.Id == result.NextUnitId);
		string nextFormName = nextRanger?.CurrentForm?.Data.Name ?? "";
		bool nextCanMorph = nextRanger != null && nextRanger.MorphState == MorphState.Unmorphed;
		bool nextCanSwitch = nextRanger != null && nextRanger.MorphState == MorphState.Morphed;
		_battleScene.CallDeferred("update_turn_indicator",
			result.NextUnitId, true, true, nextFormName, nextCanMorph, nextCanSwitch, false);
	}

	// === Morph / Form Switch ===

	/// <summary>
	/// Called from GDScript when the player presses M. Gates on loadout,
	/// opens the loadout panel on first use, then morphs the active ranger.
	/// </summary>
	public void OnMorphPressed()
	{
		if (_missionEnded || _loadoutPanelOpen || _formSwitchPanelOpen) return;

		var activeId = Context.PhaseManager.ActiveUnit?.Participant.ParticipantId;
		if (activeId == null) return;

		var ranger = Context.Rangers.FirstOrDefault(r => r.Id == activeId);
		if (ranger == null || ranger.MorphState != MorphState.Unmorphed) return;

		if (!Context.ActionBudgets.TryGetValue(activeId, out var budget) || !budget.CanAct)
		{
			GD.Print("[C#] Cannot morph — no action available");
			return;
		}

		var result = Context.LoadoutController.RequestMorph(ranger);
		switch (result)
		{
			case MorphRequestResult.NeedsLoadout:
				ShowLoadoutPanel();
				break;
			case MorphRequestResult.MorphComplete:
				CompleteMorph(ranger);
				break;
			default:
				GD.Print($"[C#] Morph failed: {result}");
				break;
		}
	}

	private bool _loadoutPanelOpen;

	private void ShowLoadoutPanel()
	{
		var screenData = Context.LoadoutController.GetLoadoutScreenData();
		var formList = new Godot.Collections.Array();
		foreach (var form in screenData.AvailableForms)
		{
			formList.Add(new Godot.Collections.Dictionary
			{
				["id"] = form.FormData.Id,
				["name"] = form.FormData.Name,
				["type"] = form.TypeName,
				["hp"] = (int)form.FormData.BaseHealth,
				["range"] = form.FormData.BasicAttackRange,
				["movement"] = form.FormData.MovementRange,
			});
		}
		_loadoutPanelOpen = true;
		_battleScene.Call("show_loadout_panel", formList, screenData.Budget);
		GD.Print($"[C#] Loadout panel opened — {screenData.AvailableForms.Count} forms, budget {screenData.Budget}");
	}

	/// <summary>
	/// Called from GDScript when the player confirms a loadout selection.
	/// </summary>
	public void OnLoadoutSubmitted(Godot.Collections.Array formIds)
	{
		var selectedIds = new List<string>();
		foreach (var id in formIds)
			selectedIds.Add(id.ToString());

		var result = Context.LoadoutController.SubmitLoadout(selectedIds);
		if (result != LoadoutResult.Accepted)
		{
			GD.Print($"[C#] Loadout rejected: {result}");
			return;
		}

		_loadoutPanelOpen = false;
		_battleScene.Call("hide_loadout_panel");
		GD.Print($"[C#] Loadout accepted: [{string.Join(", ", selectedIds)}]");

		// Re-attempt morph for the triggering ranger
		var triggeringId = Context.LoadoutController.TriggeringRangerId;
		var ranger = Context.Rangers.FirstOrDefault(r => r.Id == triggeringId);
		if (ranger != null)
		{
			var morphResult = Context.LoadoutController.RequestMorph(ranger);
			if (morphResult == MorphRequestResult.MorphComplete)
				CompleteMorph(ranger);
		}
	}

	/// <summary>
	/// Called from GDScript when the player cancels the loadout panel.
	/// </summary>
	public void OnLoadoutCancelled()
	{
		_loadoutPanelOpen = false;
		_battleScene.Call("hide_loadout_panel");
		GD.Print("[C#] Loadout cancelled");
	}

	private void CompleteMorph(Ranger ranger)
	{
		// Clear undo state — morph ends the turn
		_canUndoMove = false;
		_preMovePosition = null;

		// Consume morph action (ends turn: CanMove, CanAct, CanFormSwitch all false)
		if (Context.ActionBudgets.TryGetValue(ranger.Id, out var budget))
			ConsumeMorphAction.Execute(budget);

		// Occupy base form in the pool
		Context.FormPool.OccupyForm(ranger.CurrentForm.Data.Id, ranger.Id);

		// Update health bar (now showing form HP pool)
		PushUnitHealthBar(ranger.Id);
		UpdateTurnIndicator();

		GD.Print($"[C#] {ranger.Id} morphed into {ranger.CurrentForm?.Data.Name}");

		// Auto-end turn since morph consumes the full turn
		EndCurrentUnitTurn();
	}

	/// <summary>
	/// Called from GDScript when the player presses F. Opens the form switch panel
	/// showing equipped forms with their availability status.
	/// </summary>
	public void OnFormSwitchPressed()
	{
		if (_missionEnded || _loadoutPanelOpen || _formSwitchPanelOpen) return;

		var activeId = Context.PhaseManager.ActiveUnit?.Participant.ParticipantId;
		if (activeId == null) return;

		var ranger = Context.Rangers.FirstOrDefault(r => r.Id == activeId);
		if (ranger == null || ranger.MorphState != MorphState.Morphed) return;

		if (!Context.ActionBudgets.TryGetValue(activeId, out var budget) || !budget.CanFormSwitch)
		{
			GD.Print("[C#] Cannot form switch this turn");
			return;
		}

		ShowFormSwitchPanel(ranger);
	}

	private bool _formSwitchPanelOpen;

	private void ShowFormSwitchPanel(Ranger ranger)
	{
		var formList = new Godot.Collections.Array();

		// Always include base form (if not already in it)
		if (ranger.CurrentForm?.Data.Id != Context.FormPool.BaseFormId)
		{
			formList.Add(new Godot.Collections.Dictionary
			{
				["id"] = Context.FormPool.BaseFormId,
				["name"] = "Base Form",
				["type"] = "Normal",
				["available"] = true,
				["status"] = "Available",
				["is_current"] = false,
			});
		}

		// Equipped non-base forms
		foreach (var entry in Context.FormPool.GetPoolStatus())
		{
			if (entry.FormData.Id == Context.FormPool.BaseFormId) continue;
			if (!entry.IsEquipped) continue;

			var avail = Context.FormPool.CheckAvailability(entry.FormData.Id, ranger.Id);
			bool isCurrent = ranger.CurrentForm?.Data.Id == entry.FormData.Id;

			formList.Add(new Godot.Collections.Dictionary
			{
				["id"] = entry.FormData.Id,
				["name"] = entry.FormData.Name,
				["type"] = entry.FormData.Type.ToString(),
				["available"] = avail == FormAvailability.Available && !isCurrent,
				["status"] = isCurrent ? "Current" : avail.ToString(),
				["is_current"] = isCurrent,
			});
		}

		_formSwitchPanelOpen = true;
		_battleScene.Call("show_form_switch_panel", formList);
		GD.Print($"[C#] Form switch panel opened — {formList.Count} forms");
	}

	/// <summary>
	/// Called from GDScript when the player picks a form to switch to.
	/// </summary>
	public void OnFormSwitchSelected(string formId)
	{
		var activeId = Context.PhaseManager.ActiveUnit?.Participant.ParticipantId;
		if (activeId == null) return;

		var ranger = Context.Rangers.FirstOrDefault(r => r.Id == activeId);
		if (ranger == null) return;

		// Get the form data and instance
		var formData = Context.ContentRegistry.GetForm(formId);
		if (formData == null)
		{
			GD.Print($"[C#] Unknown form ID: {formId}");
			return;
		}

		var newFormInstance = ranger.GetOrCreateFormInstance(formData);

		// Execute switch — returns the previous form
		var previousForm = ranger.SwitchForm(newFormInstance);
		if (previousForm == null)
		{
			GD.Print("[C#] SwitchForm returned null — invalid switch");
			return;
		}

		// Clear undo state — form switch resets the action economy
		_canUndoMove = false;
		_preMovePosition = null;

		// Vacate old form (activates cooldown), occupy new form
		int magModifier = (int)(ranger.Stats.Get(StatType.MAG) / 5.0f);
		Context.FormPool.VacateForm(previousForm.Data.Id, activeId, magModifier);
		Context.FormPool.OccupyForm(formId, activeId);

		// Reset action budget — form switch is free and grants new move + act
		if (Context.ActionBudgets.TryGetValue(activeId, out var budget))
			ResetBudgetFromFormSwitch.Execute(budget);

		// Close panel, update visuals
		_formSwitchPanelOpen = false;
		_battleScene.Call("hide_form_switch_panel");
		PushUnitHealthBar(activeId);

		// Clear old highlights before re-selecting with new form stats
		if (_selectedUnitId != null) DeselectUnit();

		UpdateTurnIndicator();
		PushActiveUnitPanel();

		GD.Print($"[C#] {activeId} switched from {previousForm.Data.Name} to {formData.Name}");

		// Auto-select the ranger so movement/attack range shows immediately
		var pos = Context.Grid.GetUnitPosition(activeId);
		if (pos.HasValue)
			SelectUnit(activeId, pos.Value);
	}

	/// <summary>
	/// Called from GDScript when the player cancels the form switch panel.
	/// </summary>
	public void OnFormSwitchCancelled()
	{
		_formSwitchPanelOpen = false;
		_battleScene.Call("hide_form_switch_panel");
		GD.Print("[C#] Form switch cancelled");
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
				Context.Grid.RemoveUnit(decision.AttackTargetId);
				_gridVisual.Call("remove_unit", decision.AttackTargetId);
				Context.PhaseManager.CheckWinLoss();
			}

			if (combat.FormDied)
				GD.Print($"[C#] Form destroyed: {combat.LostFormId}");
		}
	}
}
}
