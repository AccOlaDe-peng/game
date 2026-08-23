using Catalyst.Combat;
using Catalyst.Core;
using Catalyst.Elements;
using Catalyst.Enemies;
using Catalyst.Player;
using Catalyst.Run;
using Catalyst.Spatial;
using Catalyst.Spells;
using Catalyst.Waves;
using Godot;

namespace Catalyst.Tests;

public partial class ElementReactionRunner : Node
{
    [Export] public PackedScene RunScene { get; set; } = null!;

    public override void _Ready() => CallDeferred(MethodName.RunTests);

    private void RunTests()
    {
        try
        {
            RunRoot run = RunScene.Instantiate<RunRoot>();
            AddChild(run);
            run.GetNode<SpellSystem>("SimulationRoot/SpellSystem").Enabled = false;
            run.GetNode<WaveDirector>("SimulationRoot/WaveDirector").EnemyLimit = 0;
            EnemySystem enemies = run.GetNode<EnemySystem>("SimulationRoot/EntitySystem");
            SpatialGrid grid = run.GetNode<SpatialGrid>("SimulationRoot/SpatialGrid");
            CombatSystem combat = run.GetNode<CombatSystem>("SimulationRoot/CombatSystem");
            RunStatistics statistics = run.GetNode<RunStatistics>("SimulationRoot/RunStatistics");
            CatalyzeAbility catalyze = run.GetNode<CatalyzeAbility>("WorldRoot/Player/CatalyzeAbility");
            enemies.ClearAll();

            EntityHandle steam = enemies.Spawn(new Vector2(-10, 0));
            SetStates(enemies, steam, water: 1);
            grid.Rebuild();
            Submit(combat, steam, "test.fire", 10, ElementType.Fire);
            Require(statistics.ReactionsByKind.GetValueOrDefault(ReactionKind.SteamShock) == 1,
                "Steam Shock did not replace the legacy thermal reaction.");

            EntityHandle conductorA = enemies.Spawn(new Vector2(8, 0));
            EntityHandle conductorB = enemies.Spawn(new Vector2(10, 0));
            EntityHandle conductorC = enemies.Spawn(new Vector2(12, 0));
            SetStates(enemies, conductorA, water: 1);
            SetStates(enemies, conductorB, water: 1);
            SetStates(enemies, conductorC, water: 1);
            grid.Rebuild();
            float beforeB = GetHealth(enemies, conductorB);
            Submit(combat, conductorA, "test.lightning", 4, ElementType.Lightning);
            Require(statistics.ReactionsByKind.GetValueOrDefault(ReactionKind.Conduction) == 1,
                "Conduction did not trigger exactly once.");
            Require(GetHealth(enemies, conductorB) < beforeB,
                "Conduction did not damage another wet network target.");

            EntityHandle mineTarget = enemies.Spawn(new Vector2(20, 0));
            SetStates(enemies, mineTarget, water: 1);
            grid.Rebuild();
            int beforeMine = statistics.ReactionCount;
            Submit(combat, mineTarget, "weapon.element_mine", 8, ElementType.Fire);
            Require(statistics.ReactionCount == beforeMine,
                "Element Mine incorrectly triggered an automatic cross reaction.");

            EntityHandle catalystTarget = enemies.Spawn(new Vector2(0, 15));
            SetStates(enemies, catalystTarget, water: 1, fire: 1);
            grid.Rebuild();
            catalyze.ResetCooldown();
            Require(catalyze.TriggerAt(new Vector2(0, 15)) >= 1,
                "Catalyze did not select the prepared chemistry target.");
            combat.Flush();
            Require(statistics.ReactionsByKind.GetValueOrDefault(ReactionKind.SteamShock) == 2,
                "Catalyze did not resolve an already prepared Steam Shock.");

            CatalystLog.Info("Tests", "ELEMENT_REACTIONS_OK");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            CatalystLog.Error("Tests", $"ELEMENT_REACTIONS_FAILED: {exception}");
            GetTree().Quit(1);
        }
    }

    private static void SetStates(EnemySystem enemies, EntityHandle target, int water = 0, int fire = 0)
    {
        Require(enemies.TryGet(target, out EnemyState enemy), "Could not prepare element state.");
        ElementRuntimeState state = enemy.Elements;
        state.Water = new ElementState { Stacks = water, RemainingDuration = 2.0f };
        state.Fire = new ElementState { Stacks = fire, RemainingDuration = 2.2f };
        enemies.SetElementRuntime(target, state);
    }

    private static void Submit(CombatSystem combat, EntityHandle target, string source,
        float damage, ElementType element)
    {
        combat.Submit(new DamageContext(target, source, damage, element, 1,
            DamageFlags.CanTriggerReaction));
        combat.Flush();
    }

    private static float GetHealth(EnemySystem enemies, EntityHandle target) =>
        enemies.TryGet(target, out EnemyState enemy) ? enemy.Health : 0.0f;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
