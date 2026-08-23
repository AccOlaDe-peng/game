namespace Catalyst.Run;

public sealed class RunStateMachine
{
    public RunState Current { get; private set; } = RunState.Loading;

    public bool CanTransitionTo(RunState next)
    {
        return Current switch
        {
            RunState.Loading => next is RunState.Starting,
            RunState.Starting => next is RunState.Playing,
            RunState.Playing => next is RunState.LevelUp or RunState.Paused or
                RunState.BossIntro or RunState.Victory or RunState.Defeat,
            RunState.LevelUp => next is RunState.Playing,
            RunState.Paused => next is RunState.Playing,
            RunState.BossIntro => next is RunState.Playing,
            RunState.Victory or RunState.Defeat => next is RunState.Results,
            RunState.Results => next is RunState.Unloading,
            RunState.Unloading => false,
            _ => false
        };
    }

    public bool TryTransition(RunState next)
    {
        if (!CanTransitionTo(next))
        {
            return false;
        }

        Current = next;
        return true;
    }
}
