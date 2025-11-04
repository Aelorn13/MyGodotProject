extends Node

# This singleton manages input for the entire game
# Makes it easier to handle input remapping and gamepad support later

# Movement inputs (returns -1, 0, or 1)
func get_movement_direction() -> Vector2:
	return Input.get_vector("move_left", "move_right", "move_up", "move_down")

func get_horizontal_direction() -> float:
	return Input.get_axis("move_left", "move_right")

# Action inputs (returns true on press)
func is_jump_pressed() -> bool:
	return Input.is_action_just_pressed("jump")

func is_jump_held() -> bool:
	return Input.is_action_pressed("jump")

func is_attack_pressed() -> bool:
	return Input.is_action_just_pressed("attack")

# Allow jump buffering (press jump slightly before landing)
var jump_buffer_time := 0.1
var jump_buffer_timer := 0.0

func _process(delta):
	if jump_buffer_timer > 0:
		jump_buffer_timer -= delta
	
	if is_jump_pressed():
		jump_buffer_timer = jump_buffer_time

func has_buffered_jump() -> bool:
	return jump_buffer_timer > 0

func consume_jump_buffer():
	jump_buffer_timer = 0.0
