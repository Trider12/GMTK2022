using Game.Code.Interfaces;
using Game.Code.Managers;

using Godot;

namespace Game.Code.Levels
{
    public class MainLevel : Node2D, ILevel
    {
        private const float EnemySpawnInterval = 1f;
        private const float EnemySpawnRadius = 1000f;
        private static readonly PackedScene EnemyScene = GD.Load<PackedScene>("res://Assets/Scenes/Enemies/Enemy.tscn");

        private Timer _enemySpawnTimer = new Timer();
        private Player _player;
        private Node2D _spawnPoint;
        private YSort _world;

        public MainLevel()
        {
            _enemySpawnTimer.OneShot = false;
            _enemySpawnTimer.Connect("timeout", this, nameof(OnEnemySpawnTimerTimeout));
        }

        public Node2D World => _world;

        public override void _Ready()
        {
            _world = GetNode<YSort>("World");
            _spawnPoint = GetNode<Node2D>("World/SpawnPoint");

            _player = GameManager.Instance.Player;
            _player.Position = _spawnPoint.Position;
            _player.Reset();
            AddChild(_player);

            AddChild(_enemySpawnTimer);
            _enemySpawnTimer.Start(EnemySpawnInterval);

            SpawnEnemy();
        }

        public void OnLevelLoad()
        {
            UIManager.Instance.IsHUDVisible = true;
        }

        public void OnLevelUnload()
        {
            if (_player != null)
            {
                CallDeferred("remove_child", _player);
                _player = null;
            }

            QueueFree();
        }

        private void OnEnemySpawnTimerTimeout()
        {
            SpawnEnemy();
        }

        private void SpawnEnemy()
        {
            float angle = GD.Randf() * Mathf.Pi;
            float x = Mathf.Cos(angle) * EnemySpawnRadius;
            float y = Mathf.Sin(angle) * EnemySpawnRadius;

            var enemy = EnemyScene.Instance<Enemy>();
            enemy.Position = _spawnPoint.Position + new Vector2(x, y);
            World.AddChild(enemy);
        }
    }
}