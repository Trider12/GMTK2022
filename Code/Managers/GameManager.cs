using Godot;

namespace Game.Code.Managers
{
    public class GameManager
    {
        private GameManager()
        {
        }

        public static GameManager Instance { get; } = new GameManager();

        public Player Player { get; private set; } = GD.Load<PackedScene>("res://Scenes/Player.tscn").Instance<Player>();

        public void LoadGame()
        {
            SceneManager.Instance.LoadLevel("MainLevel");
        }
    }
}