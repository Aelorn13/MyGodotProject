using Godot;

public partial class PlayerMovement : Node
{
    [Export]
    public float Speed { get; set; } = 300.0f;

    [Export]
    public float Acceleration { get; set; } = 2000.0f;

    [Export]
    public float Friction { get; set; } = 1500.0f;

    [Export]
    public float JumpVelocity { get; set; } = -400.0f;

    [Export]
    public float FallGravityMultiplier { get; set; } = 1.5f;

    [Export]
    public float MaxFallSpeed { get; set; } = 600.0f;

    [Export]
    public float CoyoteTime { get; set; } = 0.15f;

    [Export]
    public float JumpBufferTime { get; set; } = 0.1f;

    [Export]
    public float VariableJumpMultiplier { get; set; } = 0.4f;

    // Fall damage settings
    [Export]
    public float FallDamageThreshold { get; set; } = 800f;

    [Export]
    public float InstantDeathFallSpeed { get; set; } = 1500f;

    [Export]
    public float FallDamageMultiplier { get; set; } = 0.05f;

    [Export]
    public float DeathZoneY { get; set; } = 2000f;

    private Player _player;
    private Sprite2D _sprite;
    private PlayerHealth _health;
    private float _gravity;

    private double _coyoteTimer = 0.0;
    private double _jumpBufferTimer = 0.0;
    private float _lastGroundedY = 0f;
    private bool _hasTakenFallDamage = false;

    public override void _Ready()
    {
        _player = GetParent<Player>();
        _sprite = _player.GetNode<Sprite2D>("Sprite2D");
        _health = _player.GetNode<PlayerHealth>("PlayerHealth");
        _gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    }

    public void ProcessMovement(double delta)
    {
        ApplyGravity(delta);
        UpdateTimers(delta);
        HandleJump();
        HandleHorizontalMovement(delta);

        _player.MoveAndSlide();

        CheckFallDamage();
        CheckDeathZone();
    }

    private void ApplyGravity(double delta)
    {
        if (!_player.IsOnFloor())
        {
            float fDelta = (float)delta;
            float gravityMultiplier = _player.Velocity.Y > 0 ? FallGravityMultiplier : 1.0f;

            Vector2 velocity = _player.Velocity;
            velocity.Y += _gravity * gravityMultiplier * fDelta;
            velocity.Y = Mathf.Min(velocity.Y, MaxFallSpeed);
            _player.Velocity = velocity;

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

        bool canJump = _player.IsOnFloor() || _coyoteTimer > 0;
        bool wantsToJump = _jumpBufferTimer > 0;

        if (wantsToJump && canJump)
        {
            Vector2 velocity = _player.Velocity;
            velocity.Y = JumpVelocity;
            _player.Velocity = velocity;

            _jumpBufferTimer = 0;
            _coyoteTimer = 0;
        }

        if (_player.Velocity.Y < 0 && !Input.IsActionPressed("jump"))
        {
            Vector2 velocity = _player.Velocity;
            velocity.Y *= VariableJumpMultiplier;
            _player.Velocity = velocity;
        }
    }

    private void HandleHorizontalMovement(double delta)
    {
        float fDelta = (float)delta;
        float direction = Input.GetAxis("move_left", "move_right");

        Vector2 velocity = _player.Velocity;

        if (direction != 0)
        {
            velocity.X = Mathf.MoveToward(velocity.X, direction * Speed, Acceleration * fDelta);
            _sprite.FlipH = direction < 0;
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, Friction * fDelta);
        }

        _player.Velocity = velocity;
    }

    private void CheckFallDamage()
    {
        if (_player.IsOnFloor())
        {
            if (!_hasTakenFallDamage)
            {
                float fallDistance = _player.Position.Y - _lastGroundedY;

                if (fallDistance > 100f && Mathf.Abs(_player.Velocity.Y) > FallDamageThreshold)
                {
                    float fallSpeed = Mathf.Abs(_player.Velocity.Y);

                    if (fallSpeed >= InstantDeathFallSpeed)
                    {
                        _health.CurrentHealth = 0;
                    }
                    else
                    {
                        int damage = (int)(
                            (fallSpeed - FallDamageThreshold) * FallDamageMultiplier
                        );
                        if (damage > 0)
                        {
                            _health.ApplyFallDamage(damage);
                        }
                    }
                }

                _hasTakenFallDamage = true;
            }

            _lastGroundedY = _player.Position.Y;
        }
        else
        {
            _hasTakenFallDamage = false;
        }
    }

    private void CheckDeathZone()
    {
        if (_health.IsDead || _player.Position.Y <= DeathZoneY)
            return;

        _health.CurrentHealth = 0;
    }

    public void ResetFallTracking(Vector2 position)
    {
        _lastGroundedY = position.Y;
    }
}
