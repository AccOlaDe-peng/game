namespace Catalyst.Projectiles;

[Flags]
public enum ProjectileModules
{
    None = 0,
    Split = 1 << 0,
    Homing = 1 << 1,
    Boomerang = 1 << 2,
    Fragments = 1 << 3,
    Nova = 1 << 4,
    GroundField = 1 << 5,
    StatusSpread = 1 << 6,
    TriggerOnPierce = 1 << 7,
    TriggerOnBounce = 1 << 8,
    ExplosionAction = 1 << 9
}
