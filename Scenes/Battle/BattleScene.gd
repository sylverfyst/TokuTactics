extends Node2D
## Main battle scene that coordinates C# game logic with Godot presentation layer.
## Handles input, camera control, and syncs visual state with MissionContext.

@onready var grid_visual = $BattleGridVisual
@onready var camera = $Camera2D
@onready var battle_controller = $BattleController
@onready var turn_indicator = $UI/TurnIndicator

const CAMERA_PAN_SPEED = 300.0
const CAMERA_ZOOM_STEP = 0.1
const CAMERA_ZOOM_MIN = 0.5
const CAMERA_ZOOM_MAX = 4.0

func _ready():
	print("=== BattleScene ready - Isometric tilemap initialized ===")
	print("Camera position: ", camera.position)
	print("Camera zoom: ", camera.zoom)
	print("Grid visual position: ", grid_visual.position if grid_visual else "null")
	# TODO: Initialize C# MissionContext
	# mission_context = MissionContext.Create(episode, campaignData, registry)

func _process(delta):
	handle_camera_input(delta)

func handle_camera_input(delta):
	var move_dir = Vector2.ZERO

	if Input.is_action_pressed("ui_left") or Input.is_key_pressed(KEY_A):
		move_dir.x -= 1
	if Input.is_action_pressed("ui_right") or Input.is_key_pressed(KEY_D):
		move_dir.x += 1
	if Input.is_action_pressed("ui_up") or Input.is_key_pressed(KEY_W):
		move_dir.y -= 1
	if Input.is_action_pressed("ui_down") or Input.is_key_pressed(KEY_S):
		move_dir.y += 1

	if move_dir != Vector2.ZERO:
		camera.position += move_dir.normalized() * CAMERA_PAN_SPEED * delta

func _unhandled_input(event):
	# Zoom with mouse wheel
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP and event.pressed:
			zoom_camera(CAMERA_ZOOM_STEP)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN and event.pressed:
			zoom_camera(-CAMERA_ZOOM_STEP)

	# End turn with Enter or Space
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_ENTER or event.keycode == KEY_SPACE:
			if battle_controller:
				battle_controller.EndCurrentUnitTurn()
			get_viewport().set_input_as_handled()

	# Click to interact with tiles
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
		# Convert screen position to world position (accounting for camera)
		var world_pos = get_global_mouse_position()
		# Convert world position to grid coordinates
		var grid_pos = grid_visual.local_to_map(grid_visual.to_local(world_pos))
		print("Clicked tile: ", grid_pos)
		if battle_controller:
			battle_controller.OnTileClicked(grid_pos.x, grid_pos.y)

func zoom_camera(step: float):
	var new_zoom = camera.zoom + Vector2(step, step)
	new_zoom.x = clamp(new_zoom.x, CAMERA_ZOOM_MIN, CAMERA_ZOOM_MAX)
	new_zoom.y = clamp(new_zoom.y, CAMERA_ZOOM_MIN, CAMERA_ZOOM_MAX)
	camera.zoom = new_zoom

## Called by C# to sync visual state when game state changes
func sync_grid_from_csharp(grid_data: Dictionary):
	if grid_visual:
		grid_visual.sync_with_battle_grid(grid_data)

## Highlights tiles for movement/attack range
func show_tile_highlights(positions: Array, highlight_type: String):
	if grid_visual:
		grid_visual.highlight_tiles(positions, highlight_type)

func clear_tile_highlights():
	if grid_visual:
		grid_visual.clear_highlights()

## Updates the turn indicator UI with current unit and budget info
func update_turn_indicator(unit_id: String, can_move: bool, can_act: bool):
	if turn_indicator:
		var status_text = ""
		if can_move and can_act:
			status_text = " [Move + Act]"
		elif can_move:
			status_text = " [Move only]"
		elif can_act:
			status_text = " [Act only]"
		else:
			status_text = " [No actions]"

		# Format unit ID nicely (e.g., "ranger_red" -> "Red Ranger")
		var display_name = unit_id
		if unit_id.begins_with("ranger_"):
			display_name = unit_id.replace("ranger_", "").capitalize() + " Ranger"

		if not can_move and not can_act and not unit_id.begins_with("ranger_"):
			# Phase label (e.g., "Enemy Phase") — no status suffix
			turn_indicator.text = display_name
		else:
			turn_indicator.text = "Turn: " + display_name + status_text
