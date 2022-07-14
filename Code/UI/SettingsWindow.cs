using Godot;

namespace Game.Code.UI
{
    public class SettingsWindow : PopupDialog
    {
        public Button CancelButton { get; private set; }
        public OptionButton DisplayModeOption { get; private set; }
        public HSlider MasterVolumeSlider { get; private set; }
        public Button OkButton { get; private set; }
        public OptionButton ResolutionOption { get; private set; }

        public override void _Ready()
        {
            OkButton = GetNode<Button>("Body/Buttons/OkButton");
            CancelButton = GetNode<Button>("Body/Buttons/CancelButton");
            DisplayModeOption = GetNode<OptionButton>("Body/Fields/DisplayModeButton");
            ResolutionOption = GetNode<OptionButton>("Body/Fields/ResolutionButton");
            MasterVolumeSlider = GetNode<HSlider>("Body/Fields/VolumeSlider");
        }
    }
}