namespace Catalyst.Enemies;

public readonly struct EntityHandle : IEquatable<EntityHandle>
{
    public static readonly EntityHandle Invalid = new(-1, 0);

    public int Index { get; }
    public int Generation { get; }
    public bool IsValid => Index >= 0 && Generation > 0;

    public EntityHandle(int index, int generation)
    {
        Index = index;
        Generation = generation;
    }

    public bool Equals(EntityHandle other) =>
        Index == other.Index && Generation == other.Generation;

    public override bool Equals(object? obj) =>
        obj is EntityHandle other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Index, Generation);

    public static bool operator ==(EntityHandle left, EntityHandle right) => left.Equals(right);
    public static bool operator !=(EntityHandle left, EntityHandle right) => !left.Equals(right);
}
