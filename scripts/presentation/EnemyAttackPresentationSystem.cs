using Catalyst.Enemies;
using Godot;

namespace Catalyst.Presentation;

public partial class EnemyAttackPresentationSystem : Node3D
{
    private struct WarningPulse
    {
        public Vector2 Position;
        public float Radius;
        public float Age;
        public float Lifetime;
    }

    private const int Capacity = 48;
    private readonly List<WarningPulse> _pulses = new(Capacity);
    private EnemySystem _enemies = null!;
    private MultiMesh _multiMesh = null!;

    public override void _Ready()
    {
        _enemies = GetNode<EnemySystem>("../../../SimulationRoot/EntitySystem");
        CylinderMesh mesh = new()
        {
            TopRadius = 1.0f,
            BottomRadius = 1.0f,
            Height = 0.035f,
            RadialSegments = 24
        };
        mesh.Material = new StandardMaterial3D
        {
            AlbedoColor = new Color(1.0f, 0.12f, 0.06f, 0.45f),
            EmissionEnabled = true,
            Emission = new Color(1.0f, 0.05f, 0.02f),
            EmissionEnergyMultiplier = 1.4f,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            VertexColorUseAsAlbedo = true
        };
        _multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = mesh,
            InstanceCount = Capacity,
            VisibleInstanceCount = 0
        };
        GetNode<MultiMeshInstance3D>("Warnings").Multimesh = _multiMesh;
        _enemies.EnemyAttackTelegraphed += OnTelegraphed;
    }

    public override void _Process(double deltaValue)
    {
        float delta = (float)deltaValue;
        int index = 0;
        while (index < _pulses.Count)
        {
            WarningPulse pulse = _pulses[index];
            pulse.Age += delta;
            if (pulse.Age >= pulse.Lifetime)
            {
                _pulses.RemoveAt(index);
                continue;
            }
            _pulses[index] = pulse;
            index++;
        }

        for (index = 0; index < _pulses.Count; index++)
        {
            WarningPulse pulse = _pulses[index];
            float progress = pulse.Age / pulse.Lifetime;
            float scale = pulse.Radius * (0.85f + 0.15f * Mathf.Sin(progress * Mathf.Pi * 6.0f));
            Basis basis = Basis.Identity.Scaled(new Vector3(scale, 1.0f, scale));
            _multiMesh.SetInstanceTransform(index,
                new Transform3D(basis, new Vector3(pulse.Position.X, 0.08f, pulse.Position.Y)));
            _multiMesh.SetInstanceColor(index, new Color(1, 1, 1, 0.35f + progress * 0.55f));
        }
        _multiMesh.VisibleInstanceCount = _pulses.Count;
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_enemies))
        {
            _enemies.EnemyAttackTelegraphed -= OnTelegraphed;
        }
    }

    private void OnTelegraphed(Vector2 position, float radius, EnemyArchetype archetype)
    {
        if (_pulses.Count >= Capacity)
        {
            _pulses.RemoveAt(0);
        }
        float lifetime = archetype switch
        {
            EnemyArchetype.Exploder => 0.9f,
            EnemyArchetype.EliteCharger => 0.75f,
            EnemyArchetype.Caster => 0.7f,
            EnemyArchetype.BossAshenColossus => 0.9f,
            _ => 0.5f
        };
        _pulses.Add(new WarningPulse
        {
            Position = position,
            Radius = radius,
            Lifetime = lifetime
        });
    }
}
