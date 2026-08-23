using Catalyst.App;
using Godot;

namespace Catalyst.UI;

public partial class RunResults : Control
{
    [Signal]
    public delegate void RestartRequestedEventHandler();

    [Signal]
    public delegate void ReturnRequestedEventHandler();

    private Label _title = null!;
    private Label _summary = null!;
    private Label _damage = null!;
    private Label _spells = null!;
    private Label _build = null!;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _title = GetNode<Label>("%ResultTitle");
        _summary = GetNode<Label>("%SummaryLabel");
        _damage = GetNode<Label>("%DamageLabel");
        _spells = GetNode<Label>("%SpellDamageLabel");
        _build = GetNode<Label>("%BuildLabel");
        GetNode<Button>("%RestartButton").Pressed += () =>
            EmitSignal(SignalName.RestartRequested);
        GetNode<Button>("%MenuButton").Pressed += () =>
            EmitSignal(SignalName.ReturnRequested);
        Visible = false;
    }

    public void ShowSummary(RunSummary summary)
    {
        TimeSpan time = TimeSpan.FromSeconds(summary.SurvivalTime);
        _title.Text = summary.Victory ? "巨像已倒下" : "试炼结束";
        _title.Modulate = summary.Victory
            ? new Color(1.0f, 0.76f, 0.28f)
            : new Color(1.0f, 0.44f, 0.38f);
        _summary.Text =
            $"结果  {summary.Result}\n" +
            $"时间  {(int)time.TotalMinutes:00}:{time.Seconds:00}    击杀  {summary.KillCount}\n" +
            $"Seed  {summary.Seed}";
        _damage.Text =
            $"总伤害  {summary.DamageDealt:0}    承受伤害  {summary.DamageTaken:0}\n" +
            $"元素反应  {summary.ReactionCount} 次 / {summary.ReactionDamage:0} 伤害" +
            (summary.Victory || string.IsNullOrWhiteSpace(summary.DeathCause)
                ? string.Empty
                : $"\n最后伤害来源  {summary.DeathCause}");
        _spells.Text = summary.DamageBySpell.Count == 0
            ? "法术伤害  暂无"
            : "法术伤害\n" + string.Join("\n", summary.DamageBySpell
                .OrderByDescending(pair => pair.Value)
                .Select(pair => $"  {ToDisplayName(pair.Key)}  {pair.Value:0}"));
        _build.Text = "最终构筑\n" + string.Join("\n", summary.FinalBuild.Select(item => $"  {item}"));
        Visible = true;
        GetNode<Button>("%RestartButton").GrabFocus();
    }

    private static string ToDisplayName(string id) => id switch
    {
        "spell.arcane_missile" => "奥术飞弹",
        "spell.fireball" => "火球术",
        "spell.frost_lance" => "冰霜长枪",
        "spell.chain_lightning" => "连锁闪电",
        "spell.orbit_orb" => "环绕法球",
        _ => id
    };
}
