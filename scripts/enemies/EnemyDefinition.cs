using Catalyst.Core;
using Godot;

namespace Catalyst.Enemies;

[GlobalClass]
public partial class EnemyDefinition : ContentDefinition
{
    [Export] public EnemyArchetype Archetype { get; set; }
    [Export] public bool IsElite { get; set; }
    [Export(PropertyHint.Range, "1,10000,1")]
    public float MaxHealth { get; set; } = 12.0f;

    [Export(PropertyHint.Range, "0.1,30,0.1")]
    public float MoveSpeed { get; set; } = 2.6f;

    [Export(PropertyHint.Range, "0.1,5,0.1")]
    public float Radius { get; set; } = 0.55f;

    [Export(PropertyHint.Range, "1,100,1")]
    public int ExperienceValue { get; set; } = 1;

    [Export] public float ContactDamage { get; set; } = 5.0f;
    [Export] public float PreferredDistance { get; set; }
    [Export] public float AttackCooldown { get; set; } = 2.0f;
    [Export] public float SpecialDamage { get; set; } = 8.0f;
    [Export] public float VisualScale { get; set; } = 1.0f;
    [Export] public float SpawnCost { get; set; } = 1.0f;
    [Export] public float AppearsAfterSeconds { get; set; }
    [Export] public float SpawnWeight { get; set; } = 1.0f;
    [Export] public int MaximumAlive { get; set; } = 500;

    [Export]
    public Color DisplayColor { get; set; } = new(0.75f, 0.24f, 0.34f, 1.0f);
}
