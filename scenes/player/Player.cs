using Godot;

public partial class Player : CharacterBody2D
{
    // ===== MULTIPLAYER =====
    public int PlayerId { get; set; } = 1;

    // Components
    private PlayerMovement _movement;
    private PlayerCombat _combat;
    private PlayerHealth _health;

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(PlayerId);
    }

    public override void _Ready()
    {
        // Get component references
        _movement = GetNode<PlayerMovement>("PlayerMovement");
        _combat = GetNode<PlayerCombat>("PlayerCombat");
        _health = GetNode<PlayerHealth>("PlayerHealth");

        // Set up camera
        var camera = GetNodeOrNull<Camera2D>("Camera2D");
        if (camera != null)
        {
            camera.Enabled = IsMultiplayerAuthority();
        }

        // Connect to health signals
        _health.Respawned += OnRespawned;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority())
            return;

        _movement.ProcessMovement(delta);
        _combat.ProcessCombat(delta);
    }

    private void OnRespawned()
    {
        _movement.ResetFallTracking(Position);
    }

    // RPC methods that components can't handle directly
    [Rpc(
        MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    public void OnPlayerDied()
    {
        Visible = false;
        var collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        collisionShape.Disabled = true;
    }

    [Rpc(
        MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    public void OnPlayerRespawned()
    {
        Visible = true;
        var collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        collisionShape.Disabled = false;
    }
}
