using Godot;

public partial class MainMenu : Control
{
	private Button _hostButton;
	private Button _joinButton;
	private Button _quitButton;
	private LineEdit _ipInput;
	
	private const string GameScenePath = "res://scenes/levels/test_level.tscn";

	public override void _Ready()
	{
		_hostButton = GetNode<Button>("VBoxContainer/HostButton");
		_joinButton = GetNode<Button>("VBoxContainer/HBoxContainer/JoinButton");
		_quitButton = GetNode<Button>("VBoxContainer/QuitButton");
		_ipInput = GetNode<LineEdit>("VBoxContainer/HBoxContainer/IPInput");
		
		_hostButton.Pressed += OnHostPressed;
		_joinButton.Pressed += OnJoinPressed;
		_quitButton.Pressed += OnQuitPressed;
	}

	private void OnHostPressed()
	{
		GD.Print("Hosting game...");
		var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
		networkManager.CreateServer();
		
		// Change to game scene
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
		
		// Change to game scene
		GetTree().ChangeSceneToFile(GameScenePath);
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
