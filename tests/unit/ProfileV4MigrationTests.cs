using Catalyst.App;
using NUnit.Framework;

namespace Catalyst.Tests.Unit;

public sealed class ProfileV4MigrationTests
{
    [Test]
    public void V3ProfileMigratesWithoutLosingProgress()
    {
        ProfileData profile = new()
        {
            SchemaVersion = 3,
            TutorialSeen = true,
            CompletedRuns = 7,
            MemoryShards = 120,
            BossDefeated = true,
            UnlockedCharacterIds = new List<string> { "character.elementalist", "character.echo_hunter" },
            UnlockedWeaponIds = new List<string> { "weapon.pulse_rifle", "weapon.ricochet_disc" },
            CharacterMastery = new Dictionary<string, int>
            {
                ["character.elementalist"] = 5,
                ["character.echo_hunter"] = 2
            }
        };

        SaveService.MigrateToV4(profile);

        Assert.Multiple(() =>
        {
            Assert.That(profile.SchemaVersion, Is.EqualTo(4));
            Assert.That(profile.CompletedRuns, Is.EqualTo(7));
            Assert.That(profile.MemoryShards, Is.EqualTo(120));
            Assert.That(profile.BossDefeated, Is.True);
            Assert.That(profile.UnlockedCharacterIds, Does.Contain("character.echo_hunter"));
            Assert.That(profile.UnlockedWeaponIds, Does.Contain("weapon.ricochet_disc"));
            Assert.That(profile.CharacterProgress["character.elementalist"].CompletedRuns, Is.EqualTo(5));
            Assert.That(profile.CharacterProgress["character.echo_hunter"].CompletedRuns, Is.EqualTo(2));
            Assert.That(profile.SeenTutorialIds, Does.Contain("tutorial.movement"));
            Assert.That(profile.SeenTutorialIds, Does.Contain("tutorial.upgrade"));
        });
    }

    [Test]
    public void MigrationFillsDefaultUnlockedPassives()
    {
        ProfileData profile = new() { SchemaVersion = 3 };
        SaveService.MigrateToV4(profile);
        Assert.That(profile.UnlockedPassiveIds, Is.Not.Empty);
    }

    [Test]
    public void MigrationIsIdempotent()
    {
        ProfileData profile = new() { SchemaVersion = 3, CharacterMastery = new Dictionary<string, int> { ["c"] = 3 } };
        SaveService.MigrateToV4(profile);
        SaveService.MigrateToV4(profile);
        Assert.Multiple(() =>
        {
            Assert.That(profile.CharacterProgress["c"].CompletedRuns, Is.EqualTo(3));
            Assert.That(profile.SchemaVersion, Is.EqualTo(4));
        });
    }

    [Test]
    public void RunSummaryDefaultsToSchemaVersion4()
    {
        RunSummary summary = new();
        Assert.Multiple(() =>
        {
            Assert.That(summary.SchemaVersion, Is.EqualTo(4));
            Assert.That(summary.Build, Is.Not.Null);
            Assert.That(summary.MemoryShardsEarned, Is.EqualTo(0));
        });
    }
}
