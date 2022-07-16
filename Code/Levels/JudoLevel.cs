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
    private float _qteAcceptableCoverage = 0.75f;

    [Export]
    private float _qteFillDuration = 1.0f; // seconds

    [Export]
    private float _qteGreenZoneCoverPercentage = 0.05f; // 0 - 1

    [Export]
    private float _qteYellowZoneCoverPercentage = 0.05f; // 0 - 1

    private AnimatedSprite _playerCharacter;
    private AnimatedSprite _opponentCharacter;
    private HSlider _qteBar;
    private ColorRect _qteGreenZone;
    private ColorRect _qteYellowZone;
    private Tween _qteFillTween;
    private bool _qteCurrentTweenIsLTR = true;
    private bool _playerHasWon = false;

    public Node2D World => this;

    public override void _Ready()
    {
        _playerCharacter = GetNode<AnimatedSprite>("Persons/Player/AnimatedSprite");
        _opponentCharacter = GetNode<AnimatedSprite>("Persons/Opponent/AnimatedSprite");

        _qteBar = GetNode<HSlider>("QTE/VBoxContainer/HSlider");
        _qteGreenZone = GetNode<ColorRect>("QTE/VBoxContainer/HSlider/GreenZone");
        _qteYellowZone = GetNode<ColorRect>("QTE/VBoxContainer/HSlider/YellowZone");

        Reset();

        RandomizeGreenZonePosition();
        ConfigureZoneWidgets();

        _qteFillTween = new Tween();
        _qteFillTween.Connect("tween_completed", this, nameof(SwapQteBarTweenDirection));
        _qteFillTween.Connect("tween_step", this, nameof(OnBarTweenStep));
        AddChild(_qteFillTween);

        _qteCurrentTweenIsLTR = false;
        SwapQteBarTweenDirection(_qteBar);
    }

    public void OnLevelLoad()
    {
    }

    public void OnLevelUnload()
    {
        QueueFree();
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionPressed("fire"))
        {
            OnQteButtonPressed();
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

    private void OnBarTweenStep(Object obj, string path, float elapsed, float value)
    {
        float newPosX = Remap(value, 0.0f, 100.0f, 0.0f, _qteBar.RectSize.x);
        _qteYellowZone.RectPosition = new Vector2(newPosX, 0.0f);
    }

    private void Reset()
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

        _qteYellowZone.RectSize = new Vector2(
            _qteBar.RectSize.x * _qteYellowZoneCoverPercentage,
            _qteBar.RectSize.y
        );

        _qteYellowZone.RectPosition = new Vector2(0, 0);
    }

    private bool IsValueInGreenZone(float value)
    {
        float valueNorm = value / 100.0f;
        float minX = valueNorm;
        float maxX = valueNorm + _qteYellowZoneCoverPercentage;

        if (maxX < _greenZoneStartValue || minX > _greenZoneEndValue)
        {
            GD.Print("IsValueInGreenZone(): value is completely outsize");
            return false;
        }

        float globalCoverage = Mathf.Min(maxX, _greenZoneEndValue) - Mathf.Max(minX, _greenZoneStartValue);
        float coverageOfYellow = Mathf.Clamp(globalCoverage / _qteYellowZoneCoverPercentage, 0.0f, 1.0f);
        GD.Print("IsValueInGreenZone(): coverage: " + coverageOfYellow + "%");

        return coverageOfYellow >= _qteAcceptableCoverage;
    }

    private void OnQteButtonPressed()
    {
        float currentValue = (float)_qteBar.Value;

        if (IsValueInGreenZone(currentValue))
        {
            _playerHasWon = true;
            PlayThrowAnimation(_playerCharacter, _opponentCharacter, 1.0f);
        }
        else
        {
            _playerHasWon = false;
            PlayThrowAnimation(_opponentCharacter, _playerCharacter, -1.0f);
        }

        _qteFillTween.Stop(_qteBar);
    }

    private void PlayThrowAnimation(AnimatedSprite thrower, AnimatedSprite throwee, float flightDirection)
    {
        thrower.Animation = "Throw";
        thrower.Frame = 0;

        thrower.Connect("animation_finished", this, nameof(PlayThrowAnimation_AfterAnimationFinished), new Array { throwee, flightDirection });
    }

    private void OnThrowTweenCompleted()
    {
        SceneManager.Instance.LoadDiceLevel(_playerHasWon);
    }

    private void PlayThrowAnimation_AfterAnimationFinished(AnimatedSprite throwee, float flightDirection)
    {
        throwee.Animation = "InFlight";
        throwee.Frame = 0;
        throwee.Playing = true;

        Tween throwTween = new Tween();
        AddChild(throwTween);

        throwTween.Connect("tween_all_completed", this, nameof(OnThrowTweenCompleted));
        throwTween.InterpolateProperty(throwee, "position", throwee.Position, throwee.Position + new Vector2(100 * flightDirection, 0), 0.5f, Tween.TransitionType.Linear);
        throwTween.Start();
    }
}