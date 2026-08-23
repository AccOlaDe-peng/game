using Catalyst.Pickups;
using Catalyst.Player;
using Catalyst.Run;
using Catalyst.Spells;
using Catalyst.App;
using Catalyst.Meta;
using Catalyst.Weapons;
using Catalyst.Cards;
using Godot;

namespace Catalyst.Upgrades;

public partial class UpgradeSystem : Node
{
    public event Action<IReadOnlyList<UpgradeChoice>, int>? ChoicesPresented;
    public event Action? ChoicesClosed;

    private readonly List<UpgradeDefinition> _generalDefinitions = new();
    private readonly List<UpgradeChoice> _candidateBuffer = new(24);
    private readonly List<UpgradeChoice> _choices = new(3);
    private RunController _run = null!;
    private PlayerProgression _progression = null!;
    private PlayerController _player = null!;
    private PlayerHealth _health = null!;
    private SpellSystem _spells = null!;
    private PickupSystem _pickups = null!;
    private int _pendingLevelUps;
    private bool _highQualitySelection;

    public int RerollsRemaining { get; private set; } = 1;
    public IReadOnlyList<UpgradeChoice> CurrentChoices => _choices;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _run = GetNode<RunController>("../RunController");
        _progression = GetNode<PlayerProgression>("../../WorldRoot/Player/Progression");
        _player = GetNode<PlayerController>("../../WorldRoot/Player");
        _health = GetNode<PlayerHealth>("../../WorldRoot/Player/HealthComponent");
        _spells = GetNode<SpellSystem>("../SpellSystem");
        _pickups = GetNode<PickupSystem>("../PickupSystem");

        ContentCatalog catalog = GetNode<ContentCatalog>("/root/ContentCatalog");
        foreach (UpgradeDefinition definition in catalog.All<UpgradeDefinition>())
        {
            _generalDefinitions.Add(definition);
        }
        _progression.LevelGained += OnLevelGained;
    }

    public bool SelectChoice(int index)
    {
        if (_run.State != RunState.LevelUp || index < 0 || index >= _choices.Count)
        {
            return false;
        }

        Apply(_choices[index]);
        _pendingLevelUps = Math.Max(0, _pendingLevelUps - 1);
        if (_pendingLevelUps > 0)
        {
            PresentChoices();
        }
        else
        {
            ChoicesClosed?.Invoke();
            _run.ResumeFromLevelUp();
        }
        return true;
    }

    public bool Reroll()
    {
        if (_run.State != RunState.LevelUp || RerollsRemaining <= 0)
        {
            return false;
        }
        RerollsRemaining--;
        PresentChoices();
        return true;
    }

    public void RequestBonusUpgrade()
    {
        _pendingLevelUps++;
        _highQualitySelection = true;
        if (_run.State == RunState.Playing && _run.EnterLevelUp())
        {
            PresentChoices();
        }
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_progression))
        {
            _progression.LevelGained -= OnLevelGained;
        }
    }

    private void OnLevelGained(int level)
    {
        _pendingLevelUps++;
        if (_run.State == RunState.Playing && _run.EnterLevelUp())
        {
            PresentChoices();
        }
    }

    private void PresentChoices()
    {
        BuildCandidates();
        if (_highQualitySelection)
        {
            _candidateBuffer.RemoveAll(choice => choice.Kind == UpgradeChoiceKind.General);
        }
        _choices.Clear();
        while (_choices.Count < 3 && _candidateBuffer.Count > 0)
        {
            float totalWeight = _candidateBuffer.Sum(choice => Math.Max(0.01f, choice.Weight));
            float roll = _run.RandomStreams.Upgrade.RandfRange(0.0f, totalWeight);
            int selectedIndex = 0;
            for (int index = 0; index < _candidateBuffer.Count; index++)
            {
                roll -= Math.Max(0.01f, _candidateBuffer[index].Weight);
                if (roll <= 0.0f)
                {
                    selectedIndex = index;
                    break;
                }
            }
            _choices.Add(_candidateBuffer[selectedIndex]);
            _candidateBuffer.RemoveAt(selectedIndex);
        }
        ChoicesPresented?.Invoke(_choices, RerollsRemaining);
    }

    private void BuildCandidates()
    {
        _candidateBuffer.Clear();
        foreach (UpgradeDefinition definition in _generalDefinitions)
        {
            _candidateBuffer.Add(new UpgradeChoice(
                definition.Id.ToString(), definition.DisplayName, definition.Description,
                0.72f, UpgradeChoiceKind.General, definition));
        }

        string characterId = GetNode<SaveService>("/root/SaveService").ActiveCharacter.Id;
        foreach (SpellCastKind kind in CharacterCatalog.CompatibleWeapons(characterId))
        {
            SpellDefinition definition = _spells.GetDefinition(kind);
            if (_spells.TryGetRuntime(kind, out SpellRuntime? runtime) && runtime is not null)
            {
                AddSpellUpgradeCandidates(runtime);
                AddWeaponCardCandidates(runtime);
                AddStandardCardCandidates(runtime);
            }
            else if (_spells.CanAcquireSpell(kind))
            {
                _candidateBuffer.Add(new UpgradeChoice(
                    $"acquire.{definition.Id}",
                    $"获得：{definition.DisplayName}",
                    definition.Description,
                    1.15f,
                    UpgradeChoiceKind.AcquireSpell,
                    Spell: kind));
            }
        }
    }

    private void AddWeaponCardCandidates(SpellRuntime runtime)
    {
        foreach (WeaponCardDefinition card in WeaponCardCatalog.ForWeapon(runtime.Definition.CastKind))
        {
            if (!runtime.CanInstallCard(card))
            {
                continue;
            }
            int nextLevel = runtime.InstalledCards.GetValueOrDefault(card.Id) + 1;
            _candidateBuffer.Add(new UpgradeChoice(
                $"install.{runtime.Definition.Id}.{card.Id}.l{nextLevel}",
                $"{runtime.Definition.DisplayName}｜{card.DisplayName} Lv.{nextLevel}",
                card.Description,
                1.65f,
                UpgradeChoiceKind.InstallWeaponCard,
                Spell: runtime.Definition.CastKind,
                WeaponCard: card));
        }
    }

    private void AddStandardCardCandidates(SpellRuntime runtime)
    {
        foreach (StandardCardDefinition card in StandardCardCatalog.ForWeapon(runtime.Definition.CastKind))
        {
            if (!runtime.CanInstallStandardCard(card))
            {
                continue;
            }
            int nextLevel = runtime.InstalledStandardCards.GetValueOrDefault(card.Id) + 1;
            _candidateBuffer.Add(new UpgradeChoice(
                $"install.{runtime.Definition.Id}.{card.Id}.l{nextLevel}",
                $"{runtime.Definition.DisplayName}｜{card.DisplayName} Lv.{nextLevel}",
                $"[{GetCategoryName(card.Category)} · 功耗 {card.PowerCost}] {card.Description}",
                GetStandardCardWeight(runtime, card),
                UpgradeChoiceKind.InstallStandardCard,
                Spell: runtime.Definition.CastKind,
                StandardCard: card));
        }
    }

    private void AddSpellUpgradeCandidates(SpellRuntime runtime)
    {
        if (runtime.IsMaximumLevel)
        {
            return;
        }

        int nextLevel = runtime.Level + 1;
        if (nextLevel == 3)
        {
            AddSpellUpgrade(runtime, SpellBranch.A);
            AddSpellUpgrade(runtime, SpellBranch.B);
        }
        else
        {
            AddSpellUpgrade(runtime, runtime.SelectedBranch);
        }
    }

    private void AddSpellUpgrade(SpellRuntime runtime, SpellBranch branch)
    {
        int nextLevel = runtime.Level + 1;
        string branchSuffix = branch == SpellBranch.None ? string.Empty : $" · 分支 {branch}";
        _candidateBuffer.Add(new UpgradeChoice(
            $"upgrade.{runtime.Definition.Id}.l{nextLevel}.{branch}",
            $"{runtime.Definition.DisplayName} Lv.{nextLevel}{branchSuffix}",
            GetSpellUpgradeDescription(runtime.Definition.CastKind, nextLevel, branch),
            1.5f,
            UpgradeChoiceKind.UpgradeSpell,
            Spell: runtime.Definition.CastKind,
            Branch: branch));
    }

    private void Apply(UpgradeChoice choice)
    {
        _highQualitySelection = false;
        switch (choice.Kind)
        {
            case UpgradeChoiceKind.AcquireSpell:
                _spells.AcquireSpell(choice.Spell);
                return;
            case UpgradeChoiceKind.UpgradeSpell:
                _spells.UpgradeSpell(choice.Spell, choice.Branch);
                return;
            case UpgradeChoiceKind.InstallWeaponCard when choice.WeaponCard is not null:
                _spells.InstallWeaponCard(choice.Spell, choice.WeaponCard);
                return;
            case UpgradeChoiceKind.InstallStandardCard when choice.StandardCard is not null:
                _spells.InstallStandardCard(choice.Spell, choice.StandardCard);
                return;
            case UpgradeChoiceKind.General when choice.GeneralDefinition is not null:
                ApplyGeneral(choice.GeneralDefinition);
                return;
        }
    }

    private static float GetStandardCardWeight(SpellRuntime runtime, StandardCardDefinition card)
    {
        bool hasAction = runtime.InstalledStandardCards.Keys.Any(id =>
            StandardCardCatalog.Get(id).Category == StandardCardCategory.Action);
        bool hasTrigger = runtime.InstalledStandardCards.Keys.Any(id =>
            StandardCardCatalog.Get(id).Category == StandardCardCategory.Trigger);
        if ((card.Category == StandardCardCategory.Action && hasTrigger) ||
            (card.Category == StandardCardCategory.Trigger && hasAction))
        {
            return 1.9f;
        }
        return card.Category == StandardCardCategory.Payload ? 1.7f : 1.35f;
    }

    private static string GetCategoryName(StandardCardCategory category) => category switch
    {
        StandardCardCategory.Behavior => "行为",
        StandardCardCategory.Payload => "载荷",
        StandardCardCategory.Action => "动作",
        StandardCardCategory.Trigger => "触发",
        StandardCardCategory.Reaction => "反应",
        _ => category.ToString()
    };

    private void ApplyGeneral(UpgradeDefinition choice)
    {
        switch (choice.Effect)
        {
            case UpgradeEffect.SpellDamage:
                _spells.DamageMultiplier *= choice.Multiplier;
                break;
            case UpgradeEffect.SpellCooldown:
                _spells.CooldownMultiplier *= choice.Multiplier;
                break;
            case UpgradeEffect.MoveSpeed:
                _player.MoveSpeed *= choice.Multiplier;
                break;
            case UpgradeEffect.ProjectileSpeed:
                _spells.ProjectileSpeedMultiplier *= choice.Multiplier;
                break;
            case UpgradeEffect.MaximumHealth:
                _health.IncreaseMaximumHealth(choice.Multiplier);
                break;
            case UpgradeEffect.PickupRadius:
                _pickups.IncreaseAttractionRadius(choice.Multiplier);
                break;
        }
    }

    private static string GetSpellUpgradeDescription(
        SpellCastKind kind,
        int level,
        SpellBranch branch)
    {
        if (level == 2 || level == 4)
        {
            return "伤害提高 18%，冷却时间缩短 5%";
        }

        bool evolved = level == 5;
        return (kind, branch) switch
        {
            (SpellCastKind.ArcaneMissile, SpellBranch.A) => evolved
                ? "进化：同时发射 3 枚分裂飞弹"
                : "同时发射 2 枚分裂飞弹",
            (SpellCastKind.ArcaneMissile, SpellBranch.B) => evolved
                ? "进化：获得强化火焰附魔，每次施加 2 层燃烧"
                : "获得火焰附魔",
            (SpellCastKind.Fireball, SpellBranch.A) => evolved
                ? "进化：同时发射 3 枚小火球"
                : "同时发射 2 枚小火球",
            (SpellCastKind.Fireball, SpellBranch.B) => evolved
                ? "进化：爆炸范围提高 80%"
                : "爆炸范围提高 45%",
            (SpellCastKind.FrostLance, SpellBranch.A) => evolved
                ? "进化：额外穿透 4 个目标"
                : "额外穿透 2 个目标",
            (SpellCastKind.FrostLance, SpellBranch.B) => evolved
                ? "进化：释放 5 枚扇形冰锥"
                : "释放 3 枚扇形冰锥",
            (SpellCastKind.ChainLightning, SpellBranch.A) => evolved
                ? "进化：额外弹射 4 次"
                : "额外弹射 2 次",
            (SpellCastKind.ChainLightning, SpellBranch.B) => evolved
                ? "进化：弹射范围提高 4 米并施加 2 层感电"
                : "弹射范围提高 2 米",
            (SpellCastKind.OrbitOrb, SpellBranch.A) => evolved
                ? "进化：生成 3 枚环绕法球"
                : "生成 2 枚环绕法球",
            (SpellCastKind.OrbitOrb, SpellBranch.B) => evolved
                ? "进化：轨道扩大并施加 2 层寒冷"
                : "扩大轨道并获得冰霜附魔",
            _ => "伤害提高 18%，冷却时间缩短 5%"
        };
    }
}
