namespace Game.Code.UI
{
    public enum DisplayMode
    {
        Windowed = 0,
        Borderless = 1,
        Fullscreen = 2
    }

    public class Settings
    {
        public DisplayMode DisplayMode { get; set; }
        public int MasterVolume { get; set; }
        public string Resolution { get; set; }
    }
}