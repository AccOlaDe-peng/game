using Catalyst.Elements;
using NUnit.Framework;

namespace Catalyst.Tests.Unit;

public sealed class ElementResolverTests
{
    [Test]
    public void FormalResolverStoresFiveBaseStatesWithoutLegacyAutoReactions()
    {
        ElementResolver resolver = new(Array.Empty<ReactionRule>());
        ElementRuntimeState runtime = default;
        bool waterTriggered = resolver.ApplyElement(ref runtime, ElementType.Water, 1, true, out _);
        bool lightningTriggered = resolver.ApplyElement(ref runtime, ElementType.Lightning, 1, true, out _);
        Assert.Multiple(() =>
        {
            Assert.That(waterTriggered || lightningTriggered, Is.False);
            Assert.That(runtime.Water.Stacks, Is.EqualTo(1));
            Assert.That(runtime.Lightning.Stacks, Is.EqualTo(1));
        });
    }

    [TestCase(ElementType.Fire, 2.2f)]
    [TestCase(ElementType.Water, 2.0f)]
    [TestCase(ElementType.Wind, 1.2f)]
    [TestCase(ElementType.Earth, 1.6f)]
    [TestCase(ElementType.Lightning, 1.0f)]
    [TestCase(ElementType.Mark, 3.0f)]
    public void BaseStatusDurationsMatchChemistryAtlas(ElementType element, float duration) =>
        Assert.That(ElementResolver.GetStatusDuration(element), Is.EqualTo(duration));

    [Test]
    public void FireStatusTicksDamageAndExpires()
    {
        ElementResolver resolver = new(Array.Empty<ReactionRule>());
        ElementRuntimeState runtime = default;
        resolver.ApplyElement(ref runtime, ElementType.Fire, 2, false, out _);
        float damage = resolver.Tick(ref runtime, 1.0f);
        resolver.Tick(ref runtime, 2.0f);
        Assert.Multiple(() =>
        {
            Assert.That(damage, Is.EqualTo(1.4f).Within(0.001f));
            Assert.That(runtime.Fire.Stacks, Is.Zero);
        });
    }
}
