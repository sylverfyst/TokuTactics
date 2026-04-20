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

	# Keyboard actions
	if event is InputEventKey and event.pressed and not event.echo:
		# End turn with Enter or Space
		if event.keycode == KEY_ENTER or event.keycode == KEY_SPACE:
			if battle_controller and not _ui_panel_open:
				battle_controller.EndCurrentUnitTurn()
			get_viewport().set_input_as_handled()
		# Morph with M
		elif event.keycode == KEY_M:
			if battle_controller:
				battle_controller.OnMorphPressed()
			get_viewport().set_input_as_handled()
		# Form switch with F
		elif event.keycode == KEY_F:
			if battle_controller:
				battle_controller.OnFormSwitchPressed()
			get_viewport().set_input_as_handled()
		# Cancel UI panels with Escape
		elif event.keycode == KEY_ESCAPE:
			if _loadout_panel:
				battle_controller.OnLoadoutCancelled()
			elif _form_switch_panel:
				battle_controller.OnFormSwitchCancelled()
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

## Updates the turn indicator UI with current unit, budget, and morph state.
func update_turn_indicator(unit_id: String, can_move: bool, can_act: bool,
		form_name: String = "", can_morph: bool = false, can_switch: bool = false):
	if not turn_indicator:
		return

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
		turn_indicator.text = display_name
	else:
		var form_text = ""
		if form_name != "":
			form_text = " (" + form_name + ")"

		var actions = ""
		if can_morph:
			actions += " | M: Morph"
		if can_switch:
			actions += " | F: Switch"

		turn_indicator.text = "Turn: " + display_name + form_text + status_text + actions

# === UI Panel State ===

var _loadout_panel: PanelContainer = null
var _form_switch_panel: PanelContainer = null
var _loadout_selected: Array = []
var _loadout_budget: int = 0
var _ui_panel_open: bool:
	get: return _loadout_panel != null or _form_switch_panel != null

# === Loadout Panel ===

func show_loadout_panel(forms: Array, budget: int):
	if _loadout_panel:
		_loadout_panel.queue_free()

	_loadout_budget = budget
	_loadout_selected.clear()

	_loadout_panel = PanelContainer.new()
	_loadout_panel.name = "LoadoutPanel"

	var style = StyleBoxFlat.new()
	style.bg_color = Color(0.1, 0.1, 0.15, 0.95)
	style.border_width_left = 2
	style.border_width_right = 2
	style.border_width_top = 2
	style.border_width_bottom = 2
	style.border_color = Color(0.6, 0.6, 0.8)
	style.corner_radius_top_left = 8
	style.corner_radius_top_right = 8
	style.corner_radius_bottom_left = 8
	style.corner_radius_bottom_right = 8
	style.content_margin_left = 20
	style.content_margin_right = 20
	style.content_margin_top = 16
	style.content_margin_bottom = 16
	_loadout_panel.add_theme_stylebox_override("panel", style)

	var vbox = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 12)

	var title = Label.new()
	title.text = "SELECT FORMS (Budget: " + str(budget) + ")"
	title.add_theme_font_size_override("font_size", 22)
	title.add_theme_color_override("font_color", Color(1, 0.9, 0.4))
	vbox.add_child(title)

	var subtitle = Label.new()
	subtitle.name = "BudgetLabel"
	subtitle.text = "Selected: 0 / " + str(budget)
	subtitle.add_theme_font_size_override("font_size", 16)
	vbox.add_child(subtitle)

	for form in forms:
		var btn = Button.new()
		btn.name = "Form_" + form["id"]
		btn.text = form["name"] + " [" + form["type"] + "]  HP:" + str(form["hp"]) + "  Range:" + str(form["range"]) + "  Mov:" + str(form["movement"])
		btn.toggle_mode = true
		btn.add_theme_font_size_override("font_size", 16)
		btn.custom_minimum_size = Vector2(400, 36)
		var form_id = form["id"]
		btn.toggled.connect(func(pressed): _on_loadout_form_toggled(form_id, pressed))
		vbox.add_child(btn)

	var confirm_btn = Button.new()
	confirm_btn.name = "ConfirmButton"
	confirm_btn.text = "CONFIRM"
	confirm_btn.add_theme_font_size_override("font_size", 18)
	confirm_btn.custom_minimum_size = Vector2(200, 40)
	confirm_btn.pressed.connect(_on_loadout_confirm)
	vbox.add_child(confirm_btn)

	var cancel_label = Label.new()
	cancel_label.text = "(ESC to cancel)"
	cancel_label.add_theme_font_size_override("font_size", 12)
	cancel_label.add_theme_color_override("font_color", Color(0.6, 0.6, 0.6))
	vbox.add_child(cancel_label)

	_loadout_panel.add_child(vbox)
	_loadout_panel.position = Vector2(20, 160)
	$UI.add_child(_loadout_panel)

func hide_loadout_panel():
	if _loadout_panel:
		_loadout_panel.queue_free()
		_loadout_panel = null
	_loadout_selected.clear()

func _on_loadout_form_toggled(form_id: String, pressed: bool):
	if pressed:
		if _loadout_selected.size() < _loadout_budget:
			_loadout_selected.append(form_id)
		else:
			# Over budget — untoggle
			var btn = _loadout_panel.find_child("Form_" + form_id, true, false) as Button
			if btn:
				btn.set_pressed_no_signal(false)
			return
	else:
		_loadout_selected.erase(form_id)

	# Update budget label
	var label = _loadout_panel.find_child("BudgetLabel", true, false) as Label
	if label:
		label.text = "Selected: " + str(_loadout_selected.size()) + " / " + str(_loadout_budget)

func _on_loadout_confirm():
	if _loadout_selected.size() == 0:
		return
	if battle_controller:
		var ids = []
		for id in _loadout_selected:
			ids.append(id)
		battle_controller.OnLoadoutSubmitted(ids)

# === Form Switch Panel ===

func show_form_switch_panel(forms: Array):
	if _form_switch_panel:
		_form_switch_panel.queue_free()

	_form_switch_panel = PanelContainer.new()
	_form_switch_panel.name = "FormSwitchPanel"

	var style = StyleBoxFlat.new()
	style.bg_color = Color(0.1, 0.12, 0.1, 0.95)
	style.border_width_left = 2
	style.border_width_right = 2
	style.border_width_top = 2
	style.border_width_bottom = 2
	style.border_color = Color(0.4, 0.8, 0.4)
	style.corner_radius_top_left = 8
	style.corner_radius_top_right = 8
	style.corner_radius_bottom_left = 8
	style.corner_radius_bottom_right = 8
	style.content_margin_left = 20
	style.content_margin_right = 20
	style.content_margin_top = 16
	style.content_margin_bottom = 16
	_form_switch_panel.add_theme_stylebox_override("panel", style)

	var vbox = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 8)

	var title = Label.new()
	title.text = "SWITCH FORM"
	title.add_theme_font_size_override("font_size", 22)
	title.add_theme_color_override("font_color", Color(0.4, 1.0, 0.5))
	vbox.add_child(title)

	for form in forms:
		var btn = Button.new()
		var label_text = form["name"] + " [" + form["type"] + "]"
		if form["is_current"]:
			label_text += "  (current)"
		elif not form["available"]:
			label_text += "  (" + form["status"] + ")"
		btn.text = label_text
		btn.disabled = not form["available"]
		btn.add_theme_font_size_override("font_size", 16)
		btn.custom_minimum_size = Vector2(350, 36)
		var form_id = form["id"]
		btn.pressed.connect(func(): _on_form_switch_selected(form_id))
		vbox.add_child(btn)

	var cancel_label = Label.new()
	cancel_label.text = "(ESC to cancel)"
	cancel_label.add_theme_font_size_override("font_size", 12)
	cancel_label.add_theme_color_override("font_color", Color(0.6, 0.6, 0.6))
	vbox.add_child(cancel_label)

	_form_switch_panel.add_child(vbox)
	_form_switch_panel.position = Vector2(20, 160)
	$UI.add_child(_form_switch_panel)

func hide_form_switch_panel():
	if _form_switch_panel:
		_form_switch_panel.queue_free()
		_form_switch_panel = null

func _on_form_switch_selected(form_id: String):
	if battle_controller:
		battle_controller.OnFormSwitchSelected(form_id)
