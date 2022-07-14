using System.Diagnostics;

using Godot;

namespace Game.Code.Managers
{
    public class DebugManager : CanvasLayer
    {
        private const int ArgumentsToConsoleMinCount = 2;

        private Control _debugOverlay;
        private OptionButton _levelsOption;

        public static DebugManager Instance { get; private set; } = null;

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("debug"))
            {
                _debugOverlay.Visible = !_debugOverlay.Visible;
            }
        }

        public override void _Ready()
        {
            Debug.Assert(Instance == null);
            Instance = this;

#if !DEBUG
            SetProcessInput(false);
            SetPhysicsProcess(false);
            return;
#endif

            _debugOverlay = GetNode<Control>("DebugOverlay");
            _levelsOption = GetNode<OptionButton>("DebugOverlay/Buttons/LevelsOptionButton");

            GetNode<Button>("DebugOverlay/Buttons/LoadLevelButton").Connect("pressed", this, nameof(OnLoadLevelButtonPressed));
            GetNode<LineEdit>("DebugOverlay/ConsoleInput").Connect("text_entered", this, nameof(OnConsoleInput));

            foreach (var level in SceneManager.Levels.Keys)
            {
                _levelsOption.AddItem(level);
            }
        }

        private void OnConsoleInput(string text)
        {
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