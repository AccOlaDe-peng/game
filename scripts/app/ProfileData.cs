namespace Catalyst.App;

public sealed class ProfileData
{
    public int SchemaVersion { get; set; } = 3;
    public bool TutorialSeen { get; set; }
    public double BestSurvivalTime { get; set; }
    public int BestKillCount { get; set; }
    public bool BossDefeated { get; set; }
    public int CompletedRuns { get; set; }
    public int MemoryShards { get; set; }
    public string SelectedCharacterId { get; set; } = "character.elementalist";
    public List<string> UnlockedCharacterIds { get; set; } = new() { "character.elementalist" };
    public List<string> UnlockedWeaponIds { get; set; } = new() { "weapon.pulse_rifle" };
    public Dictionary<string, int> CharacterMastery { get; set; } = new();
    public RunSummary? LastRun { get; set; }
}

public sealed class RunSummary
{
    public int SchemaVersion { get; set; } = 3;
    public string CharacterId { get; set; } = "character.elementalist";
    public string StartingWeaponId { get; set; } = "weapon.pulse_rifle";
    public ulong Seed { get; set; }
    public double SurvivalTime { get; set; }
    public int KillCount { get; set; }
    public bool Victory { get; set; }
    public bool BossDefeated { get; set; }
    public double DamageDealt { get; set; }
    public double DamageTaken { get; set; }
    public int ReactionCount { get; set; }
    public double ReactionDamage { get; set; }
    public string Result { get; set; } = string.Empty;
    public string DeathCause { get; set; } = string.Empty;
    public Dictionary<string, double> DamageBySpell { get; set; } = new();
    public Dictionary<string, int> ReactionCounts { get; set; } = new();
    public List<string> FinalBuild { get; set; } = new();
    public DateTimeOffset CompletedAtUtc { get; set; }
}
