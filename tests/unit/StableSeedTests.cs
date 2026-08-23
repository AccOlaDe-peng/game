using Catalyst.Core;
using NUnit.Framework;

namespace Catalyst.Tests.Unit;

public sealed class StableSeedTests
{
    [Test]
    public void SameRootAndStreamProduceSameSeed()
    {
        ulong first = StableSeed.Derive(42UL, "upgrade");
        ulong second = StableSeed.Derive(42UL, "upgrade");

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void DifferentStreamsProduceDifferentSeeds()
    {
        ulong wave = StableSeed.Derive(42UL, "wave");
        ulong upgrade = StableSeed.Derive(42UL, "upgrade");

        Assert.That(upgrade, Is.Not.EqualTo(wave));
    }
}
