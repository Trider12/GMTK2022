using System.Diagnostics;
using System.Linq;

using Game.Code.Interfaces;
using Game.Code.Managers;

using Godot;
using Godot.Collections;

public class JudoLevel : Node2D, ILevel
{
    private float _greenZoneStartValuePercents;
    private float _greenZoneEndValuePercents;

    [Export]
    private float _playersBeginWalkDuration = 1.0f; // seconds

    [Export]
    private float _playersShakingDuration = 0.1f; // seconds

    [Export]
    private float _qteFillDuration = 0.5f; // seconds

    [Export]
    private float _qteGreenZoneWidthPercents = 0.15f; // 0 - 1

    [Export]
    private float _qteYellowZoneWidthPercents = 0.15f; // 0 - 1

    [Export]
    private float _qteTimeBeforeHurryUpPopup = 3.0f; // seconds

    [Export]
    private float _qteTimeAfterHurryUpPopup = 2.0f; // seconds

    [Export]
    private int _startBattleCountdownTime = 3; // seconds

    [Export]
    private int _throwDistance = 70;

    [Export]
    private float _inFlightAnimationDuration = 0.5f; // seconds

    [Export]
    private float _finalMessageDuration = 1.5f;

    private AnimatedSprite _playerCharacter;
    private AnimatedSprite _opponentCharacter;
    private Node2D _playerWalkTarget;
    private Node2D _opponentWalkTarget;
    private Node2D _omniLightsParent;

    private HSlider _qteBar;
    private ColorRect _qteGreenZone;
    private ColorRect _qteYellowZone;
    private Label _qteHurryUpLabel;
    private Label _qteMessageLabel;
    private Label _qteCountdownLabel;

    private Tween _playersInitialWalkAnimationTween;
    private Tween _playersShakingAnimationTween;
    private Tween _qteGrabberMovementTween;
    private Tween _startBattleCountdownTween;
    private Tween _inFlightAnimationTween;

    private Timer _qteBeforeHurryUpTimer = new Timer();
    private Timer _qteLosingAfterHurryUpTimer = new Timer();
    private Timer _qteFinalMessageTimer = new Timer();

    private float _startBattleCountdownCurrentValue;

    private bool _qteCurrentTweenIsLTR = true;
    private bool _playerHasWon = false;
    private bool _gameBegan = false;
    private bool _gameIsFinished = false;

    private float _qteSliderLowerValue;
    private float _qteSliderUpperValue;

    public Node2D World => this;

    public void OnLevelLoad()
    {
        SoundManager.Instance.PlayBattleTheme();
    }

    public void OnLevelUnload()
    {
        QueueFree();
    }

    public override void _Ready()
    {
        _playerCharacter = GetNode<AnimatedSprite>("Characters/Player/AnimatedSprite");
        _opponentCharacter = GetNode<AnimatedSprite>("Characters/Opponent/AnimatedSprite");
        _playerWalkTarget = GetNode<Node2D>("Characters/BeginGameTargetPosition_Player");
        _opponentWalkTarget = GetNode<Node2D>("Characters/BeginGameTargetPosition_Opponent");
        _omniLightsParent = GetNode<Node2D>("Arena/Scaffolding/OmniLights");

        _qteBar = GetNode<HSlider>("HUD/HSlider");
        _qteGreenZone = GetNode<ColorRect>("HUD/HSlider/GreenZone");
        _qteYellowZone = GetNode<ColorRect>("HUD/HSlider/YellowZone");
        _qteHurryUpLabel = GetNode<Label>("HUD/HSlider/HurryUpLabel");
        _qteMessageLabel = GetNode<Label>("HUD/StatusLabel");
        _qteCountdownLabel = GetNode<Label>("HUD/CountdownLabel");

        _startBattleCountdownTween = new Tween();
        AddChild(_startBattleCountdownTween);

        _playersInitialWalkAnimationTween = new Tween();
        _playersInitialWalkAnimationTween.InterpolateProperty(_playerCharacter, "global_position", _playerCharacter.GlobalPosition, _playerWalkTarget.GlobalPosition, _playersBeginWalkDuration);
        _playersInitialWalkAnimationTween.InterpolateProperty(_opponentCharacter, "global_position", _opponentCharacter.GlobalPosition, _opponentWalkTarget.GlobalPosition, _playersBeginWalkDuration);
        AddChild(_playersInitialWalkAnimationTween);

        _playersShakingAnimationTween = new Tween();
        AddChild(_playersShakingAnimationTween);

        _qteGrabberMovementTween = new Tween();
        _qteGrabberMovementTween.Connect("tween_completed", this, nameof(SwapQteBarTweenDirection));
        AddChild(_qteGrabberMovementTween);

        _inFlightAnimationTween = new Tween();
        AddChild(_inFlightAnimationTween);

        Reset();

        _startBattleCountdownTween.InterpolateProperty(this, nameof(_startBattleCountdownCurrentValue), _startBattleCountdownTime, -1, _startBattleCountdownTime);
        _startBattleCountdownTween.Connect("tween_all_completed", this, nameof(OnStartBattleCountdownTweenCompleted));
        _startBattleCountdownTween.Connect("tween_step", this, nameof(OnStartBattleCountdownTweenStep));
        _startBattleCountdownTween.Start();
    }

    public override void _Input(InputEvent @event)
    {
        if (!_gameIsFinished && _gameBegan && Input.IsActionPressed("fire"))
        {
            OnQteActionPressed();
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        _qteYellowZone.RectPosition = new Vector2((float)(_qteBar.Value / _qteBar.MaxValue) * _qteBar.RectSize.x - _qteYellowZone.RectSize.x * 0.5f, _qteYellowZone.RectPosition.y);
    }

    private void Reset()
    {
        SetCharacterAnimation(_playerCharacter, "Idle");
        SetCharacterAnimation(_opponentCharacter, "Idle");

        _qteMessageLabel.Text = "";
        _qteHurryUpLabel.Visible = false;

        _qteBar.Value = 0;
        _qteBar.Visible = false;
        _qteGreenZone.Visible = true;
        _qteYellowZone.Visible = true;

        foreach (AnimatedSprite light in _omniLightsParent.GetChildren().OfType<AnimatedSprite>())
            light.Playing = true;

        _startBattleCountdownCurrentValue = _startBattleCountdownTime;
        _qteSliderLowerValue = (float)_qteBar.MaxValue * _qteYellowZoneWidthPercents * 0.5f;
        _qteSliderUpperValue = (float)_qteBar.MaxValue * (1f - _qteYellowZoneWidthPercents * 0.5f);
    }

    private void SetCharacterAnimation(AnimatedSprite character, string animName)
    {
        character.Animation = animName;
        character.Frame = 0;
        character.Playing = true;
    }

    private void SwapQteBarTweenDirection(Object obj, string path = null)
    {
        _qteGrabberMovementTween.Stop(obj);

        if (_qteCurrentTweenIsLTR)
        {
            _qteGrabberMovementTween.InterpolateProperty(obj, "value", _qteSliderUpperValue, _qteSliderLowerValue, _qteFillDuration);
            _qteCurrentTweenIsLTR = false;
        }
        else
        {
            _qteGrabberMovementTween.InterpolateProperty(obj, "value", _qteSliderLowerValue, _qteSliderUpperValue, _qteFillDuration);
            _qteCurrentTweenIsLTR = true;
        }

        _qteGrabberMovementTween.Start();
    }

    private void OnStartBattleCountdownTweenStep(Object @object, NodePath key, float elapsed, float value)
    {
        if (value > 0)
            _qteCountdownLabel.Text = Mathf.CeilToInt(value).ToString();
        else
            _qteCountdownLabel.Text = "Fight!";
    }

    private void OnStartBattleCountdownTweenCompleted()
    {
        _qteCountdownLabel.Visible = false;
        // TODO: Play "Fight"/"Hong" sound

        SetCharacterAnimation(_playerCharacter, "Walk");
        SetCharacterAnimation(_opponentCharacter, "Walk");

        _playersInitialWalkAnimationTween.Start();
        _playersInitialWalkAnimationTween.Connect("tween_all_completed", this, nameof(OnPlayersInitialWalkTweenCompleted));
    }

    private void OnPlayersInitialWalkTweenCompleted()
    {
        SetCharacterAnimation(_playerCharacter, "Wrestle");
        SetCharacterAnimation(_opponentCharacter, "Wrestle");

        BeginActualGame();
    }

    private void BeginActualGame()
    {
        _qteBar.Visible = true;

        AddChild(_qteBeforeHurryUpTimer);
        _qteBeforeHurryUpTimer.OneShot = true;
        _qteBeforeHurryUpTimer.Start(_qteTimeBeforeHurryUpPopup);
        _qteBeforeHurryUpTimer.Connect("timeout", this, nameof(OnHurryUpTimerTimeout));

        AddChild(_qteLosingAfterHurryUpTimer);
        _qteLosingAfterHurryUpTimer.OneShot = true;
        _qteLosingAfterHurryUpTimer.Start(_qteTimeBeforeHurryUpPopup + _qteTimeAfterHurryUpPopup);
        _qteLosingAfterHurryUpTimer.Connect("timeout", this, nameof(OnLoseTimerTimeout));

        AddChild(_qteFinalMessageTimer);
        _qteFinalMessageTimer.OneShot = true;
        _qteFinalMessageTimer.Connect("timeout", this, nameof(OnFinalMessageDurationTimerTimout));

        RandomizeGreenZonePosition();
        ConfigureZoneWidgets();

        _qteCurrentTweenIsLTR = false;
        SwapQteBarTweenDirection(_qteBar);

        SetStatusLabelText("Tap to throw");

        _playersShakingAnimationTween.Connect("tween_all_completed", this, nameof(RestartShakingPlayersTween));
        RestartShakingPlayersTween();

        _gameBegan = true;
    }

    private void RandomizeGreenZonePosition()
    {
        float startValuePercents = (float)(GD.RandRange(_qteSliderLowerValue, _qteSliderUpperValue) / _qteBar.MaxValue);
        _greenZoneStartValuePercents = startValuePercents;
        _greenZoneEndValuePercents = startValuePercents + _qteGreenZoneWidthPercents;
    }

    private void ConfigureZoneWidgets()
    {
        Debug.Assert(_qteGreenZone != null);
        Debug.Assert(_qteYellowZone != null);

        _qteGreenZone.RectSize = new Vector2(
            _qteBar.RectSize.x * _qteGreenZoneWidthPercents,
            _qteBar.RectSize.y
        );

        _qteGreenZone.RectPosition = new Vector2(
            _qteBar.RectSize.x * _greenZoneStartValuePercents,
            0
        );

        _qteYellowZone.RectSize = new Vector2(
            _qteBar.RectSize.x * _qteYellowZoneWidthPercents,
            _qteBar.RectSize.y
        );

        _qteYellowZone.RectPosition = new Vector2(0, 0);
    }

    private bool IsValueInGreenZone(float rawValue)
    {
        float valuePercents = (float)(rawValue / _qteBar.MaxValue);
        return valuePercents >= _greenZoneStartValuePercents && valuePercents <= _greenZoneEndValuePercents;
    }

    private void OnQteActionPressed()
    {
        float currentValue = (float)_qteBar.Value;

        bool playerWon = IsValueInGreenZone(currentValue);

        if (playerWon)
        {
            SetStatusLabelText("Perfect!");
        }
        else
        {
            SetStatusLabelText("Miss!");
        }

        FinishGame(playerWon);
    }

    private void SetStatusLabelText(string statusText)
    {
        _qteMessageLabel.Text = statusText;
        _qteMessageLabel.Visible = true;
    }

    private void RestartShakingPlayersTween()
    {
        Vector2 randomOffset = new Vector2((int)GD.Randi() % 3 - 1, (int)GD.Randi() % 3 - 1);
        _playersShakingAnimationTween.InterpolateProperty(_playerCharacter, "global_position", _playerCharacter.GlobalPosition, _playerWalkTarget.GlobalPosition + randomOffset, _playersShakingDuration);
        _playersShakingAnimationTween.InterpolateProperty(_opponentCharacter, "global_position", _opponentCharacter.GlobalPosition, _opponentWalkTarget.GlobalPosition + randomOffset, _playersShakingDuration);
        _playersShakingAnimationTween.Start();
    }

    private void OnHurryUpTimerTimeout()
    {
        _qteHurryUpLabel.Visible = true;
    }

    private void OnLoseTimerTimeout()
    {
        SetStatusLabelText("Too Slow!");
        _qteBar.Visible = false;
        FinishGame(false);
    }

    private void FinishGame(bool playerWon)
    {
        _playerHasWon = playerWon;
        _gameIsFinished = true;
        _qteHurryUpLabel.Visible = false;

        _qteGrabberMovementTween.StopAll();
        _playersShakingAnimationTween.StopAll();

        _qteBeforeHurryUpTimer.Stop();
        _qteLosingAfterHurryUpTimer.Stop();

        if (playerWon)
        {
            PlayThrowAnimation(_playerCharacter, _opponentCharacter, 1);
        }
        else
        {
            PlayThrowAnimation(_opponentCharacter, _playerCharacter, -1);
        }
    }

    private void PlayThrowAnimation(AnimatedSprite thrower, AnimatedSprite throwee, int flightDirection)
    {
        // TODO: Play "Throw" sound

        SetCharacterAnimation(thrower, "Throw");

        thrower.Connect("animation_finished", this, nameof(OnAfterThrowerPlayedAnimation), new Array { throwee, flightDirection });
    }

    private void OnAfterThrowerPlayedAnimation(AnimatedSprite throwee, int flightDirection)
    {
        SetCharacterAnimation(throwee, "InFlight");

        _inFlightAnimationTween.Connect("tween_all_completed", this, nameof(OnInFlightTweenCompleted));
        _inFlightAnimationTween.InterpolateProperty(throwee, "position", throwee.Position, throwee.Position + new Vector2(_throwDistance * flightDirection, 0), _inFlightAnimationDuration);
        _inFlightAnimationTween.Start();
    }

    private void OnInFlightTweenCompleted()
    {
        // TODO: Play "Fall" sound
        // TODO: Play "You win/You lose" sound
        // TODO: Play "You win/You lose" music

        _qteMessageLabel.Text = _playerHasWon ? "You Win!" : "You Lose!";
        _qteFinalMessageTimer.Start(_finalMessageDuration);
    }

    private void OnFinalMessageDurationTimerTimout()
    {
        SceneManager.Instance.LoadDiceLevel(_playerHasWon);
    }
}