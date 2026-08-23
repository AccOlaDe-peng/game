using Catalyst.App;
using Catalyst.Boss;
using Catalyst.Combat;
using Catalyst.Elements;
using Catalyst.Enemies;
using Catalyst.Run;
using Catalyst.Spells;
using Catalyst.UI;
using Catalyst.Waves;
using Godot;

namespace Catalyst.Tests;

public partial class M4CompletionRunner : Node
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
            SettingsService settings = GetNode<SettingsService>("/root/SettingsService");
            SaveService saves = GetNode<SaveService>("/root/SaveService");
            Require(settings.Current.SchemaVersion == 2,
                "Settings schema was not migrated to M4.");

            RunRoot run = RunScene.Instantiate<RunRoot>();
            AddChild(run);
            RunController controller = run.GetNode<RunController>("SimulationRoot/RunController");
            WaveDirector waves = run.GetNode<WaveDirector>("SimulationRoot/WaveDirector");
            SpellSystem spells = run.GetNode<SpellSystem>("SimulationRoot/SpellSystem");
            EnemySystem enemies = run.GetNode<EnemySystem>("SimulationRoot/EntitySystem");
            CombatSystem combat = run.GetNode<CombatSystem>("SimulationRoot/CombatSystem");
            BossController boss = run.GetNode<BossController>("SimulationRoot/BossController");
            RunResults results = run.GetNode<RunResults>("UI/RunResults");
            SpringArm3D springArm = run.GetNode<SpringArm3D>(
                "WorldRoot/Player/SpringArm3D");
            Require(springArm.SpringLength >= 20.0f && springArm.Position.Y >= 1.0f &&
                springArm.CollisionMask == 0,
                "Top-down camera arm can collapse into the player or arena floor.");
            waves.EnemyLimit = 0;
            spells.Enabled = false;

            Require(boss.SpawnBoss(), "Boss could not be spawned for M4 validation.");
            Require(enemies.TryGet(boss.BossHandle, out EnemyState state) &&
                state.Archetype == EnemyArchetype.BossAshenColossus && state.MaxHealth >= 3000.0f,
                "Boss was not registered through the shared enemy target adapter.");

            EntityHandle handle = boss.BossHandle;
            ElementRuntimeState prepared = state.Elements;
            prepared.Water = new ElementState { Stacks = 1, RemainingDuration = 0.5f };
            enemies.SetElementRuntime(handle, prepared);
            combat.Submit(new DamageContext(
                handle, "test.fire", 10.0f, ElementType.Fire, 1,
                DamageFlags.CanTriggerReaction));
            combat.Flush();
            Require(boss.Stagger > 0.0f,
                "Element reaction did not add boss stagger.");

            boss.AddStagger(1000.0f);
            boss._PhysicsProcess(1.0 / 60.0);
            Require(boss.IsExposed, "Boss did not enter exposed weakness after stagger.");
            Require(enemies.TryGet(handle, out state), "Boss vanished before damage validation.");
            float healthBefore = state.Health;
            enemies.ApplyDamage(handle, 10.0f);
            Require(enemies.TryGet(handle, out state) &&
                healthBefore - state.Health >= 15.9f,
                "Exposed weakness did not amplify damage.");

            enemies.ApplyDamage(handle, 100000.0f);
            Require(controller.State == RunState.Victory && boss.IsDefeated,
                "Boss defeat did not complete the run.");
            Require(results.Visible, "Results screen was not shown after victory.");
            Require(saves.Profile.SchemaVersion == 3 &&
                saves.Profile.LastRun is { BossDefeated: true, Victory: true } &&
                saves.Profile.BossDefeated &&
                saves.Profile.UnlockedCharacterIds.Contains("character.rift_engineer") &&
                saves.Profile.UnlockedWeaponIds.Contains("weapon.element_mine"),
                "Versioned profile did not persist the completed boss run.");

            Catalyst.Core.CatalystLog.Info("Tests", "M4_COMPLETION_OK");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            Catalyst.Core.CatalystLog.Error("Tests", $"M4_COMPLETION_FAILED: {exception}");
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
