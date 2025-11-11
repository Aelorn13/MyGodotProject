using Godot;

public partial class PlayerMultiplayer : Node
{
    private Player _player;
    private MultiplayerSynchronizer _synchronizer;

    public override void _Ready()
    {
        _player = GetParent<Player>();
        SetupMultiplayerSync();
    }

    private void SetupMultiplayerSync()
    {
        _synchronizer = _player.GetNodeOrNull<MultiplayerSynchronizer>("MultiplayerSynchronizer");

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

        config.PropertySetSpawn("Sprite2D:modulate", true);
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
}
