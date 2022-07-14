using Godot;

namespace Game.Code.Interfaces
{
    public interface ILevel
    {
        Node2D World { get; }

        void OnLevelLoad();

        void OnLevelUnload();
    }
}