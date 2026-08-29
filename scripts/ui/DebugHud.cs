using Catalyst.Run;
using Catalyst.Enemies;
using Catalyst.Passives;
using Catalyst.Pickups;
using Catalyst.Projectiles;
using Godot;

namespace Catalyst.UI;

public partial class DebugHud : Control
{
    private RunController? _controller;
    private Label _label = null!;
    private EnemySystem? _enemies;
    private ProjectileSystem? _projectiles;
    private PickupSystem? _pickups;
    private RunStatistics? _statistics;
    private PassiveSystem? _passives;
    private AutoCatalysisSystem? _catalysis;
    private double _refreshRemaining;

    public void BindPassives(PassiveSystem passives, AutoCatalysisSystem catalysis)
    {
        _passives = passives;
        _catalysis = catalysis;
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _label = GetNode<Label>("Panel/Label");
    }

    public override void _Process(double delta)
    {
        _refreshRemaining -= delta;
        if (_refreshRemaining > 0.0 || _controller is null)
        {
            return;
        }

        _refreshRemaining = 0.25;
        _label.Text = $"FPS {Engine.GetFramesPerSecond():0}\n" +
            $"State {_controller.State}\n" +
            $"Time {_controller.ElapsedSeconds:0.0}s\n" +
            $"Enemies {_enemies?.ActiveCount ?? 0}\n" +
            $"Projectiles {_projectiles?.ActiveCount ?? 0}\n" +
            $"Pickups {_pickups?.ActiveCount ?? 0}\n" +
            $"Kills {_statistics?.KillCount ?? 0}\n" +
            $"Reactions {_statistics?.ReactionCount ?? 0}\n" +
            $"Seed {_controller.RunSeed}\n" +
            $"Passive events/frame {_passives?.EventsDispatchedThisFrame ?? 0}\n" +
            $"Dropped passive events {_passives?.DroppedEventCount ?? 0}\n" +
            $"Active passives {_passives?.ActivePassiveCount ?? 0}\n" +
            $"Catalysis state {_catalysis?.State}\n" +
            $"Catalysis charge {_catalysis?.Charge:0.00}\n" +
            $"Catalysis best score {_catalysis?.BestScore:0.0} / targets {_catalysis?.BestReactiveTargetCount ?? 0}";
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key &&
            key.Pressed &&
            !key.Echo &&
            key.Keycode == Key.F3)
        {
            Visible = !Visible;
            GetViewport().SetInputAsHandled();
        }
    }

    public void Bind(
        RunController controller,
        EnemySystem enemies,
        ProjectileSystem projectiles,
        PickupSystem pickups,
        RunStatistics statistics)
    {
        _controller = controller;
        _enemies = enemies;
        _projectiles = projectiles;
        _pickups = pickups;
        _statistics = statistics;
    }
}
