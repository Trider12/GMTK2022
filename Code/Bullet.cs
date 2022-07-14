using Godot;

namespace Game.Code
{
    public class Bullet : KinematicBody2D
    {
        public const float LifeTime = 30f;

        private CollisionShape2D _collisionShape;
        private Timer _timer = new Timer();

        public float Damage { get; set; }
        public Vector2 Direction { get; set; }
        public float Speed { get; set; }

        public override void _PhysicsProcess(float delta)
        {
            var collision = MoveAndCollide(Direction * Speed * delta);

            if (collision != null)
            {
                Delete();
            }
        }

        public override void _Ready()
        {
            _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");

            AddChild(_timer);
            _timer.Connect("timeout", this, nameof(OnTimerTimeout));
            _timer.OneShot = true;
            _timer.Start(LifeTime);
        }

        public void Delete()
        {
            QueueFree();
        }

        private void OnTimerTimeout()
        {
            Delete();
        }
    }
}