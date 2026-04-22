extends TileMapLayer
## Visualizes the BattleGrid from C# using isometric tiles.
## Syncs with the C# BattleGrid data to display terrain, units, and overlays.

# Tile atlas coordinates for different terrain types
const TILE_FLOOR_BLUE = Vector2i(0, 0)
const TILE_FLOOR_GREEN = Vector2i(1, 0)
const TILE_FLOOR_LIME = Vector2i(2, 0)
const TILE_FLOOR_YELLOW = Vector2i(3, 0)
const TILE_FLOOR_BEIGE = Vector2i(4, 0)
const TILE_FLOOR_BROWN = Vector2i(5, 0)
const TILE_FLOOR_DARK_BROWN = Vector2i(6, 0)
const TILE_FLOOR_PURPLE = Vector2i(0, 1)
const TILE_FLOOR_RED = Vector2i(1, 1)
const TILE_FLOOR_TEAL = Vector2i(2, 1)
const TILE_FLOOR_OLIVE = Vector2i(3, 1)
const TILE_FLOOR_GRAY = Vector2i(4, 1)
const TILE_FLOOR_DARK_GRAY = Vector2i(5, 1)
const TILE_FLOOR_PINK = Vector2i(6, 1)

# Terrain type to tile mapping
var terrain_tiles = {
	"Floor": TILE_FLOOR_GRAY,
	"Wall": TILE_FLOOR_DARK_GRAY,
	"Difficult": TILE_FLOOR_BROWN,
	"Hazard": TILE_FLOOR_RED,
	"Pit": TILE_FLOOR_DARK_BROWN
}

func _ready():
	print("=== BattleGridVisual._ready() called ===")
	# Test grid drawing removed - will be populated by C# MissionContext
	# Units will be spawned via spawn_unit() calls from BattleController.cs

	# Uncomment to run tile diagnostics:
	# call_deferred("run_tile_diagnostics")

## Draws a test checkerboard pattern to verify tilemap works
func draw_test_grid(width: int, height: int):
	print("Starting draw_test_grid(", width, ", ", height, ")")
	var tile_count = 0
	for y in range(height):
		for x in range(width):
			var tile_coord = Vector2i(x, y)
			var atlas_coord = TILE_FLOOR_GREEN if (x + y) % 2 == 0 else TILE_FLOOR_BLUE
			set_cell(tile_coord, 0, atlas_coord)
			tile_count += 1
			# Print first few tiles for verification
			if tile_count <= 3:
				print("  Set tile ", tile_coord, " with atlas ", atlas_coord, " at source ID 0")
	print("Drew ", tile_count, " tiles from (0,0) to (", width-1, ",", height-1, ")")
	print("get_used_cells() count: ", get_used_cells().size())

## Syncs the visual grid with C# BattleGrid data
func sync_with_battle_grid(grid_data: Dictionary):
	clear()

	var width = grid_data.get("Width", 12)
	var height = grid_data.get("Height", 10)
	var tiles = grid_data.get("Tiles", [])

	for y in range(height):
		for x in range(width):
			var tile_index = y * width + x
			if tile_index >= tiles.size():
				continue

			var tile_data = tiles[tile_index]
			var terrain = tile_data.get("Terrain", "Floor")
			var _elevation = tile_data.get("Elevation", 0)  # TODO: Handle elevation with z_index

			var atlas_coord = terrain_tiles.get(terrain, TILE_FLOOR_GRAY)
			var tile_coord = Vector2i(x, y)

			set_cell(tile_coord, 0, atlas_coord)

			# TODO: Handle elevation with z_index or stacked tiles

# Backup of original tile atlas coords before highlighting
var _highlighted_tiles_backup = {}

## Highlights tiles for movement range, attack range, etc.
func highlight_tiles(positions: Array, color_key: String):
	# Use different tile colors for highlights:
	# - Movement: Yellow
	# - Attack: Red
	# - Assist: Green
	var highlight_tile = {
		"movement": TILE_FLOOR_YELLOW,
		"attack": TILE_FLOOR_RED,
		"assist": TILE_FLOOR_GREEN
	}.get(color_key, TILE_FLOOR_BLUE)

	for pos in positions:
		if pos is Vector2i:
			# Backup original tile before overwriting
			if not _highlighted_tiles_backup.has(pos):
				var original_atlas = get_cell_atlas_coords(pos)
				_highlighted_tiles_backup[pos] = original_atlas
			set_cell(pos, 0, highlight_tile)

## Clears all highlights, restoring original terrain
func clear_highlights():
	# Restore all backed-up tiles
	for pos in _highlighted_tiles_backup.keys():
		var original_atlas = _highlighted_tiles_backup[pos]
		set_cell(pos, 0, original_atlas)

	# Clear the backup dictionary
	_highlighted_tiles_backup.clear()

## ===== UNIT DISPLAY =====

# Unit sprites dictionary: unit_id -> Sprite2D
var unit_sprites = {}
# Authoritative grid positions: unit_id -> Vector2i (set on spawn/move)
var unit_grid_positions = {}

# Character sprite sheet (8-directional idle sprites, clean and simple)
const SPRITE_SHEET_PATH = "res://Assets/Sprites/idle animation colors.png"
# Sprite sheet is 124x132 = 8 columns x 6 rows
# Each sprite: 124/8 = 15.5 pixels wide, 17 pixels tall
# 6 pixels of spacing below each row (except the last row)
const SPRITE_WIDTH = 15
const SPRITE_HEIGHT = 17

# Map Ranger colors to sprite sheet rows (y position in pixels)
# Sprite sheet structure: 17px sprite + 6px spacing per row
# Row 0 (y=0): Pink character
# Row 1 (y=23): Red character
# Row 2 (y=46): Blue character
# Row 3 (y=69): Yellow character
# Row 4 (y=92): Green character
# Row 5 (y=115): Dark gray character (enemies)
var ranger_sprite_rows = {
	"red": 23,      # Row 1
	"blue": 46,     # Row 2
	"yellow": 69,   # Row 3
	"green": 92,    # Row 4
	"pink": 0       # Row 0
}

## Get the CENTER position of a tile in world coordinates
func get_tile_center(grid_pos: Vector2i) -> Vector2:
	# In isometric tilemaps, map_to_local returns VERTEX positions, not tile centers
	# A tile at (x, y) has its four corners at vertices: (x, y-1), (x+1, y-1), (x, y), (x+1, y)
	# The true center is the average of these four corner positions
	var corner_top_left = map_to_local(grid_pos + Vector2i(0, -1))
	var corner_top_right = map_to_local(grid_pos + Vector2i(1, -1))
	var corner_bottom_left = map_to_local(grid_pos)
	var corner_bottom_right = map_to_local(grid_pos + Vector2i(1, 0))

	var center = (corner_top_left + corner_top_right + corner_bottom_left + corner_bottom_right) / 4.0
	return center

## Spawns a unit sprite on the grid
func spawn_unit(unit_id: String, grid_pos: Vector2i, unit_type: String, color: String):
	# Create sprite node
	var sprite = Sprite2D.new()
	sprite.name = unit_id

	# Load character sprite from atlas
	var sprite_sheet = load(SPRITE_SHEET_PATH)
	var atlas = AtlasTexture.new()
	atlas.atlas = sprite_sheet

	# Ensure nearest-neighbor filtering for pixel art
	sprite_sheet.set_meta("filter", false)

	# Get sprite row based on unit type and color
	var sprite_row_y = 0
	if unit_type == "ranger":
		sprite_row_y = ranger_sprite_rows.get(color, 23)  # Default to red row
	else:  # enemy
		sprite_row_y = 115  # Row 5 (dark gray)

	# Use north-facing frame (column 4) to face toward user
	# 8 directions: S, SE, E, NE, N, NW, W, SW (columns 0-7)
	# Column 4 = N (north) = facing toward camera
	# Sprite sheet width is 124px / 8 cols = 15.5px per sprite
	# Account for fractional spacing: columns at x = 0, 15.5, 31, 46.5, 62, 77.5, 93, 108.5
	var sprite_col_x = int(4 * 15.5)  # Column 4 = 62 pixels
	atlas.region = Rect2(sprite_col_x, sprite_row_y, SPRITE_WIDTH, SPRITE_HEIGHT)
	atlas.filter_clip = true  # Prevent bleeding between atlas regions

	sprite.texture = atlas
	sprite.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST  # Crisp pixel art

	# Position sprite at the exact center of the tile
	sprite.position = get_tile_center(grid_pos)
	sprite.centered = true
	sprite.z_index = 10  # Above tiles

	# Counter-scale to compensate for grid's 4x scale
	# Also scale up the small sprites for better visibility (4x)
	var base_scale = 4.0
	sprite.scale = Vector2(base_scale / scale.x, base_scale / scale.y)

	# Add to scene
	add_child(sprite)
	unit_sprites[unit_id] = sprite
	unit_grid_positions[unit_id] = grid_pos

	# Add a health bar as a child of the sprite (inherits transform)
	var faction_color = Color(0.3, 0.85, 0.35) if unit_type == "ranger" else Color(0.9, 0.25, 0.25)
	_create_health_bar(sprite, faction_color)

## Moves a unit sprite to a new grid position
func move_unit(unit_id: String, new_grid_pos: Vector2i):
	if unit_sprites.has(unit_id):
		var sprite = unit_sprites[unit_id]
		sprite.position = get_tile_center(new_grid_pos)
		unit_grid_positions[unit_id] = new_grid_pos
		print("Moved unit ", unit_id, " to grid pos ", new_grid_pos)
	else:
		print("Warning: Cannot move unit ", unit_id, " - sprite not found")

## Removes a unit sprite from the grid (e.g., when killed)
func remove_unit(unit_id: String):
	if unit_sprites.has(unit_id):
		var sprite = unit_sprites[unit_id]
		sprite.queue_free()
		unit_sprites.erase(unit_id)
		unit_grid_positions.erase(unit_id)
		print("Removed unit sprite: ", unit_id)
	else:
		print("Warning: Cannot remove unit ", unit_id, " - sprite not found")

## Highlights the currently active unit
func highlight_active_unit(unit_id: String):
	if unit_sprites.has(unit_id):
		var sprite = unit_sprites[unit_id]
		sprite.modulate = Color(1.5, 1.5, 1.5)  # Brighten

## Removes unit highlight
func clear_unit_highlight():
	for sprite in unit_sprites.values():
		sprite.modulate = Color(1.0, 1.0, 1.0)  # Normal

## ===== HEALTH BARS =====

# Bar dimensions in sprite-local coords (sprite is 15x17px, scaled 4x on screen).
# These produce a ~14x2 local bar = ~56x8 px on screen — narrower than the sprite.
const HEALTH_BAR_WIDTH = 14.0
const HEALTH_BAR_HEIGHT = 2.0
const HEALTH_BAR_Y_OFFSET = -11.0  # Just above the sprite's head (sprite top ≈ -8.5)

## Create a health bar as a child of the unit sprite. The bar inherits the sprite's
## transform, so it moves with the sprite automatically and is freed when the sprite
## queue_free()s on death.
func _create_health_bar(sprite: Sprite2D, fill_color: Color):
	var bar = Node2D.new()
	bar.name = "HealthBar"
	bar.position = Vector2(0, HEALTH_BAR_Y_OFFSET)
	bar.z_index = 1  # Above the sprite (z_index relative to parent)

	var half_w = HEALTH_BAR_WIDTH * 0.5
	var rect_full = PackedVector2Array([
		Vector2(-half_w, 0),
		Vector2(half_w, 0),
		Vector2(half_w, HEALTH_BAR_HEIGHT),
		Vector2(-half_w, HEALTH_BAR_HEIGHT),
	])

	var bg = Polygon2D.new()
	bg.name = "Background"
	bg.polygon = rect_full
	bg.color = Color(0.08, 0.08, 0.08, 0.9)
	bar.add_child(bg)

	var fill = Polygon2D.new()
	fill.name = "Fill"
	fill.polygon = rect_full
	fill.color = fill_color
	bar.add_child(fill)

	sprite.add_child(bar)

## Update a unit's health bar fill from current/max HP.
## Called by BattleController on DamageDealtEvent and after initial spawn.
func update_health_bar(unit_id: String, current: float, maximum: float):
	if not unit_sprites.has(unit_id):
		return

	var sprite = unit_sprites[unit_id]
	var bar = sprite.get_node_or_null("HealthBar")
	if bar == null:
		return

	var fill = bar.get_node_or_null("Fill") as Polygon2D
	if fill == null:
		return

	var pct = 0.0
	if maximum > 0.0:
		pct = clamp(current / maximum, 0.0, 1.0)

	var half_w = HEALTH_BAR_WIDTH * 0.5
	var fill_right = -half_w + (HEALTH_BAR_WIDTH * pct)
	fill.polygon = PackedVector2Array([
		Vector2(-half_w, 0),
		Vector2(fill_right, 0),
		Vector2(fill_right, HEALTH_BAR_HEIGHT),
		Vector2(-half_w, HEALTH_BAR_HEIGHT),
	])

## ===== TILE DIAGNOSTICS =====

## Diagnostic tool to map tile boundaries and test positioning
func run_tile_diagnostics():
	print("\n=== TILE DIAGNOSTICS ===")
	print("TileMapLayer scale: ", scale)
	print("TileMapLayer position: ", position)

	# Test a few specific grid positions
	var test_positions = [
		Vector2i(0, 0),
		Vector2i(1, 0),
		Vector2i(0, 1),
		Vector2i(1, 1),
		Vector2i(3, 9),  # Red Ranger's expected position
	]

	for grid_pos in test_positions:
		print("\n--- Grid Position: ", grid_pos, " ---")

		# Get the map_to_local result (top point of isometric diamond)
		var top_point = map_to_local(grid_pos)
		print("  map_to_local: ", top_point)

		# Calculate what we think is the center
		var center = top_point + Vector2(0, 8)
		print("  calculated center: ", center)

		# Get all four corners of the isometric tile
		# For a 32x16 isometric diamond in LOCAL space:
		# Top: (0, 0), Right: (16, 8), Bottom: (0, 16), Left: (-16, 8)
		var corners = {
			"top": top_point,
			"right": top_point + Vector2(16, 8),
			"bottom": top_point + Vector2(0, 16),
			"left": top_point + Vector2(-16, 8)
		}
		print("  corners: ", corners)

		# Draw debug visuals for this tile
		draw_tile_debug_box(grid_pos, corners, center)

	print("\n=== END DIAGNOSTICS ===\n")

## Draw debug visualization for a tile
func draw_tile_debug_box(grid_pos: Vector2i, corners: Dictionary, center: Vector2):
	# Draw tile border as a Polygon2D
	var border = Polygon2D.new()
	border.polygon = PackedVector2Array([
		corners["top"],
		corners["right"],
		corners["bottom"],
		corners["left"]
	])
	border.color = Color(1, 1, 0, 0.3)  # Yellow semi-transparent
	border.z_index = 5
	add_child(border)

	# Draw center point as a small sprite
	var center_marker = Sprite2D.new()
	center_marker.texture = create_debug_circle(4, Color(1, 0, 0))  # Red dot
	center_marker.position = center
	center_marker.z_index = 15
	add_child(center_marker)

	# Draw grid coordinate label
	var label = Label.new()
	label.text = str(grid_pos)
	label.position = center
	label.z_index = 20
	label.modulate = Color(0, 0, 0)  # Black text
	add_child(label)

## Create a simple circular texture for debug markers
func create_debug_circle(radius: int, color: Color) -> ImageTexture:
	var size = radius * 2
	var img = Image.create(size, size, false, Image.FORMAT_RGBA8)
	img.fill(Color(0, 0, 0, 0))  # Transparent background

	# Draw circle
	for x in range(size):
		for y in range(size):
			var dx = x - radius
			var dy = y - radius
			if dx * dx + dy * dy <= radius * radius:
				img.set_pixel(x, y, color)

	return ImageTexture.create_from_image(img)

## ===== MOVEMENT PREVIEW =====

# Path preview nodes
var _path_preview_nodes = []

## Show movement preview with path and destination outline
func show_movement_preview(path: Array):
	# Clear any existing preview
	clear_movement_preview()
	
	if path.size() == 0:
		return
	
	# Destination is the last position in path
	var destination = path[path.size() - 1]
	
	# Draw path arrows
	for i in range(path.size() - 1):
		var from_pos = path[i]
		var to_pos = path[i + 1]
		var arrow = _create_path_arrow(from_pos, to_pos)
		_path_preview_nodes.append(arrow)
		add_child(arrow)
	
	# Outline the destination tile
	var outline = _create_destination_outline(destination)
	_path_preview_nodes.append(outline)
	add_child(outline)

## Clear movement preview
func clear_movement_preview():
	for node in _path_preview_nodes:
		node.queue_free()
	_path_preview_nodes.clear()

## Create an arrow sprite from one tile to another
func _create_path_arrow(from: Vector2i, to: Vector2i) -> Line2D:
	var line = Line2D.new()
	var from_center = get_tile_center(from)
	var to_center = get_tile_center(to)
	
	line.add_point(from_center)
	line.add_point(to_center)
	line.width = 3.0
	line.default_color = Color(1.0, 1.0, 0.5, 0.8)  # Yellow-ish
	line.z_index = 15  # Above tiles, below units
	
	return line

## Create destination outline
func _create_destination_outline(pos: Vector2i) -> Polygon2D:
	var center = get_tile_center(pos)
	
	# Create diamond outline for isometric tile
	# Tile is 32x16 in local space, center is offset by (0, 8)
	var poly = Polygon2D.new()
	poly.polygon = PackedVector2Array([
		center + Vector2(-16, 0),   # Left
		center + Vector2(0, -8),    # Top
		center + Vector2(16, 0),    # Right
		center + Vector2(0, 8)      # Bottom
	])
	poly.color = Color(0, 0, 0, 0)  # Transparent fill
	poly.z_index = 14  # Just below path arrows
	
	# Add outline using Line2D child
	var outline_line = Line2D.new()
	outline_line.add_point(center + Vector2(-16, 0))
	outline_line.add_point(center + Vector2(0, -8))
	outline_line.add_point(center + Vector2(16, 0))
	outline_line.add_point(center + Vector2(0, 8))
	outline_line.add_point(center + Vector2(-16, 0))  # Close the loop
	outline_line.width = 4.0
	outline_line.default_color = Color(1.0, 1.0, 0.0, 1.0)  # Bright yellow
	outline_line.z_index = 14
	
	poly.add_child(outline_line)

	return poly

## ===== SPRITE CLICK DETECTION =====

## Find the unit whose sprite is closest to the given world position.
## Returns the unit_id or "" if no unit is close enough.
func find_unit_at_world_pos(world_pos: Vector2) -> String:
	# Convert world pos to local space (accounts for TileMapLayer transform)
	var local_pos = to_local(world_pos)
	var best_id = ""
	var best_dist = 12.0  # Max distance in local (tile) pixels
	for unit_id in unit_sprites:
		var sprite = unit_sprites[unit_id]
		var dist = local_pos.distance_to(sprite.position)
		if dist < best_dist:
			best_dist = dist
			best_id = unit_id
	return best_id

## Get the grid position of a unit by its ID.
## Returns Vector2i(-1, -1) if not found.
func get_unit_grid_pos(unit_id: String) -> Vector2i:
	if unit_grid_positions.has(unit_id):
		return unit_grid_positions[unit_id]
	return Vector2i(-1, -1)
