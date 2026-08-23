using Catalyst.Core;
using Catalyst.Elements;
using Catalyst.Enemies;
using Catalyst.Run;
using Godot;

namespace Catalyst.Combat;

public partial class CombatSystem : Node
{
    public event Action<DamageContext, EnemyState, bool>? HitResolved;
    [Export(PropertyHint.Range, "64,16384,64")]
    public int CommandCapacity { get; set; } = 4096;

    private DamageContext[] _commands = Array.Empty<DamageContext>();
    private int _readIndex;
    private int _writeIndex;
    private EnemySystem _enemies = null!;
    private ElementSystem _elements = null!;
    private RunController _run = null!;
    private RunStatistics _statistics = null!;

    public int PendingCount => _writeIndex - _readIndex;
    public int PeakCommands { get; private set; }

    public override void _Ready()
    {
        _commands = new DamageContext[CommandCapacity];
        _enemies = GetNode<EnemySystem>("../EntitySystem");
        _elements = GetNode<ElementSystem>("../ElementSystem");
        _run = GetNode<RunController>("../RunController");
        _statistics = GetNode<RunStatistics>("../RunStatistics");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_run.State == RunState.Playing)
        {
            Flush();
        }
    }

    public bool Submit(in DamageContext context)
    {
        if (context.ChainDepth > 4 || _writeIndex >= _commands.Length)
        {
            CatalystLog.Warning("Combat", "Effect command buffer limit reached.");
            return false;
        }

        _commands[_writeIndex++] = context;
        PeakCommands = Math.Max(PeakCommands, PendingCount);
        return true;
    }

    public void Flush()
    {
        while (_readIndex < _writeIndex)
        {
            DamageContext context = _commands[_readIndex++];
            if (!_enemies.TryGet(context.Target, out EnemyState beforeHit))
            {
                continue;
            }

            if (context.BaseDamage > 0.0f)
            {
                _enemies.ApplyDamage(context.Target, context.BaseDamage, out float resolvedDamage);
                _statistics.RecordSpellHit(context.SourceSpellId, resolvedDamage);
            }

            bool killed = !_enemies.TryGet(context.Target, out _);
            HitResolved?.Invoke(context, beforeHit, killed);

            if (killed)
            {
                continue;
            }

            bool reactionDamage = context.Flags.HasFlag(DamageFlags.IsReactionDamage);
            if (context.Element != ElementType.None && context.ElementStacks > 0)
            {
                bool canTrigger = context.Flags.HasFlag(DamageFlags.CanTriggerReaction) &&
                    !reactionDamage;
                _elements.ApplyElement(context, canTrigger);
            }
            else if (context.Flags.HasFlag(DamageFlags.ForceReactionCheck) && !reactionDamage)
            {
                _elements.ForceReaction(context);
            }
        }

        Array.Clear(_commands, 0, _writeIndex);
        _readIndex = 0;
        _writeIndex = 0;
    }
}
