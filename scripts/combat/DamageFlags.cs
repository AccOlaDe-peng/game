namespace Catalyst.Combat;

[Flags]
public enum DamageFlags : byte
{
    None = 0,
    CanTriggerReaction = 1 << 0,
    IsReactionDamage = 1 << 1,
    ForceReactionCheck = 1 << 2
}
