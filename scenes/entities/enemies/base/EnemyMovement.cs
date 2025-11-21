using Godot;

public partial class EnemyMovement : Node
{
	private Enemy _enemy;
	private Sprite2D _sprite;
	private float _gravity;

	public Vector2 DesiredVelocity { get; set; } = Vector2.Zero;

	public void Initialize(Enemy enemy)
	{
		_enemy = enemy;
		_sprite = enemy.GetNode<Sprite2D>("Sprite2D");
		_gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	}

	public void ProcessMovement(double delta)
	{
		Vector2 velocity = _enemy.Velocity;

		// Apply gravity
		if (!_enemy.IsOnFloor())
		{
			velocity.Y += _gravity * (float)delta;
		}

		// Apply desired horizontal movement
		velocity.X = DesiredVelocity.X;

		// Flip sprite based on movement direction
		if (velocity.X != 0)
		{
			_sprite.FlipH = velocity.X < 0;
		}

		_enemy.Velocity = velocity;
	}

	public void MoveToward(Vector2 targetPosition, float speed)
	{
		float direction = Mathf.Sign(targetPosition.X - _enemy.Position.X);
		DesiredVelocity = new Vector2(direction * speed, 0);
	}

	public void Stop()
	{
		DesiredVelocity = Vector2.Zero;
	}
}
