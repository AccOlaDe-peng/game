namespace Catalyst.Elements;

public readonly record struct ChemistryLoadout(
    ElementType Primary,
    ElementType Secondary,
    FusionIdentity Fusion)
{
    public bool Has(ElementType element) => Primary == element || Secondary == element;
}
