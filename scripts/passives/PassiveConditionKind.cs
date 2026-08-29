namespace Catalyst.Passives;

public enum PassiveConditionKind : byte
{
    Always,
    CooldownReady,
    TargetAlive,
    TargetIsMarked,
    TargetIsElite,
    TargetHasElement,
    TargetHasReactionPotential,
    MinimumNearbyEnemies,
    MinimumNearbyReactionTargets,
    DeploymentBelowLimit,
    DeploymentAtLimit,
    ChargeFull,
    PlayerHealthBelowFraction,
    WeaponMatches,
    RandomProc,
    PayloadDiscountAvailable
}
