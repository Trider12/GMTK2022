using Godot;

namespace Game.Code
{
    internal class Tile : Node2D
    {
        public Label Label { get; private set; }
        public float Offset { get; set; }

        public override void _Ready()
        {
            Label = GetNode<Label>("Label");
        }
    }
}