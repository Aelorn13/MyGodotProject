extends Node

# Networking configuration
const PORT = 7777
const MAX_PLAYERS = 4

# Player tracking
var players = {}
var player_name = "Player"

# Signals for network events
signal player_connected(peer_id, player_info)
signal player_disconnected(peer_id)
signal server_disconnected

func _ready():
	# Connect to multiplayer signals
	multiplayer.peer_connected.connect(_on_player_connected)
	multiplayer.peer_disconnected.connect(_on_player_disconnected)
	multiplayer.connected_to_server.connect(_on_connected_to_server)
	multiplayer.connection_failed.connect(_on_connection_failed)
	multiplayer.server_disconnected.connect(_on_server_disconnected)

# === Server Functions ===

func create_server():
	var peer = ENetMultiplayerPeer.new()
	var error = peer.create_server(PORT, MAX_PLAYERS)
	
	if error != OK:
		push_error("Failed to create server: " + str(error))
		return
	
	multiplayer.multiplayer_peer = peer
	
	print("Server created on port ", PORT)
	
	# Add host as a player
	var peer_id = multiplayer.get_unique_id()
	players[peer_id] = {
		"name": player_name,
		"id": peer_id
	}

# === Client Functions ===

func join_server(ip: String):
	var peer = ENetMultiplayerPeer.new()
	var error = peer.create_client(ip, PORT)
	
	if error != OK:
		push_error("Failed to connect to server: " + str(error))
		return
	
	multiplayer.multiplayer_peer = peer
	print("Connecting to server at ", ip, ":", PORT)

# === Network Callbacks ===

func _on_player_connected(id: int):
	print("Player connected: ", id)

func _on_player_disconnected(id: int):
	print("Player disconnected: ", id)
	players.erase(id)
	player_disconnected.emit(id)

func _on_connected_to_server():
	print("Successfully connected to server!")
	var peer_id = multiplayer.get_unique_id()
	players[peer_id] = {
		"name": player_name,
		"id": peer_id
	}

func _on_connection_failed():
	print("Connection failed!")
	multiplayer.multiplayer_peer = null

func _on_server_disconnected():
	print("Disconnected from server!")
	multiplayer.multiplayer_peer = null
	players.clear()
	server_disconnected.emit()

# === Utility Functions ===

func is_server() -> bool:
	return multiplayer.is_server()

func get_player_id() -> int:
	return multiplayer.get_unique_id()
