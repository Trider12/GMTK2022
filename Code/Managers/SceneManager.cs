using System.Collections.Generic;
using System.Diagnostics;

using Game.Code.Helpers;
using Game.Code.Interfaces;
using Game.Code.Levels;
using Game.Code.UI;

using Godot;

namespace Game.Code.Managers
{
    public class SceneManager : Node2D
    {
        private MainMenu _mainMenu;
        private SceneTree _tree;
        private TabletopLevel _tabletopLevel = Levels["TabletopLevel"].Instance<TabletopLevel>();

        private SceneManager()
        {
            _tree = (SceneTree)Engine.GetMainLoop();
            _mainMenu = _tree.Root.GetNode<MainMenu>("/root/MainMenu"); // starting scene
        }

        [Signal]
        public delegate void OnLevelChange();

        public static Dictionary<string, PackedScene> Levels { get; } = ResourceHelper.LoadResourcesDictionary<PackedScene>("res://Scenes/Levels", null, true);
        public static SceneManager Instance { get; } = new SceneManager();

        public ILevel CurrentLevel { get; private set; } = null;

        public void LoadMainMenu()
        {
            _tree.Root.CallDeferred("add_child", _mainMenu);

            if (CurrentLevel != null)
            {
                _tree.Root.RemoveChild(CurrentLevel as Node);
                CurrentLevel.OnLevelUnload();
                CurrentLevel = null;
            }

            SoundManager.Instance.PlayMainTheme();
        }

        public void LoadTabletopLoad()
        {
            _tabletopLevel.NeedsToUpdatePawns = true;
            LoadLevel(_tabletopLevel);
        }

        public void LoadJudoLevel()
        {
            LoadLevel("JudoLevel");
        }

        public void LoadDiceLevel(bool playerHasWon)
        {
            var level = Levels["DiceLevel"].InstanceOrNull<DiceLevel>();
            level.PlayerHasWon = playerHasWon;

            LoadLevel(level);
        }

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

        private void LoadLevel(ILevel level)
        {
            EmitSignal(nameof(OnLevelChange));

            if (CurrentLevel == null)
            {
                _tree.Root.RemoveChild(_mainMenu);
            }
            else
            {
                _tree.Root.RemoveChild(CurrentLevel as Node);
                CurrentLevel.OnLevelUnload();
            }

            Debug.Assert(level != null);
            CurrentLevel = level;

            _tree.Root.CallDeferred("add_child", CurrentLevel);
            CurrentLevel.OnLevelLoad();
        }

        public void ResetTabletop()
        {
            _tabletopLevel = Levels["TabletopLevel"].Instance<TabletopLevel>();
        }
    }
}
