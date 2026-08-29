using Catalyst.Spells;

namespace Catalyst.Meta;

public sealed record CharacterDefinition(
    string Id,
    string DisplayName,
    string Role,
    string Description,
    string StartingWeaponId,
    string StartingWeaponName,
    SpellCastKind PrototypeStartingSpell,
    IReadOnlyList<string> CorePassiveIds,
    IReadOnlyList<SpellCastKind> CompatibleWeaponIds,
    string UnlockHint,
    string ArchiveRecordId);
