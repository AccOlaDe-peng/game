namespace Catalyst.Elements;

public readonly record struct ReactionResult(
    ReactionRule Rule,
    int FirstStacksConsumed,
    int SecondStacksConsumed,
    float Damage)
{
    public int TotalStacksConsumed => FirstStacksConsumed + SecondStacksConsumed;
}
