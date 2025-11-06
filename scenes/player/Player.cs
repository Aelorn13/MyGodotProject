using Godot;

public partial class Player : CharacterBody2D
{
	// ===== MULTIPLAYER =====
	// NO [Export] here - prevents sync issues
	public int PlayerId { get; set; } = 1;
	
	private MultiplayerSynchronizer _synchronizer;
	
	// ===== MOVEMENT CONSTANTS =====
	[Export] public float Speed = 300.0f;
	[Export] public float Acceleration = 2000.0f;
	[Export] public float Friction = 1500.0f;

	[Export] public float JumpVelocity = -400.0f;
	[Export] public float FallGravityMultiplier = 1.5f;
	[Export] public float MaxFallSpeed = 600.0f;

	// ===== JUMP FEEL CONSTANTS =====
	[Export] public float CoyoteTime = 0.15f;
	[Export] public float JumpBufferTime = 0.1f;
	[Export] public float VariableJumpMultiplier = 0.4f;

	// ===== STATE TRACKING =====
	private double _coyoteTimer = 0.0;
	private double _jumpBufferTimer = 0.0;

	// Node references
	private Sprite2D _sprite;
	private float _gravity;

	public override void _EnterTree()
	{
		// Set authority FIRST, before anything else
		SetMultiplayerAuthority(PlayerId);
		
		GD.Print($"[{Multiplayer.GetUniqueId()}] Player {Name} EnterTree - PlayerId: {PlayerId}, Setting authority to: {PlayerId}");
	}

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
		
		SetupMultiplayerSync();
		
		// Enable camera ONLY for our player
		var camera = GetNodeOrNull<Camera2D>("Camera2D");
		if (camera != null)
		{
			camera.Enabled = IsMultiplayerAuthority();
		}
		
		GD.Print($"[{Multiplayer.GetUniqueId()}] Player {Name} Ready - Authority: {IsMultiplayerAuthority()}, PlayerId: {PlayerId}, Camera: {camera?.Enabled}");
	}
	
	private void SetupMultiplayerSync()
	{
		_synchronizer = GetNodeOrNull<MultiplayerSynchronizer>("MultiplayerSynchronizer");
		
		if (_synchronizer == null)
		{
			GD.PrintErr("MultiplayerSynchronizer not found!");
			return;
		}
		
		var config = new SceneReplicationConfig();
		
		config.AddProperty(":position");
		config.AddProperty(":velocity");
		config.AddProperty("Sprite2D:flip_h");
		config.AddProperty("Sprite2D:modulate");
		
		config.PropertySetSpawn(":position", true);
		config.PropertySetReplicationMode(":position", SceneReplicationConfig.ReplicationMode.OnChange);
		
		config.PropertySetSpawn(":velocity", true);
		config.PropertySetReplicationMode(":velocity", SceneReplicationConfig.ReplicationMode.OnChange);
		
		config.PropertySetSpawn("Sprite2D:flip_h", true);
		config.PropertySetReplicationMode("Sprite2D:flip_h", SceneReplicationConfig.ReplicationMode.OnChange);
		
		config.PropertySetSpawn("Sprite2D:modulate", true); // Tell it to send this value when the node spawns
		config.PropertySetReplicationMode("Sprite2D:modulate", SceneReplicationConfig.ReplicationMode.OnChange);
		
		_synchronizer.ReplicationConfig = config;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsMultiplayerAuthority())
		{
			return;
		}

		ApplyGravity(delta);
		UpdateTimers(delta);
		HandleJump();
		HandleHorizontalMovement(delta);
		
		MoveAndSlide();
	}

	private void ApplyGravity(double delta)
	{
		if (!IsOnFloor())
		{
			float fDelta = (float)delta;
			float gravityMultiplier = Velocity.Y > 0 ? FallGravityMultiplier : 1.0f;
			
			Vector2 velocity = Velocity;
			velocity.Y += _gravity * gravityMultiplier * fDelta;
			velocity.Y = Mathf.Min(velocity.Y, MaxFallSpeed);
			Velocity = velocity;
			
			_coyoteTimer -= delta;
		}
		else
		{
			_coyoteTimer = CoyoteTime;
		}
	}

	private void UpdateTimers(double delta)
	{
		if (_jumpBufferTimer > 0)
		{
			_jumpBufferTimer -= delta;
		}
	}

	private void HandleJump()
	{
		if (Input.IsActionJustPressed("jump"))
		{
			_jumpBufferTimer = JumpBufferTime;
		}

		bool canJump = IsOnFloor() || _coyoteTimer > 0;
		bool wantsToJump = _jumpBufferTimer > 0;

		if (wantsToJump && canJump)
		{
			Vector2 velocity = Velocity;
			velocity.Y = JumpVelocity;
			Velocity = velocity;

			_jumpBufferTimer = 0;
			_coyoteTimer = 0;
		}

		if (Velocity.Y < 0 && !Input.IsActionPressed("jump"))
		{
			Vector2 velocity = Velocity;
			velocity.Y *= VariableJumpMultiplier;
			Velocity = velocity;
		}
	}
	
	private void HandleHorizontalMovement(double delta)
	{
		float fDelta = (float)delta;
		float direction = Input.GetAxis("move_left", "move_right");
		
		Vector2 velocity = Velocity;

		if (direction != 0)
		{
			velocity.X = Mathf.MoveToward(velocity.X, direction * Speed, Acceleration * fDelta);
			_sprite.FlipH = direction < 0;
		}
		else
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, Friction * fDelta);
		}
		
		Velocity = velocity;
	}
}
