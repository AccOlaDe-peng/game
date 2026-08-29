namespace Catalyst.Passives;

public enum PassiveTriggerKind : byte
{
    None = 0,
    Interval = 1,
    DodgeStarted = 2,
    DodgeEnded = 3,
    WeaponCast = 4,
    ProjectileHit = 5,
    ProjectilePierced = 6,
    ProjectileBounced = 7,
    ProjectileReturned = 8,
    ElementApplied = 9,
    StatusApplied = 10,
    EnemyFrozen = 11,
    ReactionTriggered = 12,
    EnemyKilled = 13,
    EliteEnteredRange = 14,
    DeploymentCreated = 15,
    DeploymentLimitReached = 16,
    DeploymentTriggered = 17,
    CatalysisCharged = 18,
    CatalysisExecuted = 19,
    ObjectiveStarted = 20,
    ObjectiveCompleted = 21,
    LoadoutChanged = 22
}
