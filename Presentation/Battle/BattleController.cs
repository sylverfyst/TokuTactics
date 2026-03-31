using Godot;
using System;
using System.Linq;
using TokuTactics.Data.Content;
using TokuTactics.Systems.MissionSetup;
using TokuTactics.Systems.SaveLoad;
using TokuTactics.Systems.PhaseManagement;
using TokuTactics.Core.Events;

namespace TokuTactics.Presentation.Battle
{
	/// <summary>
	/// Main battle coordinator - owns MissionContext and orchestrates the battle flow.
	/// Connects game logic to presentation layer.
	/// </summary>
	public partial class BattleController : Node
	{
		// === References ===

		private GridView _gridView;
		private TurnDisplay _turnDisplay;
		private ActionMenu _actionMenu;

		// === Game Logic ===

		public MissionContext Context { get; private set; }

		// === State ===

		private string _selectedUnitId;

		// === Lifecycle ===

		public override void _Ready()
		{
			GD.Print("=== Battle Controller Starting ===");

			// Get child node references
			_gridView = GetNode<GridView>("GridView");
			_turnDisplay = GetNode<TurnDisplay>("UI/TurnDisplay");
			_actionMenu = GetNode<ActionMenu>("UI/ActionMenu");

			// Initialize mission
			InitializeMission();

			// Subscribe to events
			Context.EventBus.Subscribe<UnitTurnStartedEvent>(OnTurnStarted, EventPriority.Presentation);
			Context.EventBus.Subscribe<UnitTurnEndedEvent>(OnTurnEnded, EventPriority.Presentation);
			Context.EventBus.Subscribe<PhaseTransitionEvent>(OnPhaseChanged, EventPriority.Presentation);
			Context.EventBus.Subscribe<MissionVictoryEvent>(OnMissionWon, EventPriority.Presentation);
			Context.EventBus.Subscribe<MissionDefeatEvent>(OnMissionLost, EventPriority.Presentation);

			// Start the battle
			StartBattle();
		}

		private void InitializeMission()
		{
			GD.Print("Initializing mission...");

			var registry = ContentRegistry.CreateVerticalSlice();
			var episode = registry.GetEpisode("episode_frozen_outpost");
			var campaignData = new CampaignData();

			Context = MissionContext.Create(episode, campaignData, registry);

			GD.Print($"✓ Mission initialized: {episode.Title}");
			GD.Print($"  - Grid: {Context.Grid.Width}x{Context.Grid.Height}");
			GD.Print($"  - Rangers: {Context.Rangers.Count}");
			GD.Print($"  - Enemies: {Context.Enemies.Count}");
		}

		private void StartBattle()
		{
			GD.Print("");
			GD.Print("=== Starting Battle ===");

			// Initialize grid view
			_gridView.Initialize(Context);

			// Start mission (begins first round)
			Context.StartMission();

			// Update UI
			UpdateTurnDisplay();
		}

		// === Turn Flow ===

		private void OnTurnStarted(UnitTurnStartedEvent evt)
		{
			GD.Print($"Turn started: {evt.UnitId}");
			UpdateTurnDisplay();

			// If it's a player Ranger's turn, enable input
			if (Context.RangerLookup.ContainsKey(evt.UnitId))
			{
				_selectedUnitId = evt.UnitId;
				_gridView.HighlightUnit(evt.UnitId);
				UpdateActionMenu();
			}
		}

		private void OnTurnEnded(UnitTurnEndedEvent evt)
		{
			GD.Print($"Turn ended: {evt.UnitId}");
			_selectedUnitId = null;
			_gridView.ClearHighlights();
			_actionMenu.Hide();
		}

		private void OnPhaseChanged(PhaseTransitionEvent evt)
		{
			GD.Print($"Phase changed: {evt.FromPhaseId} → {evt.ToPhaseId}");
			UpdateTurnDisplay();

			// If entering enemy phase, run AI
			if (evt.ToPhaseId == "enemy")
			{
				RunEnemyPhase();
			}
		}

		private void OnMissionWon(MissionVictoryEvent evt)
		{
			GD.Print("");
			GD.Print("=== MISSION WON ===");
			GD.Print("");
			_turnDisplay.ShowVictory();
		}

		private void OnMissionLost(MissionDefeatEvent evt)
		{
			GD.Print("");
			GD.Print("=== MISSION LOST ===");
			GD.Print($"Reason: {evt.FallenRangerId}");
			GD.Print("");
			_turnDisplay.ShowDefeat(evt.FallenRangerId);
		}

		// === Enemy AI ===

		private void RunEnemyPhase()
		{
			GD.Print("Running enemy phase (AI placeholder)...");

			// For now, just skip enemy turns
			// TODO: Implement proper AI behavior tree execution
			while (Context.PhaseManager.PhaseState == PhaseState.EnemyPhase)
			{
				var currentUnit = Context.PhaseManager.ActiveUnit;
				if (currentUnit == null) break;

				GD.Print($"  - Skipping enemy {currentUnit.Participant.ParticipantId} turn");
				Context.PhaseManager.EndCurrentTurn();
			}
		}

		// === UI Updates ===

		private void UpdateTurnDisplay()
		{
			var phaseState = Context.PhaseManager.PhaseState;
			var phase = phaseState switch
			{
				PhaseState.PlayerPhase => "Player Phase",
				PhaseState.EnemyPhase => "Enemy Phase",
				_ => "Idle"
			};
			var currentUnit = Context.PhaseManager.ActiveUnit;

			if (currentUnit != null)
			{
				_turnDisplay.UpdateDisplay(phase, currentUnit.Participant.ParticipantId);
			}
			else
			{
				_turnDisplay.UpdateDisplay(phase, null);
			}
		}

		private void UpdateActionMenu()
		{
			if (_selectedUnitId == null)
			{
				_actionMenu.Hide();
				return;
			}

			var ranger = Context.RangerLookup[_selectedUnitId];
			var actions = new System.Collections.Generic.List<string>();

			// Basic attack always available
			actions.Add("Basic Attack");

			// Add other actions based on ranger state
			if (ranger.CurrentForm?.Data.WeaponA != null)
			{
				actions.Add("Weapon Attack");
			}

			actions.Add("End Turn");

			_actionMenu.Show();
			_actionMenu.UpdateActions(actions);
		}

		// === Input Handling ===

		public void OnActionSelected(string action)
		{
			GD.Print($"Action selected: {action}");

			switch (action)
			{
				case "Basic Attack":
					EnterTargetingMode(isWeapon: false);
					break;
				case "Weapon Attack":
					EnterTargetingMode(isWeapon: true);
					break;
				case "End Turn":
					EndCurrentTurn();
					break;
			}
		}

		private void EnterTargetingMode(bool isWeapon)
		{
			GD.Print($"Entering targeting mode (weapon: {isWeapon})");

			// Highlight enemy positions
			var enemies = Context.Enemies.Where(e => e.IsAlive).ToList();
			_gridView.HighlightTargets(enemies.Select(e => e.Id).ToList());

			// TODO: Wait for player to click a target
			// For now, just attack the first enemy
			if (enemies.Count > 0)
			{
				ExecuteAttack(enemies[0].Id, isWeapon);
			}
		}

		private void ExecuteAttack(string targetId, bool isWeapon)
		{
			GD.Print($"Executing attack: {_selectedUnitId} → {targetId} (weapon: {isWeapon})");

			var ranger = Context.RangerLookup[_selectedUnitId];
			var enemy = Context.EnemyLookup[targetId];

			// Get attack power
			float power = isWeapon && ranger.CurrentForm?.Data.WeaponA != null
				? ranger.CurrentForm.Data.WeaponA.BasePower
				: 10.0f; // Basic attack power

			// Build assist states
			var assistStates = Context.BuildAssistStates();

			// Execute attack via CombatResolver
			var result = Context.CombatResolver.ResolveRangerAttack(
				ranger,
				enemy,
				power,
				isWeapon && ranger.CurrentForm?.Data.WeaponA != null ? ranger.CurrentForm.Data.WeaponA.StatusEffect : null,
				assistStates
			);

			GD.Print($"  → Damage: {result.PrimaryDamage}, Target died: {result.TargetDied}");

			// Update visuals
			_gridView.UpdateUnits();

			// Check win/loss
			Context.PhaseManager.CheckWinLoss();

			// Clear highlights
			_gridView.ClearHighlights();
		}

		private void EndCurrentTurn()
		{
			GD.Print("Ending current turn...");
			Context.PhaseManager.EndCurrentTurn();
		}
	}
}
