using Catalyst.App;
using Catalyst.Core;
using Catalyst.Enemies;
using Catalyst.Events;
using Catalyst.Run;
using Catalyst.Spells;
using Catalyst.Spatial;
using Catalyst.Upgrades;
using Catalyst.Waves;
using Catalyst.Boss;
using Godot;

namespace Catalyst.Tests;

public partial class M3ContentRunner : Node
{
    [Export] public PackedScene RunScene { get; set; } = null!;

    public override void _Ready()
    {
        CallDeferred(MethodName.RunTests);
    }

    private void RunTests()
    {
        try
        {
            ContentCatalog catalog = GetNode<ContentCatalog>("/root/ContentCatalog");
            Require(catalog.IsValid && catalog.Count == 27,
                "Content catalog did not validate the base and V2 weapon definitions.");

            RunRoot run = RunScene.Instantiate<RunRoot>();
            AddChild(run);
            RunController controller = run.GetNode<RunController>("SimulationRoot/RunController");
            SpellSystem spells = run.GetNode<SpellSystem>("SimulationRoot/SpellSystem");
            UpgradeSystem upgrades = run.GetNode<UpgradeSystem>("SimulationRoot/UpgradeSystem");
            EnemySystem enemies = run.GetNode<EnemySystem>("SimulationRoot/EntitySystem");
            SpatialGrid grid = run.GetNode<SpatialGrid>("SimulationRoot/SpatialGrid");
            WaveDirector waves = run.GetNode<WaveDirector>("SimulationRoot/WaveDirector");
            RunEventSystem events = run.GetNode<RunEventSystem>("SimulationRoot/RunEventSystem");
            BossController boss = run.GetNode<BossController>("SimulationRoot/BossController");
            waves.EnemyLimit = 0;
            spells.Enabled = false;

            Require(spells.Loadout.Count == 1 &&
                spells.Loadout[0].Definition.CastKind == SpellCastKind.ArcaneMissile,
                "Run did not start with only Arcane Missile.");
            Require(spells.AcquireSpell(SpellCastKind.Fireball), "Could not acquire Fireball.");
            Require(spells.AcquireSpell(SpellCastKind.FrostLance), "Could not acquire Frost Lance.");
            Require(spells.AcquireSpell(SpellCastKind.OrbitOrb), "Could not acquire Orbit Orb.");
            Require(!spells.AcquireSpell(SpellCastKind.ChainLightning) && spells.Loadout.Count == 4,
                "Four-slot spell limit was not enforced.");

            Require(spells.UpgradeSpell(SpellCastKind.ArcaneMissile, SpellBranch.None),
                "Arcane Missile could not reach level 2.");
            Require(spells.UpgradeSpell(SpellCastKind.ArcaneMissile, SpellBranch.A),
                "Arcane Missile branch A could not be selected.");
            Require(!spells.UpgradeSpell(SpellCastKind.ArcaneMissile, SpellBranch.B),
                "Conflicting branch B was accepted after selecting branch A.");
            Require(spells.UpgradeSpell(SpellCastKind.ArcaneMissile, SpellBranch.A) &&
                spells.UpgradeSpell(SpellCastKind.ArcaneMissile, SpellBranch.A),
                "Arcane Missile could not reach level 5.");
            Require(spells.TryGetRuntime(SpellCastKind.ArcaneMissile, out SpellRuntime? arcane) &&
                arcane is not null && arcane.IsMaximumLevel && arcane.Stats.Count == 3,
                "Arcane Missile branch evolution was not applied.");
            Require(spells.UpgradeSpell(SpellCastKind.Fireball, SpellBranch.None) &&
                spells.UpgradeSpell(SpellCastKind.Fireball, SpellBranch.B),
                "Fireball area branch could not be formed.");
            Require(spells.UpgradeSpell(SpellCastKind.FrostLance, SpellBranch.None) &&
                spells.UpgradeSpell(SpellCastKind.FrostLance, SpellBranch.B),
                "Frost Lance fan branch could not be formed.");
            Require(spells.TryGetRuntime(SpellCastKind.Fireball, out SpellRuntime? fireball) &&
                fireball is not null && fireball.Stats.ExplosionRadius > fireball.Definition.ExplosionRadius &&
                spells.TryGetRuntime(SpellCastKind.FrostLance, out SpellRuntime? frost) &&
                frost is not null && frost.Stats.Count == 3,
                "Distinct branch behavior was not reflected in aggregated spell stats.");

            enemies.ClearAll();
            IReadOnlyList<EnemyDefinition> definitions = catalog.All<EnemyDefinition>();
            Require(definitions.Count == 8, "Expected six normal enemies and two elites.");
            for (int index = 0; index < definitions.Count; index++)
            {
                float angle = Mathf.Tau * index / definitions.Count;
                Require(enemies.Spawn(Vector2.FromAngle(angle) * 18.0f, definitions[index]).IsValid,
                    $"Could not spawn {definitions[index].Id}.");
            }
            for (int index = 0; index < definitions.Count; index++)
            {
                Require(enemies.CountArchetype(definitions[index].Archetype) == 1,
                    $"Archetype {definitions[index].Archetype} was not represented.");
            }
            grid.Rebuild();
            enemies._PhysicsProcess(1.0 / 60.0);

            enemies.ClearAll();
            EnemyDefinition swarmer = definitions.Single(item => item.Archetype == EnemyArchetype.Swarmer);
            for (int index = 0; index < 500; index++)
            {
                float angle = Mathf.Tau * index / 500.0f;
                float radius = 18.0f + index % 12;
                Require(enemies.Spawn(Vector2.FromAngle(angle) * radius, swarmer).IsValid,
                    $"Could not create M3 stress enemy {index}.");
            }
            System.Diagnostics.Stopwatch benchmark = System.Diagnostics.Stopwatch.StartNew();
            for (int tick = 0; tick < 120; tick++)
            {
                enemies._PhysicsProcess(1.0 / 60.0);
                grid.Rebuild();
            }
            benchmark.Stop();
            Require(enemies.ActiveCount == 500, "500-enemy stress population changed unexpectedly.");
            CatalystLog.Info("Tests", $"500-enemy/120-tick benchmark: {benchmark.ElapsedMilliseconds} ms.");
            enemies.ClearAll();

            Require(events.StartEventForTests(RunEventKind.SealingRift, Vector2.Zero),
                "Could not start Sealing Rift.");
            events._PhysicsProcess(13.0);
            ResolvePendingUpgrades(controller, upgrades);
            Require(events.State == RunEventState.Inactive,
                "Sealing Rift did not complete and clean up.");

            Require(events.StartEventForTests(RunEventKind.StabilizationCircle, Vector2.Zero),
                "Could not start Stabilization Circle.");
            events._PhysicsProcess(11.0);
            ResolvePendingUpgrades(controller, upgrades);
            Require(events.State == RunEventState.Inactive,
                "Stabilization Circle did not complete and clean up.");

            controller._PhysicsProcess(720.0);
            waves._PhysicsProcess(1.0 / 60.0);
            boss._PhysicsProcess(1.0 / 60.0);
            Require(controller.State == RunState.Playing && boss.HasSpawned && boss.BossHandle.IsValid,
                "The 12-minute run did not transition into the boss encounter.");
            Require(enemies.ApplyDamage(boss.BossHandle, 100000.0f),
                "Could not defeat the milestone boss.");
            Require(controller.State == RunState.Victory && GetTree().Paused,
                "Defeating the milestone boss did not reach victory.");

            CatalystLog.Info("Tests", "M3_CONTENT_OK");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            CatalystLog.Error("Tests", $"M3_CONTENT_FAILED: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void ResolvePendingUpgrades(RunController controller, UpgradeSystem upgrades)
    {
        int selections = 0;
        while (controller.State == RunState.LevelUp && selections < 8)
        {
            Require(upgrades.CurrentChoices.Count == 3,
                "Event reward did not produce three valid upgrade choices.");
            Require(upgrades.SelectChoice(0), "Could not select event reward upgrade.");
            selections++;
        }
        Require(controller.State == RunState.Playing,
            "Run did not resume after resolving event rewards.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
