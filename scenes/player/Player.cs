using Godot;

public partial class Player : CharacterBody2D
{
    // ===== MULTIPLAYER =====
    // NO [Export] here - prevents sync issues
    public int PlayerId { get; set; } = 1;

    private MultiplayerSynchronizer _synchronizer;

    // ===== MOVEMENT CONSTANTS =====
    [Export]
    public float Speed = 300.0f;

    [Export]
    public float Acceleration = 2000.0f;

    [Export]
    public float Friction = 1500.0f;

    [Export]
    public float JumpVelocity = -400.0f;

    [Export]
    public float FallGravityMultiplier = 1.5f;

    [Export]
    public float MaxFallSpeed = 600.0f;

    // ===== JUMP FEEL CONSTANTS =====
    [Export]
    public float CoyoteTime = 0.15f;

    [Export]
    public float JumpBufferTime = 0.1f;

    [Export]
    public float VariableJumpMultiplier = 0.4f;

    // ===== FALL DAMAGE =====
    [Export]
    public float FallDamageThreshold = 800f; // Fall speed that starts damage

    [Export]
    public float InstantDeathFallSpeed = 1500f; // Instant death speed

    [Export]
    public float FallDamageMultiplier = 0.05f; // Damage per speed unit

    [Export]
    public float DeathZoneY = 2000f; // Y position that kills instantly

    // ===== STATE TRACKING =====
    private double _coyoteTimer = 0.0;
    private double _jumpBufferTimer = 0.0;
    private float _lastGroundedY = 0f;
    private bool _isDead = false;
    private bool _hasTakenFallDamage = false;

    // Node references
    private Sprite2D _sprite;
    private float _gravity;
    private Area2D _attackArea;

    public override void _EnterTree()
    {
        // Set authority FIRST, before anything else
        SetMultiplayerAuthority(PlayerId);

        GD.Print(
            $"[{Multiplayer.GetUniqueId()}] Player {Name} EnterTree - PlayerId: {PlayerId}, Setting authority to: {PlayerId}"
        );
    }

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
        _attackArea = GetNode<Area2D>("AttackArea");
        CurrentHealth = MaxHealth;
        SetupMultiplayerSync();

        // Enable camera ONLY for our player
        var camera = GetNodeOrNull<Camera2D>("Camera2D");
        if (camera != null)
        {
            camera.Enabled = IsMultiplayerAuthority();
        }

        GD.Print(
            $"[{Multiplayer.GetUniqueId()}] Player {Name} Ready - Authority: {IsMultiplayerAuthority()}, PlayerId: {PlayerId}, Camera: {camera?.Enabled}"
        );
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
        config.AddProperty(":visible");

        config.PropertySetSpawn(":position", true);
        config.PropertySetReplicationMode(
            ":position",
            SceneReplicationConfig.ReplicationMode.OnChange
        );

        config.PropertySetSpawn(":velocity", true);
        config.PropertySetReplicationMode(
            ":velocity",
            SceneReplicationConfig.ReplicationMode.OnChange
        );

        config.PropertySetSpawn("Sprite2D:flip_h", true);
        config.PropertySetReplicationMode(
            "Sprite2D:flip_h",
            SceneReplicationConfig.ReplicationMode.OnChange
        );

        config.PropertySetSpawn("Sprite2D:modulate", true); // Tell it to send this value when the node spawns
        config.PropertySetReplicationMode(
            "Sprite2D:modulate",
            SceneReplicationConfig.ReplicationMode.OnChange
        );

        config.PropertySetSpawn(":visible", true);
        config.PropertySetReplicationMode(
            ":visible",
            SceneReplicationConfig.ReplicationMode.OnChange
        );
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
        HandleAttack();

        MoveAndSlide();
        CheckFallDamage();
        CheckDeathZone();
    }

    private void CheckFallDamage()
    {
        if (IsOnFloor())
        {
            // Only check fall damage once per landing
            if (!_hasTakenFallDamage)
            {
                float fallDistance = Position.Y - _lastGroundedY;

                if (fallDistance > 100f && Mathf.Abs(Velocity.Y) > FallDamageThreshold)
                {
                    float fallSpeed = Mathf.Abs(Velocity.Y);

                    if (fallSpeed >= InstantDeathFallSpeed)
                    {
                        GD.Print($"Player {PlayerId} died from extreme fall! Speed: {fallSpeed}");
                        CurrentHealth = 0;
                    }
                    else
                    {
                        int damage = (int)(
                            (fallSpeed - FallDamageThreshold) * FallDamageMultiplier
                        );

                        if (damage > 0)
                        {
                            CurrentHealth -= damage;
                            GD.Print(
                                $"Player {PlayerId} took {damage} fall damage! Speed: {fallSpeed}"
                            );
                            FlashDamage();
                        }
                    }
                }

                _hasTakenFallDamage = true; // Mark that we checked
            }

            _lastGroundedY = Position.Y;
        }
        else
        {
            // Reset flag when in air
            _hasTakenFallDamage = false;
        }
    }

    // NEW METHOD - Check if player fell out of bounds
    private void CheckDeathZone()
    {
        if (_isDead || Position.Y <= DeathZoneY)
            return;

        GD.Print($"Player {PlayerId} fell out of bounds at Y: {Position.Y}");
        CurrentHealth = 0; // Instant death
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
        if (_attackCooldownTimer > 0)
        {
            _attackCooldownTimer -= delta;
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

    private void HandleAttack()
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

        // Flip the collision shape too
        var collisionShape = _attackArea.GetNode<CollisionShape2D>("CollisionShape2D");
        collisionShape.Position = new Vector2(direction * 15, 0);

        // Show attack area briefly
        _attackArea.Visible = true;

        // Check for hits
        CheckAttackHits();

        // Hide attack area after a short delay
        GetTree().CreateTimer(0.1).Timeout += () =>
        {
            if (_attackArea != null)
            {
                _attackArea.Visible = false;
            }
            _isAttacking = false;
        };

        GD.Print($"Player {PlayerId} attacked!");
    }

    private void CheckAttackHits()
    {
        var overlappingBodies = _attackArea.GetOverlappingBodies();

        foreach (var body in overlappingBodies)
        {
            // Don't hit ourselves
            if (body == this)
                continue;

            // Check if it's another player
            if (body is Player otherPlayer)
            {
                // Deal damage via RPC
                RpcId(otherPlayer.PlayerId, MethodName.TakeDamage, AttackDamage, PlayerId);
                GD.Print($"Hit player {otherPlayer.PlayerId}!");
            }
        }
    }

    // RPC to take damage (called on the victim's client)
    [Rpc(
        MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void TakeDamage(int damage, int attackerId)
    {
        // Only the victim (authority of this node) processes damage
        if (!IsMultiplayerAuthority())
            return;

        CurrentHealth -= damage;

        GD.Print(
            $"Player {PlayerId} took {damage} damage from {attackerId}. Health: {CurrentHealth}/{MaxHealth}"
        );

        // Visual feedback - flash red
        FlashDamage();
    }

    // Visual feedback when taking damage
    private void FlashDamage()
    {
        var originalColor = _sprite.Modulate;
        _sprite.Modulate = new Color(1, 0.3f, 0.3f); // Red tint

        GetTree().CreateTimer(0.1).Timeout += () =>
        {
            if (_sprite != null)
            {
                _sprite.Modulate = originalColor;
            }
        };
    }

    // NEW METHOD - Handle death
    private void Die()
    {
        if (!IsMultiplayerAuthority() || _isDead)
            return;
        _isDead = true;

        GD.Print($"Player {PlayerId} died!");

        // Tell everyone this player died
        Rpc(MethodName.OnPlayerDied);

        // Respawn after delay
        GetTree().CreateTimer(2.0).Timeout += Respawn;
    }

    // RPC to notify all clients of death
    [Rpc(
        MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void OnPlayerDied()
    {
        // Hide player temporarily
        Visible = false;

        // Disable collisions
        var collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        collisionShape.Disabled = true;

        GD.Print($"Player {PlayerId} is dead and hidden");
    }

    // NEW METHOD - Respawn playerd
    private void Respawn()
    {
        if (!IsMultiplayerAuthority())
            return;
        _isDead = false;
        // Reset health
        CurrentHealth = MaxHealth;

        // Reset velocity
        Velocity = Vector2.Zero;
        var level = GetTree().Root.GetNode<MultiplayerLevel>("test_level");
        if (level != null)
        {
            Position = level.GetRespawnPosition(PlayerId);
            _lastGroundedY = Position.Y; // Reset fall tracking
        }
        Rpc(MethodName.OnPlayerRespawned);
        // Get new spawn position from level
        RequestRespawn();
    }

    // Request respawn position from server
    private void RequestRespawn()
    {
        // For now, just respawn at a default position
        // Later, the level manager will handle this properly

        // Tell everyone to show this player again
        Rpc(MethodName.OnPlayerRespawned);
    }

    // RPC to show player after respawn
    [Rpc(
        MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void OnPlayerRespawned()
    {
        // Show player
        Visible = true;

        // Enable collisions
        var collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        collisionShape.Disabled = false;

        GD.Print($"Player {PlayerId} respawned with {CurrentHealth} health");
    }

    // ===== HEALTH SYSTEM =====
    [Export]
    public int MaxHealth { get; set; } = 100;
    private int _currentHealth;

    public int CurrentHealth
    {
        get => _currentHealth;
        set
        {
            _currentHealth = Mathf.Clamp(value, 0, MaxHealth);
            GD.Print($"Player {PlayerId} health: {_currentHealth}/{MaxHealth}");
            if (_currentHealth <= 0)
            {
                Die();
            }
        }
    }

    [Export]
    public int AttackDamage { get; set; } = 20;

    [Export]
    public float AttackRange { get; set; } = 50f;

    [Export]
    public float AttackCooldown { get; set; } = 0.5f;

    private double _attackCooldownTimer = 0.0;
    private bool _isAttacking = false;
}
