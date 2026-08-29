namespace Catalyst.Passives;

public sealed class PassiveRuntime
{
    public PassiveRuntime(PassiveBlueprint definition)
    {
        Definition = definition;
        Enabled = definition.EnabledByDefault;
    }

    public PassiveBlueprint Definition { get; }
    public bool Enabled { get; set; }
    public float CooldownRemaining { get; set; }
    public float IntervalRemaining { get; set; }
    public int TriggersThisSecond { get; set; }
    public double RateWindowStartedAt { get; set; }
    public ulong LastProcessedSequence { get; set; }
    public ulong LastTriggeredSequence { get; set; }
    public int TotalTriggerCount { get; set; }
    public double TotalValueProduced { get; set; }
    public object? SharedState { get; set; }
}
