using System.Diagnostics;

using Game.Code.Interfaces;
using Game.Code.Managers;

using Godot;
using Godot.Collections;

public class JudoLevel : Node2D, ILevel
{
    private float _greenZoneStartValue;
    private float _greenZoneEndValue;

    [Export]
    private float _playersBeginWalkDuration = 1.0f; // seconds

    [Export]
    private float _playersShakingDuration = 0.1f; // seconds

    [Export]
    private float _qteFillDuration = 1.0f; // seconds

    [Export]
    private float _qteGreenZoneCoverPercentage = 0.05f; // 0 - 1

    [Export]
    private float _qteHurryUpTime = 3.0f; // seconds

    [Export]
    private float _qteTimeToLoseAfterHurryUp = 2.0f; // seconds

    private AnimatedSprite _playerCharacter;
    private AnimatedSprite _opponentCharacter;
    private Node2D _playerWalkTarget;
    private Node2D _opponentWalkTarget;

    private HSlider _qteBar;
    private ColorRect _qteGreenZone;
    private Label _qteHurryUpLabel;
    private Label _qteLabel;

    private Tween _playersInitialWalkAnimationTween;
    private Tween _playersShakingAnimationTween;
    private Tween _qteFillTween;

    private Timer _qteHurryUpTimer;
    private Timer _qteLoseTimer;
    private bool _qteCurrentTweenIsLTR = true;
    private bool _playerHasWon = false;
    private bool _gameIsFinished = false;

    public Node2D World => this;

    public override void _Ready()
    {
        _playerCharacter = GetNode<AnimatedSprite>("Characters/Player/AnimatedSprite");
        _opponentCharacter = GetNode<AnimatedSprite>("Characters/Opponent/AnimatedSprite");
        _playerWalkTarget = GetNode<Node2D>("Characters/BeginGameTargetPosition_Player");
        _opponentWalkTarget = GetNode<Node2D>("Characters/BeginGameTargetPosition_Opponent");

        _playersInitialWalkAnimationTween = new Tween();
        _playersInitialWalkAnimationTween.InterpolateProperty(_playerCharacter, "global_position", _playerCharacter.GlobalPosition, _playerWalkTarget.GlobalPosition, _playersBeginWalkDuration);
        _playersInitialWalkAnimationTween.InterpolateProperty(_opponentCharacter, "global_position", _opponentCharacter.GlobalPosition, _opponentWalkTarget.GlobalPosition, _playersBeginWalkDuration);
        AddChild(_playersInitialWalkAnimationTween);

        _playersShakingAnimationTween = new Tween();
        AddChild(_playersShakingAnimationTween);

        _qteBar = GetNode<HSlider>("QTE/HSlider");
        _qteGreenZone = GetNode<ColorRect>("QTE/HSlider/GreenZone");
        _qteHurryUpLabel = GetNode<Label>("QTE/HSlider/HurryUpLabel");
        _qteLabel = GetNode<Label>("QTE/StatusLabel");

        _qteFillTween = new Tween();
        _qteFillTween.Connect("tween_completed", this, nameof(SwapQteBarTweenDirection));
        AddChild(_qteFillTween);

        Reset();

        BeginGame();
    }

    public void OnLevelLoad()
    {
        SoundManager.Instance.PlayBattleTheme();
    }

    public void OnLevelUnload()
    {
        QueueFree();
    }

    public override void _Input(InputEvent @event)
    {
        if (!_gameIsFinished && Input.IsActionPressed("fire"))
        {
            OnQteActionPressed();
        }
    }

    private void SwapQteBarTweenDirection(Object obj, string path = null)
    {
        if (_qteCurrentTweenIsLTR)
        {
            _qteFillTween.Stop(obj);
            _qteFillTween.InterpolateProperty(obj, "value", 100.0f, 0.0f, _qteFillDuration);
            _qteFillTween.Start();
            _qteCurrentTweenIsLTR = false;
        }
        else
        {
            _qteFillTween.Stop(obj);
            _qteFillTween.InterpolateProperty(obj, "value", 0.0f, 100.0f, _qteFillDuration);
            _qteFillTween.Start();
            _qteCurrentTweenIsLTR = true;
        }
    }

    private float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    private void Reset()
    {
        SetCharacterAnimation(_playerCharacter, "Idle");
        SetCharacterAnimation(_opponentCharacter, "Idle");

        _qteLabel.Text = "";
        _qteHurryUpLabel.Visible = false;

        _qteGreenZone.Visible = false;
        _qteBar.Value = 0;
    }

    private void RandomizeGreenZonePosition()
    {
        float startValue = GD.Randf();
        if (startValue + _qteGreenZoneCoverPercentage > 1.0f)
            startValue = 1.0f - _qteGreenZoneCoverPercentage;

        _greenZoneStartValue = startValue;
        _greenZoneEndValue = startValue + _qteGreenZoneCoverPercentage;
    }

    private void ConfigureZoneWidgets()
    {
        Debug.Assert(_qteGreenZone != null);

        _qteGreenZone.RectSize = new Vector2(
            _qteBar.RectSize.x * _qteGreenZoneCoverPercentage,
            _qteBar.RectSize.y
        );

        _qteGreenZone.RectPosition = new Vector2(
            _qteBar.RectSize.x * _greenZoneStartValue,
            0
        );
    }

    private bool IsValueInGreenZone(float value)
    {
        float valueNorm = value / 100.0f;
        return valueNorm >= _greenZoneStartValue && valueNorm <= _greenZoneEndValue;
    }

    private void OnQteActionPressed()
    {
        float currentValue = (float)_qteBar.Value;

        FinishGame(IsValueInGreenZone(currentValue));
    }

    private void PlayThrowAnimation(AnimatedSprite thrower, AnimatedSprite throwee, float flightDirection)
    {
        SetCharacterAnimation(thrower, "Throw");

        thrower.Connect("animation_finished", this, nameof(OnAfterThrowerPlayedAnimation), new Array { throwee, flightDirection });
    }

    private void OnAfterThrowerPlayedAnimation(AnimatedSprite throwee, float flightDirection)
    {
        SetCharacterAnimation(throwee, "InFlight");

        Tween throwTween = new Tween();
        AddChild(throwTween);

        throwTween.Connect("tween_all_completed", this, nameof(OnThrowTweenCompleted));
        throwTween.InterpolateProperty(throwee, "position", throwee.Position, throwee.Position + new Vector2(100 * flightDirection, 0), 0.5f, Tween.TransitionType.Linear);
        throwTween.Start();
    }

    private void OnThrowTweenCompleted()
    {
        SceneManager.Instance.LoadDiceLevel(_playerHasWon);
    }

    private void SetStatusLabelText(string statusText)
    {
        _qteLabel.Text = statusText;
        _qteLabel.Visible = true;
    }

    private void BeginGame()
    {
        SetCharacterAnimation(_playerCharacter, "Walk");
        SetCharacterAnimation(_opponentCharacter, "Walk");

        _playersInitialWalkAnimationTween.Start();
        _playersInitialWalkAnimationTween.Connect("tween_all_completed", this, nameof(OnPlayersBeginWalkTweenCompleted));
    }

    private void OnPlayersBeginWalkTweenCompleted()
    {
        SetCharacterAnimation(_playerCharacter, "Wrestle");
        SetCharacterAnimation(_opponentCharacter, "Wrestle");

        OnGameBegan();
    }

    private void OnGameBegan()
    {
        _qteGreenZone.Visible = true;

        _qteHurryUpTimer = new Timer();
        AddChild(_qteHurryUpTimer);
        _qteHurryUpTimer.OneShot = true;
        _qteHurryUpTimer.Start(_qteHurryUpTime);
        _qteHurryUpTimer.Connect("timeout", this, nameof(OnHurryUpTimerTimeout));

        _qteLoseTimer = new Timer();
        AddChild(_qteLoseTimer);
        _qteLoseTimer.OneShot = true;
        _qteLoseTimer.Start(_qteHurryUpTime + _qteTimeToLoseAfterHurryUp);
        _qteLoseTimer.Connect("timeout", this, nameof(OnLoseTimerTimeout));

        RandomizeGreenZonePosition();
        ConfigureZoneWidgets();

        _qteCurrentTweenIsLTR = false;
        SwapQteBarTweenDirection(_qteBar);

        SetStatusLabelText("Tap to throw");

        _playersShakingAnimationTween.Connect("tween_all_completed", this, nameof(RestartShakingPlayersTween));
        RestartShakingPlayersTween();
    }

    private void RestartShakingPlayersTween()
    {
        Vector2 randomOffset = new Vector2((int)GD.Randi() % 3 - 1, (int)GD.Randi() % 3 - 1);
        _playersShakingAnimationTween.InterpolateProperty(_playerCharacter, "global_position", _playerCharacter.GlobalPosition, _playerWalkTarget.GlobalPosition + randomOffset, _playersShakingDuration);
        _playersShakingAnimationTween.InterpolateProperty(_opponentCharacter, "global_position", _opponentCharacter.GlobalPosition, _opponentWalkTarget.GlobalPosition + randomOffset, _playersShakingDuration);
        _playersShakingAnimationTween.Start();
    }

    private void FinishGame(bool playerWon)
    {
        _gameIsFinished = true;
        _qteHurryUpLabel.Visible = false;
        _playerHasWon = playerWon;
        _qteFillTween.StopAll();
        _playersShakingAnimationTween.StopAll();
        _qteHurryUpTimer.Stop();
        _qteLoseTimer.Stop();

        if (playerWon)
        {
            SetStatusLabelText("Perfect!");
            PlayThrowAnimation(_playerCharacter, _opponentCharacter, 1.0f);
        }
        else
        {
            SetStatusLabelText("Really bad!");
            PlayThrowAnimation(_opponentCharacter, _playerCharacter, -1.0f);
        }
    }

    private void SetCharacterAnimation(AnimatedSprite character, string animName)
    {
        character.Animation = animName;
        character.Frame = 0;
        character.Playing = true;
    }

    private void OnHurryUpTimerTimeout()
    {
        _qteHurryUpLabel.Visible = true;
    }

    private void OnLoseTimerTimeout()
    {
        FinishGame(false);
    }
}