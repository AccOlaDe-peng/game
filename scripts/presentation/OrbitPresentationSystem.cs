using Catalyst.Elements;
using Catalyst.Spells;
using Godot;

namespace Catalyst.Presentation;

public partial class OrbitPresentationSystem : Node3D
{
    private SpellSystem _spells = null!;
    private CharacterBody3D _player = null!;
    private MultiMesh _multiMesh = null!;

    public override void _Ready()
    {
        _spells = GetNode<SpellSystem>("../../../SimulationRoot/SpellSystem");
        _player = GetNode<CharacterBody3D>("../../Player");
        Mesh mesh = VfxMaterials.BuildBillboardMesh(0.56f, "star_03", new Color(0.3f, 0.7f, 1.0f));
        _multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = mesh,
            InstanceCount = 3,
            VisibleInstanceCount = 0
        };
        GetNode<MultiMeshInstance3D>("Orbs").Multimesh = _multiMesh;
    }

    public override void _Process(double delta)
    {
        if (!_spells.TryGetRuntime(SpellCastKind.OrbitOrb, out SpellRuntime? runtime) || runtime is null)
        {
            _multiMesh.VisibleInstanceCount = 0;
            return;
        }

        int count = Math.Min(3, runtime.Stats.Count);
        float time = Time.GetTicksMsec() * 0.0014f;
        for (int index = 0; index < count; index++)
        {
            float angle = time + Mathf.Tau * index / count;
            Vector3 origin = _player.GlobalPosition + new Vector3(
                Mathf.Cos(angle) * runtime.Stats.OrbitRadius,
                0.75f,
                Mathf.Sin(angle) * runtime.Stats.OrbitRadius);
            _multiMesh.SetInstanceTransform(index, new Transform3D(Basis.Identity, origin));
            Color color = runtime.Stats.Element == ElementType.Frost
                ? new Color(0.2f, 0.86f, 1.0f)
                : new Color(0.5f, 0.74f, 1.0f);
            _multiMesh.SetInstanceColor(index, color);
        }
        _multiMesh.VisibleInstanceCount = count;
    }
}
