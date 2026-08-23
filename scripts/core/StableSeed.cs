namespace Catalyst.Core;

public static class StableSeed
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static ulong Derive(ulong rootSeed, string streamName)
    {
        ulong hash = OffsetBasis ^ rootSeed;
        foreach (char value in streamName)
        {
            hash ^= (byte)(value & 0xff);
            hash *= Prime;
            hash ^= (byte)(value >> 8);
            hash *= Prime;
        }

        return hash;
    }
}
