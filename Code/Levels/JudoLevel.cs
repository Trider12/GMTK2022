using System.Diagnostics;

using Godot;
using Game.Code.Interfaces;

using Godot.Collections;

public class JudoLevel : Node2D, ILevel
{
    [Signal] public delegate void OnTimedOut();
    [Signal] public delegate void OnGoodResult();
    [Signal] public delegate void OnBadResult();

    public Node2D World => _world;
    private Node2D _world;

    private float _greenZoneStartValue; // 0 - 1
    private float _greenZoneEndValue; // 0 - 1

    [Export] private float _qteAcceptableCoverage = 0.75f;

    [Export] private float _qteFillDuration = 3.0f; // seconds
    [Export] private float _qteGreenZoneCoverPercentage = 0.05f;
    [Export] private float _qteYellowZoneCoverPercentage = 0.05f;

    private AnimatedSprite _playerCharacter;
    private AnimatedSprite _opponentCharacter;

    private HSlider _qteBar;
    private ColorRect _qteGreenZone;
    private Button _qteButton;
    private Tween _qteFillTween;
    private bool _qteCurrentTweenIsLTR = true;

    public override void _Ready()
    {
        base._Ready();

        _world = GetNode<Node2D>("World");

        _playerCharacter = GetNode<AnimatedSprite>("Persons/Player/AnimatedSprite");
        _opponentCharacter = GetNode<AnimatedSprite>("Persons/Opponent/AnimatedSprite");

        _qteBar = GetNode<HSlider>("QTE/VBoxContainer/HSlider");
        _qteGreenZone = GetNode<ColorRect>("QTE/VBoxContainer/HSlider/GreenZone");
        _qteButton = GetNode<Button>("QTE/Button");

        // _qteGrabberNormWidth = (float)_qteBarGrabberWidthInPixels / _qteBar.RectSize.x;

        ResetUiState();

        RandomizeGreenZonePosition();
        ConfigureZoneWidgets();

        _qteButton.Connect("pressed", this, nameof(OnQteButtonPressed));

        _qteFillTween = new Tween();
        _qteFillTween.Connect("tween_completed", this, nameof(SwapQteBarTweenDirection));

        AddChild(_qteFillTween);

        _qteCurrentTweenIsLTR = false;
        SwapQteBarTweenDirection(_qteBar, string.Empty);
    }

    private void SwapQteBarTweenDirection(Object obj, string path) // path is required for "tween_completed" signal
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

    private void ResetUiState()
    {
        _qteBar.Value = 0;

        _playerCharacter.Animation = "Idle";
        _playerCharacter.Playing = true;
        _opponentCharacter.Animation = "Idle";
        _opponentCharacter.Playing = true;
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

    public void OnLevelLoad()
    {
    }

    public void OnLevelUnload()
    {
        QueueFree();
    }

    private void OnQteButtonPressed()
    {
        float currentValue = (float) _qteBar.Value;

        if (IsValueInGreenZone(currentValue))
            OnQteGoodUserResult();
        else
            OnQteBadUserResult();
    }

    private void PlayThrowAnimation(AnimatedSprite thrower, AnimatedSprite throwee, float flightDirection)
    {
        thrower.Animation = "Throw";
        thrower.Frame = 0;

        thrower.Connect("animation_finished", this, nameof(PlayThrowAnimation_AfterAnimationFinished), new Array { throwee, flightDirection });
    }

    private void PlayThrowAnimation_AfterAnimationFinished(AnimatedSprite throwee, float flightDirection)
    {
        throwee.Animation = "InFlight";
        throwee.Frame = 0;
        throwee.Playing = true;

        Tween throwTween = new Tween();
        AddChild(throwTween);

        throwTween.InterpolateProperty(throwee, "position", throwee.Position, throwee.Position + new Vector2(100 * flightDirection, 0), 0.5f, Tween.TransitionType.Linear);
        throwTween.Start();
    }

    private void OnQteBadUserResult()
    {
        EmitSignal(nameof(OnBadResult));

        _qteFillTween.Stop(_qteBar);

        PlayThrowAnimation(_opponentCharacter, _playerCharacter, -1.0f);
    }

    private void OnQteGoodUserResult()
    {
        EmitSignal(nameof(OnGoodResult));

        _qteFillTween.Stop(_qteBar);

        PlayThrowAnimation(_playerCharacter, _opponentCharacter, 1.0f);
    }
}
