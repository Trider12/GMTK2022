using Game.Code.Interfaces;
using Game.Code.Managers;

using Godot;

namespace Game.Code.Levels
{
    public class DiceLevel : Node2D, ILevel
    {
        public bool PlayerHasWon = true;
        private Label _label;
        private Timer _levelLifetimeTimer = new Timer();
        private float _levelLifetime = 2f;
        private uint _score = 0;

        public Node2D World => this;

        public override void _Ready()
        {
            _score = GD.Randi() % 6 + 1;

            _label = GetNode<Label>("CanvasLayer/Label");
            _label.Text = _score.ToString();

            AddChild(_levelLifetimeTimer);
            _levelLifetimeTimer.OneShot = true;
            _levelLifetimeTimer.Connect("timeout", this, nameof(OnLevelLifetimeTimerTimeout));
            _levelLifetimeTimer.Start(_levelLifetime);
        }

        public void OnLevelLoad()
        {
        }

        public void OnLevelUnload()
        {
            QueueFree();
        }

        private void OnLevelLifetimeTimerTimeout()
        {
            if (PlayerHasWon)
            {
                GameManager.Instance.BluePlayerScore += _score;
            }
            else
            {
                GameManager.Instance.RedPlayerScore += _score;
            }

            SceneManager.Instance.LoadTabletopLoad();
        }
    }
}