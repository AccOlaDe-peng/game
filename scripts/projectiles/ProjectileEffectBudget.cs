namespace Catalyst.Projectiles;

public static class ProjectileEffectBudget
{
    public const int MaximumChainDepth = 4;
    public const int MaximumChildEvents = 8;

    public static bool CanSpawnChild(int chainDepth, int remainingEvents) =>
        chainDepth < MaximumChainDepth && remainingEvents > 0;
}
