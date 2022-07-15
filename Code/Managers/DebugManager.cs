using System.Diagnostics;

using Godot;

namespace Game.Code.Managers
{
    public class DebugManager : CanvasLayer
    {
        private const int ArgumentsToConsoleMinCount = 2;

        private LineEdit _consoleInput;
        private Control _debugOverlay;
        private OptionButton _levelsOption;
        private bool _prevPauseState = false;

        public DebugManager()
        {
            Debug.Assert(Instance == null);
            Instance = this;
        }

        public static DebugManager Instance { get; private set; }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("debug"))
            {
                if (_debugOverlay.Visible)
                {
                    var tree = GetTree();
                    tree.Paused = _prevPauseState;
                    _debugOverlay.Visible = false;
                }
                else
                {
                    var tree = GetTree();
                    _prevPauseState = tree.Paused;
                    tree.Paused = true;
                    _debugOverlay.Visible = true;
                }
            }
        }

        public override void _Ready()
        {
#if !DEBUG
            SetProcessInput(false);
            SetPhysicsProcess(false);
            return;
#endif
            PauseMode = PauseModeEnum.Process;

            _debugOverlay = GetNode<Control>("DebugOverlay");
            _levelsOption = GetNode<OptionButton>("DebugOverlay/Buttons/LevelsOptionButton");
            _consoleInput = GetNode<LineEdit>("DebugOverlay/ConsoleInput");
            _consoleInput.Connect("text_entered", this, nameof(OnConsoleInputTextEntered));
            GetNode<Button>("DebugOverlay/Buttons/LoadLevelButton").Connect("pressed", this, nameof(OnLoadLevelButtonPressed));

            foreach (var level in SceneManager.Levels.Keys)
            {
                _levelsOption.AddItem(level);
            }

            _debugOverlay.Visible = false;
        }

        private void OnConsoleInputTextEntered(string text)
        {
            _consoleInput.Text = string.Empty;
            GD.Print(text);

            string[] splittedText = text.Split(" ");

            if (splittedText.Length >= ArgumentsToConsoleMinCount)
            {
                switch (splittedText[0])
                {
                    case "level":
                    {
                        string levelName = splittedText[1];
                        SceneManager.Instance.LoadLevel(levelName);

                        break;
                    }
                    default:
                        break;
                }
            }
        }

        private void OnLoadLevelButtonPressed()
        {
            SceneManager.Instance.LoadLevel(_levelsOption.GetItemText(_levelsOption.Selected));
        }
    }
}