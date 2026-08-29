using Catalyst.Meta;
using Catalyst.Passives;
using NUnit.Framework;

namespace Catalyst.Tests.Unit;

public sealed class CharacterPassiveTests
{
    [Test]
    public void EveryCharacterHasAtLeastTwoCorePassives()
    {
        foreach (CharacterDefinition character in CharacterCatalog.All)
        {
            Assert.That(character.CorePassiveIds, Has.Count.GreaterThanOrEqualTo(2),
                $"{character.Id} needs at least two core passives.");
            Assert.That(character.CorePassiveIds, Is.Unique);
        }
    }

    [Test]
    public void CorePassiveIdsResolveToRealDefinitions()
    {
        foreach (CharacterDefinition character in CharacterCatalog.All)
        {
            foreach (string passiveId in character.CorePassiveIds)
            {
                Assert.DoesNotThrow(() => PassiveCatalog.Get(passiveId),
                    $"{character.Id} references unknown passive {passiveId}.");
            }
        }
    }

    [Test]
    public void ElementalistPayloadAdapterIsSingleUseByDesign()
    {
        PassiveBlueprint adapter = PassiveCatalog.Get(PassiveCatalog.ElementalistPayloadAdapter);
        Assert.Multiple(() =>
        {
            Assert.That(adapter.Trigger, Is.EqualTo(PassiveTriggerKind.LoadoutChanged));
            Assert.That(adapter.Effect, Is.EqualTo(PassiveEffectKind.ApplyPayloadDiscount));
        });
    }

    [Test]
    public void EchoHunterDiscPassivesExist()
    {
        PassiveBlueprint recall = PassiveCatalog.Get(PassiveCatalog.EchoHunterAutoRecall);
        PassiveBlueprint spread = PassiveCatalog.Get(PassiveCatalog.EchoHunterMarkSpread);
        Assert.Multiple(() =>
        {
            Assert.That(spread.CooldownSeconds, Is.EqualTo(0.1f));
            Assert.That(spread.Count, Is.EqualTo(2));
            Assert.That(recall.Effect, Is.EqualTo(PassiveEffectKind.RecallProjectile));
        });
    }

    [Test]
    public void RiftEngineerSteadyMineUsesWeakenedBaseline()
    {
        PassiveBlueprint mine = PassiveCatalog.Get(PassiveCatalog.RiftEngineerSteadyMine);
        Assert.Multiple(() =>
        {
            Assert.That(mine.Value, Is.EqualTo(0.45f));
            Assert.That(mine.SecondaryValue, Is.EqualTo(0.8f));
            Assert.That(mine.Trigger, Is.EqualTo(PassiveTriggerKind.Interval));
            Assert.That(mine.IntervalSeconds, Is.EqualTo(6.0f));
        });
    }
}
