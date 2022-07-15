using System;
using System.Linq;

using Game.Code.Managers;

using Godot;

using Newtonsoft.Json;

namespace Game.Code.UI
{
    public class MainMenu : Control
    {
        private const string SettingsFileName = "user://settings.json";

        private static readonly Vector2[] Resolutions =
        {
            new Vector2(1024, 576),
            new Vector2(1280, 720),
            new Vector2(1366, 768),
            new Vector2(1536, 864),
            new Vector2(1600, 900),
            new Vector2(1920, 1080),
            new Vector2(2560, 1440),
        };

        private static readonly string[] ResolutionsNames = Resolutions.Select(res => $"{(int)res.x}x{(int)res.y}").ToArray();

        private Settings _settings;
        private SettingsWindow _settingsWindow;

        public override void _Ready()
        {
            GetNode<Button>("Buttons/NewGameButton").Connect("pressed", this, nameof(OnNewGameButtonPressed));
            GetNode<Button>("Buttons/SettingsButton").Connect("pressed", this, nameof(OnSettingsButtonPressed));
            GetNode<Button>("Buttons/AuthorsButton").Connect("pressed", this, nameof(OnAuthorsButtonPressed));
            GetNode<Button>("Buttons/ExitButton").Connect("pressed", this, nameof(OnExitButtonPressed));

            _settingsWindow = GetNode<SettingsWindow>("SettingsWindow");
            _settingsWindow.OkButton.Connect("pressed", this, nameof(OnSettingsOkButtonPressed));
            _settingsWindow.CancelButton.Connect("pressed", this, nameof(OnSettingsCancelButtonPressed));
            _settingsWindow.DisplayModeOption.Connect("item_selected", this, nameof(OnDisplayModeItemSelected));
            _settingsWindow.MasterVolumeSlider.Connect("value_changed", this, nameof(OnMasterVolumeValueChanged));

            foreach (var res in ResolutionsNames)
            {
                _settingsWindow.ResolutionOption.AddItem(res);
            }

            foreach (var mode in Enum.GetValues(typeof(DisplayMode)) as DisplayMode[])
            {
                _settingsWindow.DisplayModeOption.AddItem(mode.ToString());
            }

            LoadSettings();
            ApplySettings();
        }

        private void ApplySettings()
        {
            Vector2 resolution = Resolutions[0];

            try
            {
                resolution = Resolutions[Array.IndexOf(ResolutionsNames, _settings.Resolution)];
            }
            catch
            {
            }

            OS.WindowBorderless = _settings.DisplayMode == DisplayMode.Borderless;
            OS.WindowFullscreen = _settings.DisplayMode == DisplayMode.Fullscreen;
            OS.WindowSize = _settings.DisplayMode == DisplayMode.Borderless ? OS.GetScreenSize() : resolution;
            OS.CenterWindow();
            GD.Print("Window Resolution: " + OS.WindowSize);

            AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), GD.Linear2Db(_settings.MasterVolume / 100f));
        }

        private void LoadSettings()
        {
            var saveFile = new File();

            if (saveFile.FileExists(SettingsFileName))
            {
                saveFile.Open(SettingsFileName, File.ModeFlags.Read);
                _settings = JsonConvert.DeserializeObject<Settings>(saveFile.GetLine());
                saveFile.Close();
            }
            else
            {
                _settings = new Settings
                {
                    Resolution = ResolutionsNames[1],
                    DisplayMode = 0,
                    MasterVolume = 50
                };
            }

            _settingsWindow.ResolutionOption.Selected = Array.IndexOf(ResolutionsNames, _settings.Resolution);
            _settingsWindow.DisplayModeOption.Selected = (int)_settings.DisplayMode;
            _settingsWindow.MasterVolumeSlider.Value = _settings.MasterVolume;
        }

        private void OnAuthorsButtonPressed()
        {
            // TODO
        }

        private void OnDisplayModeItemSelected(int index)
        {
            _settingsWindow.ResolutionOption.Disabled = index != 0;
        }

        private void OnExitButtonPressed()
        {
            GetTree().Quit();
        }

        private void OnMasterVolumeValueChanged(float value)
        {
            AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), GD.Linear2Db(value / 100f));
        }

        private void OnNewGameButtonPressed()
        {
            GameManager.Instance.LoadGame();
        }

        private void OnSettingsButtonPressed()
        {
            _settingsWindow.PopupCentered();
        }

        private void OnSettingsCancelButtonPressed()
        {
            _settingsWindow.ResolutionOption.Selected = Array.IndexOf(ResolutionsNames, _settings.Resolution);
            _settingsWindow.DisplayModeOption.Selected = (int)_settings.DisplayMode;
            _settingsWindow.MasterVolumeSlider.Value = _settings.MasterVolume;

            _settingsWindow.Hide();
        }

        private void OnSettingsOkButtonPressed()
        {
            _settings.Resolution = ResolutionsNames[_settingsWindow.ResolutionOption.Selected];
            _settings.DisplayMode = (DisplayMode)_settingsWindow.DisplayModeOption.Selected;
            _settings.MasterVolume = (int)_settingsWindow.MasterVolumeSlider.Value;

            ApplySettings();
            SaveSettings();

            _settingsWindow.Hide();
        }

        private void SaveSettings()
        {
            var saveFile = new File();
            saveFile.Open(SettingsFileName, File.ModeFlags.Write);
            saveFile.StoreLine(JsonConvert.SerializeObject(_settings));
            saveFile.Close();
        }
    }
}