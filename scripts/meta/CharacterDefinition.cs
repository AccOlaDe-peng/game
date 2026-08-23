using Catalyst.Spells;

namespace Catalyst.Meta;

public sealed record CharacterDefinition(
    string Id,
    string DisplayName,
    string Role,
    string Description,
    string ActiveAbility,
    string PassiveAbility,
    string StartingWeaponId,
    string StartingWeaponName,
    SpellCastKind PrototypeStartingSpell,
    string UnlockHint);
