namespace Catalyst.Passives;

public sealed class PassiveEventBudget
{
    public int MaximumEventsPerPhysicsFrame { get; init; } = 2048;
    public int MaximumChainDepth { get; init; } = 4;
    public int MaximumEffectsPerRootEvent { get; init; } = 32;
    public int MaximumTargetsPerEffect { get; set; } = 64;
}
