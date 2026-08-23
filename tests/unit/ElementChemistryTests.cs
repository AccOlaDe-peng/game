using Catalyst.Elements;
using NUnit.Framework;

namespace Catalyst.Tests.Unit;

public sealed class ElementChemistryTests
{
    [TestCase(ElementType.Water, ElementType.Lightning, FusionIdentity.ConductiveLightning)]
    [TestCase(ElementType.Fire, ElementType.Water, FusionIdentity.Steam)]
    [TestCase(ElementType.Water, ElementType.Wind, FusionIdentity.Ice)]
    [TestCase(ElementType.Fire, ElementType.Wind, FusionIdentity.Firestorm)]
    [TestCase(ElementType.Fire, ElementType.Earth, FusionIdentity.Lava)]
    [TestCase(ElementType.Fire, ElementType.Lightning, FusionIdentity.Plasma)]
    [TestCase(ElementType.Water, ElementType.Earth, FusionIdentity.Mud)]
    [TestCase(ElementType.Wind, ElementType.Lightning, FusionIdentity.Storm)]
    [TestCase(ElementType.Wind, ElementType.Earth, FusionIdentity.Sandstorm)]
    [TestCase(ElementType.Earth, ElementType.Lightning, FusionIdentity.CrystalMagnet)]
    public void CompilesEveryPair(ElementType first, ElementType second, FusionIdentity expected)
    {
        ChemistryLoadout result = ElementChemistry.Compile(new[] { first, second });
        Assert.That(result.Fusion, Is.EqualTo(expected));
        Assert.That(result.Has(first) && result.Has(second), Is.True);
    }

    [Test]
    public void ThreeElementsChooseHighestPriorityCompletePair()
    {
        ChemistryLoadout result = ElementChemistry.Compile(new[]
        {
            ElementType.Fire, ElementType.Water, ElementType.Lightning
        });
        Assert.That(result.Fusion, Is.EqualTo(FusionIdentity.ConductiveLightning));
    }

    [Test]
    public void MarkNeverParticipatesInFusion()
    {
        ChemistryLoadout result = ElementChemistry.Compile(new[] { ElementType.Mark, ElementType.Fire });
        Assert.Multiple(() =>
        {
            Assert.That(result.Fusion, Is.EqualTo(FusionIdentity.None));
            Assert.That(result.Primary, Is.EqualTo(ElementType.Fire));
        });
    }

    [Test]
    public void FusionCatalogContainsExactlyTenIdentities()
    {
        Assert.That(ElementChemistry.AllFusions, Has.Count.EqualTo(10));
        Assert.That(ElementChemistry.AllFusions, Is.Unique);
    }
}
