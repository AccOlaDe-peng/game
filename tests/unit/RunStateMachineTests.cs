using Catalyst.Run;
using NUnit.Framework;

namespace Catalyst.Tests.Unit;

public sealed class RunStateMachineTests
{
    [Test]
    public void ValidRunFlowCanReachResults()
    {
        RunStateMachine machine = new();

        Assert.Multiple(() =>
        {
            Assert.That(machine.TryTransition(RunState.Starting), Is.True);
            Assert.That(machine.TryTransition(RunState.Playing), Is.True);
            Assert.That(machine.TryTransition(RunState.Victory), Is.True);
            Assert.That(machine.TryTransition(RunState.Results), Is.True);
            Assert.That(machine.Current, Is.EqualTo(RunState.Results));
        });
    }

    [Test]
    public void InvalidTransitionDoesNotChangeState()
    {
        RunStateMachine machine = new();

        bool changed = machine.TryTransition(RunState.Victory);

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.False);
            Assert.That(machine.Current, Is.EqualTo(RunState.Loading));
        });
    }
}
