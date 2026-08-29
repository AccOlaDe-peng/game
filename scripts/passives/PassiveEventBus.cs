using Catalyst.Core;
using Catalyst.Run;

namespace Catalyst.Passives;

/// <summary>
/// Fixed-capacity ordered event queue. Combat systems publish semantic events;
/// the passive system drains and dispatches them during the passive phase of
/// each physics frame. The bus never executes gameplay itself.
/// </summary>
public sealed class PassiveEventBus
{
    private readonly PassiveEventContext[] _buffer;
    private int _writeIndex;
    private int _readIndex;
    private ulong _nextSequence;
    private readonly HashSet<ulong> _throttledRoots = new();
    private readonly PassiveEventBudget _budget;

    public PassiveEventBus(PassiveEventBudget? budget = null)
    {
        _budget = budget ?? new PassiveEventBudget();
        _buffer = new PassiveEventContext[_budget.MaximumEventsPerPhysicsFrame];
    }

    public int PendingCount => _writeIndex - _readIndex;
    public int DroppedEventCount { get; private set; }
    public int EventsThisFrame { get; private set; }
    public int MaximumChainDepth => _budget.MaximumChainDepth;
    public PassiveEventBudget Budget => _budget;

    public void BeginFrame()
    {
        EventsThisFrame = 0;
        _throttledRoots.Clear();
    }

    public ulong NextSequence() => ++_nextSequence;

    /// <summary>Publish a derived (chained) event. Returns false when the event was dropped.</summary>
    public bool Publish(in PassiveEventContext context)
    {
        if (context.ChainDepth > _budget.MaximumChainDepth ||
            (context.ChainDepth > 0 && EventsThisFrame >= _budget.MaximumEventsPerPhysicsFrame) ||
            _writeIndex >= _buffer.Length)
        {
            DroppedEventCount++;
            LogThrottle(context.RootSequence);
            return false;
        }

        if (_writeIndex >= _buffer.Length)
        {
            DroppedEventCount++;
            LogThrottle(context.RootSequence);
            return false;
        }

        _buffer[_writeIndex++] = context;
        EventsThisFrame++;
        return true;
    }

    /// <summary>Copies the queued events in publish order into <paramref name="snapshot"/>.</summary>
    public void Drain(List<PassiveEventContext> snapshot)
    {
        snapshot.Clear();
        for (int index = _readIndex; index < _writeIndex; index++)
        {
            snapshot.Add(_buffer[index]);
        }
        _readIndex = _writeIndex;
    }

    public void EndFrame()
    {
        Array.Clear(_buffer, 0, _writeIndex);
        _readIndex = 0;
        _writeIndex = 0;
    }

    private void LogThrottle(ulong rootSequence)
    {
        if (!_throttledRoots.Add(rootSequence))
        {
            return;
        }
        CatalystLog.Warning("Passives",
            $"Passive event budget reached; dropped derived events for root {rootSequence}.");
    }
}
