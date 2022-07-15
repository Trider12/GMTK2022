using System.Collections.Generic;
using System.Diagnostics;

using Game.Code.Helpers;
using Game.Code.Interfaces;
using Game.Code.UI;

using Godot;

namespace Game.Code.Managers
{
    public class SceneManager : Node2D
    {
        private MainMenu _mainMenu;
        private SceneTree _tree;

        private SceneManager()
        {
            _tree = (SceneTree)Engine.GetMainLoop();
            _mainMenu = _tree.Root.GetNode<MainMenu>("/root/MainMenu"); // starting scene
        }

        [Signal]
        public delegate void OnLevelChange();

        public static SceneManager Instance { get; } = new SceneManager();
        public static Dictionary<string, PackedScene> Levels { get; } = PrefabHelper.LoadPrefabsDictionary("res://Scenes/Levels", null, true);

        public ILevel CurrentLevel { get; private set; } = null;

        public void LoadLevel(string levelName)
        {
            if (Levels.ContainsKey(levelName))
            {
                LoadLevel(Levels[levelName].InstanceOrNull<ILevel>());
            }
            else
            {
                GD.PushError($"Invalid level \"{levelName}\"");
            }
        }

        public void LoadMainMenu()
        {
            _tree.Root.CallDeferred("add_child", _mainMenu);

            if (CurrentLevel != null)
            {
                CurrentLevel.OnLevelUnload();
                CurrentLevel = null;
            }
        }

        private void LoadLevel(ILevel level)
        {
            EmitSignal(nameof(OnLevelChange));

            if (CurrentLevel == null)
            {
                _tree.Root.RemoveChild(_mainMenu);
            }
            else
            {
                CurrentLevel.OnLevelUnload();
            }

            Debug.Assert(level != null);
            CurrentLevel = level;

            _tree.Root.CallDeferred("add_child", CurrentLevel);
            CurrentLevel.OnLevelLoad();
        }
    }
}