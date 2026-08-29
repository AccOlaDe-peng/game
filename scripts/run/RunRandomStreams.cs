using Catalyst.Core;
using Godot;

namespace Catalyst.Run;

public sealed class RunRandomStreams
{
    public RandomNumberGenerator Wave { get; }
    public RandomNumberGenerator Upgrade { get; }
    public RandomNumberGenerator SpawnPosition { get; }
    public RandomNumberGenerator GameplayProc { get; }
    public RandomNumberGenerator PassiveProc { get; }
    public RandomNumberGenerator Presentation { get; }

    public RunRandomStreams(ulong rootSeed)
    {
        Wave = Create(rootSeed, "wave");
        Upgrade = Create(rootSeed, "upgrade");
        SpawnPosition = Create(rootSeed, "spawn_position");
        GameplayProc = Create(rootSeed, "gameplay_proc");
        PassiveProc = Create(rootSeed, "passive_proc");
        Presentation = Create(rootSeed, "presentation");
    }

    private static RandomNumberGenerator Create(ulong rootSeed, string name)
    {
        return new RandomNumberGenerator
        {
            Seed = StableSeed.Derive(rootSeed, name)
        };
    }
}
