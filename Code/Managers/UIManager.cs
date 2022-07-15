using System.Diagnostics;

using Godot;

namespace Game.Code.Managers
{
    public class UIManager : CanvasLayer
    {
        private Control _hud;
        private Control _pauseMenu;

        public UIManager()
        {
            Debug.Assert(Instance == null);
            Instance = this;
        }

        public static UIManager Instance { get; private set; }

        public bool IsHUDVisible
        {
            get => _hud.Visible;
            set => _hud.Visible = value;
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_cancel"))
            {
                TogglePause();
            }
        }

        public override void _Ready()
        {
            PauseMode = PauseModeEnum.Process;

            _hud = GetNode<Control>("HUD");
            _hud.Visible = false;
            _hud.PauseMode = PauseModeEnum.Stop;
            _pauseMenu = GetNode<Control>("PauseMenu");
            _pauseMenu.Visible = false;
            _pauseMenu.PauseMode = PauseModeEnum.Process;

            GetNode<Button>("HUD/PauseButton").Connect("pressed", this, nameof(OnPauseButtonPressed));
            GetNode<Button>("PauseMenu/Buttons/ResumeButton").Connect("pressed", this, nameof(OnResumeButtonPressed));
            GetNode<Button>("PauseMenu/Buttons/MainMenuButton").Connect("pressed", this, nameof(OnMainMenuButtonPressed));
        }

        public void LoadMainMenu()
        {
            SetPause(false);
            IsHUDVisible = false;
            SceneManager.Instance.LoadMainMenu();
        }

        private void OnMainMenuButtonPressed()
        {
            LoadMainMenu();
        }

        private void OnPauseButtonPressed()
        {
            SetPause(true);
        }

        private void OnResumeButtonPressed()
        {
            SetPause(false);
        }

        private void SetPause(bool value)
        {
            var tree = GetTree();
            tree.Paused = value;
            _pauseMenu.Visible = value;
        }

        private void TogglePause()
        {
            var tree = GetTree();
            tree.Paused = !tree.Paused;
            _pauseMenu.Visible = tree.Paused;
        }
    }
}