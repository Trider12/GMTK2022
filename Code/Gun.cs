using System.Diagnostics;

using Game.Code.Managers;

using Godot;

namespace Game.Code
{
    internal class Gun : Node2D
    {
        private const float BulletDamage = 50;
        private const float BulletSpeed = 500;
        private const int MagSize = 10;
        private const float RateOfFire = 5;
        private static readonly PackedScene BulletScene = GD.Load<PackedScene>("res://Scenes/Guns/Bullet.tscn");

        private int _ammoCount = 0;
        private bool _canShoot = true;
        private Node2D _muzzlePoint;
        private Timer _shootTimer = new Timer();

        public Gun()
        {
            _shootTimer.OneShot = true;
            _shootTimer.Connect("timeout", this, nameof(OnShootTimerTimeout));
        }

        private float ShotDelay => 1f / RateOfFire;

        public override void _Ready()
        {
            _ammoCount = MagSize;
            _muzzlePoint = GetNode<Node2D>("MuzzlePoint");
            AddChild(_shootTimer);
        }

        public void FinishReloading()
        {
            _ammoCount = MagSize;
        }

        public void StartReloading()
        {
            _ammoCount = 0;
        }

        public void TryShoot()
        {
            if (!_canShoot || MagSize != 0 && _ammoCount <= 0)
            {
                return;
            }

            SpawnProjectiles();

            _canShoot = false;
            _shootTimer.Start(ShotDelay);
        }

        private Bullet InstanceBullet()
        {
            var bullet = BulletScene.InstanceOrNull<Bullet>();
            Debug.Assert(bullet != null);

            bullet.Direction = (GetGlobalMousePosition() - GlobalPosition).Normalized();
            bullet.Damage = BulletDamage;
            bullet.Speed = BulletSpeed;
            bullet.GlobalPosition = _muzzlePoint.GlobalPosition;
            bullet.GlobalRotation = bullet.Direction.Angle();

            return bullet;
        }

        private void OnShootTimerTimeout()
        {
            _canShoot = true;
        }

        private void SpawnProjectiles()
        {
            var bullet = InstanceBullet();
            SceneManager.Instance.CurrentLevel.World.AddChild(bullet);
        }
    }
}