using Catalyst.Passives;

namespace Catalyst.App;

public sealed class ProfileData
{
    public int SchemaVersion { get; set; } = 4;
    // V3 field kept only as a migration source.
    public bool TutorialSeen { get; set; }
    public double BestSurvivalTime { get; set; }
    public int BestKillCount { get; set; }
    public bool BossDefeated { get; set; }
    public int CompletedRuns { get; set; }
    public int MemoryShards { get; set; }
    public string SelectedCharacterId { get; set; } = "character.elementalist";
    public List<string> UnlockedCharacterIds { get; set; } = new() { "character.elementalist" };
    public List<string> UnlockedWeaponIds { get; set; } = new() { "weapon.pulse_rifle" };
    public List<string> UnlockedPassiveIds { get; set; } = new();
    public List<string> SeenTutorialIds { get; set; } = new();
    public List<string> DiscoveredReactionIds { get; set; } = new();
    public List<string> DiscoveredFusionIds { get; set; } = new();
    public List<string> UnlockedRecordIds { get; set; } = new();
    public List<string> ViewedRecordIds { get; set; } = new();
    // V3 field kept only as a migration source.
    public Dictionary<string, int> CharacterMastery { get; set; } = new();
    public Dictionary<string, CharacterProgressData> CharacterProgress { get; set; } = new();
    public RunSummary? LastRun { get; set; }
}

public sealed class CharacterProgressData
{
    public int CompletedRuns { get; set; }
    public int Victories { get; set; }
    public int BossKills { get; set; }
    public int MasteryExperience { get; set; }
    public int HighestDifficulty { get; set; }
    public List<string> ClaimedRewardIds { get; set; } = new();
}

public sealed class RunSummary
{
    public int SchemaVersion { get; set; } = 4;
    public string CharacterId { get; set; } = "character.elementalist";
    public string StartingWeaponId { get; set; } = "weapon.pulse_rifle";
    public ulong Seed { get; set; }
    public int Difficulty { get; set; }
    public double SurvivalTime { get; set; }
    public int KillCount { get; set; }
    public bool Victory { get; set; }
    public bool BossDefeated { get; set; }
    public double DamageDealt { get; set; }
    public double DamageTaken { get; set; }
    public int ReactionCount { get; set; }
    public double ReactionDamage { get; set; }
    public int CatalysisExecutionCount { get; set; }
    public int CatalysisReactionCount { get; set; }
    public double CatalysisReactionDamage { get; set; }
    public int RerollCount { get; set; }
    public int MemoryShardsEarned { get; set; }
    public string Result { get; set; } = string.Empty;
    public string DeathCause { get; set; } = string.Empty;
    public Dictionary<string, double> DamageBySpell { get; set; } = new();
    public Dictionary<string, int> ReactionCounts { get; set; } = new();
    public Dictionary<string, int> PassiveTriggerCounts { get; set; } = new();
    public RunBuildSnapshot Build { get; set; } = new();
    public List<string> NewlyDiscoveredIds { get; set; } = new();
    public List<string> NewlyUnlockedIds { get; set; } = new();
    public DateTimeOffset CompletedAtUtc { get; set; }
}
