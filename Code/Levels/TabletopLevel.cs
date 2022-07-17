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

        private Button _playButton;
        private Button _playAgainButton;
        private Label _playAgainLabel;

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

            // TODO: @Damir, replace path to Play button
            // _playButton = GetNode<Button>(TODO);
            // _playAgainButton = GetNode<Button>(TODO);
            // _playAgainLabel = GetNode<Label>(TODO);

            var curve = GetNode<Path2D>("Path2D").Curve;
            _bluePawn = GetNode<Pawn>("Path2D/BluePawn");
            _bluePawn.UnitOffset = 0f;
            _redPawn = GetNode<Pawn>("Path2D/RedPawn");
            _redPawn.UnitOffset = 0f;

            _tiles = GetNode<Node2D>("Tiles").GetChildren().OfType<Tile>().ToArray();
            GameManager.Instance.MaxScore = (uint)_tiles.Length - 1;

            Debug.Assert(_tiles.Length == curve.GetPointCount());

            for (int i = 0; i < _tiles.Length; i++)
            {
                var tile = _tiles[i];
                tile.Label.Text = i.ToString();
                tile.Offset = curve.GetClosestOffset(curve.GetPointPosition(i));
            }

            AddChild(_pawnsMovementTween);
            _pawnsMovementTween.Connect("tween_all_completed", this, nameof(OnPawnsMovementTweenCompleted));

            AddChild(_postAnimationDurationTimer);
            _postAnimationDurationTimer.Connect("timeout", this, nameof(OnPostAnimationDurationTimerTimeout));

            // TODO: @Damir, uncomment
            // _playButton.Connect("pressed", this, nameof(OnPlayButtonPressed));
            // _playAgainButton.Connect("pressed", this, nameof(OnPlayAgainButtonPressed));
            // _playAgainButton.Visible = false;
            // _playAgainLabel.Visible = false;

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
            UIManager.Instance.IsHUDVisible = true;
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
            // TODO: @Damir, uncomment
            // _playButton.Visible = false;

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

            if (GameManager.Instance.BluePlayerScore >= GameManager.Instance.MaxScore)
            {
                OnGameFinished(true);
            }
            else if (GameManager.Instance.RedPlayerScore >= GameManager.Instance.MaxScore)
            {
                OnGameFinished(false);
            }
            else
            {
                // @Damir, uncomment
                // _playButton.Visible = true;
                // @Damir, remove after uncomment
                SceneManager.Instance.LoadJudoLevel();                
            }
        }

        private void OnPlayButtonPressed()
        {
            SceneManager.Instance.LoadJudoLevel();
            // _playButton.Visible = false;
        }
        
        private void OnPlayAgainButtonPressed()
        {
            SceneManager.Instance.ResetTabletop();
            GameManager.Instance.Reset();
            SceneManager.Instance.LoadJudoLevel();
        }

        private void OnGameFinished(bool playerHasWon)
        {
            // TODO: Reset this object
            if (false)
            {
                // TODO: @Damir, UI needed
                _playAgainLabel.Text = playerHasWon ? "You won!" : "You lose!";
                _playAgainLabel.Visible = true;
                _playAgainButton.Visible = true;
                _playButton.Visible = false;
            }
        }
    }
}
