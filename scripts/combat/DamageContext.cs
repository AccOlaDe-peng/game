using Catalyst.Elements;
using Catalyst.Enemies;
using Godot;

namespace Catalyst.Combat;

public readonly record struct DamageContext(
    EntityHandle Target,
    StringName SourceSpellId,
    float BaseDamage,
    ElementType Element,
    int ElementStacks,
    DamageFlags Flags,
    int ChainDepth = 0,
    ElementType SecondaryElement = ElementType.None,
    FusionIdentity Fusion = FusionIdentity.None);
