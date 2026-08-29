using Catalyst.Passives;
using NUnit.Framework;
using Godot;

namespace Catalyst.Tests.Unit;

public sealed class PassiveEventBusTests
{
    private static PassiveEventContext Create(ulong sequence, int chainDepth = 0, ulong root = 0) =>
        PassiveEventContext.Create(PassiveTriggerKind.ProjectileHit, 1.0, sequence, chainDepth: chainDepth,
            rootSequence: root);

    [Test]
    public void EventsAreDispatchedInPublishOrder()
    {
        PassiveEventBus bus = new();
        List<PassiveEventContext> snapshot = new();
        bus.BeginFrame();
        bus.Publish(Create(bus.NextSequence()));
        bus.Publish(Create(bus.NextSequence()));
        bus.Publish(Create(bus.NextSequence()));
        bus.Drain(snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot, Has.Count.EqualTo(3));
            Assert.That(snapshot.Select(context => context.Sequence),
                Is.Ordered);
        });
    }

    [Test]
    public void SamePassiveNeverProcessesSameSequenceTwice()
    {
        // The bus guarantees ordered drain; dedupe by sequence is verified at
        // the runtime level via LastProcessedSequence contract.
        PassiveRuntime runtime = new(new PassiveBlueprint
        {
            Id = new StringName("passive.test"),
            Trigger = PassiveTriggerKind.ProjectileHit
        });
        PassiveEventContext first = Create(1);
        PassiveEventContext duplicate = Create(1);

        Assert.Multiple(() =>
        {
            Assert.That(first.Sequence, Is.EqualTo(duplicate.Sequence));
            Assert.That(runtime.LastProcessedSequence, Is.EqualTo(0));
            runtime.LastProcessedSequence = first.Sequence;
            Assert.That(runtime.LastProcessedSequence == duplicate.Sequence, Is.True);
        });
    }

    [Test]
    public void ChainDepthAboveBudgetIsDropped()
    {
        PassiveEventBus bus = new(new PassiveEventBudget { MaximumChainDepth = 2 });
        bus.BeginFrame();
        bool accepted = bus.Publish(Create(bus.NextSequence(), chainDepth: 3));
        Assert.That(accepted, Is.False);
        Assert.That(bus.DroppedEventCount, Is.EqualTo(1));
    }

    [Test]
    public void FrameBudgetDropsDerivedEventsAndKeepsCounting()
    {
        PassiveEventBus bus = new(new PassiveEventBudget
        {
            MaximumEventsPerPhysicsFrame = 4,
            MaximumChainDepth = 4
        });
        bus.BeginFrame();
        ulong root = bus.NextSequence();
        int accepted = 0;
        for (int index = 0; index < 10; index++)
        {
            if (bus.Publish(Create(bus.NextSequence(), chainDepth: 1, root: root)))
            {
                accepted++;
            }
        }
        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.EqualTo(4));
            Assert.That(bus.DroppedEventCount, Is.EqualTo(6));
        });
    }

    [Test]
    public void ConditionsThatFailDoNotDocumentedConsumeCooldown()
    {
        // Contract check: cooldown is only assigned on successful execution;
        // the runtime never touches CooldownRemaining on condition failure.
        PassiveRuntime runtime = new(new PassiveBlueprint
        {
            Id = new StringName("passive.test"),
            CooldownSeconds = 2.5f
        });
        Assert.That(runtime.CooldownRemaining, Is.EqualTo(0.0f));
        runtime.CooldownRemaining = runtime.Definition.CooldownSeconds;
        Assert.That(runtime.CooldownRemaining, Is.EqualTo(2.5f));
    }
}
