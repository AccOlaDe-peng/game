using Catalyst.App;
using Catalyst.Core;
using Catalyst.Enemies;
using Catalyst.Elements;
using Catalyst.Projectiles;
using Godot;

namespace Catalyst.Tests;

/// <summary>
/// Visual Target showcase (ART_STYLE_GUIDE § Visual Target): assembles one full arena frame
/// with ~40 enemies across archetypes, one elite, a five-element fan of projectiles and an
/// element reaction, for the art review screenshot. Run it from the editor and capture
/// docs/art/visual_target_v1.png. Headless runs auto-quit after a short showcase.
/// </summary>
public partial class VisualTargetRunner : Node
{
    [Export]
    public PackedScene RunScene { get; set; } = null!;

    private EnemySystem? _enemies;
    private ProjectileSystem? _projectiles;
    private CharacterBody3D? _player;
    private ElementType[] _elements =
    {
        ElementType.Fire,
        ElementType.Water,
        ElementType.Wind,
        ElementType.Earth,
        ElementType.Lightning
    };
    private int _frames;
    private int _secondBurst;
    private bool _headless;

    public override void _Ready()
    {
        // Keep counting even when the run pauses on level-up, so headless validation always quits.
        ProcessMode = ProcessModeEnum.Always;
        _headless = DisplayServer.GetName() == "headless";
        Node run = RunScene.Instantiate();
        AddChild(run);
        CallDeferred(MethodName.Setup);
    }

    private void Setup()
    {
        _enemies = GetNode<EnemySystem>("RunRoot/SimulationRoot/EntitySystem");
        _projectiles = GetNode<ProjectileSystem>("RunRoot/SimulationRoot/ProjectileSystem");
        _player = GetNode<CharacterBody3D>("RunRoot/WorldRoot/Player");

        var catalog = GetNode<ContentCatalog>("/root/ContentCatalog");
        SpawnPopulation(catalog);
        FireElementFan(Vector3.Zero, 6.0f);

        CatalystLog.Info("VisualTarget", $"Population spawned, elements: " +
            $"{string.Join(',', _elements.Select(e => e.ToString()))}.");
    }

    private void SpawnPopulation(ContentCatalog catalog)
    {
        var definitions = catalog.All<EnemyDefinition>()
            .ToDictionary(definition => definition.Id, definition => definition);

        string[] composition =
        {
            "enemy.swarmer", "enemy.swarmer", "enemy.swarmer", "enemy.swarmer",
            "enemy.swarmer", "enemy.swarmer", "enemy.swarmer", "enemy.swarmer",
            "enemy.hunter", "enemy.hunter", "enemy.hunter", "enemy.hunter", "enemy.hunter",
            "enemy.heavy", "enemy.heavy", "enemy.heavy",
            "enemy.caster", "enemy.caster", "enemy.caster", "enemy.caster",
            "enemy.exploder", "enemy.exploder", "enemy.exploder",
            "enemy.summoner", "enemy.summoner", "enemy.summoner",
            "enemy.element_guard", "enemy.element_guard"
        };

        int index = 0;
        foreach (string id in composition)
        {
            if (!definitions.TryGetValue(id, out EnemyDefinition? definition))
            {
                CatalystLog.Warning("VisualTarget", $"Missing enemy definition {id}.");
                continue;
            }
            float angle = Mathf.Tau * index / 34.0f + 0.35f;
            float radius = 9.0f + index % 5 * 2.4f;
            _enemies!.Spawn(new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius), definition);
            index++;
        }

        if (definitions.TryGetValue("enemy.elite_charger", out EnemyDefinition? elite))
        {
            _enemies!.Spawn(new Vector2(7.0f, 3.0f), elite);
        }
        else
        {
            CatalystLog.Warning("VisualTarget", "Missing elite_charger definition.");
        }
    }

    private void FireElementFan(Vector3 from, float speed)
    {
        if (_projectiles is null || _player is null)
        {
            return;
        }
        Vector2 origin = new(from.X, from.Z);
        int perElement = 6;
        for (int elementIndex = 0; elementIndex < _elements.Length; elementIndex++)
        {
            ElementType element = _elements[elementIndex];
            float baseAngle = -1.1f + elementIndex * 0.55f;
            for (int shot = 0; shot < perElement; shot++)
            {
                float angle = baseAngle + (shot - perElement * 0.5f) * 0.07f;
                _projectiles.Spawn(origin, Vector2.FromAngle(angle), 6.0f, speed,
                    2.4f, "weapon.visual_target", element, 3, 2);
            }
        }
    }

    /// <summary>Wet then ignite the front crowd so SteamShock fires through the normal pipeline.</summary>
    private void FireReactionBurst()
    {
        if (_projectiles is null)
        {
            return;
        }
        for (int pass = 0; pass < 2; pass++)
        {
            ElementType element = pass == 0 ? ElementType.Water : ElementType.Fire;
            for (int shot = -4; shot <= 4; shot++)
            {
                float angle = -0.25f + shot * 0.06f;
                _projectiles.Spawn(Vector2.Zero, Vector2.FromAngle(angle), 5.0f, 7.5f,
                    2.0f, "weapon.visual_target", element, 4, 1);
            }
        }
    }

    public override void _Process(double delta)
    {
        _frames++;
        if (_headless)
        {
            if (_frames == 240)
            {
                // Focused water then fire into the front crowd to force a SteamShock reaction.
                FireReactionBurst();
            }
            if (_frames >= 360)
            {
                CatalystLog.Info("VisualTarget",
                    $"Active enemies={_enemies?.ActiveCount}, projectiles={_projectiles?.ActiveCount}");
                GetTree().Quit(0);
            }
            return;
        }

        // In the editor keep the frame alive: re-fire a fan every few seconds so the shot is readable.
        _secondBurst--;
        if (_secondBurst <= 0 && _player is not null && _projectiles is not null &&
            _projectiles.ActiveCount < 40)
        {
            FireElementFan(_player.GlobalPosition, 6.0f);
            _secondBurst = 180;
        }
    }
}
