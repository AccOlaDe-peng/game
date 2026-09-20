using Catalyst.Elements;
using Godot;

namespace Catalyst.Presentation;

/// <summary>
/// Spawns a one-shot runtime-built GPUParticles3D burst when an element fusion
/// reaction fires, so Frost/Lightning/Steam/etc. read as distinct chip-bursts the
/// way the Binbun fire pack gives Fire one. Textures come from the CC0 Kenney pool
/// and colors from <see cref="ElementPalette"/>. Self-contained; no combat surgery.
/// </summary>
public partial class ElementalBurstVfx : Node3D
{
    private const int MaxActive = 24;
    private const float Lifetime = 0.7f;

    private struct ActiveBurst
    {
        public GpuParticles3D Node;
        public float Age;
        public bool Fading;
    }

    private ElementSystem _elements = null!;
    private readonly List<ActiveBurst> _active = new(MaxActive);

    /// <summary>Per-fusion look: (particle texture, tint).</summary>
    private static readonly Dictionary<FusionIdentity, (string Texture, Color Color)> FusionLook = new()
    {
        [FusionIdentity.ConductiveLightning] = ("trace_01", new Color(1.00f, 0.96f, 0.35f)),
        [FusionIdentity.Steam] = ("smoke_01", new Color(0.95f, 0.97f, 1.00f)),
        [FusionIdentity.Ice] = ("spark_01", ElementPalette.Frost),
        [FusionIdentity.Firestorm] = ("flame_02", ElementPalette.Fire),
        [FusionIdentity.Lava] = ("flare_01", new Color(0.95f, 0.40f, 0.10f)),
        [FusionIdentity.Plasma] = ("star_03", new Color(1.00f, 0.72f, 0.30f)),
        [FusionIdentity.Mud] = ("dirt_01", ElementPalette.Earth),
        [FusionIdentity.Sandstorm] = ("smoke_01", new Color(0.85f, 0.70f, 0.45f)),
        [FusionIdentity.Storm] = ("spark_01", ElementPalette.Lightning),
        [FusionIdentity.CrystalMagnet] = ("star_01", ElementPalette.Mark)
    };

    public override void _Ready()
    {
        _elements = GetNode<ElementSystem>("../../../SimulationRoot/ElementSystem");
        _elements.ChemistryReactionTriggered += OnChemistryReaction;
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_elements))
        {
            _elements.ChemistryReactionTriggered -= OnChemistryReaction;
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
            if (burst.Age >= Lifetime && !burst.Fading)
            {
                burst.Node.Emitting = false;
                burst.Fading = true;
            }
            if (burst.Age >= Lifetime + 0.4f)
            {
                burst.Node.QueueFree();
                _active.RemoveAt(index);
                continue;
            }
            _active[index] = burst;
            index++;
        }
    }

    private void OnChemistryReaction(FusionIdentity fusion, Vector2 position)
    {
        if (_active.Count >= MaxActive)
        {
            _active[0].Node.QueueFree();
            _active.RemoveAt(0);
        }
        GpuParticles3D burst = BuildBurst(fusion);
        burst.Position = new Vector3(position.X, 0.3f, position.Y);
        AddChild(burst);
        _active.Add(new ActiveBurst { Node = burst, Age = 0.0f, Fading = false });
    }

    private static GpuParticles3D BuildBurst(FusionIdentity fusion)
    {
        (string textureName, Color color) =
            FusionLook.GetValueOrDefault(fusion, ("magic_03", ElementPalette.Arcane));

        Texture2D? texture = VfxMaterials.LoadParticle(textureName);
        QuadMesh billboard = new() { Size = new Vector2(0.55f, 0.55f) };
        billboard.Material = VfxMaterials.BuildBillboardMaterial(
            texture ?? new PlaceholderTexture2D(), color, additive: true);

        ParticleProcessMaterial process = new()
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
            EmissionSphereRadius = 0.4f,
            Direction = new Vector3(0, 1, 0),
            Spread = 55.0f,
            LifetimeMin = 0.30f,
            LifetimeMax = 0.70f,
            InitialVelocityMin = 0.6f,
            InitialVelocityMax = 3.2f,
            Gravity = new Vector3(0, 5, 0),
            ScaleMin = 0.7f,
            ScaleMax = 1.4f,
            DampingMin = 0.5f,
            DampingMax = 1.2f
        };

        GpuParticles3D particles = new()
        {
            Amount = 24,
            OneShot = true,
            Explosiveness = 0.95f,
            Randomness = 0.2f,
            ProcessMaterial = process
        };
        particles.DrawPass1 = billboard;
        return particles;
    }
}