using Catalyst.Core;
using Catalyst.Meta;
using Godot;

namespace Catalyst.App;

public partial class SaveService : Node
{
    public ProfileData Profile { get; private set; } = new();
    public CharacterDefinition ActiveCharacter { get; private set; } = CharacterCatalog.Default;

    private string ProfilePath =>
        ProjectSettings.GlobalizePath("user://profile.json");

    private string LastRunPath =>
        ProjectSettings.GlobalizePath("user://last_run.json");

    public override void _Ready()
    {
        Profile = AtomicJsonStore.LoadOrDefault(ProfilePath, static () => new ProfileData());
        if (Profile.SchemaVersion < 3)
        {
            Profile.SchemaVersion = 3;
            NormalizeProfile();
            AtomicJsonStore.Save(ProfilePath, Profile);
            CatalystLog.Info("Save", "Profile migrated to schema version 3.");
        }
        NormalizeProfile();
        ActiveCharacter = CharacterCatalog.Get(Profile.SelectedCharacterId);
        CatalystLog.Info("Save", "Profile service ready.");
    }

    public bool IsCharacterUnlocked(string characterId) =>
        Profile.UnlockedCharacterIds.Contains(characterId, StringComparer.Ordinal);

    public bool SelectCharacter(string characterId)
    {
        if (!IsCharacterUnlocked(characterId))
        {
            return false;
        }
        ActiveCharacter = CharacterCatalog.Get(characterId);
        Profile.SelectedCharacterId = ActiveCharacter.Id;
        AtomicJsonStore.Save(ProfilePath, Profile);
        return true;
    }

    public void RecordRun(RunSummary summary)
    {
        Profile.LastRun = summary;
        Profile.BestSurvivalTime = Math.Max(Profile.BestSurvivalTime, summary.SurvivalTime);
        Profile.BestKillCount = Math.Max(Profile.BestKillCount, summary.KillCount);
        Profile.BossDefeated |= summary.BossDefeated;
        Profile.CompletedRuns++;
        Profile.MemoryShards += CalculateMemoryShards(summary);
        Profile.CharacterMastery[summary.CharacterId] =
            Profile.CharacterMastery.GetValueOrDefault(summary.CharacterId) + 1;
        if (Profile.CompletedRuns >= 1)
        {
            Unlock(CharacterCatalog.EchoHunterId, CharacterCatalog.RicochetDiscId);
        }
        if (Profile.BossDefeated)
        {
            Unlock(CharacterCatalog.RiftEngineerId, CharacterCatalog.ElementMineId);
        }
        AtomicJsonStore.Save(ProfilePath, Profile);
        AtomicJsonStore.Save(LastRunPath, summary);
    }

    private void NormalizeProfile()
    {
        Profile.UnlockedCharacterIds ??= new List<string>();
        Profile.UnlockedWeaponIds ??= new List<string>();
        Profile.CharacterMastery ??= new Dictionary<string, int>();
        if (!Profile.UnlockedCharacterIds.Contains(CharacterCatalog.ElementalistId))
            Profile.UnlockedCharacterIds.Add(CharacterCatalog.ElementalistId);
        if (!Profile.UnlockedWeaponIds.Contains(CharacterCatalog.PulseRifleId))
            Profile.UnlockedWeaponIds.Add(CharacterCatalog.PulseRifleId);
        if (!Profile.UnlockedCharacterIds.Contains(Profile.SelectedCharacterId))
            Profile.SelectedCharacterId = CharacterCatalog.ElementalistId;
    }

    private void Unlock(string characterId, string weaponId)
    {
        if (!Profile.UnlockedCharacterIds.Contains(characterId))
            Profile.UnlockedCharacterIds.Add(characterId);
        if (!Profile.UnlockedWeaponIds.Contains(weaponId))
            Profile.UnlockedWeaponIds.Add(weaponId);
    }

    public static int CalculateMemoryShards(RunSummary summary) =>
        5 + (int)(summary.SurvivalTime / 60.0) * 2 + summary.KillCount / 20 +
        (summary.BossDefeated ? 20 : 0);
}
