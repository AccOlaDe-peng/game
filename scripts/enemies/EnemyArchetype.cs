namespace Catalyst.Enemies;

public enum EnemyArchetype : byte
{
    Swarmer,
    Hunter,
    Heavy,
    Caster,
    Exploder,
    Summoner,
    EliteCharger,
    ElementGuard,
    BossAshenColossus
}

public enum EnemyBehaviorState : byte
{
    Chasing,
    Positioning,
    Telegraphing,
    Charging,
    Recovering,
    Exposed
}
