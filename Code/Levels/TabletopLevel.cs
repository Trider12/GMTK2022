using System.Diagnostics;
using System.Linq;

using Game.Code.Interfaces;
using Game.Code.Managers;

using Godot;

namespace Game.Code.Levels
{
    public class TabletopLevel : Node2D, ILevel
    {
        private Pawn _bluePawn, _redPawn;
        private Label _blueScoreLabel, _redScoreLabel;
        private Button _rollButton;
        private Tween _animationTween = new Tween();
        private Tile[] _tiles;

        [Export]
        public float PawnSpeed { get; set; } = 100f;

        [Export]
        public string[] MinigameNames { get; set; } = { "JudoLevel" };

        public Node2D World => this;
        public bool NeedsToUpdatePawns { get; set; } = false;

        public override void _Ready()
        {
            _blueScoreLabel = GetNode<Label>("CanvasLayer/BlueScoreLabel");
            _blueScoreLabel.Text = GameManager.Instance.BluePlayerScore.ToString();
            _redScoreLabel = GetNode<Label>("CanvasLayer/RedScoreLabel");
            _redScoreLabel.Text = GameManager.Instance.RedPlayerScore.ToString();

            var curve = GetNode<Path2D>("Path2D").Curve;
            _bluePawn = GetNode<Pawn>("Path2D/BluePawn");
            _bluePawn.UnitOffset = 0f;
            _redPawn = GetNode<Pawn>("Path2D/RedPawn");
            _redPawn.UnitOffset = 0f;

            _tiles = GetNode<Node2D>("Tiles").GetChildren().OfType<Tile>().ToArray();

            Debug.Assert(_tiles.Length == curve.GetPointCount());

            for (int i = 0; i < _tiles.Length; i++)
            {
                var tile = _tiles[i];
                tile.Label.Text = (i + 1).ToString();
                tile.Offset = curve.GetClosestOffset(curve.GetPointPosition(i));
            }

            _rollButton = GetNode<Button>("CanvasLayer/RollButton");
            _rollButton.Connect("pressed", this, nameof(OnRollButtonPressed));

            AddChild(_animationTween);
            _animationTween.Connect("tween_all_completed", this, nameof(OnAnimationTweenCompleted));
        }

        public override void _PhysicsProcess(float delta)
        {
            if (NeedsToUpdatePawns)
            {
                UpdatePawns();
            }
        }

        public void OnLevelLoad()
        {
            SoundManager.Instance.PlayMainTheme();
        }

        public void OnLevelUnload()
        {
        }

        public void UpdatePawns()
        {
            NeedsToUpdatePawns = false;

            if (_rollButton == null)
            {
                return;
            }

            uint blueScore = GameManager.Instance.BluePlayerScore;
            uint redScore = GameManager.Instance.RedPlayerScore;

            _rollButton.Visible = false;
            _blueScoreLabel.Text = blueScore.ToString();
            _redScoreLabel.Text = redScore.ToString();

            AnimatePawn(_bluePawn, blueScore);
            AnimatePawn(_redPawn, redScore);
            _animationTween.Start();
        }

        private void OnAnimationTweenCompleted()
        {
            _rollButton.Visible = true;
        }

        private void AnimatePawn(Pawn pawn, uint score)
        {
            uint index = System.Math.Min(score, (uint)_tiles.Length) - 1;
            float initialOffset = pawn.Offset;
            float targetOffset = _tiles[index].Offset;
            float delta = targetOffset - initialOffset;
            float time = delta < 0.1f ? 0f : delta / PawnSpeed;
            _animationTween.InterpolateProperty(pawn, "offset", initialOffset, targetOffset, time);
        }

        private void OnRollButtonPressed()
        {
            uint levelIndex = GD.Randi() % (uint)MinigameNames.Length;

            SceneManager.Instance.LoadLevel(MinigameNames[levelIndex]);
        }
    }
}