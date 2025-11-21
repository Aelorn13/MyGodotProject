using Godot;

public partial class SlimeAI : EnemyAI
{
    // Slime-specific settings
    [Export]
    public float JumpForce { get; set; } = -350f;

    [Export]
    public float IdleJumpInterval { get; set; } = 3.0f; // Jump every 3 seconds when idle

    [Export]
    public float AggroJumpInterval { get; set; } = 1.0f; // Jump every 1 second when chasing

    [Export]
    public float JumpTowardPlayerChance { get; set; } = 0.7f; // 70% chance to jump toward player

    private double _jumpTimer = 0.0;
    private RandomNumberGenerator _random = new RandomNumberGenerator();
    private double _lastJumpDebugTime = 0.0;

    public override void Initialize(Enemy enemy)
    {
        base.Initialize(enemy);
        _random.Randomize();
        GD.Print("SlimeAI initialized!");
    }

    public override void ProcessAI(double delta)
    {
        base.ProcessAI(delta);

        _jumpTimer += delta;
        _lastJumpDebugTime += delta;

        bool hasTarget = _enemy.CurrentTarget != null && HasLineOfSight(_enemy.CurrentTarget);
        float jumpInterval = hasTarget ? AggroJumpInterval : IdleJumpInterval;

        // Debug every 2 seconds
        if (_lastJumpDebugTime >= 2.0)
        {
            GD.Print(
                $"Slime check - Timer: {_jumpTimer:F1}/{jumpInterval:F1}, OnFloor: {_enemy.IsOnFloor()}, Velocity: {_enemy.Velocity}"
            );
            _lastJumpDebugTime = 0.0;
        }

        if (_jumpTimer >= jumpInterval && _enemy.IsOnFloor())
        {
            PerformJump(hasTarget);
            _jumpTimer = 0.0;
        }
    }

    private void PerformJump(bool hasTarget)
    {
        Vector2 velocity = _enemy.Velocity;

        // Vertical jump
        velocity.Y = JumpForce;

        // Horizontal movement
        if (hasTarget && _random.Randf() < JumpTowardPlayerChance)
        {
            // Jump toward player
            float directionToPlayer = Mathf.Sign(
                _enemy.CurrentTarget.Position.X - _enemy.Position.X
            );
            velocity.X = directionToPlayer * _enemy.MoveSpeed * 0.8f;
        }
        else
        {
            // Random small jump
            velocity.X = _random.RandfRange(-0.5f, 0.5f) * _enemy.MoveSpeed * 0.5f;
        }

        _enemy.Velocity = velocity;

        GD.Print($"Slime jumped! Has target: {hasTarget}");
    }

    protected override void UpdatePatrol(double delta)
    {
        // Slimes don't walk - they just idle and jump randomly
        _movement.Stop();
    }

    protected override void UpdateChase(double delta, Player target)
    {
        // Slimes don't chase by walking - they jump toward player
        // Movement is handled by jumping logic
        _movement.Stop();
    }

    protected override void UpdateAttack(double delta, Player target)
    {
        // Stop moving, let combat system handle the attack
        _movement.Stop();
    }
}
