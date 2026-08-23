using Godot;

namespace Catalyst.App;

public partial class SceneRouter : Node
{
    [Signal]
    public delegate void ScreenChangedEventHandler(Node currentScreen);

    private Node _host = null!;
    private Node? _current;

    public override void _Ready()
    {
        _host = GetNode<Node>("../CurrentScreen");
    }

    public T Show<T>(PackedScene scene)
        where T : Node
    {
        if (_current is not null && IsInstanceValid(_current))
        {
            _host.RemoveChild(_current);
            _current.QueueFree();
        }

        T next = scene.Instantiate<T>();
        _host.AddChild(next);
        _current = next;
        EmitSignal(SignalName.ScreenChanged, next);
        return next;
    }
}
