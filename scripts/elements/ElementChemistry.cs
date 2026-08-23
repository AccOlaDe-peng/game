namespace Catalyst.Elements;

public static class ElementChemistry
{
    private static readonly (ElementType A, ElementType B, FusionIdentity Fusion)[] Priority =
    {
        (ElementType.Water, ElementType.Lightning, FusionIdentity.ConductiveLightning),
        (ElementType.Fire, ElementType.Water, FusionIdentity.Steam),
        (ElementType.Water, ElementType.Wind, FusionIdentity.Ice),
        (ElementType.Fire, ElementType.Wind, FusionIdentity.Firestorm),
        (ElementType.Fire, ElementType.Earth, FusionIdentity.Lava),
        (ElementType.Fire, ElementType.Lightning, FusionIdentity.Plasma),
        (ElementType.Water, ElementType.Earth, FusionIdentity.Mud),
        (ElementType.Wind, ElementType.Lightning, FusionIdentity.Storm),
        (ElementType.Wind, ElementType.Earth, FusionIdentity.Sandstorm),
        (ElementType.Earth, ElementType.Lightning, FusionIdentity.CrystalMagnet)
    };

    private static readonly ElementType[] SinglePriority =
    {
        ElementType.Fire, ElementType.Water, ElementType.Wind, ElementType.Earth,
        ElementType.Lightning, ElementType.Frost, ElementType.Mark
    };

    public static ChemistryLoadout Compile(IEnumerable<ElementType> elements)
    {
        HashSet<ElementType> set = new(elements.Where(element => element != ElementType.None));
        foreach ((ElementType a, ElementType b, FusionIdentity fusion) in Priority)
        {
            if (set.Contains(a) && set.Contains(b))
            {
                return new ChemistryLoadout(a, b, fusion);
            }
        }
        ElementType primary = SinglePriority.FirstOrDefault(set.Contains);
        return new ChemistryLoadout(primary, ElementType.None, FusionIdentity.None);
    }

    public static IReadOnlyList<FusionIdentity> AllFusions => Priority.Select(item => item.Fusion).ToArray();
}
