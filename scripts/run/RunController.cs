using Catalyst.Core;
using Godot;

namespace Catalyst.Run;

public partial class RunController : Node
{
    [Signal]
    public delegate void StateChangedEventHandler(int state);

    private readonly RunStateMachine _stateMachine = new();

    public RunState State => _stateMachine.Current;
    public double ElapsedSeconds { get; private set; }
    public ulong RunSeed { get; private set; }
    public RunRandomStreams RandomStreams { get; private set; } = null!;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (State == RunState.Playing)
        {
            ElapsedSeconds += delta;
        }
    }

    public void BeginRun(ulong? requestedSeed = null)
    {
        GetTree().Paused = false;
        ElapsedSeconds = 0.0;
        RunSeed = requestedSeed ?? CreateTimeSeed();
        RandomStreams = new RunRandomStreams(RunSeed);
        TransitionTo(RunState.Starting);
        TransitionTo(RunState.Playing);
        CatalystLog.Info("Run", $"Run started with seed {RunSeed}.");
    }

    public void TogglePause()
    {
        if (State == RunState.Playing)
        {
            TransitionTo(RunState.Paused);
            GetTree().Paused = true;
        }
        else if (State == RunState.Paused)
        {
            GetTree().Paused = false;
            TransitionTo(RunState.Playing);
        }
    }

    public bool EnterLevelUp()
    {
        if (State != RunState.Playing)
        {
            return false;
        }

        TransitionTo(RunState.LevelUp);
        GetTree().Paused = true;
        return true;
    }

    public void ResumeFromLevelUp()
    {
        if (State != RunState.LevelUp)
        {
            return;
        }

        GetTree().Paused = false;
        TransitionTo(RunState.Playing);
    }

    public void EnterDefeat()
    {
        if (State != RunState.Playing)
        {
            return;
        }

        TransitionTo(RunState.Defeat);
        GetTree().Paused = true;
    }

    public void EnterVictory()
    {
        if (State != RunState.Playing)
        {
            return;
        }
        TransitionTo(RunState.Victory);
        GetTree().Paused = true;
    }

    private void TransitionTo(RunState next)
    {
        if (!_stateMachine.TryTransition(next))
        {
            CatalystLog.Error("Run", $"Invalid run transition: {State} -> {next}");
            return;
        }

        EmitSignal(SignalName.StateChanged, (int)next);
    }

    private static ulong CreateTimeSeed()
    {
        ulong utcTicks = (ulong)DateTimeOffset.UtcNow.UtcTicks;
        return StableSeed.Derive(utcTicks, "run");
    }
}
