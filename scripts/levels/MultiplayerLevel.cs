using Godot;

public partial class MultiplayerLevel : Node2D
{
	private PackedScene PlayerScene => GD.Load<PackedScene>("res://scenes/player/player.tscn");
	
	private Node2D _spawnPoints;
	private Node2D _playersContainer;

	public override void _Ready()
	{
		_spawnPoints = GetNode<Node2D>("SpawnPoints");
		_playersContainer = GetNode<Node2D>("Players");
		
		// Connect to network events
		var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
		networkManager.PlayerConnected += OnPlayerConnected;
		networkManager.PlayerDisconnected += OnPlayerDisconnected;
		
		// If we're the server, spawn players for existing connections
		if (Multiplayer.IsServer())
		{
			// Spawn for host
			SpawnPlayer(Multiplayer.GetUniqueId());
			
			// Spawn for already connected peers
			foreach (int peerId in Multiplayer.GetPeers())
			{
				SpawnPlayer(peerId);
			}
		}
	}

	private void OnPlayerConnected(int peerId)
	{
		GD.Print($"Player {peerId} connected, spawning player");
		
		// Only server spawns players
		if (Multiplayer.IsServer())
		{
			SpawnPlayer(peerId);
		}
	}

	private void OnPlayerDisconnected(int peerId)
	{
		GD.Print($"Player {peerId} disconnected, removing player");
		
		// Remove player node
		var playerNode = _playersContainer.GetNodeOrNull($"Player_{peerId}");
		if (playerNode != null)
		{
			playerNode.QueueFree();
		}
	}

	private void SpawnPlayer(int peerId)
	{
		if (PlayerScene == null)
		{
			GD.PrintErr("PlayerScene not assigned!");
			return;
		}
		
		var player = PlayerScene.Instantiate<Player>();
		player.Name = $"Player_{peerId}";
		player.PlayerId = peerId;
		
		// Get spawn point (cycle through available spawn points)
		int spawnIndex = (peerId - 1) % _spawnPoints.GetChildCount();
		var spawnPoint = _spawnPoints.GetChild<Node2D>(spawnIndex);
		player.GlobalPosition = spawnPoint.GlobalPosition;
		
		// Add different colors for different players (optional visual feedback)
		var sprite = player.GetNode<Sprite2D>("Sprite2D");
		sprite.Modulate = GetPlayerColor(peerId);
		
		_playersContainer.AddChild(player, true); // true = force readable name
		
		GD.Print($"Spawned player {peerId} at {player.GlobalPosition}");
	}
	
	private Color GetPlayerColor(int peerId)
	{
		// Simple color variation based on peer ID
		Color[] colors = {
			Colors.White,      // Player 1
			Colors.LightBlue,  // Player 2
			Colors.LightGreen, // Player 3
			Colors.Yellow      // Player 4
		};
		
		return colors[(peerId - 1) % colors.Length];
	}
}
