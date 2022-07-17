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
    private float _qteFillDuration = 1.0f; // seconds

    [Export]
    private float _qteGreenZoneWidthPercents = 0.05f; // 0 - 1

    [Export]
    private float _qteHurryUpTime = 3.0f; // seconds

    [Export]
    private float _qteTimeToLoseAfterHurryUp = 2.0f; // seconds

    [Export]
    private int _startBattleCountdownTime = 3; // seconds

    [Export]
    private int _throwDistance = 70;

    [Export]
    private float _inFlightAnimationDuration = 0.5f; // seconds

    private AnimatedSprite _playerCharacter;
    private AnimatedSprite _opponentCharacter;
    private Node2D _playerWalkTarget;
    private Node2D _opponentWalkTarget;
    private Node2D _omniLightsParent;

    private HSlider _qteBar;
    private ColorRect _qteGreenZone;
    private Label _qteHurryUpLabel;
    private Label _qteLabel;
    private Label _countdownLabel;

    private Tween _playersInitialWalkAnimationTween;
    private Tween _playersShakingAnimationTween;
    private Tween _qteFillTween;

    private Timer _qteHurryUpTimer;
    private Timer _qteLoseTimer;
    private Tween _startBattleCountdownTween;

    private float _countdownValue;

    private bool _qteCurrentTweenIsLTR = true;
    private bool _playerHasWon = false;
    private bool _gameIsFinished = false;
    private Tween _inFlightAnimationTween;

    public Node2D World => this;

    public override void _Ready()
    {
        _playerCharacter = GetNode<AnimatedSprite>("Characters/Player/AnimatedSprite");
        _opponentCharacter = GetNode<AnimatedSprite>("Characters/Opponent/AnimatedSprite");
        _playerWalkTarget = GetNode<Node2D>("Characters/BeginGameTargetPosition_Player");
        _opponentWalkTarget = GetNode<Node2D>("Characters/BeginGameTargetPosition_Opponent");
        _omniLightsParent = GetNode<Node2D>("Arena/Scaffolding/OmniLights");

        _qteBar = GetNode<HSlider>("HUD/HSlider");
        _qteGreenZone = GetNode<ColorRect>("HUD/HSlider/GreenZone");
        _qteHurryUpLabel = GetNode<Label>("HUD/HSlider/HurryUpLabel");
        _qteLabel = GetNode<Label>("HUD/StatusLabel");
        _countdownLabel = GetNode<Label>("HUD/CountdownLabel");

        _startBattleCountdownTween = new Tween();
        AddChild(_startBattleCountdownTween);

        _playersInitialWalkAnimationTween = new Tween();
        _playersInitialWalkAnimationTween.InterpolateProperty(_playerCharacter, "global_position", _playerCharacter.GlobalPosition, _playerWalkTarget.GlobalPosition, _playersBeginWalkDuration);
        _playersInitialWalkAnimationTween.InterpolateProperty(_opponentCharacter, "global_position", _opponentCharacter.GlobalPosition, _opponentWalkTarget.GlobalPosition, _playersBeginWalkDuration);
        AddChild(_playersInitialWalkAnimationTween);

        _playersShakingAnimationTween = new Tween();
        AddChild(_playersShakingAnimationTween);
        
        _qteFillTween = new Tween();
        _qteFillTween.Connect("tween_completed", this, nameof(SwapQteBarTweenDirection));
        AddChild(_qteFillTween);

        _inFlightAnimationTween = new Tween();
        AddChild(_inFlightAnimationTween);
        
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

        foreach (AnimatedSprite light in _omniLightsParent.GetChildren().OfType<AnimatedSprite>())
            light.Playing = true;
    }

    private void RandomizeGreenZonePosition()
    {
        float startValue = GD.Randf();
        if (startValue + _qteGreenZoneWidthPercents > 1.0f)
            startValue = 1.0f - _qteGreenZoneWidthPercents;

        _greenZoneStartValuePercents = startValue;
        _greenZoneEndValuePercents = startValue + _qteGreenZoneWidthPercents;
    }

    private void ConfigureZoneWidgets()
    {
        Debug.Assert(_qteGreenZone != null);

        _qteGreenZone.RectSize = new Vector2(
            _qteBar.RectSize.x * _qteGreenZoneWidthPercents,
            _qteBar.RectSize.y
        );

        _qteGreenZone.RectPosition = new Vector2(
            _qteBar.RectSize.x * _greenZoneStartValuePercents,
            0
        );
    }

    private bool IsValueInGreenZone(float value)
    {
        float valueNorm = (float)(value / _qteBar.MaxValue);
        return valueNorm >= _greenZoneStartValuePercents && valueNorm <= _greenZoneEndValuePercents;
    }

    private void OnQteActionPressed()
    {
        float currentValue = (float)_qteBar.Value;

        FinishGame(IsValueInGreenZone(currentValue));
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

        // After all sounds played do
        SceneManager.Instance.LoadDiceLevel(_playerHasWon);
    }

    private void SetStatusLabelText(string statusText)
    {
        _qteLabel.Text = statusText;
        _qteLabel.Visible = true;
    }

    private void BeginGame()
    {
        _startBattleCountdownTween.InterpolateProperty(this, nameof(_countdownValue), _startBattleCountdownTime, -1, _startBattleCountdownTime);
        _startBattleCountdownTween.Connect("tween_all_completed", this, nameof(OnStartBattleCountdownTweenCompleted));
        _startBattleCountdownTween.Connect("tween_step", this, nameof(OnStartBattleCountdownTweenStep));
        _startBattleCountdownTween.Start();
    }

    private void OnStartBattleCountdownTweenStep(Object @object, NodePath key, float elapsed, float value)
    {
        if (value > 0)
            _countdownLabel.Text = Mathf.CeilToInt(value).ToString();
        else
            _countdownLabel.Text = "Fight!";
    }

    private void OnStartBattleCountdownTweenCompleted()
    {
        _countdownLabel.Visible = false;
        // TODO: Play "Fight"/"Hong" sound

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
            PlayThrowAnimation(_playerCharacter, _opponentCharacter, 1);
        }
        else
        {
            SetStatusLabelText("Really bad!");
            PlayThrowAnimation(_opponentCharacter, _playerCharacter, -1);
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
