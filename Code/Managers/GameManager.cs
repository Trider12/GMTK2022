using System.Diagnostics;

using Godot;

namespace Game.Code.Managers
{
    public class GameManager : Node
    {
        public GameManager()
        {
            Debug.Assert(Instance == null);
            Instance = this;
        }

        public static GameManager Instance { get; private set; }

        public uint BluePlayerScore { get; set; } = 1;
        public uint RedPlayerScore { get; set; } = 1;
        public uint MaxScore { get; set; } = 1;
        public float DifficultyPercentage => 1f + (BluePlayerScore - RedPlayerScore) * 1f / MaxScore; // [0 - 2]

        public override void _Ready()
        {
            GD.Randomize();
            SoundManager.Instance.PlayMainMenuTheme();
            GD.Print("SHOULD PLAY THEME IN WEB!");
        }

        public void LoadGame()
        {
            BluePlayerScore = 1;
            RedPlayerScore = 1;

            SceneManager.Instance.LoadJudoLevel();
        }
    }
}
