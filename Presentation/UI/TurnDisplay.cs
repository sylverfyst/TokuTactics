using Godot;
using TokuTactics.Systems.PhaseManagement;

namespace TokuTactics.Presentation.Battle
{
	/// <summary>
	/// UI display showing current phase and active unit.
	/// </summary>
	public partial class TurnDisplay : Control
	{
		private Label _phaseLabel;
		private Label _unitLabel;
		private Label _statusLabel;

		public override void _Ready()
		{
			// Create UI elements
			var vbox = new VBoxContainer();
			vbox.Position = new Vector2(10, 10);
			AddChild(vbox);

			_phaseLabel = new Label();
			_phaseLabel.AddThemeFontSizeOverride("font_size", 24);
			_phaseLabel.AddThemeColorOverride("font_color", Colors.White);
			vbox.AddChild(_phaseLabel);

			_unitLabel = new Label();
			_unitLabel.AddThemeFontSizeOverride("font_size", 18);
			_unitLabel.AddThemeColorOverride("font_color", Colors.LightGray);
			vbox.AddChild(_unitLabel);

			_statusLabel = new Label();
			_statusLabel.AddThemeFontSizeOverride("font_size", 18);
			_statusLabel.AddThemeColorOverride("font_color", Colors.Yellow);
			_statusLabel.Visible = false;
			vbox.AddChild(_statusLabel);
		}

		public void UpdateDisplay(string phase, string unitId)
		{
			_phaseLabel.Text = $"Phase: {phase}";

			if (unitId != null)
			{
				_unitLabel.Text = $"Turn: {FormatUnitName(unitId)}";
				_unitLabel.Visible = true;
			}
			else
			{
				_unitLabel.Visible = false;
			}

			_statusLabel.Visible = false;
		}

		public void ShowVictory()
		{
			_statusLabel.Text = "VICTORY!";
			_statusLabel.AddThemeColorOverride("font_color", Colors.Green);
			_statusLabel.Visible = true;
		}

		public void ShowDefeat(string reason)
		{
			_statusLabel.Text = $"DEFEAT: {reason}";
			_statusLabel.AddThemeColorOverride("font_color", Colors.Red);
			_statusLabel.Visible = true;
		}

		private string FormatUnitName(string unitId)
		{
			if (unitId.StartsWith("ranger_"))
				return unitId.Replace("ranger_", "").Replace("_", " ").ToUpper();
			if (unitId.StartsWith("enemy_"))
				return unitId.Replace("enemy_", "Enemy ").Replace("_", " ");
			return unitId;
		}
	}
}
