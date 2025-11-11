using Godot;

public partial class EnemyCombat : Node
{
	private Enemy _enemy;
	private Area2D _attackArea;
	private double _attackCooldownTimer = 0.0;
	private bool _isAttacking = false;

	public bool CanAttack => _attackCooldownTimer <= 0 && !_isAttacking;

	public void Initialize(Enemy enemy)
	{
		_enemy = enemy;
		_attackArea = enemy.GetNodeOrNull<Area2D>("AttackArea");
	}

	public void ProcessCombat(double delta)
	{
		if (_attackCooldownTimer > 0)
		{
			_attackCooldownTimer -= delta;
		}
	}

	public void PerformAttack()
	{
		if (!CanAttack)
			return;
		
		_isAttacking = true;
		_attackCooldownTimer = _enemy.AttackCooldown;
		
		// Show attack area briefly
		if (_attackArea != null)
		{
			_attackArea.Visible = true;
			
			// Position based on facing direction
			int direction = _enemy.GetNode<Sprite2D>("Sprite2D").FlipH ? -1 : 1;
			_attackArea.Position = new Vector2(direction * 30, 0);
			
			CheckAttackHits();
			
			// Hide after brief delay
			_enemy.GetTree().CreateTimer(0.2).Timeout += () =>
			{
				if (_attackArea != null)
					_attackArea.Visible = false;
				_isAttacking = false;
			};
		}
		
		GD.Print($"Enemy {_enemy.EnemyName} attacked!");
	}

	private void CheckAttackHits()
	{
		if (_attackArea == null)
			return;
		
		var overlappingBodies = _attackArea.GetOverlappingBodies();
		
		foreach (var body in overlappingBodies)
		{
			if (body is Player player)
			{
				var playerHealth = player.GetNode<PlayerHealth>("PlayerHealth");
				if (playerHealth != null)
				{
					playerHealth.RpcId(player.PlayerId, PlayerHealth.MethodName.TakeDamage, _enemy.AttackDamage, _enemy.EnemyId);
					GD.Print($"Enemy hit player {player.PlayerId}!");
				}
			}
		}
	}
}
