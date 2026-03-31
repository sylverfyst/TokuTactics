using Godot;
using System.Collections.Generic;
using System.Linq;
using TokuTactics.Systems.MissionSetup;
using TokuTactics.Core.Grid;

namespace TokuTactics.Presentation.Battle
{
	/// <summary>
	/// Visual representation of the battle grid using isometric projection.
	/// Renders tiles and units, handles highlighting.
	/// </summary>
	public partial class GridView : Node2D
	{
		// === Configuration ===

		private const float TILE_WIDTH = 64f;  // Width of isometric diamond
		private const float TILE_HEIGHT = 32f; // Height of isometric diamond
		private const float UNIT_SIZE = 32f;

		// Offset to center the grid on screen
		private const float GRID_OFFSET_X = 400f;
		private const float GRID_OFFSET_Y = 100f;

		// === State ===

		private MissionContext _context;
		private Dictionary<GridPosition, TileSprite> _tiles = new();
		private Dictionary<string, UnitView> _units = new();

		// === Initialization ===

		public void Initialize(MissionContext context)
		{
			_context = context;
			GD.Print($"Initializing isometric GridView for {context.Grid.Width}x{context.Grid.Height} grid");

			CreateTiles();
			CreateUnits();
		}

		private void CreateTiles()
		{
			for (int row = 0; row < _context.Grid.Height; row++)
			{
				for (int col = 0; col < _context.Grid.Width; col++)
				{
					var pos = new GridPosition(col, row);
					var tile = _context.Grid.GetTile(pos);
					var worldPos = GridToWorld(pos);

					var tileSprite = new TileSprite(pos, tile, TILE_WIDTH, TILE_HEIGHT, worldPos);
					AddChild(tileSprite);
					_tiles[pos] = tileSprite;
				}
			}

			GD.Print($"Created {_tiles.Count} isometric tiles");
		}

		private void CreateUnits()
		{
			// Create Ranger units
			foreach (var ranger in _context.Rangers)
			{
				var position = _context.Grid.GetUnitPosition(ranger.Id);
				if (position.HasValue)
				{
					var unitView = new UnitView(ranger.Id, isRanger: true, UNIT_SIZE);
					unitView.Position = GridToWorld(position.Value);
					AddChild(unitView);
					_units[ranger.Id] = unitView;
				}
			}

			// Create Enemy units
			foreach (var enemy in _context.Enemies)
			{
				var position = _context.Grid.GetUnitPosition(enemy.Id);
				if (position.HasValue)
				{
					var unitView = new UnitView(enemy.Id, isRanger: false, UNIT_SIZE);
					unitView.Position = GridToWorld(position.Value);
					AddChild(unitView);
					_units[enemy.Id] = unitView;
				}
			}

			GD.Print($"Created {_units.Count} unit views");
		}

		// === Coordinate Conversion ===

		private Vector2 GridToWorld(GridPosition gridPos)
		{
			// Isometric projection formula:
			// x = (col - row) * (tile_width / 2)
			// y = (col + row) * (tile_height / 2)
			float x = (gridPos.Col - gridPos.Row) * (TILE_WIDTH / 2f) + GRID_OFFSET_X;
			float y = (gridPos.Col + gridPos.Row) * (TILE_HEIGHT / 2f) + GRID_OFFSET_Y;
			return new Vector2(x, y);
		}

		// === Highlighting ===

		public void HighlightUnit(string unitId)
		{
			ClearHighlights();

			var position = _context.Grid.GetUnitPosition(unitId);
			if (position.HasValue && _tiles.TryGetValue(position.Value, out var tile))
			{
				tile.SetHighlight(Colors.Yellow);
			}
		}

		public void HighlightTargets(List<string> targetIds)
		{
			ClearHighlights();

			foreach (var targetId in targetIds)
			{
				var position = _context.Grid.GetUnitPosition(targetId);
				if (position.HasValue && _tiles.TryGetValue(position.Value, out var tile))
				{
					tile.SetHighlight(Colors.Red);
				}
			}
		}

		public void ClearHighlights()
		{
			foreach (var tile in _tiles.Values)
			{
				tile.ClearHighlight();
			}
		}

		// === Updates ===

		public void UpdateUnits()
		{
			// Remove dead units
			var deadUnits = _units.Where(kvp =>
			{
				if (_context.RangerLookup.TryGetValue(kvp.Key, out var ranger))
					return !ranger.IsAlive;
				if (_context.EnemyLookup.TryGetValue(kvp.Key, out var enemy))
					return !enemy.IsAlive;
				return false;
			}).ToList();

			foreach (var kvp in deadUnits)
			{
				kvp.Value.QueueFree();
				_units.Remove(kvp.Key);
			}

			if (deadUnits.Count > 0)
			{
				GD.Print($"Removed {deadUnits.Count} dead unit(s)");
			}
		}
	}

	/// <summary>
	/// Individual isometric tile sprite in the grid.
	/// </summary>
	public partial class TileSprite : Node2D
	{
		private Polygon2D _diamond;
		private Polygon2D _highlight;
		private readonly GridPosition _position;

		public TileSprite(GridPosition position, Tile tile, float tileWidth, float tileHeight, Vector2 worldPos)
		{
			_position = position;
			Position = worldPos;

			// Create diamond shape points (isometric tile)
			var points = new Vector2[]
			{
				new Vector2(0, -tileHeight / 2),        // Top
				new Vector2(tileWidth / 2, 0),          // Right
				new Vector2(0, tileHeight / 2),         // Bottom
				new Vector2(-tileWidth / 2, 0)          // Left
			};

			// Background diamond (checkerboard pattern)
			_diamond = new Polygon2D();
			_diamond.Polygon = points;
			_diamond.Color = (position.Col + position.Row) % 2 == 0
				? new Color(0.4f, 0.5f, 0.3f)      // Green-ish
				: new Color(0.3f, 0.4f, 0.25f);    // Darker green-ish
			AddChild(_diamond);

			// Border outline
			var border = new Line2D();
			border.AddPoint(points[0]);
			border.AddPoint(points[1]);
			border.AddPoint(points[2]);
			border.AddPoint(points[3]);
			border.AddPoint(points[0]); // Close the diamond
			border.Width = 1f;
			border.DefaultColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
			AddChild(border);

			// Highlight overlay (initially transparent)
			_highlight = new Polygon2D();
			_highlight.Polygon = points;
			_highlight.Color = Colors.Transparent;
			AddChild(_highlight);
		}

		public void SetHighlight(Color color)
		{
			_highlight.Color = new Color(color.R, color.G, color.B, 0.4f);
		}

		public void ClearHighlight()
		{
			_highlight.Color = Colors.Transparent;
		}
	}

	/// <summary>
	/// Individual unit sprite (Ranger or Enemy).
	/// </summary>
	public partial class UnitView : Node2D
	{
		private readonly string _unitId;
		private readonly bool _isRanger;
		private Polygon2D _sprite;
		private Label _label;

		public UnitView(string unitId, bool isRanger, float size)
		{
			_unitId = unitId;
			_isRanger = isRanger;

			// Create colored diamond sprite (slightly smaller than tile)
			var halfSize = size / 2f;
			var points = new Vector2[]
			{
				new Vector2(0, -halfSize),
				new Vector2(halfSize, 0),
				new Vector2(0, halfSize),
				new Vector2(-halfSize, 0)
			};

			_sprite = new Polygon2D();
			_sprite.Polygon = points;
			_sprite.Color = GetUnitColor(unitId, isRanger);
			AddChild(_sprite);

			// Add label
			_label = new Label();
			_label.Text = isRanger ? GetRangerInitial(unitId) : "E";
			_label.Position = new Vector2(-8, -12); // Centered on diamond
			_label.AddThemeColorOverride("font_color", Colors.White);
			_label.AddThemeFontSizeOverride("font_size", 16);
			AddChild(_label);
		}

		private string GetRangerInitial(string unitId)
		{
			if (unitId.Contains("red")) return "R";
			if (unitId.Contains("blue")) return "B";
			if (unitId.Contains("yellow")) return "Y";
			if (unitId.Contains("green")) return "G";
			if (unitId.Contains("pink")) return "P";
			return "?";
		}

		private Color GetUnitColor(string unitId, bool isRanger)
		{
			if (!isRanger)
				return new Color(0.3f, 0.3f, 0.3f); // Dark gray enemies

			// Ranger colors
			if (unitId.Contains("red")) return new Color(0.9f, 0.2f, 0.2f);
			if (unitId.Contains("blue")) return new Color(0.2f, 0.4f, 0.9f);
			if (unitId.Contains("yellow")) return new Color(0.9f, 0.9f, 0.2f);
			if (unitId.Contains("green")) return new Color(0.3f, 0.8f, 0.3f);
			if (unitId.Contains("pink")) return new Color(0.9f, 0.4f, 0.7f);

			return Colors.Purple; // Default
		}
	}
}
