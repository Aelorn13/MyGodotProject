extends CharacterBody2D
class_name Player

const SPEED = 300.0
const JUMP_VELOCITY = -500.0

# Coyote time (allows jumping shortly after leaving platform)
const COYOTE_TIME = 0.15
var coyote_timer = 0.0

@export var player_id: int = 1

func _enter_tree():
	set_multiplayer_authority(player_id)

func _physics_process(delta):
	if is_multiplayer_authority():
		_handle_movement(delta)
	
	move_and_slide()

func _handle_movement(delta):
	# Gravity
	if not is_on_floor():
		velocity += get_gravity() * delta
		coyote_timer -= delta
	else:
		coyote_timer = COYOTE_TIME
	
	# Jump with buffering and coyote time
	if InputManager.has_buffered_jump() and (is_on_floor() or coyote_timer > 0):
		velocity.y = JUMP_VELOCITY
		InputManager.consume_jump_buffer()
		coyote_timer = 0
	
	# Variable jump height (release jump early = smaller jump)
	if velocity.y < 0 and not InputManager.is_jump_held():
		velocity.y *= 0.5
	
	# Horizontal movement
	var direction = InputManager.get_horizontal_direction()
	
	if direction != 0:
		velocity.x = direction * SPEED
		$Sprite2D.flip_h = direction < 0
	else:
		velocity.x = move_toward(velocity.x, 0, SPEED * delta * 10)
