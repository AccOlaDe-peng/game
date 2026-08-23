namespace Catalyst.Events;

public enum RunEventKind
{
    SealingRift,
    StabilizationCircle
}

public enum RunEventState
{
    Inactive,
    Telegraphing,
    Active,
    Succeeded,
    Failed,
    Cleanup
}
