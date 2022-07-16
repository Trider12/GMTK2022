using Godot;

namespace Game.Code.Managers
{
    public class GameManager : Node
    {
        private GameManager()
        {
        }

        public static GameManager Instance { get; } = new GameManager();

        public Player Player { get; private set; } = GD.Load<PackedScene>("res://Scenes/Player.tscn").Instance<Player>();

        public float BluePlayerScore { get; private set; } = 0;
        public float RedPlayerScore { get; private set; } = 0;
        
        public override void _Ready()
        {
            base._Ready();

            GD.Randomize();
        }

        public void LoadGame()
        {
            SceneManager.Instance.LoadLevel("JudoLevel");
        }
    }
}