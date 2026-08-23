namespace Catalyst.Elements;

public sealed record ReactionRule(
    string Id,
    string DisplayName,
    ReactionKind Kind,
    ElementType First,
    ElementType Second,
    int AutomaticThreshold,
    int CatalyzeThreshold,
    int StacksConsumed,
    float BaseDamage,
    float DamagePerConsumedStack,
    float EffectRadius,
    int PropagationCount,
    float InternalCooldown,
    int Priority)
{
    public ReactionKey Key => new(First, Second);
}
