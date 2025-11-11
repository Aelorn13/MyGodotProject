using Godot;

public partial class PlayerHealth : Node
{
	[Export]
	public int MaxHealth { get; set; } = 100;

	private int _currentHealth;
	private bool _isDead = false;

	private Player _player;
	private Sprite2D _sprite;

	[Signal]
	public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

	[Signal]
	public delegate void DiedEventHandler();

	[Signal]
	public delegate void RespawnedEventHandler();

	public int CurrentHealth
	{
		get => _currentHealth;
		set
		{
			_currentHealth = Mathf.Clamp(value, 0, MaxHealth);
			EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);

			if (_currentHealth <= 0 && !_isDead)
			{
				Die();
			}
		}
	}

	public bool IsDead => _isDead;

	public override void _Ready()
	{
		_player = GetParent<Player>();
		_sprite = _player.GetNode<Sprite2D>("Sprite2D");
		CurrentHealth = MaxHealth;
	}

	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		CallLocal = true,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	public void TakeDamage(int damage, int attackerId)
	{
		if (!_player.IsMultiplayerAuthority())
			return;

		CurrentHealth -= damage;
		GD.Print(
			$"Player {_player.PlayerId} took {damage} damage. Health: {CurrentHealth}/{MaxHealth}"
		);

		FlashDamage();
	}

	public void ApplyFallDamage(int damage)
	{
		if (!_player.IsMultiplayerAuthority())
			return;

		CurrentHealth -= damage;
		GD.Print(
			$"Player {_player.PlayerId} took {damage} fall damage. Health: {CurrentHealth}/{MaxHealth}"
		);
		FlashDamage();
	}

	private void FlashDamage()
	{
		var originalColor = _sprite.Modulate;
		_sprite.Modulate = new Color(1, 0.3f, 0.3f);

		_player.GetTree().CreateTimer(0.1).Timeout += () =>
		{
			if (_sprite != null)
			{
				_sprite.Modulate = originalColor;
			}
		};
	}

	private void Die()
	{
		if (!_player.IsMultiplayerAuthority() || _isDead)
			return;

		_isDead = true;
		GD.Print($"Player {_player.PlayerId} died!");

		EmitSignal(SignalName.Died);
		_player.Rpc(Player.MethodName.OnPlayerDied);

		_player.GetTree().CreateTimer(2.0).Timeout += Respawn;
	}

	public void Respawn(Vector2 spawnPosition)
	{
		if (!_player.IsMultiplayerAuthority())
			return;

		_isDead = false;
		CurrentHealth = MaxHealth;
		_player.Velocity = Vector2.Zero;
		_player.Position = spawnPosition;

		EmitSignal(SignalName.Respawned);
		_player.Rpc(Player.MethodName.OnPlayerRespawned);
	}

	private void Respawn()
	{
		var level = _player.GetTree().Root.GetNode<MultiplayerLevel>("test_level");
		if (level != null)
		{
			Respawn(level.GetRespawnPosition(_player.PlayerId));
		}
	}
}
