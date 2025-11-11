using Godot;

public partial class EnemyHealth : Node
{
	private Enemy _enemy;
	private int _maxHealth;
	private int _currentHealth;
	
	[Signal]
	public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);
	
	[Signal]
	public delegate void DiedEventHandler();

	public int CurrentHealth => _currentHealth;
	public int MaxHealth => _maxHealth;
	public bool IsDead => _currentHealth <= 0;

	public void Initialize(Enemy enemy, int maxHealth)
	{
		_enemy = enemy;
		_maxHealth = maxHealth;
		_currentHealth = maxHealth;
	}

	public void TakeDamage(int damage, int attackerId)
	{
		if (IsDead)
			return;
		
		_currentHealth = Mathf.Max(0, _currentHealth - damage);
		EmitSignal(SignalName.HealthChanged, _currentHealth, _maxHealth);
		
		GD.Print($"Enemy {_enemy.EnemyName} took {damage} damage. Health: {_currentHealth}/{_maxHealth}");
		
		if (_currentHealth <= 0)
		{
			Die();
		}
	}

	private void Die()
	{
		EmitSignal(SignalName.Died);
		GD.Print($"Enemy {_enemy.EnemyName} died!");
		
		// Tell all clients about death
		_enemy.Rpc(Enemy.MethodName.OnEnemyDied);
		
		// Despawn after delay
		_enemy.GetTree().CreateTimer(2.0).Timeout += () =>
		{
			if (_enemy != null)
				_enemy.QueueFree();
		};
	}
}
