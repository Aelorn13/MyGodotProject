using System.Collections.Generic;
using Godot;

public partial class EnemyAI : Node
{
    public enum AIState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Dead,
    }

    protected Enemy _enemy;
    protected EnemyMovement _movement;
    protected EnemyCombat _combat;
    protected EnemyHealth _health;

    protected AIState _currentState = AIState.Patrol;

    // Patrol points
    [Export]
    public Godot.Collections.Array<Vector2> PatrolPoints { get; set; } = new();
    protected int _currentPatrolIndex = 0;
    protected double _idleTimer = 0.0;
    protected const double IdleTime = 1.0;

    public virtual void Initialize(Enemy enemy)
    {
        _enemy = enemy;
        _movement = enemy.GetNode<EnemyMovement>("EnemyMovement");
        _combat = enemy.GetNode<EnemyCombat>("EnemyCombat");
        _health = enemy.GetNode<EnemyHealth>("EnemyHealth");

        _health.Died += OnDied;

        // Set default patrol if none set
        if (PatrolPoints.Count == 0)
        {
            GD.Print($"Enemy has no patrol points, creating defaults around {_enemy.Position}");
            PatrolPoints.Add(_enemy.Position + new Vector2(-100, 0));
            PatrolPoints.Add(_enemy.Position + new Vector2(100, 0));
        }

        GD.Print($"Enemy initialized with {PatrolPoints.Count} patrol points");
        for (int i = 0; i < PatrolPoints.Count; i++)
        {
            GD.Print($"  Point {i}: {PatrolPoints[i]}");
        }
    }

    public virtual void ProcessAI(double delta)
    {
        if (_health.IsDead)
        {
            _currentState = AIState.Dead;
            return;
        }

        // Find nearby players
        Player nearestPlayer = FindNearestPlayer();
        _enemy.CurrentTarget = nearestPlayer;

        // State machine
        switch (_currentState)
        {
            case AIState.Patrol:
                UpdatePatrol(delta);
                break;
            case AIState.Chase:
                UpdateChase(delta, nearestPlayer);
                break;
            case AIState.Attack:
                UpdateAttack(delta, nearestPlayer);
                break;
        }

        // Transition logic
        CheckStateTransitions(nearestPlayer);
    }

    protected virtual void UpdatePatrol(double delta)
    {
        if (PatrolPoints.Count == 0)
        {
            GD.Print("No patrol points!");
            _movement.Stop();
            return;
        }

        Vector2 targetPoint = PatrolPoints[_currentPatrolIndex];
        float distanceToTarget = _enemy.Position.DistanceTo(targetPoint);

        GD.Print(
            $"Patrolling: Going to point {_currentPatrolIndex} at {targetPoint}, distance: {distanceToTarget}"
        );

        if (distanceToTarget < 10f)
        {
            _movement.Stop();
            _idleTimer += delta;

            if (_idleTimer >= IdleTime)
            {
                _currentPatrolIndex = (_currentPatrolIndex + 1) % PatrolPoints.Count;
                _idleTimer = 0;
                GD.Print($"Reached patrol point, moving to next: {_currentPatrolIndex}");
            }
        }
        else
        {
            _movement.MoveToward(targetPoint, _enemy.MoveSpeed * 0.5f);
        }
    }

    protected virtual void UpdateChase(double delta, Player target)
    {
        if (target == null)
        {
            _movement.Stop();
            return;
        }

        _movement.MoveToward(target.Position, _enemy.MoveSpeed);
    }

    protected virtual void UpdateAttack(double delta, Player target)
    {
        if (target == null)
        {
            _movement.Stop();
            return;
        }

        // Stop moving and attack
        _movement.Stop();

        if (_combat.CanAttack)
        {
            _combat.PerformAttack();
        }
    }

    protected virtual void CheckStateTransitions(Player nearestPlayer)
    {
        if (nearestPlayer == null)
        {
            // No player nearby, patrol
            if (_currentState != AIState.Patrol)
            {
                _currentState = AIState.Patrol;
                GD.Print($"Enemy entering PATROL state");
            }
            return;
        }

        float distanceToPlayer = _enemy.Position.DistanceTo(nearestPlayer.Position);

        if (distanceToPlayer <= _enemy.AttackRange)
        {
            // In attack range
            if (_currentState != AIState.Attack)
            {
                _currentState = AIState.Attack;
                GD.Print($"Enemy entering ATTACK state");
            }
        }
        else if (distanceToPlayer <= _enemy.DetectionRange)
        {
            // In detection range, chase
            if (_currentState != AIState.Chase)
            {
                _currentState = AIState.Chase;
                GD.Print($"Enemy entering CHASE state");
            }
        }
        else
        {
            // Out of range, patrol
            if (_currentState != AIState.Patrol)
            {
                _currentState = AIState.Patrol;
                GD.Print($"Enemy entering PATROL state");
            }
        }
    }

    // Add this method to EnemyAI class
    protected bool HasLineOfSight(Player target)
    {
        if (target == null)
            return false;

        var spaceState = _enemy.GetWorld2D().DirectSpaceState;

        // Raycast from enemy center to player center
        var query = PhysicsRayQueryParameters2D.Create(
            _enemy.GlobalPosition,
            target.GlobalPosition
        );

        // Exclude the enemy and the target player from collision check
        query.Exclude = new Godot.Collections.Array<Rid> { _enemy.GetRid(), target.GetRid() };

        // Only check world geometry (layer 1 = tiles/walls)
        query.CollideWithBodies = true;
        query.CollisionMask = 1; // ONLY tiles, not players or enemies

        var result = spaceState.IntersectRay(query);

        // If raycast hit something (a wall), LOS is blocked
        return result.Count == 0; // Empty result = clear LOS
    }

    // Add these fields at top of EnemyAI
    private bool _hadLOSLastFrame = false;

    protected Player FindNearestPlayer()
    {
        var playersContainer = _enemy.GetTree().Root.GetNodeOrNull<Node2D>("test_level/Players");
        if (playersContainer == null)
            return null;

        Player nearestPlayer = null;
        float nearestDistance = float.MaxValue;

        foreach (var child in playersContainer.GetChildren())
        {
            if (child is Player player)
            {
                float distance = _enemy.Position.DistanceTo(player.Position);

                if (distance < _enemy.DetectionRange && distance < nearestDistance)
                {
                    bool hasLOS = HasLineOfSight(player);

                    // Only print when LOS state changes
                    if (hasLOS != _hadLOSLastFrame)
                    {
                        GD.Print($"Enemy LOS to player: {hasLOS}");
                        _hadLOSLastFrame = hasLOS;
                    }

                    if (hasLOS)
                    {
                        nearestDistance = distance;
                        nearestPlayer = player;
                    }
                }
            }
        }

        // If no player found, reset LOS tracking
        if (nearestPlayer == null)
            _hadLOSLastFrame = false;

        return nearestPlayer;
    }

    protected virtual void OnDied()
    {
        _currentState = AIState.Dead;
        _movement.Stop();
    }
}
