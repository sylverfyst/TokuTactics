using Godot;
using System.Collections.Generic;

namespace TokuTactics.Presentation.Battle
{
	/// <summary>
	/// UI menu showing available actions for the current unit.
	/// </summary>
	public partial class ActionMenu : Control
	{
		private VBoxContainer _container;
		private List<Button> _buttons = new();
		private BattleController _controller;

		public override void _Ready()
		{
			// Position in bottom-right
			Position = new Vector2(900, 500);
			Size = new Vector2(200, 300);

			// Background panel
			var panel = new Panel();
			panel.Size = Size;
			AddChild(panel);

			// Container for buttons
			_container = new VBoxContainer();
			_container.Position = new Vector2(10, 10);
			AddChild(_container);

			// Get controller reference
			_controller = GetNode<BattleController>("/root/BattleScene/BattleController");

			// Start hidden
			Visible = false;
		}

		public void UpdateActions(List<string> actions)
		{
			// Clear existing buttons
			foreach (var button in _buttons)
			{
				button.QueueFree();
			}
			_buttons.Clear();

			// Create new buttons
			foreach (var action in actions)
			{
				var button = new Button();
				button.Text = action;
				button.CustomMinimumSize = new Vector2(180, 40);
				button.Pressed += () => OnActionPressed(action);
				_container.AddChild(button);
				_buttons.Add(button);
			}
		}

		private void OnActionPressed(string action)
		{
			_controller.OnActionSelected(action);
		}

		public new void Show()
		{
			Visible = true;
		}

		public new void Hide()
		{
			Visible = false;
		}
	}
}
