extends Control

# References to menu panels
@onready var menu_buttons = $MenuContainer/MenuButtons
@onready var multiplayer_menu = $MultiplayerMenu
@onready var settings_menu = $SettingsMenu

# References to buttons
@onready var singleplayer_button = $MenuContainer/MenuButtons/SingleplayerButton
@onready var multiplayer_button = $MenuContainer/MenuButtons/MultiplayerButton
@onready var settings_button = $MenuContainer/MenuButtons/SettingsButton
@onready var exit_button = $MenuContainer/MenuButtons/ExitButton

# Multiplayer menu elements
@onready var host_button = $MultiplayerMenu/MarginContainer/VBoxContainer/HostButton
@onready var ip_input = $MultiplayerMenu/MarginContainer/VBoxContainer/ConnectPanel/IPAddressInput
@onready var connect_button = $MultiplayerMenu/MarginContainer/VBoxContainer/ConnectPanel/ConnectButton
@onready var mp_back_button = $MultiplayerMenu/MarginContainer/VBoxContainer/BackButton

# Settings menu elements
@onready var settings_back_button = $SettingsMenu/MarginContainer/VBoxContainer/BackButton

# Scene paths
const TEST_LEVEL = "res://scenes/levels/test_level.tscn"

func _ready():
	# Connect main menu buttons
	singleplayer_button.pressed.connect(_on_singleplayer_pressed)
	multiplayer_button.pressed.connect(_on_multiplayer_pressed)
	settings_button.pressed.connect(_on_settings_pressed)
	exit_button.pressed.connect(_on_exit_pressed)
	
	# Connect multiplayer menu buttons
	host_button.pressed.connect(_on_host_pressed)
	connect_button.pressed.connect(_on_connect_pressed)
	mp_back_button.pressed.connect(_on_multiplayer_back_pressed)
	
	# Connect settings menu buttons
	settings_back_button.pressed.connect(_on_settings_back_pressed)
	
	# Set up IP input
	ip_input.text_submitted.connect(_on_ip_submitted)

# === Main Menu Buttons ===

func _on_singleplayer_pressed():
	print("Starting singleplayer game...")
	# Change to game scene
	get_tree().change_scene_to_file(TEST_LEVEL)

func _on_multiplayer_pressed():
	print("Opening multiplayer menu...")
	_show_multiplayer_menu()

func _on_settings_pressed():
	print("Opening settings menu...")
	_show_settings_menu()

func _on_exit_pressed():
	print("Exiting game...")
	get_tree().quit()

# === Multiplayer Menu ===

func _show_multiplayer_menu():
	menu_buttons.visible = false
	multiplayer_menu.visible = true

func _on_multiplayer_back_pressed():
	multiplayer_menu.visible = false
	menu_buttons.visible = true

func _on_host_pressed():
	print("Hosting game...")
	# TODO: Implement hosting logic
	# For now, just start the game as host
	NetworkManager.create_server()
	get_tree().change_scene_to_file(TEST_LEVEL)

func _on_connect_pressed():
	var ip = ip_input.text.strip_edges()
	if ip.is_empty():
		ip = "localhost"
	
	print("Connecting to: ", ip)
	# TODO: Implement connection logic
	NetworkManager.join_server(ip)
	get_tree().change_scene_to_file(TEST_LEVEL)

func _on_ip_submitted(text: String):
	# Allow pressing Enter to connect
	_on_connect_pressed()

# === Settings Menu ===

func _show_settings_menu():
	menu_buttons.visible = false
	settings_menu.visible = true

func _on_settings_back_pressed():
	settings_menu.visible = false
	menu_buttons.visible = true
