using Catalyst.Elements;
using Godot;

namespace Catalyst.Spells;

public struct WeaponFieldState
{
    public Vector2 Position;
    public StringName SourceId;
    public float Damage;
    public ElementType Element;
    public int ElementStacks;
    public float Remaining;
    public float PulseRemaining;
    public int ChainDepth;
}
