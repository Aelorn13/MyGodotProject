using Godot;

public partial class MultiplayerLevel : Node2D
{
    private PackedScene PlayerScene => GD.Load<PackedScene>("res://scenes/player/player.tscn");

    private Node2D _spawnPoints;
    private Node2D _playersContainer;
    private MultiplayerSpawner _multiplayerSpawner;

    public override void _Ready()
    {
        // --- 1. GET NODE REFERENCES ---
        _spawnPoints = GetNode<Node2D>("SpawnPoints");
        _playersContainer = GetNode<Node2D>("Players");

        if (PlayerScene == null)
        {
            GD.PrintErr("FATAL: Could not load player scene!");
            return;
        }

        // --- 2. CREATE AND CONFIGURE THE SPAWNER ---
        _multiplayerSpawner = new MultiplayerSpawner();
        _multiplayerSpawner.Name = "MultiplayerSpawner";
        AddChild(_multiplayerSpawner);

        _multiplayerSpawner.SpawnPath = _playersContainer.GetPath();
        _multiplayerSpawner.AddSpawnableScene(PlayerScene.ResourcePath);
        _multiplayerSpawner.SpawnFunction = new Callable(this, MethodName.SpawnPlayerFunction);
        GD.Print("MultiplayerSpawner configured in _Ready.");

        // --- 3. CONNECT SIGNALS ---
        var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
        networkManager.PlayerConnected += OnPlayerConnected;
        networkManager.PlayerDisconnected += OnPlayerDisconnected;

        // --- 4. SERVER SPAWNS ITSELF ---
        if (Multiplayer.IsServer())
        {
            GD.Print("Server is in _Ready. Spawning host (player 1).");
            SpawnPlayer(Multiplayer.GetUniqueId());
        }
    }

    private void OnPlayerConnected(int peerId)
    {
        // The server's own connection signal (ID 1) is ignored because the host is already handled by _Ready().
        if (peerId == 1)
        {
            return;
        }

        // The server spawns all connecting clients.
        if (Multiplayer.IsServer())
        {
            GD.Print($"Peer {peerId} has connected. Server is now spawning their player.");
            SpawnPlayer(peerId);
        }
    }

    private void OnPlayerDisconnected(int peerId)
    {
        GD.Print($"Player {peerId} disconnected. Removing player node.");
        var playerNode = _playersContainer.GetNodeOrNull($"Player_{peerId}");
        if (playerNode != null)
        {
            playerNode.QueueFree();
        }
    }

    private void SpawnPlayer(int peerId)
    {
        // Safety check remains.
        if (_playersContainer.GetNodeOrNull($"Player_{peerId}") != null)
        {
            GD.PrintErr($"Attempted to spawn Player_{peerId}, but they already exist. Aborting.");
            return;
        }

        GD.Print($"Server is issuing spawn command for peer {peerId}.");
        var spawnData = new Godot.Collections.Array { peerId };
        _multiplayerSpawner.Spawn(spawnData);
    }

    private Node SpawnPlayerFunction(Godot.Collections.Array data)
    {
        int peerId = data[0].AsInt32();

        var player = PlayerScene.Instantiate<Player>();
        player.Name = $"Player_{peerId}";
        player.PlayerId = peerId;

        int spawnIndex = (peerId - 1) % _spawnPoints.GetChildCount();
        var spawnPoint = _spawnPoints.GetChild<Node2D>(spawnIndex);
        player.GlobalPosition = spawnPoint.GlobalPosition;

        var sprite = player.GetNode<Sprite2D>("Sprite2D");
        sprite.Modulate = GetPlayerColor(peerId);

        GD.Print($"Spawn function executed for player {peerId}.");

        return player;
    }

    private Color GetPlayerColor(int peerId)
    {
        Color[] colors = { Colors.White, Colors.LightBlue, Colors.LightGreen, Colors.Yellow };
        return colors[(peerId - 1) % colors.Length];
    }

    public Vector2 GetRespawnPosition(int peerId)
    {
        // Cycle through spawn points
        int spawnIndex = (peerId - 1) % _spawnPoints.GetChildCount();
        var spawnPoint = _spawnPoints.GetChild<Node2D>(spawnIndex);
        return spawnPoint.GlobalPosition;
    }
}
