using Godot;

namespace Game.Code
{
    public abstract class Entity : KinematicBody2D
    {
        protected Area2D _hitBox;
        protected Vector2 _velocity = Vector2.Zero;

        protected Entity()
        {
            CurrentHealth = MaxHealth;
        }

        public virtual float CurrentHealth { get; protected set; }
        public virtual float MaxHealth { get; protected set; } = 100;

        protected virtual float Acceleration { get; } = 3000;
        protected virtual float Deceleration { get; } = 3000;
        protected virtual float MaxSpeed { get; } = 400;

        public override void _Ready()
        {
            _hitBox = GetNode<Area2D>("HitBox");
        }

        public virtual void GetDamage(float damage)
        {
            CurrentHealth -= damage;

            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        protected abstract void Die();
    }
}