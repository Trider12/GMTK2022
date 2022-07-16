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

    private float _greenZoneStartValue; // 0 - 100
    private float _greenZoneEndValue; // 0 - 100
    private float _greenZoneSize; // 0 - 100

    [Export] private float _qteFillDuration = 3.0f; // seconds
    [Export] private float _qteGreenZoneCoverPercentage = 0.05f;

    private AnimatedSprite _playerCharacter;
    private AnimatedSprite _opponentCharacter;

    private ProgressBar _qteProgressBar;
    private ColorRect _qteGreenZone;
    private Button _qteButton;
    private Tween _qteFillTween;

    public override void _Ready()
    {
        base._Ready();

        _world = GetNode<Node2D>("World");

        _playerCharacter = GetNode<AnimatedSprite>("Persons/Player/AnimatedSprite");
        _opponentCharacter = GetNode<AnimatedSprite>("Persons/Opponent/AnimatedSprite");

        _qteProgressBar = GetNode<ProgressBar>("QTE/VBoxContainer/ProgressBar");
        _qteGreenZone = GetNode<ColorRect>("QTE/VBoxContainer/ProgressBar/GreenZone");
        _qteButton = GetNode<Button>("QTE/VBoxContainer/HBoxContainer/Button");

        ResetUiState();

        _greenZoneSize = _qteGreenZoneCoverPercentage * 100.0f;
        RandomizeGreenZone();
        ConfigureGreenZoneWidget();

        _qteFillTween = new Tween();
        _qteFillTween.InterpolateProperty(_qteProgressBar, "value", 0.0f, 100.0f, _qteFillDuration, Tween.TransitionType.Linear);

        _qteProgressBar.Connect("value_changed", this, nameof(OnQteProgressBarValueChanged));
        _qteButton.Connect("pressed", this, nameof(OnQteButtonPressed));

        AddChild(_qteFillTween);
        _qteFillTween.Start();
    }

    private void ResetUiState()
    {
        _qteProgressBar.Value = 0;

        _playerCharacter.Animation = "Idle";
        _playerCharacter.Playing = true;
        _opponentCharacter.Animation = "Idle";
        _opponentCharacter.Playing = true;
    }

    private void RandomizeGreenZone()
    {
        float startValue = GD.Randf() * 100.0f;
        if (startValue + _greenZoneSize > 100.0f)
        {
            startValue = 100.0f - _greenZoneSize;
        }

        _greenZoneStartValue = startValue;
        _greenZoneEndValue = startValue + _greenZoneSize;
    }

    private void ConfigureGreenZoneWidget()
    {
        Debug.Assert(_qteGreenZone != null);

        _qteGreenZone.RectSize = new Vector2(
            _qteProgressBar.RectSize.x * _qteGreenZoneCoverPercentage,
            _qteProgressBar.RectSize.y
        );

        _qteGreenZone.RectPosition = new Vector2(
            _qteProgressBar.RectSize.x * _greenZoneStartValue / 100.0f,
            0
        );
    }

    private bool IsValueInGreenZone(float value)
    {
        return value >= _greenZoneStartValue && value <= _greenZoneEndValue;
    }

    public void OnLevelLoad()
    {
    }

    public void OnLevelUnload()
    {
    }

    private void OnQteProgressBarValueChanged(float value)
    {
        if (value >= 100.0f)
        {
            OnQteTimedOut();
            return;
        }
    }

    private void OnQteButtonPressed()
    {
        float currentValue = (float) _qteProgressBar.Value;

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

    private void OnQteTimedOut()
    {
        EmitSignal(nameof(OnTimedOut));
        GD.Print("You lost");

        PlayThrowAnimation(_opponentCharacter, _playerCharacter, -1.0f);
    }

    private void OnQteBadUserResult()
    {
        EmitSignal(nameof(OnBadResult));
        GD.Print("You pressed not in time :(");

        _qteFillTween.Stop(_qteProgressBar);

        PlayThrowAnimation(_opponentCharacter, _playerCharacter, -1.0f);
    }

    private void OnQteGoodUserResult()
    {
        EmitSignal(nameof(OnGoodResult));
        GD.Print("You pressed in time :)");

        _qteFillTween.Stop(_qteProgressBar);

        PlayThrowAnimation(_playerCharacter, _opponentCharacter, 1.0f);
    }
}
