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
    public IReadOnlyDictionary<ReactionKind, int> ReactionsByKind => _reactionsByKind;
    public IReadOnlyDictionary<string, double> DamageBySpell => _damageBySpell;
    public IReadOnlyDictionary<string, int> HitsBySpell => _hitsBySpell;
    public string LastDamageCause { get; private set; } = "未知";

    private readonly Dictionary<ReactionKind, int> _reactionsByKind = new();
    private readonly Dictionary<string, double> _damageBySpell = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _hitsBySpell = new(StringComparer.Ordinal);

    public void RecordKill() => KillCount++;
    public void RecordDamage(double amount) => DamageDealt += Math.Max(0.0, amount);
    public void RecordDamageTaken(double amount, string cause)
    {
        DamageTaken += Math.Max(0.0, amount);
        if (!string.IsNullOrWhiteSpace(cause))
        {
            LastDamageCause = cause;
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
