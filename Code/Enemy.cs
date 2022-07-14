using Game.Code.Managers;
using Game.Code.UI;

using Godot;

namespace Game.Code
{
    public class Enemy : Entity
    {
        private HealthBar _healthBar;

        public override void _PhysicsProcess(float delta)
        {
            var player = GameManager.Instance.Player;

            _velocity = (player.GlobalPosition - GlobalPosition).Normalized() * MaxSpeed;
            _velocity = MoveAndSlide(_velocity);
        }

        public override void _Ready()
        {
            base._Ready();

            _hitBox.Connect("body_entered", this, nameof(OnHitBoxBodyEntered));
            _healthBar = GetNode<HealthBar>("HealthBar");
            _healthBar.MaxValue = MaxHealth;
            _healthBar.Value = CurrentHealth;
        }

        public override void GetDamage(float damage)
        {
            base.GetDamage(damage);

            _healthBar.Value = CurrentHealth;
        }

        protected override void Die()
        {
            QueueFree();
        }

        protected void OnHitBoxBodyEntered(Node body)
        {
            var bullet = body as Bullet;

            if (bullet == null)
            {
                return;
            }

            GetDamage(bullet.Damage);
            bullet.Delete();
        }
    }
}