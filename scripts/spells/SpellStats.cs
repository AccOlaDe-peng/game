using Catalyst.Elements;

namespace Catalyst.Spells;

public readonly record struct SpellStats(
    float Damage,
    float Cooldown,
    float Range,
    float ProjectileSpeed,
    float Lifetime,
    ElementType Element,
    int ElementStacks,
    int Count,
    float SpreadDegrees,
    int MaximumHits,
    float ExplosionRadius,
    int ChainCount,
    float ChainRange,
    float OrbitRadius,
    ElementType SecondaryElement,
    FusionIdentity Fusion);
