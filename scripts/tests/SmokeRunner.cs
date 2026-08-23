using Catalyst.App;
using Catalyst.Core;
using Catalyst.Enemies;
using Catalyst.Pickups;
using Catalyst.Player;
using Catalyst.Run;
using Catalyst.Spatial;
using Catalyst.UI;
using Catalyst.Upgrades;
using Godot;

namespace Catalyst.Tests;

public partial class SmokeRunner : Node
{
    [Export]
    public PackedScene AppScene { get; set; } = null!;

    public override void _Ready()
    {
        CallDeferred(MethodName.RunSmokeTest);
    }

    private async void RunSmokeTest()
    {
        try
        {
            AppRoot app = AppScene.Instantiate<AppRoot>();
            AddChild(app);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(app.GetNodeOrNull("CurrentScreen/MainMenu") is not null,
                "Main menu was not created.");

            app.ShowRun();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            RunRoot? run = app.GetNodeOrNull<RunRoot>("CurrentScreen/RunRoot");
            Require(run is not null,
                "Run scene was not created.");
            RunController controller = run!.GetNode<RunController>("SimulationRoot/RunController");
            Label seedLabel = run.GetNode<Label>("UI/RunHud/TopBar/SeedLabel");
            Require(controller.RunSeed != 0, "Run seed was not initialized.");
            Require(seedLabel.Text.Contains(controller.RunSeed.ToString()),
                "Run HUD did not receive the initialized seed.");

            EnemySystem enemies = run.GetNode<EnemySystem>("SimulationRoot/EntitySystem");
            SpatialGrid grid = run.GetNode<SpatialGrid>("SimulationRoot/SpatialGrid");
            PickupSystem pickups = run.GetNode<PickupSystem>("SimulationRoot/PickupSystem");
            PlayerProgression progression = run.GetNode<PlayerProgression>("WorldRoot/Player/Progression");
            UpgradeSystem upgrades = run.GetNode<UpgradeSystem>("SimulationRoot/UpgradeSystem");
            UpgradeScreen upgradeScreen = run.GetNode<UpgradeScreen>("UI/UpgradeScreen");

            EntityHandle target = enemies.Spawn(Vector2.Zero);
            Require(target.IsValid, "Enemy system could not spawn a test enemy.");
            Require(enemies.ApplyDamage(target, 999.0f), "Enemy did not receive damage.");
            Require(pickups.ActiveCount == 1, "Enemy death did not create experience.");
            pickups._PhysicsProcess(1.0 / 60.0);
            Require(pickups.ActiveCount == 0 && progression.Experience == 1,
                "Experience was not collected by the player.");

            progression.AddExperience(5);
            Require(controller.State == RunState.LevelUp && GetTree().Paused,
                "Level up did not pause the run.");
            Require(upgrades.CurrentChoices.Count == 3 && upgradeScreen.Visible,
                "Upgrade screen did not present three choices.");
            Require(upgrades.SelectChoice(0), "Upgrade choice was not accepted.");
            Require(controller.State == RunState.Playing && !GetTree().Paused,
                "Run did not resume after choosing an upgrade.");

            enemies.ClearAll();
            for (int index = 0; index < 300; index++)
            {
                float angle = Mathf.Tau * index / 300.0f;
                float radius = 18.0f + index % 7;
                Require(enemies.Spawn(Vector2.FromAngle(angle) * radius).IsValid,
                    $"Could not create stress enemy {index}.");
            }

            Require(enemies.ActiveCount == 300, "Stress setup did not reach 300 enemies.");
            System.Diagnostics.Stopwatch benchmark = System.Diagnostics.Stopwatch.StartNew();
            for (int tick = 0; tick < 120; tick++)
            {
                enemies._PhysicsProcess(1.0 / 60.0);
                grid.Rebuild();
            }
            benchmark.Stop();
            Require(enemies.ActiveCount == 300, "Enemy count changed during the stress update.");
            CatalystLog.Info("Tests", $"300-enemy/120-tick benchmark: {benchmark.ElapsedMilliseconds} ms.");
            enemies.ClearAll();

            controller.TogglePause();
            Require(GetTree().Paused && controller.State == RunState.Paused,
                "Run did not enter the paused state.");
            controller.TogglePause();
            Require(!GetTree().Paused && controller.State == RunState.Playing,
                "Run did not resume from pause.");

            app.ShowMainMenu();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(app.GetNodeOrNull("CurrentScreen/MainMenu") is not null,
                "Could not return to main menu.");

            CatalystLog.Info("Tests", "SMOKE_OK");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            CatalystLog.Error("Tests", $"SMOKE_FAILED: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
