using Catalyst.Core;
using Catalyst.Elements;
using Catalyst.Enemies;
using Catalyst.Events;
using Catalyst.Spells;
using Catalyst.Upgrades;
using Catalyst.Waves;
using Godot;

namespace Catalyst.App;

public partial class ContentCatalog : Node
{
    private static readonly string[] ContentPaths =
    {
        "res://resources/spells/arcane_missile.tres",
        "res://resources/spells/fireball.tres",
        "res://resources/spells/frost_lance.tres",
        "res://resources/spells/chain_lightning.tres",
        "res://resources/spells/orbit_orb.tres",
        "res://resources/spells/ricochet_disc.tres",
        "res://resources/spells/element_mine.tres",
        "res://resources/enemies/swarmer.tres",
        "res://resources/enemies/hunter.tres",
        "res://resources/enemies/heavy.tres",
        "res://resources/enemies/caster.tres",
        "res://resources/enemies/exploder.tres",
        "res://resources/enemies/summoner.tres",
        "res://resources/enemies/elite_charger.tres",
        "res://resources/enemies/element_guard.tres",
        "res://resources/upgrades/arcane_power.tres",
        "res://resources/upgrades/rapid_casting.tres",
        "res://resources/upgrades/swift_steps.tres",
        "res://resources/upgrades/accelerated_missiles.tres",
        "res://resources/upgrades/vitality.tres",
        "res://resources/upgrades/energy_magnet.tres",
        "res://resources/waves/phase_early.tres",
        "res://resources/waves/phase_growth.tres",
        "res://resources/waves/phase_pressure.tres",
        "res://resources/waves/phase_finale.tres",
        "res://resources/events/sealing_rift.tres",
        "res://resources/events/stabilization_circle.tres"
    };

    private readonly Dictionary<StringName, ContentDefinition> _definitions = new();
    private readonly List<string> _validationErrors = new();

    public int Count => _definitions.Count;
    public bool IsValid => _validationErrors.Count == 0;
    public IReadOnlyList<string> ValidationErrors => _validationErrors;

    public override void _Ready()
    {
        foreach (string path in ContentPaths)
        {
            ContentDefinition? definition = ResourceLoader.Load<ContentDefinition>(path);
            if (definition is null)
            {
                _validationErrors.Add($"Could not load content resource: {path}");
                continue;
            }
            Register(definition);
        }
        ValidateDefinitions();

        if (IsValid)
        {
            CatalystLog.Info("Content", $"Catalog validated {Count} definitions.");
        }
        else
        {
            foreach (string error in _validationErrors)
            {
                CatalystLog.Error("Content", error);
            }
        }
    }

    public bool Register(ContentDefinition definition)
    {
        if (definition.Id.IsEmpty)
        {
            _validationErrors.Add($"{definition.GetType().Name} has an empty id.");
            return false;
        }
        if (!_definitions.TryAdd(definition.Id, definition))
        {
            _validationErrors.Add($"Duplicate content id: {definition.Id}");
            return false;
        }
        return true;
    }

    public bool TryGet<T>(StringName id, out T? definition)
        where T : ContentDefinition
    {
        if (_definitions.TryGetValue(id, out ContentDefinition? value) && value is T typed)
        {
            definition = typed;
            return true;
        }
        definition = null;
        return false;
    }

    public IReadOnlyList<T> All<T>() where T : ContentDefinition =>
        _definitions.Values.OfType<T>().OrderBy(value => value.Id.ToString(), StringComparer.Ordinal).ToArray();

    private void ValidateDefinitions()
    {
        HashSet<ReactionKey> reactionKeys = new();
        foreach (ReactionDefinition reaction in All<ReactionDefinition>())
        {
            if (reaction.First == ElementType.None || reaction.Second == ElementType.None ||
                reaction.First == reaction.Second)
            {
                _validationErrors.Add($"{reaction.Id}: reaction elements must be different and non-empty.");
            }
            if (reaction.AutomaticThreshold < reaction.CatalyzeThreshold)
            {
                _validationErrors.Add($"{reaction.Id}: automatic threshold is below catalyze threshold.");
            }
            if (!reactionKeys.Add(new ReactionKey(reaction.First, reaction.Second)))
            {
                _validationErrors.Add($"{reaction.Id}: duplicate unordered reaction pair.");
            }
        }

        HashSet<SpellCastKind> spellKinds = new();
        foreach (SpellDefinition spell in All<SpellDefinition>())
        {
            if (spell.BaseDamage < 0.0f || spell.Cooldown <= 0.0f || spell.Range <= 0.0f)
            {
                _validationErrors.Add($"{spell.Id}: invalid damage, cooldown or range.");
            }
            if (!spellKinds.Add(spell.CastKind))
            {
                _validationErrors.Add($"{spell.Id}: duplicate spell cast kind {spell.CastKind}.");
            }
        }

        foreach (EnemyDefinition enemy in All<EnemyDefinition>())
        {
            if (enemy.MaxHealth <= 0.0f || enemy.MoveSpeed <= 0.0f || enemy.MaximumAlive <= 0)
            {
                _validationErrors.Add($"{enemy.Id}: invalid health, speed or simultaneous limit.");
            }
        }

        float previousStart = -1.0f;
        foreach (WaveDefinition wave in All<WaveDefinition>().OrderBy(wave => wave.StartsAtSeconds))
        {
            if (wave.StartsAtSeconds <= previousStart || wave.SpawnInterval <= 0.0f || wave.BatchSize <= 0)
            {
                _validationErrors.Add($"{wave.Id}: invalid or duplicate wave phase timing.");
            }
            previousStart = wave.StartsAtSeconds;
        }

        foreach (RunEventDefinition runEvent in All<RunEventDefinition>())
        {
            if (runEvent.Radius <= 0.0f || runEvent.RequiredProgressSeconds <= 0.0f ||
                runEvent.TimeLimitSeconds < runEvent.RequiredProgressSeconds)
            {
                _validationErrors.Add($"{runEvent.Id}: invalid event radius or timing.");
            }
        }
    }
}
