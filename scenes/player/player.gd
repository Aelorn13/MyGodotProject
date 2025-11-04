extends CharacterBody2D

# ===== MOVEMENT CONSTANTS =====
const SPEED = 300.0
const ACCELERATION = 2000.0  # How fast we reach max speed
const FRICTION = 1500.0      # How fast we stop

const JUMP_VELOCITY = -400.0
const FALL_GRAVITY_MULTIPLIER = 1.5  # Fall faster than we rise
const MAX_FALL_SPEED = 600.0

# ===== JUMP FEEL CONSTANTS =====
const COYOTE_TIME = 0.15      # Can jump this long after leaving platform
const JUMP_BUFFER_TIME = 0.1  # Can press jump this early before landing
const VARIABLE_JUMP_MULTIPLIER = 0.4  # How much to cut jump when releasing

# ===== LEDGE CLIMB CONSTANTS =====
const CLIMB_HEIGHT = 20       # How high we can climb up
const CLIMB_CHECK_DISTANCE = 8  # How far ahead to check for ledge

# ===== STATE TRACKING =====
var coyote_timer = 0.0
var jump_buffer_timer = 0.0
var is_climbing = false

func _physics_process(delta):
	if is_climbing:
		_handle_climb()
		return
	
	_apply_gravity(delta)
	_update_timers(delta)
	_handle_jump()
	_handle_horizontal_movement(delta)
	_check_for_ledge_climb()
	
	move_and_slide()

# ===== GRAVITY =====
func _apply_gravity(delta):
	if not is_on_floor():
		# Fall faster than we rise (feels better)
		var gravity_multiplier = FALL_GRAVITY_MULTIPLIER if velocity.y > 0 else 1.0
		velocity.y += get_gravity().y * gravity_multiplier * delta
		
		# Cap fall speed
		velocity.y = min(velocity.y, MAX_FALL_SPEED)
		
		# Decrease coyote time while in air
		coyote_timer -= delta
	else:
		# Reset coyote time when on ground
		coyote_timer = COYOTE_TIME

# ===== TIMERS =====
func _update_timers(delta):
	# Jump buffer timer
	if jump_buffer_timer > 0:
		jump_buffer_timer -= delta

# ===== JUMP =====
func _handle_jump():
	# Buffer jump input
	if Input.is_action_just_pressed("jump"):
		jump_buffer_timer = JUMP_BUFFER_TIME
	
	# Check if we can jump (on ground OR coyote time OR buffered)
	var can_jump = is_on_floor() or coyote_timer > 0
	var wants_to_jump = jump_buffer_timer > 0
	
	if wants_to_jump and can_jump:
		velocity.y = JUMP_VELOCITY
		jump_buffer_timer = 0  # Consume the buffer
		coyote_timer = 0       # Consume coyote time
	
	# Variable jump height (release early = smaller jump)
	if velocity.y < 0 and not Input.is_action_pressed("jump"):
		velocity.y *= VARIABLE_JUMP_MULTIPLIER

# ===== HORIZONTAL MOVEMENT =====
func _handle_horizontal_movement(delta):
	var direction = Input.get_axis("move_left", "move_right")
	
	if direction != 0:
		# Accelerate toward max speed
		velocity.x = move_toward(velocity.x, direction * SPEED, ACCELERATION * delta)
		
		# Flip sprite
		$Sprite2D.flip_h = direction < 0
	else:
		# Apply friction
		velocity.x = move_toward(velocity.x, 0, FRICTION * delta)

# ===== LEDGE CLIMB =====
func _check_for_ledge_climb():
	# Only check if moving forward and in the air
	if is_on_floor() or velocity.y > 0:
		return
	
	var direction = 1 if not $Sprite2D.flip_h else -1
	
	# Check if there's a wall in front
	var wall_check = Vector2(direction * CLIMB_CHECK_DISTANCE, 0)
	var space_state = get_world_2d().direct_space_state
	
	# Raycast forward to detect wall
	var wall_query = PhysicsRayQueryParameters2D.create(
		global_position,
		global_position + wall_check
	)
	var wall_result = space_state.intersect_ray(wall_query)
	
	if wall_result.is_empty():
		return  # No wall
	
	# Check if there's empty space above the wall (the ledge top)
	var ledge_check = Vector2(direction * CLIMB_CHECK_DISTANCE, -CLIMB_HEIGHT)
	var ledge_query = PhysicsRayQueryParameters2D.create(
		global_position,
		global_position + ledge_check
	)
	var ledge_result = space_state.intersect_ray(ledge_query)
	
	# If we hit a wall but NOT a ledge top, we can climb
	if not ledge_result.is_empty():
		return
	
	# Initiate climb
	if Input.is_action_pressed("move_right" if direction > 0 else "move_left"):
		_start_climb(direction)

func _start_climb(direction):
	is_climbing = true
	velocity = Vector2.ZERO
	
	# Animate climbing up
	var climb_target = global_position + Vector2(direction * CLIMB_CHECK_DISTANCE, -CLIMB_HEIGHT)
	
	var tween = create_tween()
	tween.tween_property(self, "global_position", climb_target, 0.3)
	tween.finished.connect(_finish_climb)

func _handle_climb():
	# Do nothing while climbing animation plays
	pass

func _finish_climb():
	is_climbing = false
