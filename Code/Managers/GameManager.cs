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

        public Player Player { get; private set; } = GD.Load<PackedScene>("res://Scenes/Player.tscn").Instance<Player>();
        public uint BluePlayerScore { get; set; } = 1;
        public uint RedPlayerScore { get; set; } = 1;

        public override void _Ready()
        {
            GD.Randomize();
        }

        public void LoadGame()
        {
            BluePlayerScore = 1;
            RedPlayerScore = 1;

            SceneManager.Instance.LoadTabletopScene();
        }
    }
}