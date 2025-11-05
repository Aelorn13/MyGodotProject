using Godot;
using Godot.Collections;

public partial class NetworkManager : Node
{
	// Networking configuration
	private const int Port = 7777;
	private const int MaxPlayers = 4;
	
	// Player tracking
	private Dictionary<int, Dictionary> _players = new Dictionary<int, Dictionary>();
	
	// Signals
	[Signal]
	public delegate void PlayerConnectedEventHandler(int peerId);
	
	[Signal]
	public delegate void PlayerDisconnectedEventHandler(int peerId);
	
	[Signal]
	public delegate void ServerDisconnectedEventHandler();

	public override void _Ready()
	{
		// Connect to multiplayer signals
		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
		Multiplayer.ConnectedToServer += OnConnectedToServer;
		Multiplayer.ConnectionFailed += OnConnectionFailed;
		Multiplayer.ServerDisconnected += OnServerDisconnected;
		
		GD.Print("NetworkManager initialized (C#)");
	}

	// ===== SERVER FUNCTIONS =====
	public void CreateServer()
	{
		var peer = new ENetMultiplayerPeer();
		Error error = peer.CreateServer(Port, MaxPlayers);
		
		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to create server: {error}");
			return;
		}
		
		Multiplayer.MultiplayerPeer = peer;
		GD.Print($"Server created on port {Port}");
		
		// Add host as a player
		int peerId = Multiplayer.GetUniqueId();
		_players[peerId] = new Dictionary
		{
			{ "id", peerId },
			{ "name", "Host" }
		};
	}

	// ===== CLIENT FUNCTIONS =====
	public void JoinServer(string ip)
	{
		var peer = new ENetMultiplayerPeer();
		Error error = peer.CreateClient(ip, Port);
		
		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to connect to server: {error}");
			return;
		}
		
		Multiplayer.MultiplayerPeer = peer;
		GD.Print($"Connecting to server at {ip}:{Port}");
	}

	public void Disconnect()
	{
		if (Multiplayer.MultiplayerPeer != null)
		{
			Multiplayer.MultiplayerPeer.Close();
			Multiplayer.MultiplayerPeer = null;
		}
		_players.Clear();
		GD.Print("Disconnected from multiplayer");
	}

	// ===== NETWORK CALLBACKS =====
	private void OnPeerConnected(long id)
	{
		GD.Print($"Peer connected: {id}");
		EmitSignal(SignalName.PlayerConnected, (int)id);
	}

	private void OnPeerDisconnected(long id)
	{
		GD.Print($"Peer disconnected: {id}");
		_players.Remove((int)id);
		EmitSignal(SignalName.PlayerDisconnected, (int)id);
	}

	private void OnConnectedToServer()
	{
		GD.Print("Successfully connected to server!");
		int peerId = Multiplayer.GetUniqueId();
		_players[peerId] = new Dictionary
		{
			{ "id", peerId },
			{ "name", "Player" }
		};
	}

	private void OnConnectionFailed()
	{
		GD.Print("Connection failed!");
		Multiplayer.MultiplayerPeer = null;
	}

	private void OnServerDisconnected()
	{
		GD.Print("Server disconnected!");
		Multiplayer.MultiplayerPeer = null;
		_players.Clear();
		EmitSignal(SignalName.ServerDisconnected);
	}

	// ===== UTILITY FUNCTIONS =====
	public bool IsServer()
	{
		return Multiplayer.IsServer();
	}

	public int GetPlayerId()
	{
		return Multiplayer.GetUniqueId();
	}
	
	public Dictionary<int, Dictionary> GetPlayers()
	{
		return _players;
	}
}
