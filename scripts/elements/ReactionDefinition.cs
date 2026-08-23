using Catalyst.Core;
using Godot;

namespace Catalyst.Elements;

[GlobalClass]
public partial class ReactionDefinition : ContentDefinition
{
    [Export] public ReactionKind Kind { get; set; }
    [Export] public ElementType First { get; set; }
    [Export] public ElementType Second { get; set; }
    [Export] public int AutomaticThreshold { get; set; } = 3;
    [Export] public int CatalyzeThreshold { get; set; } = 1;
    [Export] public int StacksConsumed { get; set; } = 2;
    [Export] public float BaseDamage { get; set; } = 8.0f;
    [Export] public float DamagePerConsumedStack { get; set; } = 3.0f;
    [Export] public float EffectRadius { get; set; } = 4.0f;
    [Export] public int PropagationCount { get; set; } = 4;
    [Export] public float InternalCooldown { get; set; } = 0.35f;
    [Export] public int Priority { get; set; } = 100;

    public ReactionRule ToRule() => new(
        Id.ToString(), DisplayName, Kind, First, Second,
        AutomaticThreshold, CatalyzeThreshold, StacksConsumed,
        BaseDamage, DamagePerConsumedStack, EffectRadius,
        PropagationCount, InternalCooldown, Priority);
}
