using Godot;

public partial class MainMenu : Control
{
	private Button _hostButton;
	private Button _joinButton;
	private Button _quitButton;
	private LineEdit _ipInput;
	private Label _statusLabel; // NEW
	
	private const string GameScenePath = "res://scenes/levels/test_level.tscn";

	public override void _Ready()
	{
		_hostButton = GetNodeOrNull<Button>("VBoxContainer/HostButton");
		_joinButton = GetNodeOrNull<Button>("VBoxContainer/HBoxContainer/JoinButton");
		_quitButton = GetNodeOrNull<Button>("VBoxContainer/QuitButton");
		_ipInput = GetNodeOrNull<LineEdit>("VBoxContainer/HBoxContainer/IPInput");
		_statusLabel = GetNodeOrNull<Label>("VBoxContainer/StatusLabel"); // NEW
		
		if (_hostButton == null || _joinButton == null || _quitButton == null || _ipInput == null)
		{
			GD.PrintErr("Some UI nodes not found! Check scene structure.");
			return;
		}
		
		_hostButton.Pressed += OnHostPressed;
		_joinButton.Pressed += OnJoinPressed;
		_quitButton.Pressed += OnQuitPressed;
		
	
		
		GD.Print("MainMenu initialized successfully!");
	}



	private void OnHostPressed()
	{
		GD.Print("Hosting game...");
		var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
		networkManager.CreateServer();
		
		GetTree().ChangeSceneToFile(GameScenePath);
	}

	private void OnJoinPressed()
	{
		string ip = _ipInput.Text.StripEdges();
		if (string.IsNullOrEmpty(ip))
		{
			ip = "localhost";
		}
		
		GD.Print($"Joining game at {ip}...");
		var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
		networkManager.JoinServer(ip);
		
		GetTree().ChangeSceneToFile(GameScenePath);
	}

	private void OnQuitPressed()
	{
		GD.Print("Quitting game...");
		GetTree().Quit();
	}
}
