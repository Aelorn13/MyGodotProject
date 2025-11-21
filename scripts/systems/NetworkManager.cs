using Godot;
using Godot.Collections;

public partial class NetworkManager : Node
{
	private const int Port = 7777;
	private const int MaxPlayers = 4;

	private Dictionary<int, Dictionary> _players = new Dictionary<int, Dictionary>();
	private Upnp _upnp;

	// Signals
	[Signal]
	public delegate void PlayerConnectedEventHandler(int peerId);

	[Signal]
	public delegate void PlayerDisconnectedEventHandler(int peerId);

	[Signal]
	public delegate void ServerDisconnectedEventHandler();

	[Signal]
	public delegate void UpnpCompletedEventHandler(bool success, string message);

	public override void _Ready()
	{
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
		GD.Print("Creating server...");

		// Start UPnP setup in background (async)
		SetupUpnpAsync();

		// Create server immediately (don't wait for UPnP)
        var peer = new ENetMultiplayerPeer();
        Error error = peer.CreateServer(Port, MaxPlayers);

        if (error != Error.Ok)
        {
            GD.PrintErr($"Failed to create server: {error}");
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
        GD.Print($"Server created on port {Port}");

        int peerId = Multiplayer.GetUniqueId();
        _players[peerId] = new Dictionary { { "id", peerId }, { "name", "Host" } };

        // Get and display public IP
        GetPublicIp();
    }

    private async void SetupUpnpAsync()
    {
        // Run UPnP discovery in a separate thread to avoid blocking
        await System.Threading.Tasks.Task.Run(() =>
        {
            _upnp = new Upnp();

            GD.Print("UPnP: Starting router discovery...");
            int discoveryResult = _upnp.Discover(2000, 2, "InternetGatewayDevice");

            if (discoveryResult == (int)Upnp.UpnpResult.Success)
            {
                GD.Print("UPnP: Router discovered. Mapping ports...");

                // Map both UDP and TCP
                int udpResult = _upnp.AddPortMapping(Port, Port, "Godot Game", "UDP");
                int tcpResult = _upnp.AddPortMapping(Port, Port, "Godot Game", "TCP");

                if (
                    udpResult == (int)Upnp.UpnpResult.Success
                    && tcpResult == (int)Upnp.UpnpResult.Success
                )
                {
                    string externalIp = _upnp.QueryExternalAddress();
                    CallDeferred(
                        MethodName.EmitSignal,
                        SignalName.UpnpCompleted,
                        true,
                        $"UPnP Success! Share IP: {externalIp}:{Port}"
                    );
                    GD.Print($"UPnP: Ports mapped! External IP: {externalIp}");
                }
                else
                {
                    CallDeferred(
                        MethodName.EmitSignal,
                        SignalName.UpnpCompleted,
                        false,
                        "UPnP: Failed. Try manual port forwarding."
                    );
                    GD.PrintErr("UPnP: Failed to map ports.");
                }
            }
            else
            {
                CallDeferred(
                    MethodName.EmitSignal,
                    SignalName.UpnpCompleted,
                    false,
                    "UPnP: Router not found or unsupported."
                );
                GD.PrintErr("UPnP: Discovery failed.");
            }
        });
    }

    private void GetPublicIp()
    {
		// Use Godot's HttpRequest node
		var httpRequest = new HttpRequest();
		AddChild(httpRequest);

		httpRequest.RequestCompleted += OnPublicIpReceived;

		Error error = httpRequest.Request("https://api.ipify.org");
		if (error != Error.Ok)
		{
			GD.PrintErr("Failed to request public IP");
			httpRequest.QueueFree();
		}
	}

	private void OnPublicIpReceived(long result, long responseCode, string[] headers, byte[] body)
	{
		if (responseCode == 200)
		{
			string publicIp = System.Text.Encoding.UTF8.GetString(body);
			GD.Print($"═══════════════════════════════════════");
			GD.Print($"Your Public IP: {publicIp}:{Port}");
			GD.Print($"Give this address to your friends!");
			GD.Print($"═══════════════════════════════════════");

			// Emit signal so UI can display it
			EmitSignal(SignalName.UpnpCompleted, true, $"Share this IP: {publicIp}:{Port}");
		}
		else
		{
			GD.PrintErr($"Failed to get public IP. Response code: {responseCode}");
		}

		// Clean up the HttpRequest node
		var sender = GetNodeOrNull<HttpRequest>("HttpRequest");
		if (sender != null)
		{
			sender.QueueFree();
		}
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
		// Clean up UPnP port mappings
		if (_upnp != null && _upnp.GetGateway() != null)
		{
			_upnp.DeletePortMapping(Port, "UDP");
			_upnp.DeletePortMapping(Port, "TCP");
			GD.Print("UPnP: Port mappings removed.");
		}

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
		_players[peerId] = new Dictionary { { "id", peerId }, { "name", "Player" } };
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
