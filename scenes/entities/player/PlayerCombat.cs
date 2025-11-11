using Godot;

public partial class PlayerCombat : Node
{
	[Export]
	public int AttackDamage { get; set; } = 20;

	[Export]
	public float AttackRange { get; set; } = 50f;

	[Export]
	public float AttackCooldown { get; set; } = 0.5f;

	private Player _player;
	private Sprite2D _sprite;
	private Area2D _attackArea;

	private double _attackCooldownTimer = 0.0;
	private bool _isAttacking = false;

	public override void _Ready()
	{
		_player = GetParent<Player>();
		_sprite = _player.GetNode<Sprite2D>("Sprite2D");
		_attackArea = _player.GetNode<Area2D>("AttackArea");
	}

	public void ProcessCombat(double delta)
	{
		UpdateCooldown(delta);
		HandleAttackInput();
	}

	private void UpdateCooldown(double delta)
	{
		if (_attackCooldownTimer > 0)
		{
			_attackCooldownTimer -= delta;
		}
	}

	private void HandleAttackInput()
	{
		if (Input.IsActionJustPressed("attack") && _attackCooldownTimer <= 0)
		{
			PerformAttack();
		}
	}

	private void PerformAttack()
	{
		_isAttacking = true;
		_attackCooldownTimer = AttackCooldown;

		// Position attack area based on facing direction
		int direction = _sprite.FlipH ? -1 : 1;
		_attackArea.Position = new Vector2(direction * 30, 0);

		var collisionShape = _attackArea.GetNode<CollisionShape2D>("CollisionShape2D");
		collisionShape.Position = new Vector2(direction * 15, 0);

		_attackArea.Visible = true;

		CheckAttackHits();

		_player.GetTree().CreateTimer(0.1).Timeout += () =>
		{
			if (_attackArea != null)
			{
				_attackArea.Visible = false;
			}
			_isAttacking = false;
		};
	}

	private void CheckAttackHits()
	{
		var overlappingBodies = _attackArea.GetOverlappingBodies();

		foreach (var body in overlappingBodies)
		{
			if (body == _player)
				continue;

			if (body is Player otherPlayer)
			{
				var otherHealth = otherPlayer.GetNode<PlayerHealth>("PlayerHealth");
				otherHealth.RpcId(
					otherPlayer.PlayerId,
					PlayerHealth.MethodName.TakeDamage,
					AttackDamage,
					_player.PlayerId
				);
			}
			else if (body is Enemy enemy)
		{
			var enemyHealth = enemy.GetNode<EnemyHealth>("EnemyHealth");
			enemyHealth.TakeDamage(AttackDamage, _player.PlayerId);
		}
		}
	}
}
