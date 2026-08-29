using Catalyst.Core;
using Catalyst.Meta;
using Catalyst.Passives;
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
        if (Profile.SchemaVersion < 4)
        {
            MigrateToV4(Profile);
            NormalizeProfile();
            AtomicJsonStore.Save(ProfilePath, Profile);
            CatalystLog.Info("Save", $"Profile migrated to schema version {Profile.SchemaVersion}.");
        }
        NormalizeProfile();
        ActiveCharacter = CharacterCatalog.Get(Profile.SelectedCharacterId);
        CatalystLog.Info("Save", "Profile service ready.");
    }

    /// <summary>Pure V3 → V4 migration: never invents card ids, never drops progress.</summary>
    public static void MigrateToV4(ProfileData profile)
    {
        if (profile.SchemaVersion >= 4)
        {
            return;
        }

        foreach ((string characterId, int runs) in profile.CharacterMastery)
        {
            CharacterProgressData progress = GetOrCreateProgress(profile, characterId);
            progress.CompletedRuns = Math.Max(progress.CompletedRuns, runs);
        }

        if (profile.TutorialSeen)
        {
            profile.SeenTutorialIds.Add("tutorial.movement");
            profile.SeenTutorialIds.Add("tutorial.upgrade");
        }

        profile.UnlockedPassiveIds.AddRange(
            PassiveCatalog.CorePassives.Select(passive => passive.Id.ToString())
                .Where(id => !profile.UnlockedPassiveIds.Contains(id)));

        // Old FinalBuild strings cannot be parsed reliably; they are dropped
        // without guessing card ids. LastRun itself is preserved.
        profile.SchemaVersion = 4;
    }

    public static CharacterProgressData GetOrCreateProgress(ProfileData profile, string characterId)
    {
        profile.CharacterProgress ??= new Dictionary<string, CharacterProgressData>();
        if (!profile.CharacterProgress.TryGetValue(characterId, out CharacterProgressData? progress))
        {
            progress = new CharacterProgressData();
            profile.CharacterProgress[characterId] = progress;
        }
        return progress;
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

    /// <summary>
    /// Test/tool hook: switch the active character without persisting the
    /// selection, so scene runners stay independent of the player's profile.
    /// </summary>
    public void PreviewCharacter(string characterId) =>
        ActiveCharacter = CharacterCatalog.Get(characterId);

    public void RecordRun(RunSummary summary)
    {
        Profile.LastRun = summary;
        Profile.BestSurvivalTime = Math.Max(Profile.BestSurvivalTime, summary.SurvivalTime);
        Profile.BestKillCount = Math.Max(Profile.BestKillCount, summary.KillCount);
        Profile.BossDefeated |= summary.BossDefeated;
        Profile.CompletedRuns++;
        Profile.MemoryShards += CalculateMemoryShards(summary);

        CharacterProgressData progress = GetOrCreateProgress(Profile, summary.CharacterId);
        progress.CompletedRuns++;
        if (summary.Victory) progress.Victories++;
        if (summary.BossDefeated) progress.BossKills++;
        progress.MasteryExperience += 10 + summary.KillCount / 10;
        progress.HighestDifficulty = Math.Max(progress.HighestDifficulty, summary.Difficulty);

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
        Profile.UnlockedPassiveIds ??= new List<string>();
        Profile.SeenTutorialIds ??= new List<string>();
        Profile.DiscoveredReactionIds ??= new List<string>();
        Profile.DiscoveredFusionIds ??= new List<string>();
        Profile.UnlockedRecordIds ??= new List<string>();
        Profile.ViewedRecordIds ??= new List<string>();
        Profile.CharacterProgress ??= new Dictionary<string, CharacterProgressData>();
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
