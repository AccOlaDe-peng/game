using Catalyst.App;
using Catalyst.Cards;
using Catalyst.Meta;
using Catalyst.Passives;
using Catalyst.Spells;
using NUnit.Framework;

namespace Catalyst.Tests.Unit;

public sealed class BuildSnapshotTests
{
    private sealed class StubWeapon : IWeaponBuildView
    {
        public string WeaponDefinitionId { get; init; } = "spell.arcane_missile";
        public int WeaponLevel { get; init; } = 3;
        public SpellBranch SelectedBranch { get; init; } = SpellBranch.A;
        public int CurrentPower { get; init; } = 4;
        public int PowerCapacity { get; init; } = 12;
        public IReadOnlyDictionary<string, int> InstalledCards { get; init; } =
            new Dictionary<string, int>();
        public IReadOnlyDictionary<string, int> InstalledStandardCards { get; init; } =
            new Dictionary<string, int>();
    }

    [Test]
    public void SnapshotCapturesEveryWeaponAndCard()
    {
        RunBuildSnapshot snapshot = RunBuildSnapshotBuilder.Build(
            CharacterCatalog.ElementalistId,
            CharacterCatalog.PulseRifleId,
            CharacterCatalog.Get(CharacterCatalog.ElementalistId).CorePassiveIds,
            new IWeaponBuildView[] { new StubWeapon() });

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.CharacterId, Is.EqualTo(CharacterCatalog.ElementalistId));
            Assert.That(snapshot.StartingWeaponId, Is.EqualTo(CharacterCatalog.PulseRifleId));
            Assert.That(snapshot.CharacterPassiveIds, Is.Not.Empty);
            Assert.That(snapshot.Weapons, Has.Count.EqualTo(1));
            Assert.That(snapshot.Weapons[0].WeaponId, Is.EqualTo("spell.arcane_missile"));
            Assert.That(snapshot.Weapons[0].Level, Is.EqualTo(3));
            Assert.That(snapshot.Weapons[0].BranchId, Is.EqualTo(SpellBranch.A.ToString()));
            Assert.That(snapshot.Weapons[0].PowerUsed, Is.EqualTo(4));
            Assert.That(snapshot.Weapons[0].PowerCapacity, Is.EqualTo(12));
        });
    }

    [Test]
    public void ConnectionsPairTriggersWithActions()
    {
        Dictionary<string, int> cards = new()
        {
            ["card.trigger.on_pierce"] = 1,
            ["card.action.explosion"] = 1,
            ["card.action.nova"] = 1
        };
        List<CardConnectionSnapshot> connections =
            RunBuildSnapshotBuilder.BuildConnections(cards);

        Assert.Multiple(() =>
        {
            Assert.That(connections, Has.Count.EqualTo(1));
            Assert.That(connections[0].TriggerCardId, Is.EqualTo("card.trigger.on_pierce"));
            Assert.That(connections[0].ActionCardId, Is.EqualTo("card.action.explosion"));
        });
    }

    [Test]
    public void DescribeRendersReadableReportLines()
    {
        RunBuildSnapshot snapshot = new()
        {
            CharacterId = CharacterCatalog.ElementalistId,
            Weapons = new List<WeaponBuildSnapshot>
            {
                new()
                {
                    WeaponId = "spell.arcane_missile",
                    Level = 2,
                    BranchId = "None",
                    PowerUsed = 4,
                    PowerCapacity = 12
                }
            }
        };
        string text = RunBuildSnapshotBuilder.Describe(snapshot);
        Assert.That(text, Does.Contain("奥术飞弹"));
        Assert.That(text, Does.Contain("4/12"));
    }

    [Test]
    public void DescribeHandlesEmptyBuild()
    {
        Assert.That(RunBuildSnapshotBuilder.Describe(new RunBuildSnapshot()),
            Does.Contain("暂无武器"));
    }
}
