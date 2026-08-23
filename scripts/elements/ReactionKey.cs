namespace Catalyst.Elements;

public readonly struct ReactionKey : IEquatable<ReactionKey>
{
    public ElementType First { get; }
    public ElementType Second { get; }

    public ReactionKey(ElementType first, ElementType second)
    {
        First = first < second ? first : second;
        Second = first < second ? second : first;
    }

    public bool Equals(ReactionKey other) => First == other.First && Second == other.Second;
    public override bool Equals(object? obj) => obj is ReactionKey other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(First, Second);
    public static bool operator ==(ReactionKey left, ReactionKey right) => left.Equals(right);
    public static bool operator !=(ReactionKey left, ReactionKey right) => !left.Equals(right);
}
