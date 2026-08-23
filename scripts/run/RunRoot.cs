using Catalyst.App;
using Catalyst.UI;
using Catalyst.Enemies;
using Catalyst.Pickups;
using Catalyst.Player;
using Catalyst.Projectiles;
using Catalyst.Upgrades;
using Catalyst.Elements;
using Catalyst.Spells;
using Catalyst.Events;
using Catalyst.Boss;
using Godot;

namespace Catalyst.Run;

public partial class RunRoot : Node
{
    [Signal]
    public delegate void ReturnToMenuRequestedEventHandler();

    [Signal]
    public delegate void RestartRequestedEventHandler();

    private RunController _controller = null!;
    private RunHud _hud = null!;
    private DebugHud _debugHud = null!;
    private UpgradeScreen _upgradeScreen = null!;
    private RunResults _results = null!;
    private RunStatistics _statistics = null!;
    private SpellSystem _spells = null!;
    private BossController _boss = null!;
    private bool _summaryRecorded;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _controller = GetNode<RunController>("SimulationRoot/RunController");
        _hud = GetNode<RunHud>("UI/RunHud");
        _debugHud = GetNode<DebugHud>("UI/DebugHud");
        _upgradeScreen = GetNode<UpgradeScreen>("UI/UpgradeScreen");
        _results = GetNode<RunResults>("UI/RunResults");

        PlayerHealth health = GetNode<PlayerHealth>("WorldRoot/Player/HealthComponent");
        PlayerProgression progression = GetNode<PlayerProgression>("WorldRoot/Player/Progression");
        CatalyzeAbility catalyze = GetNode<CatalyzeAbility>("WorldRoot/Player/CatalyzeAbility");
        EnemySystem enemies = GetNode<EnemySystem>("SimulationRoot/EntitySystem");
        ProjectileSystem projectiles = GetNode<ProjectileSystem>("SimulationRoot/ProjectileSystem");
        PickupSystem pickups = GetNode<PickupSystem>("SimulationRoot/PickupSystem");
        _statistics = GetNode<RunStatistics>("SimulationRoot/RunStatistics");
        UpgradeSystem upgrades = GetNode<UpgradeSystem>("SimulationRoot/UpgradeSystem");
        ElementSystem elements = GetNode<ElementSystem>("SimulationRoot/ElementSystem");
        _spells = GetNode<SpellSystem>("SimulationRoot/SpellSystem");
        RunEventSystem events = GetNode<RunEventSystem>("SimulationRoot/RunEventSystem");
        _boss = GetNode<BossController>("SimulationRoot/BossController");

        _controller.BeginRun();
        _hud.Bind(_controller, health, progression, catalyze, elements, _spells, events, enemies, _boss);
        _debugHud.Bind(_controller, enemies, projectiles, pickups, _statistics);
        _upgradeScreen.Bind(upgrades);
        _hud.PauseRequested += _controller.TogglePause;
        _hud.ReturnRequested += ReturnToMenu;
        _hud.RestartRequested += RestartRun;
        _results.RestartRequested += RestartRun;
        _results.ReturnRequested += ReturnToMenu;
        _controller.StateChanged += OnStateChanged;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(InputBootstrap.Pause))
        {
            _controller.TogglePause();
            GetViewport().SetInputAsHandled();
        }
    }

    private void ReturnToMenu()
    {
        GetTree().Paused = false;
        EmitSignal(SignalName.ReturnToMenuRequested);
    }

    private void RestartRun()
    {
        GetTree().Paused = false;
        EmitSignal(SignalName.RestartRequested);
    }

    private void OnStateChanged(int stateValue)
    {
        RunState state = (RunState)stateValue;
        if (_summaryRecorded || state is not (RunState.Defeat or RunState.Victory))
        {
            return;
        }
        _summaryRecorded = true;
        bool victory = state == RunState.Victory;
        RunSummary summary = new()
        {
            CharacterId = GetNode<SaveService>("/root/SaveService").ActiveCharacter.Id,
            StartingWeaponId = GetNode<SaveService>("/root/SaveService").ActiveCharacter.StartingWeaponId,
            Seed = _controller.RunSeed,
            SurvivalTime = _controller.ElapsedSeconds,
            KillCount = _statistics.KillCount,
            Victory = victory,
            BossDefeated = _boss.IsDefeated,
            DamageDealt = _statistics.DamageDealt,
            DamageTaken = _statistics.DamageTaken,
            ReactionCount = _statistics.ReactionCount,
            ReactionDamage = _statistics.ReactionDamage,
            Result = victory ? "胜利" : "失败",
            DeathCause = victory ? string.Empty : _statistics.LastDamageCause,
            DamageBySpell = _statistics.DamageBySpell.ToDictionary(pair => pair.Key, pair => pair.Value),
            ReactionCounts = _statistics.ReactionsByKind.ToDictionary(
                pair => pair.Key.ToString(), pair => pair.Value),
            FinalBuild = _spells.Loadout.Select(runtime =>
                $"{runtime.Definition.DisplayName} Lv.{runtime.Level}" +
                (runtime.SelectedBranch == SpellBranch.None
                    ? string.Empty
                    : $" / 分支 {runtime.SelectedBranch}"))
                .ToList(),
            CompletedAtUtc = DateTimeOffset.UtcNow
        };
        GetNode<SaveService>("/root/SaveService").RecordRun(summary);
        _results.ShowSummary(summary);
    }
}
