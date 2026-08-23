using Catalyst.Elements;
using Catalyst.Weapons;
using Catalyst.Cards;

namespace Catalyst.Spells;

public sealed class SpellRuntime
{
    public SpellDefinition Definition { get; }
    public int Level { get; private set; } = 1;
    public SpellBranch SelectedBranch { get; private set; }
    public float CooldownRemaining { get; set; }
    public SpellStats Stats { get; private set; }
    public IReadOnlyDictionary<string, int> InstalledCards => _installedCards;
    public IReadOnlyDictionary<string, int> InstalledStandardCards => _installedStandardCards;
    public int PowerCapacity => 12;
    public int CurrentPower =>
        _installedCards.Sum(pair => WeaponCardCatalog.Get(pair.Key).PowerCost * pair.Value) +
        _installedStandardCards.Sum(pair => StandardCardCatalog.Get(pair.Key).PowerCost * pair.Value);

    private readonly Dictionary<string, int> _installedCards = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _installedStandardCards = new(StringComparer.Ordinal);

    public bool IsMaximumLevel => Level >= 5;

    public SpellRuntime(SpellDefinition definition)
    {
        Definition = definition;
        Stats = BuildStats();
    }

    public bool CanUpgrade(SpellBranch branch)
    {
        if (IsMaximumLevel)
        {
            return false;
        }
        int nextLevel = Level + 1;
        if (nextLevel == 3)
        {
            return branch is SpellBranch.A or SpellBranch.B;
        }
        return branch == SpellBranch.None || branch == SelectedBranch;
    }

    public bool Upgrade(SpellBranch branch)
    {
        if (!CanUpgrade(branch))
        {
            return false;
        }
        Level++;
        if (Level == 3)
        {
            SelectedBranch = branch;
        }
        Stats = BuildStats();
        return true;
    }

    public bool CanInstallCard(WeaponCardDefinition card) =>
        card.Weapon == Definition.CastKind &&
        _installedCards.GetValueOrDefault(card.Id) < card.MaximumLevel &&
        CurrentPower + card.PowerCost <= PowerCapacity;

    public bool InstallCard(WeaponCardDefinition card)
    {
        if (!CanInstallCard(card))
        {
            return false;
        }
        _installedCards[card.Id] = _installedCards.GetValueOrDefault(card.Id) + 1;
        Stats = BuildStats();
        return true;
    }

    public bool CanInstallStandardCard(StandardCardDefinition card)
    {
        if (!card.IsCompatible(Definition.CastKind) ||
            _installedStandardCards.GetValueOrDefault(card.Id) >= card.MaximumLevel ||
            CurrentPower + card.PowerCost > PowerCapacity)
        {
            return false;
        }
        if (card.Category == StandardCardCategory.Trigger &&
            !_installedStandardCards.Keys.Any(id =>
                StandardCardCatalog.Get(id).Category == StandardCardCategory.Action))
        {
            return false;
        }
        if (card.Effect is StandardCardEffect.Shatter or StandardCardEffect.OnFreeze &&
            Stats.Fusion != FusionIdentity.Ice)
        {
            return false;
        }
        if (card.Effect == StandardCardEffect.BurnPropagation &&
            Stats.Element != ElementType.Fire && Stats.SecondaryElement != ElementType.Fire)
        {
            return false;
        }
        return true;
    }

    public bool InstallStandardCard(StandardCardDefinition card)
    {
        if (!CanInstallStandardCard(card))
        {
            return false;
        }
        _installedStandardCards[card.Id] = _installedStandardCards.GetValueOrDefault(card.Id) + 1;
        Stats = BuildStats();
        return true;
    }

    public bool HasStandardEffect(StandardCardEffect effect) =>
        _installedStandardCards.Keys.Any(id => StandardCardCatalog.Get(id).Effect == effect);

    private SpellStats BuildStats()
    {
        float damage = Definition.BaseDamage * (1.0f + (Level - 1) * 0.18f);
        float cooldown = Definition.Cooldown * MathF.Pow(0.95f, Level - 1);
        int count = 1;
        float spread = 0.0f;
        int maximumHits = Math.Max(1, Definition.MaximumHits);
        float explosionRadius = Definition.ExplosionRadius;
        int chainCount = Definition.ChainCount;
        float chainRange = Definition.ChainRange;
        float orbitRadius = Math.Max(2.8f, Definition.Range);
        var element = Definition.Element;
        int stacks = Definition.ElementStacks;

        bool evolved = Level >= 5;
        if (Level >= 3)
        {
            switch (Definition.CastKind, SelectedBranch)
            {
                case (SpellCastKind.ArcaneMissile, SpellBranch.A):
                    count = evolved ? 3 : 2;
                    spread = 9.0f;
                    break;
                case (SpellCastKind.ArcaneMissile, SpellBranch.B):
                    element = ElementType.Fire;
                    stacks = evolved ? 2 : 1;
                    break;
                case (SpellCastKind.Fireball, SpellBranch.A):
                    count = evolved ? 3 : 2;
                    spread = 13.0f;
                    damage *= 0.82f;
                    break;
                case (SpellCastKind.Fireball, SpellBranch.B):
                    explosionRadius *= evolved ? 1.8f : 1.45f;
                    break;
                case (SpellCastKind.FrostLance, SpellBranch.A):
                    maximumHits += evolved ? 4 : 2;
                    break;
                case (SpellCastKind.FrostLance, SpellBranch.B):
                    count = evolved ? 5 : 3;
                    spread = 12.0f;
                    damage *= 0.75f;
                    break;
                case (SpellCastKind.ChainLightning, SpellBranch.A):
                    chainCount += evolved ? 4 : 2;
                    break;
                case (SpellCastKind.ChainLightning, SpellBranch.B):
                    chainRange += evolved ? 4.0f : 2.0f;
                    stacks = evolved ? 2 : stacks;
                    break;
                case (SpellCastKind.OrbitOrb, SpellBranch.A):
                    count = evolved ? 3 : 2;
                    break;
                case (SpellCastKind.OrbitOrb, SpellBranch.B):
                    orbitRadius += evolved ? 3.0f : 1.5f;
                    element = ElementType.Frost;
                    stacks = evolved ? 2 : 1;
                    break;
            }
        }

        foreach ((string cardId, int cardLevel) in _installedCards)
        {
            WeaponCardDefinition card = WeaponCardCatalog.Get(cardId);
            float amount = card.Value * cardLevel;
            switch (card.Effect)
            {
                case WeaponCardEffect.Damage:
                    damage *= 1.0f + amount;
                    break;
                case WeaponCardEffect.Cooldown:
                    cooldown *= Math.Max(0.35f, 1.0f - amount);
                    break;
                case WeaponCardEffect.ProjectileSpeed:
                    break;
                case WeaponCardEffect.ProjectileCount:
                    count += (int)amount;
                    break;
                case WeaponCardEffect.MaximumHits:
                    maximumHits += (int)amount;
                    break;
                case WeaponCardEffect.ExplosionRadius:
                    explosionRadius *= 1.0f + amount;
                    break;
                case WeaponCardEffect.BounceCount:
                    chainCount += (int)amount;
                    maximumHits += (int)amount;
                    break;
                case WeaponCardEffect.BounceRange:
                    chainRange *= 1.0f + amount;
                    break;
                case WeaponCardEffect.Lifetime:
                    break;
            }
        }

        foreach ((string cardId, int cardLevel) in _installedStandardCards)
        {
            StandardCardDefinition card = StandardCardCatalog.Get(cardId);
            switch (card.Effect)
            {
                case StandardCardEffect.Pierce:
                    maximumHits += 2 * cardLevel;
                    break;
                case StandardCardEffect.Bounce:
                    chainCount += 2 * cardLevel;
                    maximumHits += 2 * cardLevel;
                    break;
                case StandardCardEffect.Multishot:
                    count += 2 * cardLevel;
                    spread = Math.Max(spread, 9.0f);
                    break;
                case StandardCardEffect.Explosion:
                    explosionRadius = Math.Max(explosionRadius, 2.4f + 0.5f * cardLevel);
                    break;
                case StandardCardEffect.ProjectileCopy:
                    count += cardLevel;
                    break;
                case StandardCardEffect.FirePayload:
                    element = ElementType.Fire;
                    stacks = 1;
                    break;
                case StandardCardEffect.WaterPayload:
                    element = ElementType.Water;
                    stacks = 1;
                    break;
                case StandardCardEffect.WindPayload:
                    element = ElementType.Wind;
                    stacks = 1;
                    break;
                case StandardCardEffect.EarthPayload:
                    element = ElementType.Earth;
                    stacks = 1;
                    break;
                case StandardCardEffect.LightningPayload:
                    element = ElementType.Lightning;
                    stacks = 1;
                    break;
            }
        }

        float projectileSpeed = Definition.ProjectileSpeed;
        float lifetime = Definition.Lifetime;
        foreach ((string cardId, int cardLevel) in _installedCards)
        {
            WeaponCardDefinition card = WeaponCardCatalog.Get(cardId);
            if (card.Effect == WeaponCardEffect.ProjectileSpeed)
                projectileSpeed *= 1.0f + card.Value * cardLevel;
            else if (card.Effect == WeaponCardEffect.Lifetime)
                lifetime *= 1.0f + card.Value * cardLevel;
        }

        List<ElementType> payloads = new();
        if (Definition.Element != ElementType.None)
        {
            payloads.Add(Definition.Element);
        }
        foreach (string cardId in _installedStandardCards.Keys)
        {
            ElementType payload = StandardCardCatalog.Get(cardId).Effect switch
            {
                StandardCardEffect.FirePayload => ElementType.Fire,
                StandardCardEffect.WaterPayload => ElementType.Water,
                StandardCardEffect.WindPayload => ElementType.Wind,
                StandardCardEffect.EarthPayload => ElementType.Earth,
                StandardCardEffect.LightningPayload => ElementType.Lightning,
                StandardCardEffect.MarkPayload => ElementType.Mark,
                _ => ElementType.None
            };
            if (payload != ElementType.None) payloads.Add(payload);
        }
        ChemistryLoadout chemistry = ElementChemistry.Compile(payloads);
        element = chemistry.Primary;
        if (element != ElementType.None && stacks <= 0) stacks = 1;

        return new SpellStats(
            damage, cooldown, Definition.Range, projectileSpeed,
            lifetime, element, stacks, count, spread,
            maximumHits, explosionRadius, chainCount, chainRange, orbitRadius,
            chemistry.Secondary, chemistry.Fusion);
    }
}
