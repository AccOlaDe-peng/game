using Godot;
using Catalyst.Elements;

namespace Catalyst.Enemies;

public struct EnemyState
{
    public EntityHandle Handle;
    public EnemyArchetype Archetype;
    public EnemyBehaviorState Behavior;
    public bool IsElite;
    public Vector2 Position;
    public Vector2 Velocity;
    public float Health;
    public float MaxHealth;
    public float MoveSpeed;
    public float Radius;
    public int ExperienceValue;
    public float ContactDamage;
    public float PreferredDistance;
    public float AttackCooldown;
    public float AttackRemaining;
    public float SpecialDamage;
    public float VisualScale;
    public float BehaviorTimer;
    public Vector2 LockedDirection;
    public Color BaseColor;
    public ElementType ResistantElement;
    public float AnimationPhase;
    public float DamageTakenMultiplier;
    public ElementRuntimeState Elements;
}
