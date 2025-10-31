extends CharacterBody2D
class_name Player

const SPEED = 300.0
const JUMP_VELOCITY = -400.0

# Multiplayer sync properties
@export var player_id: int = 1

func _enter_tree():
	# Set the multiplayer authority
	# This ensures only the owning client controls this player
	set_multiplayer_authority(player_id)


func _physics_process(delta):
	# Only process input if this is OUR player
	if is_multiplayer_authority():
		_handle_movement(delta)
	
	# Physics runs on all clients for smooth prediction
	move_and_slide()

func _handle_movement(delta):
	# Add gravity
	if not is_on_floor():
		velocity += get_gravity() * delta
	
	# Handle jump
	if Input.is_action_just_pressed("jump") and is_on_floor():
		velocity.y = JUMP_VELOCITY
	
	# Get input direction
	var direction = Input.get_axis("move_left", "move_right")
	
	if direction != 0:
		velocity.x = direction * SPEED
		# Flip sprite based on direction
		$Sprite2D.flip_h = direction < 0
	else:
		# Apply friction when not moving
		velocity.x = move_toward(velocity.x, 0, SPEED * delta * 10)
