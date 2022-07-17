using Game.Code.Managers;

using Godot;

namespace Game.Code.UI
{
    public class MainMenu : Control
    {
        private NinePatchRect _mainRect, _settingsRect;
        private HSlider _volumeSlider;

        public override void _Ready()
        {
            _mainRect = GetNode<NinePatchRect>("Main");
            _settingsRect = GetNode<NinePatchRect>("Settings");

            _mainRect.Visible = true;
            _settingsRect.Visible = false;

            GetNode<Button>("Main/Buttons/PlayButton").Connect("pressed", this, nameof(OnPlayGameButtonPressed));
            GetNode<Button>("Main/Buttons/SettingsButton").Connect("pressed", this, nameof(OnSettingsButtonPressed));
            GetNode<Button>("Main/Buttons/AuthorsButton").Connect("pressed", this, nameof(OnAuthorsButtonPressed));
            GetNode<Button>("Main/Buttons/ExitButton").Connect("pressed", this, nameof(OnExitButtonPressed));
            GetNode<TextureButton>("Settings/CloseButton").Connect("pressed", this, nameof(OnSettingsCloseButtonPressed));
            _volumeSlider = GetNode<HSlider>("Settings/HSlider");
            _volumeSlider.Connect("value_changed", this, nameof(OnMasterVolumeValueChanged));
            _volumeSlider.Value = _volumeSlider.MaxValue;
        }

        private void OnPlayGameButtonPressed()
        {
            SoundManager.Instance.PlayClickStartButtonSound();
            GameManager.Instance.LoadGame();
        }

        private void OnSettingsButtonPressed()
        {
            SoundManager.Instance.PlayClickButtonSound();
            _mainRect.Visible = false;
            _settingsRect.Visible = true;
        }

        private void OnSettingsCloseButtonPressed()
        {
            SoundManager.Instance.PlayClickButtonSound();
            _mainRect.Visible = true;
            _settingsRect.Visible = false;
        }

        private void OnAuthorsButtonPressed()
        {
            SoundManager.Instance.PlayClickButtonSound();
            // TODO
        }

        private void OnExitButtonPressed()
        {
            SoundManager.Instance.PlayClickButtonSound();
            GetTree().Quit();
        }

        private void OnMasterVolumeValueChanged(float value)
        {
            AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), GD.Linear2Db(value / (float)_volumeSlider.MaxValue));
        }
    }
}