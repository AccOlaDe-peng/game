using Catalyst.Enemies;
using Catalyst.Elements;
using Catalyst.Combat;
using Godot;

namespace Catalyst.Projectiles;

public struct ProjectileState
{
    public Vector2 Position;
    public Vector2 PreviousPosition;
    public Vector2 Velocity;
    public float Radius;
    public float RemainingLife;
    public float Damage;
    public StringName SourceSpellId;
    public ElementType Element;
    public ElementType SecondaryElement;
    public FusionIdentity Fusion;
    public int ElementStacks;
    public DamageFlags Flags;
    public float ExplosionRadius;
    public int RemainingHits;
    public int RemainingBounces;
    public float BounceRange;
    public bool DetonateOnExpire;
    public ProjectileModules Modules;
    public Vector2 Origin;
    public bool HasSplit;
    public bool IsReturning;
    public int ChainDepth;
    public int ChildEventsRemaining;
    public EntityHandle Hit0;
    public EntityHandle Hit1;
    public EntityHandle Hit2;
    public EntityHandle Hit3;
    public EntityHandle Hit4;
    public EntityHandle Hit5;
    public EntityHandle Hit6;
    public EntityHandle Hit7;
}
