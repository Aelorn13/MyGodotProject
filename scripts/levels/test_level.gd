extends Node2D

# Preload player scene
const PLAYER_SCENE = preload("res://scenes/player/player.tscn")

@onready var spawn_points = $SpawnPoints

func _ready():
	# For now, spawn a single player for testing
	spawn_player(1)

func spawn_player(peer_id: int):
	var player = PLAYER_SCENE.instantiate()
	player.player_id = peer_id
	player.name = "Player_" + str(peer_id)
	
	# Get spawn point (use modulo for multiple spawn points)
	var spawn_index = (peer_id - 1) % spawn_points.get_child_count()
	var spawn_point = spawn_points.get_child(spawn_index)
	
	player.global_position = spawn_point.global_position
	add_child(player)
	
	return player
