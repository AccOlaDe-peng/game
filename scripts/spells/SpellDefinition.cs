using Catalyst.Core;
using Catalyst.Elements;
using Godot;

namespace Catalyst.Spells;

[GlobalClass]
public partial class SpellDefinition : ContentDefinition
{
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
    [Export] public SpellCastKind CastKind { get; set; }
    [Export] public ElementType Element { get; set; }
    [Export] public float BaseDamage { get; set; } = 5.0f;
    [Export] public float Cooldown { get; set; } = 1.0f;
    [Export] public float Range { get; set; } = 32.0f;
    [Export] public float ProjectileSpeed { get; set; } = 20.0f;
    [Export] public float Lifetime { get; set; } = 2.0f;
    [Export] public int ElementStacks { get; set; } = 1;
    [Export] public int MaximumHits { get; set; } = 1;
    [Export] public float ExplosionRadius { get; set; }
    [Export] public int ChainCount { get; set; }
    [Export] public float ChainRange { get; set; }
}
