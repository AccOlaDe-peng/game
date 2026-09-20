using Catalyst.Elements;
using Godot;

namespace Catalyst.Presentation;

/// <summary>
/// Instanties the Binbun Elemental Magic FX fire range burst at live element-reaction
/// sites (currently SteamShock = Fire + Water). Kept separate from the MultiMesh
/// billboard system in <see cref="ReactionPresentationSystem"/>. CC0 asset, tinted fire.
/// </summary>
public partial class BinbunBurstVfx : Node3D
{
    private const string BurstScenePath =
        "res://assets/art/vfx/BinbunVFX_Vol2/ElementalMagicFX/effects/area/vfx_fire_area_01.tscn";
    private const int MaxActive = 16;
    private const float KeepAlive = 2.6f;
    private const float FadeTail = 0.8f;

    private struct ActiveBurst
    {
        public Node3D Node;
        public float Age;
        public bool Fading;
    }

    private PackedScene _burstScene = null!;
    private ElementSystem _elements = null!;
    private readonly List<ActiveBurst> _active = new(MaxActive);

    public override void _Ready()
    {
        _burstScene = GD.Load<PackedScene>(BurstScenePath);
        _elements = GetNode<ElementSystem>("../../../SimulationRoot/ElementSystem");
        _elements.ReactionTriggered += OnReactionTriggered;
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_elements))
        {
            _elements.ReactionTriggered -= OnReactionTriggered;
        }
    }

    public override void _Process(double deltaValue)
    {
        float delta = (float)deltaValue;
        for (int index = 0; index < _active.Count;)
        {
            ActiveBurst burst = _active[index];
            burst.Age += delta;
            if (!IsInstanceValid(burst.Node))
            {
                _active.RemoveAt(index);
                continue;
            }
            if (burst.Age >= KeepAlive && !burst.Fading)
            {
                StopEmitting(burst.Node);
                burst.Fading = true;
            }
            if (burst.Age >= KeepAlive + FadeTail)
            {
                burst.Node.QueueFree();
                _active.RemoveAt(index);
                continue;
            }
            _active[index] = burst;
            index++;
        }
    }

    private void OnReactionTriggered(ReactionKind kind, Vector2 position, float damage)
    {
        if (kind != ReactionKind.SteamShock)
        {
            return;
        }
        if (_active.Count >= MaxActive)
        {
            _active[0].Node.QueueFree();
            _active.RemoveAt(0);
        }
        Node3D burst = _burstScene.Instantiate<Node3D>();
        burst.Position = new Vector3(position.X, 0.0f, position.Y);
        AddChild(burst);
        _active.Add(new ActiveBurst { Node = burst, Age = 0.0f, Fading = false });
    }

    private static void StopEmitting(Node3D burst)
    {
        foreach (Node child in burst.GetChildren())
        {
            if (child is GpuParticles3D particles)
            {
                particles.Emitting = false;
            }
        }
    }
}