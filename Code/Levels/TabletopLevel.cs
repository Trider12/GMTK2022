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
        private Tween _pawnsMovementTween = new Tween();
        private Tile[] _tiles;
        private Timer _postAnimationDurationTimer = new Timer();

        private bool _isReady = false;

        [Export]
        public float PostAnimationDuration { get; private set; } = 2f;

        [Export]
        public float PawnSpeed { get; private set; } = 100f;

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

            AddChild(_pawnsMovementTween);
            _pawnsMovementTween.Connect("tween_all_completed", this, nameof(OnPawnsMovementTweenCompleted));

            AddChild(_postAnimationDurationTimer);
            _postAnimationDurationTimer.Connect("timeout", this, nameof(OnPostAnimationDurationTimerTimeout));

            _isReady = true;
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
            SoundManager.Instance.PlayTabletopTheme();
        }

        public void OnLevelUnload()
        {
        }

        public void UpdatePawns()
        {
            NeedsToUpdatePawns = false;

            if (!_isReady)
            {
                return;
            }

            uint blueScore = GameManager.Instance.BluePlayerScore;
            uint redScore = GameManager.Instance.RedPlayerScore;

            _blueScoreLabel.Text = blueScore.ToString();
            _redScoreLabel.Text = redScore.ToString();

            AnimatePawn(_bluePawn, blueScore);
            AnimatePawn(_redPawn, redScore);
            _pawnsMovementTween.Start();
        }

        private void OnPawnsMovementTweenCompleted()
        {
            _postAnimationDurationTimer.Start(PostAnimationDuration);
            SoundManager.Instance.PlayTabletopPawnPlacedSound();
        }

        private void AnimatePawn(Pawn pawn, uint score)
        {
            uint index = System.Math.Min(score, (uint)_tiles.Length) - 1;
            float initialOffset = pawn.Offset;
            float targetOffset = _tiles[index].Offset;
            float delta = targetOffset - initialOffset;
            float time = delta < 0.1f ? 0f : delta / PawnSpeed;
            _pawnsMovementTween.InterpolateProperty(pawn, "offset", initialOffset, targetOffset, time);
        }

        private void OnPostAnimationDurationTimerTimeout()
        {
            _postAnimationDurationTimer.Stop(); // this is weird

            uint maxScore = (uint)_tiles.Length;
            
            if (GameManager.Instance.BluePlayerScore >= maxScore)
            {
                OnGameFinished(true);
            }
            else if (GameManager.Instance.RedPlayerScore >= maxScore)
            {
                OnGameFinished(false);
            }
            else
            {
                SceneManager.Instance.LoadJudoLevel();
            }
        }

        private void OnGameFinished(bool playerHasWon)
        {
            // TODO: Reset this object

            SceneManager.Instance.LoadMainMenu();
            SceneManager.Instance.ResetTabletop();
        }
    }
}
