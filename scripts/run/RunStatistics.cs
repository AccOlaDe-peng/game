using Godot;
using Catalyst.Elements;

namespace Catalyst.Run;

public partial class RunStatistics : Node
{
    public int KillCount { get; private set; }
    public double DamageDealt { get; private set; }
    public double DamageTaken { get; private set; }
    public int ReactionCount { get; private set; }
    public double ReactionDamage { get; private set; }
    public int CatalysisChargeCompletedCount { get; private set; }
    public int CatalysisExecutionCount { get; private set; }
    public int CatalysisReactionCount { get; private set; }
    public double CatalysisReactionDamage { get; private set; }
    public int UpgradeRerollCount { get; private set; }
    public IReadOnlyDictionary<ReactionKind, int> ReactionsByKind => _reactionsByKind;
    public IReadOnlyDictionary<string, double> DamageBySpell => _damageBySpell;
    public IReadOnlyDictionary<string, int> HitsBySpell => _hitsBySpell;
    public IReadOnlyDictionary<string, int> PassiveTriggerCounts => _passiveTriggerCounts;
    public IReadOnlyDictionary<string, double> PassiveProducedValue => _passiveProducedValue;
    public string LastDamageCause { get; private set; } = "未知";

    private readonly Dictionary<ReactionKind, int> _reactionsByKind = new();
    private readonly Dictionary<string, double> _damageBySpell = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _hitsBySpell = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _passiveTriggerCounts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, double> _passiveProducedValue = new(StringComparer.Ordinal);

    public void RecordKill() => KillCount++;
    public void RecordDamage(double amount) => DamageDealt += Math.Max(0.0, amount);
    public void RecordUpgradeReroll() => UpgradeRerollCount++;
    public void RecordDamageTaken(double amount, string cause)
    {
        DamageTaken += Math.Max(0.0, amount);
        if (!string.IsNullOrWhiteSpace(cause))
        {
            LastDamageCause = cause;
        }
    }

    public void RecordCatalysisChargeCompleted() => CatalysisChargeCompletedCount++;

    public void RecordCatalysisExecution(int reactionCount, double reactionDamage)
    {
        CatalysisExecutionCount++;
        CatalysisReactionCount += Math.Max(0, reactionCount);
        CatalysisReactionDamage += Math.Max(0.0, reactionDamage);
    }

    public void RecordPassiveTrigger(string passiveId, double producedValue = 0.0)
    {
        _passiveTriggerCounts[passiveId] = _passiveTriggerCounts.GetValueOrDefault(passiveId) + 1;
        if (producedValue > 0.0)
        {
            _passiveProducedValue[passiveId] =
                _passiveProducedValue.GetValueOrDefault(passiveId) + producedValue;
        }
    }

    public void RecordSpellHit(StringName spellId, double amount)
    {
        string key = spellId.IsEmpty ? "unknown" : spellId.ToString();
        _damageBySpell[key] = _damageBySpell.GetValueOrDefault(key) + Math.Max(0.0, amount);
        _hitsBySpell[key] = _hitsBySpell.GetValueOrDefault(key) + 1;
    }
    public void RecordReaction(ReactionKind kind, double damage)
    {
        ReactionCount++;
        ReactionDamage += Math.Max(0.0, damage);
        _reactionsByKind[kind] = _reactionsByKind.GetValueOrDefault(kind) + 1;
    }
}
