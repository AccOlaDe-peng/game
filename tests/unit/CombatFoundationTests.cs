using Catalyst.Enemies;
using Catalyst.Player;
using Catalyst.Projectiles;
using Catalyst.Spells;
using NUnit.Framework;

namespace Catalyst.Tests.Unit;

public sealed class CombatFoundationTests
{
    [Test]
    public void EntityHandleIdentityIncludesGeneration()
    {
        EntityHandle original = new(12, 3);
        EntityHandle same = new(12, 3);
        EntityHandle recycled = new(12, 4);

        Assert.Multiple(() =>
        {
            Assert.That(original, Is.EqualTo(same));
            Assert.That(original, Is.Not.EqualTo(recycled));
            Assert.That(EntityHandle.Invalid.IsValid, Is.False);
        });
    }

    [TestCase(1, 6)]
    [TestCase(2, 10)]
    [TestCase(5, 22)]
    public void ExperienceCurveGrowsPredictably(int level, int expected)
    {
        Assert.That(PlayerProgression.GetExperienceRequired(level), Is.EqualTo(expected));
    }

    [Test]
    public void ProjectileEffectBudgetStopsRecursiveChains()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ProjectileEffectBudget.CanSpawnChild(0, 8), Is.True);
            Assert.That(ProjectileEffectBudget.CanSpawnChild(4, 8), Is.False);
            Assert.That(ProjectileEffectBudget.CanSpawnChild(2, 0), Is.False);
            Assert.That(ProjectileEffectBudget.MaximumChildEvents, Is.EqualTo(8));
        });
    }

    [Test]
    public void PersistentFieldUsesAtlasTimingBudget()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WeaponFieldRules.Duration, Is.EqualTo(2.4f));
            Assert.That(WeaponFieldRules.PulseInterval, Is.EqualTo(0.45f));
            Assert.That(WeaponFieldRules.MaximumPulses, Is.EqualTo(6));
        });
    }
}
