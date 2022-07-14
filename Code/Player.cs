using Game.Code.Managers;
using Game.Code.UI;

using Godot;

namespace Game.Code
{
    public class Player : Entity
    {
        private const float InvincibilityDuration = 1f;

        private Gun _gun;
        private HealthBar _healthBar;
        private Timer _invincibilityTimer = new Timer();
        private bool _isInvincible = false;

        public Player() : base()
        {
            _invincibilityTimer.OneShot = true;
            _invincibilityTimer.Connect("timeout", this, nameof(OnInvincibilityTimerTimeout));
        }

        public override void _PhysicsProcess(float delta)
        {
            var inputVector = new Vector2(Input.GetActionStrength("right") - Input.GetActionStrength("left"), Input.GetActionStrength("down") - Input.GetActionStrength("up")).Normalized();

            if (inputVector.IsEqualApprox(Vector2.Zero))
            {
                _velocity = _velocity.MoveToward(Vector2.Zero, Deceleration * delta);
            }
            else
            {
                _velocity = _velocity.MoveToward(inputVector * MaxSpeed, Acceleration * delta);
            }

            _velocity = MoveAndSlide(_velocity);
        }

        public override void _Process(float delta)
        {
            if (Input.IsActionPressed("fire"))
            {
                _gun.TryShoot();
            }

            _gun.LookAt(GetGlobalMousePosition());
        }

        public override void _Ready()
        {
            base._Ready();

            _hitBox.Connect("area_entered", this, nameof(OnHitBoxAreaEntered));
            _gun = GetNode<Gun>("Gun");
            _healthBar = GetNode<HealthBar>("HealthBar");
            _healthBar.MaxValue = MaxHealth;
            _healthBar.Value = CurrentHealth;

            AddChild(_invincibilityTimer);
        }

        public override void GetDamage(float damage)
        {
            if (_isInvincible)
            {
                return;
            }

            _isInvincible = true;
            _invincibilityTimer.Start(InvincibilityDuration);

            base.GetDamage(damage);

            _healthBar.Value = CurrentHealth;
        }

        public void Reset()
        {
            CurrentHealth = MaxHealth;

            if (_healthBar != null)
            {
                _healthBar.MaxValue = MaxHealth;
                _healthBar.Value = CurrentHealth;
            }
        }

        protected override void Die()
        {
            UIManager.Instance.LoadMainMenu();
        }

        private void OnHitBoxAreaEntered(Area2D area)
        {
            if (area.CollisionLayer == 1 << 8) // EnemyAttackBox
            {
                GetDamage(10);
            }
        }

        private void OnInvincibilityTimerTimeout()
        {
            _isInvincible = false;
        }
    }
}