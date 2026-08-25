using Catalyst.Elements;
using Godot;

namespace Catalyst.Presentation;

/// <summary>
/// Single source of truth for the five-element palette (ART_STYLE_GUIDE § Element Colors).
/// All gameplay VFX, projectile and reaction tinting resolves through here so that Fire is
/// always the same Fire, on a dark additive-friendly background.
/// </summary>
public static class ElementPalette
{
    // Saturated, emissive-friendly colors tuned for a dark arena backdrop.
    public static readonly Color Fire = new(1.00f, 0.24f, 0.10f);
    public static readonly Color Water = new(0.10f, 0.56f, 1.00f);
    public static readonly Color Wind = new(0.42f, 1.00f, 0.70f);
    public static readonly Color Earth = new(0.80f, 0.55f, 0.22f);
    public static readonly Color Lightning = new(1.00f, 0.92f, 0.25f);
    public static readonly Color Frost = new(0.30f, 0.82f, 1.00f);
    public static readonly Color Mark = new(0.90f, 0.32f, 1.00f);
    public static readonly Color Arcane = new(0.42f, 0.82f, 1.00f);

    public static Color For(ElementType element) => element switch
    {
        ElementType.Fire => Fire,
        ElementType.Frost => Frost,
        ElementType.Lightning => Lightning,
        ElementType.Water => Water,
        ElementType.Wind => Wind,
        ElementType.Earth => Earth,
        ElementType.Mark => Mark,
        _ => Arcane
    };

    /// <summary>Hot white steam cloud for Fire + Water, electric gold for Water + Lightning.</summary>
    public static Color Reaction(ReactionKind kind) => kind switch
    {
        ReactionKind.SteamShock => new Color(0.95f, 0.97f, 1.00f),
        ReactionKind.Conduction => new Color(1.00f, 0.96f, 0.35f),
        _ => Lightning
    };

    /// <summary>Desaturated glow used behind colored VFX so tint stays legible.</summary>
    public static Color Glow(Color tint, float amount = 0.25f) =>
        tint.Lerp(Colors.White, amount);
}
