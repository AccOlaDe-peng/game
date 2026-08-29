namespace Catalyst.Passives;

public enum PassiveEffectKind : byte
{
    None,
    ModifyCatalysisCharge,
    ExecuteCatalysis,
    ApplyElement,
    ApplyMark,
    SpreadMark,
    SpawnProjectile,
    RecallProjectile,
    DuplicateProjectile,
    CreateDeployment,
    TriggerOldestDeployment,
    TriggerNearestDeployment,
    CreateGroundField,
    ModifyNextWeaponCast,
    ReduceCooldown,
    GrantTemporaryStat,
    EmitReactionAction,
    ApplyPayloadDiscount,
    OverloadNearestDeployment
}
