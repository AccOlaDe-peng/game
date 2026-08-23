using Catalyst.App;
using Catalyst.Meta;
using Catalyst.Spells;
using Catalyst.Weapons;
using Catalyst.Cards;
using NUnit.Framework;

namespace Catalyst.Tests.Unit;

public sealed class MetaProgressionTests
{
    [Test]
    public void CharacterIdsAndStartingWeaponsAreStableAndUnique()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CharacterCatalog.All.Select(item => item.Id), Is.Unique);
            Assert.That(CharacterCatalog.All.Select(item => item.StartingWeaponId), Is.Unique);
            Assert.That(CharacterCatalog.All.Select(item => item.PrototypeStartingSpell), Is.Unique);
            Assert.That(CharacterCatalog.Default.PrototypeStartingSpell, Is.EqualTo(SpellCastKind.ArcaneMissile));
        });
    }

    [Test]
    public void MemoryShardRewardIncludesRunAndBossProgress()
    {
        RunSummary ordinary = new() { SurvivalTime = 125, KillCount = 45 };
        RunSummary boss = new() { SurvivalTime = 125, KillCount = 45, BossDefeated = true };

        Assert.Multiple(() =>
        {
            Assert.That(SaveService.CalculateMemoryShards(ordinary), Is.EqualTo(11));
            Assert.That(SaveService.CalculateMemoryShards(boss), Is.EqualTo(31));
        });
    }

    [Test]
    public void EveryCoreWeaponHasAnIndependentSixCardPool()
    {
        SpellCastKind[] cores =
        {
            SpellCastKind.ArcaneMissile,
            SpellCastKind.RicochetDisc,
            SpellCastKind.ElementMine
        };

        Assert.Multiple(() =>
        {
            Assert.That(WeaponCardCatalog.All, Has.Count.EqualTo(18));
            Assert.That(WeaponCardCatalog.All.Select(card => card.Id), Is.Unique);
            foreach (SpellCastKind core in cores)
            {
                Assert.That(WeaponCardCatalog.ForWeapon(core).Count(), Is.EqualTo(6));
            }
        });
    }

    [Test]
    public void CharacterWeaponPoolsDoNotLeakOtherCoreWeapons()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CharacterCatalog.IsCompatibleWeapon(CharacterCatalog.ElementalistId, SpellCastKind.ArcaneMissile), Is.True);
            Assert.That(CharacterCatalog.IsCompatibleWeapon(CharacterCatalog.ElementalistId, SpellCastKind.ElementMine), Is.False);
            Assert.That(CharacterCatalog.IsCompatibleWeapon(CharacterCatalog.EchoHunterId, SpellCastKind.RicochetDisc), Is.True);
            Assert.That(CharacterCatalog.IsCompatibleWeapon(CharacterCatalog.EchoHunterId, SpellCastKind.ArcaneMissile), Is.False);
            Assert.That(CharacterCatalog.IsCompatibleWeapon(CharacterCatalog.RiftEngineerId, SpellCastKind.ElementMine), Is.True);
            Assert.That(CharacterCatalog.IsCompatibleWeapon(CharacterCatalog.RiftEngineerId, SpellCastKind.RicochetDisc), Is.False);
        });
    }

    [Test]
    public void StandardModuleAtlasIsCompleteAndUsesStableIds()
    {
        Dictionary<StandardCardCategory, int> expected = new()
        {
            [StandardCardCategory.Behavior] = 6,
            [StandardCardCategory.Payload] = 6,
            [StandardCardCategory.Action] = 6,
            [StandardCardCategory.Trigger] = 4,
            [StandardCardCategory.Reaction] = 4
        };

        Assert.Multiple(() =>
        {
            Assert.That(StandardCardCatalog.All, Has.Count.EqualTo(26));
            Assert.That(StandardCardCatalog.All.Select(card => card.Id), Is.Unique);
            Assert.That(StandardCardCatalog.All.All(card => card.PowerCost > 0), Is.True);
            foreach ((StandardCardCategory category, int count) in expected)
            {
                Assert.That(StandardCardCatalog.All.Count(card => card.Category == category), Is.EqualTo(count));
            }
            Assert.That(StandardCardCatalog.All.Any(card => card.Effect == StandardCardEffect.OnDash &&
                card.IsCompatible(SpellCastKind.ElementMine)), Is.True);
            Assert.That(StandardCardCatalog.All.Any(card => card.Effect == StandardCardEffect.OnDash &&
                card.IsCompatible(SpellCastKind.ArcaneMissile)), Is.False);
        });
    }
}
