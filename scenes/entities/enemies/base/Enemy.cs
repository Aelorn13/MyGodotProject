using Godot;

public partial class Enemy : CharacterBody2D
{
	// ===== IDENTIFICATION =====
	[Export] public string EnemyName { get; set; } = "Enemy";
	[Export] public int EnemyId { get; set; } = 0; // Set by spawner
	
	// ===== BASE STATS =====
	[Export] public int MaxHealth { get; set; } = 50;
	[Export] public int AttackDamage { get; set; } = 10;
	[Export] public float MoveSpeed { get; set; } = 150f;
	[Export] public float DetectionRange { get; set; } = 300f;
	[Export] public float AttackRange { get; set; } = 40f;
	[Export] public float AttackCooldown { get; set; } = 1.5f;
	
	// Components (will be set by children)
	protected EnemyHealth _health;
	protected EnemyAI _ai;
	protected EnemyMovement _movement;
	protected EnemyCombat _combat;
	protected Sprite2D _sprite;
	
	// Reference to current target
	public Player CurrentTarget { get; set; }

	public override void _Ready()
	{
		// Get component references
		_health = GetNodeOrNull<EnemyHealth>("EnemyHealth");
		_ai = GetNodeOrNull<EnemyAI>("EnemyAI");
		_movement = GetNodeOrNull<EnemyMovement>("EnemyMovement");
		_combat = GetNodeOrNull<EnemyCombat>("EnemyCombat");
		_sprite = GetNode<Sprite2D>("Sprite2D");
		
		// Set up multiplayer authority (server controls enemies)
		if (Multiplayer.IsServer())
		{
			SetMultiplayerAuthority(1); // Server always ID 1
		}
		
		// Initialize components
		if (_health != null)
			_health.Initialize(this, MaxHealth);
		
		if (_combat != null)
			_combat.Initialize(this);
		
		if (_movement != null)
			_movement.Initialize(this);
		
		if (_ai != null)
			_ai.Initialize(this);
	}

public override void _PhysicsProcess(double delta)
{
	// Only server controls enemy AI and physics
	if (!IsMultiplayerAuthority())
		return;
	
	// Process AI (decides what to do)
	_ai?.ProcessAI(delta);
	
	// Process movement (executes movement)
	_movement?.ProcessMovement(delta);
	
	// Process combat (executes attacks)
	_combat?.ProcessCombat(delta);
	
	// Don't allow enemies to be pushed by players
	var beforePosition = Position;
	MoveAndSlide();
	
	// If we were pushed, restore position
	CheckPlayerPush(beforePosition);
}

private void CheckPlayerPush(Vector2 beforePosition)
{
	// Check if we collided with a player
	for (int i = 0; i < GetSlideCollisionCount(); i++)
	{
		var collision = GetSlideCollision(i);
		if (collision.GetCollider() is Player)
		{
			// Restore position to prevent being pushed
			Position = beforePosition;
			return;
		}
	}
}
	
	// RPC for death animation/effects
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void OnEnemyDied()
	{
		// Visual death effects (all clients see this)
		_sprite.Modulate = new Color(1, 0, 0, 0.5f); // Red transparent
		
		// Disable collision
		var collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collisionShape != null)
			collisionShape.Disabled = true;
	}
}
