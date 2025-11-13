using Godot;

public partial class SlimeCombat : EnemyCombat
{
	// Slime attacks in 360° radius around itself
	[Export] public float AttackRadius { get; set; } = 60f;
	
	private CollisionShape2D _attackShape;

	public virtual  void Initialize(Enemy enemy)
	{
		base.Initialize(enemy); // It's good practice to call the base method
		
		// Set up circular attack area
		// The base Initialize method should have already found _attackArea
		if (_attackArea != null)
		{
			_attackShape = _attackArea.GetNode<CollisionShape2D>("CollisionShape2D");
			
			// Make it a circle
			var circle = new CircleShape2D();
			circle.Radius = AttackRadius;
			_attackShape.Shape = circle;
			
			// Center it on the slime
			_attackShape.Position = Vector2.Zero;
		}
	}

	public virtual  void PerformAttack()
	{
		if (!CanAttack)
			return;
		
		// These variables are now accessible because they are 'protected' in the base class
		_isAttacking = true;
		_attackCooldownTimer = _enemy.AttackCooldown;
		
		GD.Print($"Slime performing area attack!");
		
		// Show attack area (pulsing effect)
		if (_attackArea != null)
		{
			// Note: If you want to see the area for debugging, you might need to enable "Visible Collision Shapes" in the editor
			// Making the Area2D visible doesn't render its shape.
			
			// Animate the attack
			AnimateAttack();
			
			CheckAttackHits(); // Assuming this is a method in your base class
			
			// Hide after animation
			_enemy.GetTree().CreateTimer(0.4f).Timeout += () => // Use 0.4f for float
			{
				_isAttacking = false;
			};
		}
		else
		{
			_isAttacking = false; // Ensure we can attack again if there's no attack area
		}
	}

	private void AnimateAttack()
	{
		// Pulse the slime sprite to show attack
		var sprite = _enemy.GetNode<Sprite2D>("Sprite2D");
		var originalScale = sprite.Scale;
		
		// Grow
		var tween = _enemy.CreateTween();
		tween.TweenProperty(sprite, "scale", originalScale * 1.3f, 0.2f);
		tween.TweenProperty(sprite, "scale", originalScale, 0.2f);
	}
}
