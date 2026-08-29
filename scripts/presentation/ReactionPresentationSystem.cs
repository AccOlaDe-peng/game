using Catalyst.Elements;
using Catalyst.Passives;
using Catalyst.Spells;
using Godot;

namespace Catalyst.Presentation;

public partial class ReactionPresentationSystem : Node3D
{
    private const int CapacityPerKind = 32;

    private struct Pulse
    {
        public ReactionKind Kind;
        public Vector2 Position;
        public float Age;
        public float Lifetime;
    }

    private struct Beam
    {
        public Vector2 Start;
        public Vector2 End;
        public float Age;
    }

    private readonly List<Pulse> _pulses = new(CapacityPerKind * 3);
    private readonly List<Beam> _beams = new(32);
    private ElementSystem _elements = null!;
    private SpellSystem _spells = null!;
    private AutoCatalysisSystem _catalyze = null!;
    private MultiMesh _thermal = null!;
    private MultiMesh _conductive = null!;
    private MultiMesh _catalyzePulses = null!;
    private MultiMesh _lightningBeams = null!;

    public override void _Ready()
    {
        _elements = GetNode<ElementSystem>("../../../SimulationRoot/ElementSystem");
        _spells = GetNode<SpellSystem>("../../../SimulationRoot/SpellSystem");
        _catalyze = GetNode<AutoCatalysisSystem>("../../../SimulationRoot/AutoCatalysisSystem");
        _thermal = BuildMultiMesh(
            GetNode<MultiMeshInstance3D>("SteamShock"),
            VfxMaterials.BuildBillboardMesh(1.3f, "smoke_01", ElementPalette.Reaction(ReactionKind.SteamShock)));
        _conductive = BuildMultiMesh(
            GetNode<MultiMeshInstance3D>("Conduction"),
            VfxMaterials.BuildGroundMesh(1.5f, "spark_01", ElementPalette.Reaction(ReactionKind.Conduction)));
        _catalyzePulses = BuildMultiMesh(
            GetNode<MultiMeshInstance3D>("CatalyzePulse"),
            VfxMaterials.BuildGroundMesh(1.6f, "circle_02", ElementPalette.Arcane));
        _lightningBeams = BuildMultiMesh(
            GetNode<MultiMeshInstance3D>("LightningBeams"),
            VfxMaterials.BuildVerticalBeamMesh(1.0f, 0.3f, "trace_01", ElementPalette.Lightning));
        _elements.ReactionTriggered += OnReactionTriggered;
        _spells.LightningJumped += OnLightningJumped;
        _catalyze.Executed += OnCatalyzed;
    }

    public override void _Process(double deltaValue)
    {
        float delta = (float)deltaValue;
        int index = 0;
        while (index < _pulses.Count)
        {
            Pulse pulse = _pulses[index];
            pulse.Age += delta;
            if (pulse.Age >= pulse.Lifetime)
            {
                _pulses.RemoveAt(index);
                continue;
            }
            _pulses[index] = pulse;
            index++;
        }

        index = 0;
        while (index < _beams.Count)
        {
            Beam beam = _beams[index];
            beam.Age += delta;
            if (beam.Age >= 0.14f)
            {
                _beams.RemoveAt(index);
                continue;
            }
            _beams[index] = beam;
            index++;
        }

        SyncPresentation();
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_elements))
        {
            _elements.ReactionTriggered -= OnReactionTriggered;
        }
        if (IsInstanceValid(_spells))
        {
            _spells.LightningJumped -= OnLightningJumped;
        }
        if (IsInstanceValid(_catalyze))
        {
            _catalyze.Executed -= OnCatalyzed;
        }
    }

    private void OnReactionTriggered(ReactionKind kind, Vector2 position, float damage)
    {
        if (_pulses.Count >= CapacityPerKind * 3)
        {
            _pulses.RemoveAt(0);
        }
        _pulses.Add(new Pulse
        {
            Kind = kind,
            Position = position,
            Age = 0.0f,
            Lifetime = 0.55f
        });
    }

    private void OnCatalyzed(Vector2 position, int reactionCount, float damage)
    {
        _pulses.Add(new Pulse
        {
            Kind = ReactionKind.None,
            Position = position,
            Age = 0.0f,
            Lifetime = 0.42f
        });
    }

    private void OnLightningJumped(Vector2 start, Vector2 end)
    {
        if (_beams.Count >= 32)
        {
            _beams.RemoveAt(0);
        }
        _beams.Add(new Beam { Start = start, End = end, Age = 0.0f });
    }

    private void SyncPresentation()
    {
        int thermalCount = 0;
        int conductiveCount = 0;
        int catalyzeCount = 0;
        foreach (Pulse pulse in _pulses)
        {
            float progress = pulse.Age / pulse.Lifetime;
            float alpha = 1.0f - progress;
            Vector3 origin = new(pulse.Position.X, 0.18f, pulse.Position.Y);
            switch (pulse.Kind)
            {
                case ReactionKind.None when catalyzeCount < CapacityPerKind:
                {
                    Vector3 scale = new(0.5f + progress * 5.2f, 1.0f, 0.5f + progress * 5.2f);
                    Basis basis = Basis.Identity.Scaled(scale);
                    _catalyzePulses.SetInstanceTransform(catalyzeCount, new Transform3D(basis, origin));
                    _catalyzePulses.SetInstanceColor(catalyzeCount++, new Color(1, 1, 1, alpha));
                    break;
                }
                case ReactionKind.SteamShock when thermalCount < CapacityPerKind:
                {
                    Vector3 scale = Vector3.One * (0.4f + progress * 2.4f);
                    Basis basis = Basis.Identity.Scaled(scale);
                    _thermal.SetInstanceTransform(thermalCount, new Transform3D(basis, origin));
                    _thermal.SetInstanceColor(thermalCount++, new Color(1, 1, 1, alpha));
                    break;
                }
                case ReactionKind.Conduction when conductiveCount < CapacityPerKind:
                {
                    Basis basis = new Basis(Vector3.Up, pulse.Age * 5.0f)
                        .Scaled(new Vector3(0.7f + progress * 3.5f, 1.0f, 1.0f));
                    _conductive.SetInstanceTransform(conductiveCount, new Transform3D(basis, origin));
                    _conductive.SetInstanceColor(conductiveCount++, new Color(1, 1, 1, alpha));
                    break;
                }
            }
        }

        _thermal.VisibleInstanceCount = thermalCount;
        _conductive.VisibleInstanceCount = conductiveCount;
        _catalyzePulses.VisibleInstanceCount = catalyzeCount;

        int beamCount = Math.Min(_beams.Count, 32);
        for (int index = 0; index < beamCount; index++)
        {
            Beam beam = _beams[index];
            Vector2 offset = beam.End - beam.Start;
            float length = offset.Length();
            Vector2 midpoint = (beam.Start + beam.End) * 0.5f;
            float angle = -offset.Angle();
            Basis basis = new Basis(Vector3.Up, angle)
                .Scaled(new Vector3(length, 1.0f, 1.0f));
            _lightningBeams.SetInstanceTransform(index,
                new Transform3D(basis, new Vector3(midpoint.X, 0.85f, midpoint.Y)));
            _lightningBeams.SetInstanceColor(index,
                new Color(1, 1, 1, 1.0f - beam.Age / 0.14f));
        }
        _lightningBeams.VisibleInstanceCount = beamCount;
    }

    private static MultiMesh BuildMultiMesh(
        MultiMeshInstance3D view,
        Mesh mesh)
    {
        MultiMesh multiMesh = new()
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = mesh,
            InstanceCount = CapacityPerKind,
            VisibleInstanceCount = 0
        };
        view.Multimesh = multiMesh;
        return multiMesh;
    }
}
