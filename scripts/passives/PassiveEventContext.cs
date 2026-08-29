using Catalyst.Combat;
using Catalyst.Elements;
using Catalyst.Enemies;
using Godot;

namespace Catalyst.Passives;

public readonly record struct PassiveEventContext(
    ulong Sequence,
    PassiveTriggerKind Trigger,
    double SimulationTime,
    EntityHandle Source,
    EntityHandle Target,
    StringName SourceDefinitionId,
    StringName WeaponId,
    StringName EffectId,
    Vector2 Position,
    Vector2 Direction,
    float Value,
    int Count,
    ElementType Element,
    ReactionKind Reaction,
    DamageFlags DamageFlags,
    int ChainDepth,
    ulong RootSequence)
{
    public static PassiveEventContext Create(
        PassiveTriggerKind trigger,
        double simulationTime,
        ulong sequence,
        Vector2 position = default,
        EntityHandle target = default,
        StringName weaponId = null!,
        float value = 0f,
        int count = 0,
        ElementType element = ElementType.None,
        ReactionKind reaction = ReactionKind.None,
        DamageFlags flags = DamageFlags.None,
        int chainDepth = 0,
        ulong rootSequence = 0)
    {
        return new PassiveEventContext(
            sequence, trigger, simulationTime, EntityHandle.Invalid, target,
            new StringName(), weaponId, new StringName(), position, Vector2.Zero,
            value, count, element, reaction, flags, chainDepth,
            rootSequence == 0 ? sequence : rootSequence);
    }
}
