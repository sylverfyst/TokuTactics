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
			if battle_controller and not _ui_panel_open and not _enemy_panel_has_attack:
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
		# Undo move with U
		elif event.keycode == KEY_U:
			if battle_controller:
				battle_controller.UndoMove()
			get_viewport().set_input_as_handled()
		# Cancel UI panels with Escape
		elif event.keycode == KEY_ESCAPE:
			if _loadout_panel:
				battle_controller.OnLoadoutCancelled()
			elif _form_switch_panel:
				battle_controller.OnFormSwitchCancelled()
			get_viewport().set_input_as_handled()

	# Click to interact with tiles or unit sprites
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
		var world_pos = get_global_mouse_position()

		# Primary: check if click hits a unit sprite directly
		var sprite_unit = grid_visual.find_unit_at_world_pos(world_pos)
		if sprite_unit != "":
			var sprite_grid = grid_visual.get_unit_grid_pos(sprite_unit)
			if sprite_grid != Vector2i(-1, -1):
				print("Clicked unit: ", sprite_unit, " at ", sprite_grid)
				if battle_controller:
					battle_controller.OnTileClicked(sprite_grid.x, sprite_grid.y)
				return

		# Fallback: convert click to tile grid coordinates
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
		form_name: String = "", can_morph: bool = false, can_switch: bool = false,
		can_undo: bool = false):
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
		if can_undo:
			actions += " | U: Undo Move"

		turn_indicator.text = "Turn: " + display_name + form_text + status_text + actions

# === UI Panel State ===

var _loadout_panel: PanelContainer = null
var _form_switch_panel: PanelContainer = null
var _loadout_selected: Array = []
var _loadout_budget: int = 0
var _active_unit_panel: PanelContainer = null
var _enemy_panel: PanelContainer = null
var _enemy_panel_has_attack: bool = false
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

# === Active Unit Panel (bottom-left) ===

func update_active_unit_panel(data: Dictionary):
	if _active_unit_panel:
		_active_unit_panel.queue_free()

	_active_unit_panel = PanelContainer.new()
	_active_unit_panel.name = "ActiveUnitPanel"

	var style = StyleBoxFlat.new()
	style.bg_color = Color(0.08, 0.1, 0.15, 0.92)
	style.border_width_left = 2
	style.border_width_right = 2
	style.border_width_top = 2
	style.border_width_bottom = 2
	style.border_color = Color(0.4, 0.6, 1.0)
	style.corner_radius_top_left = 6
	style.corner_radius_top_right = 6
	style.corner_radius_bottom_left = 6
	style.corner_radius_bottom_right = 6
	style.content_margin_left = 12
	style.content_margin_right = 12
	style.content_margin_top = 10
	style.content_margin_bottom = 10
	_active_unit_panel.add_theme_stylebox_override("panel", style)

	var vbox = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 4)

	# Ranger name
	var display_name = data.get("id", "")
	if display_name.begins_with("ranger_"):
		display_name = display_name.replace("ranger_", "").capitalize() + " Ranger"
	var name_label = Label.new()
	name_label.text = display_name
	name_label.add_theme_font_size_override("font_size", 18)
	name_label.add_theme_color_override("font_color", Color(0.4, 0.7, 1.0))
	vbox.add_child(name_label)

	# Form name + type
	var form_label = Label.new()
	form_label.text = str(data.get("form_name", "")) + " [" + str(data.get("form_type", "")) + "]"
	form_label.add_theme_font_size_override("font_size", 14)
	form_label.add_theme_color_override("font_color", Color(0.8, 0.8, 0.8))
	vbox.add_child(form_label)

	# HP
	var hp_label = Label.new()
	hp_label.text = "HP: " + str(data.get("hp_current", 0)) + " / " + str(data.get("hp_max", 0))
	hp_label.add_theme_font_size_override("font_size", 14)
	hp_label.add_theme_color_override("font_color", Color(0.4, 0.9, 0.4))
	vbox.add_child(hp_label)

	# Budget status
	var can_move = data.get("can_move", false)
	var can_act = data.get("can_act", false)
	var budget_text = ""
	if can_move and can_act:
		budget_text = "Move + Act"
	elif can_move:
		budget_text = "Move only"
	elif can_act:
		budget_text = "Act only"
	else:
		budget_text = "No actions"
	var budget_label = Label.new()
	budget_label.text = budget_text
	budget_label.add_theme_font_size_override("font_size", 12)
	budget_label.add_theme_color_override("font_color", Color(0.6, 0.6, 0.6))
	vbox.add_child(budget_label)

	# Weapons
	if data.has("weapon_a_name"):
		var wa_text = str(data["weapon_a_name"]) + " (" + str(snapped(data.get("weapon_a_power", 0), 0.1)) + " Pow, " + str(data.get("weapon_a_range", 0)) + " Rng"
		if data.get("weapon_a_status", "") != "":
			wa_text += ", " + str(data["weapon_a_status"])
		wa_text += ")"
		var wa_label = Label.new()
		wa_label.text = "A: " + wa_text
		wa_label.add_theme_font_size_override("font_size", 13)
		wa_label.add_theme_color_override("font_color", Color(1.0, 0.85, 0.5))
		vbox.add_child(wa_label)

	if data.has("weapon_b_name"):
		var wb_text = str(data["weapon_b_name"]) + " (" + str(snapped(data.get("weapon_b_power", 0), 0.1)) + " Pow, " + str(data.get("weapon_b_range", 0)) + " Rng"
		if data.get("weapon_b_status", "") != "":
			wb_text += ", " + str(data["weapon_b_status"])
		wb_text += ")"
		var wb_label = Label.new()
		wb_label.text = "B: " + wb_text
		wb_label.add_theme_font_size_override("font_size", 13)
		wb_label.add_theme_color_override("font_color", Color(1.0, 0.85, 0.5))
		vbox.add_child(wb_label)

	# Undo hint
	if data.get("can_undo", false):
		var undo_label = Label.new()
		undo_label.text = "U: Undo Move"
		undo_label.add_theme_font_size_override("font_size", 12)
		undo_label.add_theme_color_override("font_color", Color(0.7, 0.7, 0.4))
		vbox.add_child(undo_label)

	_active_unit_panel.add_child(vbox)

	# Position at bottom-left
	_active_unit_panel.set_anchors_preset(Control.PRESET_BOTTOM_LEFT)
	_active_unit_panel.position = Vector2(10, -10)
	_active_unit_panel.grow_vertical = Control.GROW_DIRECTION_BEGIN
	$UI.add_child(_active_unit_panel)

func hide_active_unit_panel():
	if _active_unit_panel:
		_active_unit_panel.queue_free()
		_active_unit_panel = null

# === Enemy Info Panel (bottom-right) ===

func show_enemy_panel(info: Dictionary):
	if _enemy_panel:
		_enemy_panel.queue_free()

	_enemy_panel_has_attack = info.get("can_attack", false)

	_enemy_panel = PanelContainer.new()
	_enemy_panel.name = "EnemyPanel"

	var style = StyleBoxFlat.new()
	style.bg_color = Color(0.15, 0.08, 0.08, 0.92)
	style.border_width_left = 2
	style.border_width_right = 2
	style.border_width_top = 2
	style.border_width_bottom = 2
	style.border_color = Color(1.0, 0.4, 0.4)
	style.corner_radius_top_left = 6
	style.corner_radius_top_right = 6
	style.corner_radius_bottom_left = 6
	style.corner_radius_bottom_right = 6
	style.content_margin_left = 12
	style.content_margin_right = 12
	style.content_margin_top = 10
	style.content_margin_bottom = 10
	_enemy_panel.add_theme_stylebox_override("panel", style)

	var vbox = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 4)

	# Enemy name
	var name_label = Label.new()
	name_label.text = str(info.get("name", "Enemy"))
	name_label.add_theme_font_size_override("font_size", 18)
	name_label.add_theme_color_override("font_color", Color(1.0, 0.5, 0.5))
	vbox.add_child(name_label)

	# Tier + Type
	var tier_text = str(info.get("tier", ""))
	var type_text = str(info.get("type", "???"))
	var type_revealed = info.get("type_revealed", false)
	var info_label = Label.new()
	info_label.text = tier_text + " | Type: " + type_text
	info_label.add_theme_font_size_override("font_size", 14)
	if not type_revealed:
		info_label.add_theme_color_override("font_color", Color(0.6, 0.6, 0.6))
	else:
		info_label.add_theme_color_override("font_color", Color(0.8, 0.8, 0.8))
	vbox.add_child(info_label)

	# HP
	var hp_label = Label.new()
	hp_label.text = "HP: " + str(info.get("hp_current", 0)) + " / " + str(info.get("hp_max", 0))
	hp_label.add_theme_font_size_override("font_size", 14)
	hp_label.add_theme_color_override("font_color", Color(0.9, 0.4, 0.4))
	vbox.add_child(hp_label)

	# Weapon buttons (if can attack)
	if info.get("can_attack", false):
		var sep = HSeparator.new()
		vbox.add_child(sep)

		var atk_title = Label.new()
		atk_title.text = "ATTACK WITH:"
		atk_title.add_theme_font_size_override("font_size", 13)
		atk_title.add_theme_color_override("font_color", Color(1.0, 0.9, 0.4))
		vbox.add_child(atk_title)

		if info.has("weapon_a_name"):
			var btn_a = Button.new()
			var wa_text = str(info["weapon_a_name"]) + " (" + str(snapped(info.get("weapon_a_power", 0), 0.1)) + " Pow, " + str(info.get("weapon_a_range", 0)) + " Rng"
			if info.get("weapon_a_status", "") != "":
				wa_text += ", " + str(info["weapon_a_status"])
			wa_text += ")"
			btn_a.text = "A: " + wa_text
			btn_a.disabled = not info.get("weapon_a_in_range", false)
			btn_a.add_theme_font_size_override("font_size", 14)
			btn_a.custom_minimum_size = Vector2(280, 32)
			btn_a.pressed.connect(func(): _on_attack_weapon("A"))
			vbox.add_child(btn_a)

		if info.has("weapon_b_name"):
			var btn_b = Button.new()
			var wb_text = str(info["weapon_b_name"]) + " (" + str(snapped(info.get("weapon_b_power", 0), 0.1)) + " Pow, " + str(info.get("weapon_b_range", 0)) + " Rng"
			if info.get("weapon_b_status", "") != "":
				wb_text += ", " + str(info["weapon_b_status"])
			wb_text += ")"
			btn_b.text = "B: " + wb_text
			btn_b.disabled = not info.get("weapon_b_in_range", false)
			btn_b.add_theme_font_size_override("font_size", 14)
			btn_b.custom_minimum_size = Vector2(280, 32)
			btn_b.pressed.connect(func(): _on_attack_weapon("B"))
			vbox.add_child(btn_b)

	_enemy_panel.add_child(vbox)

	# Position at bottom-right
	_enemy_panel.set_anchors_preset(Control.PRESET_BOTTOM_RIGHT)
	_enemy_panel.position = Vector2(-10, -10)
	_enemy_panel.grow_horizontal = Control.GROW_DIRECTION_BEGIN
	_enemy_panel.grow_vertical = Control.GROW_DIRECTION_BEGIN
	$UI.add_child(_enemy_panel)

func hide_enemy_panel():
	if _enemy_panel:
		_enemy_panel.queue_free()
		_enemy_panel = null
	_enemy_panel_has_attack = false

func _on_attack_weapon(slot: String):
	if battle_controller:
		battle_controller.AttackWithWeapon(slot)

# === Effectiveness Text ===

func show_effectiveness_text(text: String, is_super_effective: bool):
	var label = Label.new()
	label.name = "EffectivenessText"
	label.text = text
	label.add_theme_font_size_override("font_size", 28)
	if is_super_effective:
		label.add_theme_color_override("font_color", Color(1.0, 0.3, 0.1))
	else:
		label.add_theme_color_override("font_color", Color(0.5, 0.5, 0.7))
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.set_anchors_preset(Control.PRESET_CENTER)
	label.grow_horizontal = Control.GROW_DIRECTION_BOTH
	label.grow_vertical = Control.GROW_DIRECTION_BOTH
	$UI.add_child(label)

	# Auto-remove after 1.5 seconds
	var timer = Timer.new()
	timer.wait_time = 1.5
	timer.one_shot = true
	timer.timeout.connect(func():
		label.queue_free()
		timer.queue_free()
	)
	add_child(timer)
	timer.start()

# === Mission Result ===

func show_mission_result(result_text: String, rounds: int):
	var panel = PanelContainer.new()
	panel.name = "MissionResultPanel"

	var style = StyleBoxFlat.new()
	if result_text == "VICTORY":
		style.bg_color = Color(0.05, 0.15, 0.05, 0.95)
		style.border_color = Color(0.3, 1.0, 0.3)
	else:
		style.bg_color = Color(0.2, 0.05, 0.05, 0.95)
		style.border_color = Color(1.0, 0.3, 0.3)
	style.border_width_left = 3
	style.border_width_right = 3
	style.border_width_top = 3
	style.border_width_bottom = 3
	style.corner_radius_top_left = 12
	style.corner_radius_top_right = 12
	style.corner_radius_bottom_left = 12
	style.corner_radius_bottom_right = 12
	style.content_margin_left = 40
	style.content_margin_right = 40
	style.content_margin_top = 30
	style.content_margin_bottom = 30
	panel.add_theme_stylebox_override("panel", style)

	var vbox = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 16)
	vbox.alignment = BoxContainer.ALIGNMENT_CENTER

	var title = Label.new()
	title.text = "MISSION " + result_text
	title.add_theme_font_size_override("font_size", 36)
	if result_text == "VICTORY":
		title.add_theme_color_override("font_color", Color(0.3, 1.0, 0.4))
	else:
		title.add_theme_color_override("font_color", Color(1.0, 0.3, 0.3))
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(title)

	var rounds_label = Label.new()
	rounds_label.text = "Rounds: " + str(rounds)
	rounds_label.add_theme_font_size_override("font_size", 20)
	rounds_label.add_theme_color_override("font_color", Color(0.8, 0.8, 0.8))
	rounds_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(rounds_label)

	panel.add_child(vbox)

	# Center on screen
	panel.anchors_preset = Control.PRESET_CENTER
	panel.grow_horizontal = Control.GROW_DIRECTION_BOTH
	panel.grow_vertical = Control.GROW_DIRECTION_BOTH

	$UI.add_child(panel)

	# Also update turn indicator
	if turn_indicator:
		turn_indicator.text = "MISSION " + result_text
