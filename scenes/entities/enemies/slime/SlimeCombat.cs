using Godot;

public partial class SlimeCombat : EnemyCombat
{
    [Export]
    public float AttackRadius { get; set; } = 60f;

    private CollisionShape2D _attackShape;

    public override void Initialize(Enemy enemy) // Use override
    {
        base.Initialize(enemy);

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

    public override void PerformAttack() // Use override
    {
        if (!CanAttack)
            return;

        _isAttacking = true;
        _attackCooldownTimer = _enemy.AttackCooldown;

        GD.Print($"Slime performing area attack!");

        if (_attackArea != null)
        {
            _attackArea.Visible = true; // Make visible for debug

            AnimateAttack();
            CheckAttackHits();

            _enemy.GetTree().CreateTimer(0.4f).Timeout += () =>
            {
                if (_attackArea != null)
                    _attackArea.Visible = false;
                _isAttacking = false;
            };
        }
    }

    private void AnimateAttack()
    {
        var sprite = _enemy.GetNode<Sprite2D>("Sprite2D");
        var originalScale = sprite.Scale;

        var tween = _enemy.CreateTween();
        tween.TweenProperty(sprite, "scale", originalScale * 1.3f, 0.2f);
        tween.TweenProperty(sprite, "scale", originalScale, 0.2f);
    }
}
