using Catalyst.Core;
using Catalyst.Elements;
using Catalyst.Player;
using Catalyst.Run;
using Godot;

namespace Catalyst.Enemies;

public partial class EnemySystem : Node
{
    [Export(PropertyHint.Range, "1,2000,1")]
    public int Capacity { get; set; } = 600;

    [Export]
    public EnemyDefinition DefaultDefinition { get; set; } = null!;

    public event Action<Vector2, int>? EnemyKilled;
    public event Action<EnemyState, bool>? EnemyRemoved;
    public event Action<Vector2, float, EnemyArchetype>? EnemyAttackTelegraphed;
    public event Action<Vector2, float>? DamageApplied;

    private EnemyState[] _states = Array.Empty<EnemyState>();
    private int[] _denseToSlot = Array.Empty<int>();
    private int[] _slotToDense = Array.Empty<int>();
    private int[] _generations = Array.Empty<int>();
    private Stack<int> _freeSlots = null!;
    private readonly List<Vector2> _summonRequests = new(16);
    private CharacterBody3D _player = null!;
    private PlayerHealth _playerHealth = null!;
    private RunController _run = null!;
    private RunStatistics _statistics = null!;
    private ArchetypePresentation[] _presentations = null!;

    private const float PresentationTargetHeight = 1.0f;

    private static readonly string[] ArchetypeModelPaths =
    {
        "res://assets/art/enemies/monsters/blob/GreenBlob.gltf",
        "res://assets/art/enemies/monsters/blob/Cat.gltf",
        "res://assets/art/enemies/monsters/big/Yeti.gltf",
        "res://assets/art/enemies/monsters/blob/Wizard.gltf",
        "res://assets/art/enemies/monsters/blob/GreenSpikyBlob.gltf",
        "res://assets/art/enemies/monsters/flying/Ghost.gltf",
        "res://assets/art/enemies/monsters/flying/Dragon.gltf",
        "res://assets/art/enemies/monsters/big/Alien.gltf",
        "res://assets/art/enemies/monsters/big/MushroomKing.gltf"
    };

    public int ActiveCount { get; private set; }

    public override void _Ready()
    {
        _player = GetNode<CharacterBody3D>("../../WorldRoot/Player");
        _playerHealth = GetNode<PlayerHealth>("../../WorldRoot/Player/HealthComponent");
        _run = GetNode<RunController>("../RunController");
        _statistics = GetNode<RunStatistics>("../RunStatistics");

        _states = new EnemyState[Capacity];
        _denseToSlot = new int[Capacity];
        _slotToDense = new int[Capacity];
        _generations = new int[Capacity];
        Array.Fill(_slotToDense, -1);
        _freeSlots = new Stack<int>(Capacity);
        for (int index = Capacity - 1; index >= 0; index--)
        {
            _freeSlots.Push(index);
            _generations[index] = 1;
        }
        BuildPresentation();
    }

    public override void _PhysicsProcess(double deltaValue)
    {
        if (_run.State != RunState.Playing)
        {
            return;
        }

        float delta = (float)deltaValue;
        Vector2 playerPosition = ToSimulation(_player.GlobalPosition);
        _summonRequests.Clear();

        int denseIndex = 0;
        while (denseIndex < ActiveCount)
        {
            ref EnemyState enemy = ref _states[denseIndex];
            enemy.AttackRemaining = Math.Max(0.0f, enemy.AttackRemaining - delta);
            enemy.BehaviorTimer = Math.Max(0.0f, enemy.BehaviorTimer - delta);
            Vector2 offset = playerPosition - enemy.Position;
            float distance = offset.Length();
            Vector2 direction = distance > 0.0001f ? offset / distance : Vector2.Zero;
            float movementMultiplier = ElementResolver.GetMovementMultiplier(enemy.Elements);
            bool removed = UpdateBehavior(ref enemy, direction, distance, playerPosition, delta, movementMultiplier);
            if (removed)
            {
                continue;
            }

            enemy.Position += enemy.Velocity * delta;
            enemy.Position = new Vector2(
                Mathf.Clamp(enemy.Position.X, -39.0f, 39.0f),
                Mathf.Clamp(enemy.Position.Y, -39.0f, 39.0f));
            enemy.AnimationPhase += delta * (1.5f + enemy.MoveSpeed * 0.2f);
            denseIndex++;
        }

        foreach (Vector2 position in _summonRequests)
        {
            Spawn(position, DefaultDefinition);
        }
        SyncPresentation();
    }

    public EntityHandle Spawn(Vector2 position) => Spawn(position, DefaultDefinition);

    public EntityHandle Spawn(Vector2 position, EnemyDefinition? definition)
    {
        if (_freeSlots.Count == 0 || definition is null)
        {
            CatalystLog.Warning("Enemies", $"Enemy capacity exhausted at {ActiveCount}/{Capacity}.");
            return EntityHandle.Invalid;
        }

        int slot = _freeSlots.Pop();
        int denseIndex = ActiveCount++;
        EntityHandle handle = new(slot, _generations[slot]);
        _slotToDense[slot] = denseIndex;
        _denseToSlot[denseIndex] = slot;
        _states[denseIndex] = new EnemyState
        {
            Handle = handle,
            Archetype = definition.Archetype,
            Behavior = EnemyBehaviorState.Chasing,
            IsElite = definition.IsElite,
            Position = position,
            Health = definition.MaxHealth,
            MaxHealth = definition.MaxHealth,
            MoveSpeed = definition.MoveSpeed,
            Radius = definition.Radius,
            ExperienceValue = definition.ExperienceValue,
            ContactDamage = definition.ContactDamage,
            PreferredDistance = definition.PreferredDistance,
            AttackCooldown = definition.AttackCooldown,
            AttackRemaining = definition.AttackCooldown * 0.5f,
            SpecialDamage = definition.SpecialDamage,
            VisualScale = definition.VisualScale,
            AnimationPhase = denseIndex * 0.37f,
            BaseColor = definition.DisplayColor
            ,DamageTakenMultiplier = 1.0f
        };
        return handle;
    }

    public bool TryGet(EntityHandle handle, out EnemyState state)
    {
        if (!TryResolveDenseIndex(handle, out int denseIndex))
        {
            state = default;
            return false;
        }
        state = _states[denseIndex];
        return true;
    }

    public EntityHandle GetHandleAtDenseIndex(int denseIndex) =>
        denseIndex >= 0 && denseIndex < ActiveCount
            ? _states[denseIndex].Handle
            : EntityHandle.Invalid;

    public Vector2 GetPositionAtDenseIndex(int denseIndex) => _states[denseIndex].Position;

    public int CountArchetype(EnemyArchetype archetype)
    {
        int count = 0;
        for (int index = 0; index < ActiveCount; index++)
        {
            count += _states[index].Archetype == archetype ? 1 : 0;
        }
        return count;
    }

    public void TelegraphAttack(Vector2 position, float radius, EnemyArchetype archetype)
    {
        EnemyAttackTelegraphed?.Invoke(position, radius, archetype);
    }

    public bool TryGetPriorityElite(out EnemyState elite)
    {
        elite = default;
        float lowestHealthFraction = float.MaxValue;
        bool found = false;
        for (int index = 0; index < ActiveCount; index++)
        {
            EnemyState candidate = _states[index];
            if (!candidate.IsElite)
            {
                continue;
            }
            if (candidate.Archetype == EnemyArchetype.BossAshenColossus)
            {
                elite = candidate;
                return true;
            }
            float fraction = candidate.Health / candidate.MaxHealth;
            if (!found || fraction < lowestHealthFraction)
            {
                found = true;
                lowestHealthFraction = fraction;
                elite = candidate;
            }
        }
        return found;
    }

    public bool ApplyDamage(EntityHandle handle, float amount) =>
        ApplyDamage(handle, amount, out _);

    public bool ApplyDamage(EntityHandle handle, float amount, out float resolvedDamage)
    {
        resolvedDamage = 0.0f;
        if (amount <= 0.0f || !TryResolveDenseIndex(handle, out int denseIndex))
        {
            return false;
        }
        ref EnemyState enemy = ref _states[denseIndex];
        resolvedDamage = Math.Min(
            enemy.Health,
            amount * Math.Max(0.0f, enemy.DamageTakenMultiplier));
        enemy.Health -= resolvedDamage;
        _statistics.RecordDamage(resolvedDamage);
        DamageApplied?.Invoke(enemy.Position, resolvedDamage);
        if (enemy.Health <= 0.0f)
        {
            KillAtDenseIndex(denseIndex, true);
        }
        return true;
    }

    public bool SetPosition(EntityHandle handle, Vector2 position)
    {
        if (!TryResolveDenseIndex(handle, out int denseIndex))
        {
            return false;
        }
        _states[denseIndex].Position = new Vector2(
            Mathf.Clamp(position.X, -39.0f, 39.0f),
            Mathf.Clamp(position.Y, -39.0f, 39.0f));
        return true;
    }

    public bool SetDamageTakenMultiplier(EntityHandle handle, float multiplier)
    {
        if (!TryResolveDenseIndex(handle, out int denseIndex))
        {
            return false;
        }
        _states[denseIndex].DamageTakenMultiplier = Math.Max(0.0f, multiplier);
        return true;
    }

    public bool SetElementRuntime(EntityHandle handle, in ElementRuntimeState runtime)
    {
        if (!TryResolveDenseIndex(handle, out int denseIndex))
        {
            return false;
        }
        _states[denseIndex].Elements = runtime;
        return true;
    }

    public void ClearAll()
    {
        while (ActiveCount > 0)
        {
            ReleaseSlotAtDenseIndex(ActiveCount - 1);
        }
        SyncPresentation();
    }

    private bool UpdateBehavior(
        ref EnemyState enemy,
        Vector2 direction,
        float distance,
        Vector2 playerPosition,
        float delta,
        float movementMultiplier)
    {
        switch (enemy.Archetype)
        {
            case EnemyArchetype.Hunter:
                enemy.Velocity = direction * enemy.MoveSpeed * movementMultiplier *
                    (1.0f + Math.Max(0.0f, Mathf.Sin(enemy.AnimationPhase * 0.8f)) * 0.55f);
                break;
            case EnemyArchetype.Caster:
                UpdateCaster(ref enemy, direction, distance, movementMultiplier);
                break;
            case EnemyArchetype.Exploder:
                if (UpdateExploder(ref enemy, direction, distance, movementMultiplier))
                {
                    int denseIndex = _slotToDense[enemy.Handle.Index];
                    KillAtDenseIndex(denseIndex, false);
                    return true;
                }
                break;
            case EnemyArchetype.Summoner:
                UpdateSummoner(ref enemy, direction, distance, movementMultiplier);
                break;
            case EnemyArchetype.EliteCharger:
                UpdateCharger(ref enemy, direction, distance, movementMultiplier);
                break;
            case EnemyArchetype.ElementGuard:
                UpdateElementGuard(ref enemy, direction, movementMultiplier, delta);
                break;
            case EnemyArchetype.BossAshenColossus:
                enemy.Velocity = Vector2.Zero;
                break;
            default:
                enemy.Velocity = direction * enemy.MoveSpeed * movementMultiplier;
                break;
        }
        return false;
    }

    private void UpdateCaster(
        ref EnemyState enemy,
        Vector2 direction,
        float distance,
        float movementMultiplier)
    {
        if (enemy.Behavior == EnemyBehaviorState.Telegraphing)
        {
            enemy.Velocity = Vector2.Zero;
            if (enemy.BehaviorTimer <= 0.0f)
            {
                if (distance <= enemy.PreferredDistance + 4.0f)
                {
                    _playerHealth.ApplyDamage(enemy.SpecialDamage);
                }
                enemy.Behavior = EnemyBehaviorState.Recovering;
                enemy.AttackRemaining = enemy.AttackCooldown;
            }
            return;
        }

        if (distance < enemy.PreferredDistance - 2.0f)
        {
            enemy.Velocity = -direction * enemy.MoveSpeed * movementMultiplier;
        }
        else if (distance > enemy.PreferredDistance + 2.0f)
        {
            enemy.Velocity = direction * enemy.MoveSpeed * movementMultiplier;
        }
        else
        {
            enemy.Velocity = new Vector2(-direction.Y, direction.X) * enemy.MoveSpeed * 0.45f * movementMultiplier;
            if (enemy.AttackRemaining <= 0.0f)
            {
                enemy.Behavior = EnemyBehaviorState.Telegraphing;
                enemy.BehaviorTimer = 0.7f;
                EnemyAttackTelegraphed?.Invoke(enemy.Position, 1.4f, enemy.Archetype);
            }
        }
    }

    private bool UpdateExploder(
        ref EnemyState enemy,
        Vector2 direction,
        float distance,
        float movementMultiplier)
    {
        if (enemy.Behavior == EnemyBehaviorState.Telegraphing)
        {
            enemy.Velocity = Vector2.Zero;
            if (enemy.BehaviorTimer <= 0.0f)
            {
                if (distance <= 4.0f)
                {
                    _playerHealth.ApplyDamage(enemy.SpecialDamage);
                }
                return true;
            }
        }
        else
        {
            enemy.Velocity = direction * enemy.MoveSpeed * movementMultiplier;
            if (distance <= 2.6f)
            {
                enemy.Behavior = EnemyBehaviorState.Telegraphing;
                enemy.BehaviorTimer = 0.9f;
                EnemyAttackTelegraphed?.Invoke(enemy.Position, 3.8f, enemy.Archetype);
            }
        }
        return false;
    }

    private void UpdateSummoner(
        ref EnemyState enemy,
        Vector2 direction,
        float distance,
        float movementMultiplier)
    {
        float preferred = Math.Max(9.0f, enemy.PreferredDistance);
        enemy.Velocity = distance < preferred
            ? -direction * enemy.MoveSpeed * movementMultiplier
            : direction * enemy.MoveSpeed * 0.55f * movementMultiplier;
        if (enemy.AttackRemaining <= 0.0f && _summonRequests.Count < 16)
        {
            Vector2 side = new(-direction.Y, direction.X);
            _summonRequests.Add(enemy.Position + side * 1.2f);
            _summonRequests.Add(enemy.Position - side * 1.2f);
            enemy.AttackRemaining = enemy.AttackCooldown;
            EnemyAttackTelegraphed?.Invoke(enemy.Position, 2.2f, enemy.Archetype);
        }
    }

    private void UpdateCharger(
        ref EnemyState enemy,
        Vector2 direction,
        float distance,
        float movementMultiplier)
    {
        if (enemy.Behavior == EnemyBehaviorState.Telegraphing)
        {
            enemy.Velocity = Vector2.Zero;
            if (enemy.BehaviorTimer <= 0.0f)
            {
                enemy.Behavior = EnemyBehaviorState.Charging;
                enemy.BehaviorTimer = 0.55f;
            }
        }
        else if (enemy.Behavior == EnemyBehaviorState.Charging)
        {
            enemy.Velocity = enemy.LockedDirection * 13.0f * movementMultiplier;
            if (enemy.BehaviorTimer <= 0.0f)
            {
                enemy.Behavior = EnemyBehaviorState.Recovering;
                enemy.AttackRemaining = enemy.AttackCooldown;
            }
        }
        else
        {
            enemy.Velocity = direction * enemy.MoveSpeed * movementMultiplier;
            if (enemy.AttackRemaining <= 0.0f && distance is > 4.0f and < 16.0f)
            {
                enemy.Behavior = EnemyBehaviorState.Telegraphing;
                enemy.BehaviorTimer = 0.75f;
                enemy.LockedDirection = direction;
                EnemyAttackTelegraphed?.Invoke(enemy.Position, 7.0f, enemy.Archetype);
            }
        }
    }

    private static void UpdateElementGuard(
        ref EnemyState enemy,
        Vector2 direction,
        float movementMultiplier,
        float delta)
    {
        enemy.Velocity = direction * enemy.MoveSpeed * movementMultiplier;
        if (enemy.BehaviorTimer <= 0.0f)
        {
            enemy.ResistantElement = enemy.ResistantElement switch
            {
                ElementType.Fire => ElementType.Frost,
                ElementType.Frost => ElementType.Lightning,
                _ => ElementType.Fire
            };
            enemy.BehaviorTimer = 4.0f;
        }
    }

    private bool TryResolveDenseIndex(EntityHandle handle, out int denseIndex)
    {
        if (!handle.IsValid || handle.Index >= _slotToDense.Length ||
            _generations[handle.Index] != handle.Generation)
        {
            denseIndex = -1;
            return false;
        }
        denseIndex = _slotToDense[handle.Index];
        return denseIndex >= 0 && denseIndex < ActiveCount;
    }

    private void KillAtDenseIndex(int denseIndex, bool grantReward)
    {
        EnemyState killed = _states[denseIndex];
        if (grantReward)
        {
            EnemyKilled?.Invoke(killed.Position, killed.ExperienceValue);
            _statistics.RecordKill();
        }
        EnemyRemoved?.Invoke(killed, grantReward);
        ReleaseSlotAtDenseIndex(denseIndex);
    }

    private void ReleaseSlotAtDenseIndex(int denseIndex)
    {
        int releasedSlot = _denseToSlot[denseIndex];
        int lastDenseIndex = ActiveCount - 1;
        if (denseIndex != lastDenseIndex)
        {
            _states[denseIndex] = _states[lastDenseIndex];
            int movedSlot = _denseToSlot[lastDenseIndex];
            _denseToSlot[denseIndex] = movedSlot;
            _slotToDense[movedSlot] = denseIndex;
        }
        _states[lastDenseIndex] = default;
        _denseToSlot[lastDenseIndex] = -1;
        _slotToDense[releasedSlot] = -1;
        _generations[releasedSlot] = _generations[releasedSlot] == int.MaxValue
            ? 1
            : _generations[releasedSlot] + 1;
        _freeSlots.Push(releasedSlot);
        ActiveCount--;
    }

    private void BuildPresentation()
    {
        Node3D container = GetNode<Node3D>("../../WorldRoot/EnemyPresentation");
        _presentations = new ArchetypePresentation[ArchetypeModelPaths.Length];
        for (int archetype = 0; archetype < ArchetypeModelPaths.Length; archetype++)
        {
            Mesh? mesh = LoadEnemyMesh(ArchetypeModelPaths[archetype]) ?? BuildFallbackMesh();
            float scale = PresentationTargetHeight / Math.Max(0.01f, mesh.GetAabb().Size.Y);
            float groundOffset = -mesh.GetAabb().Position.Y * scale;

            MultiMeshInstance3D view = new()
            {
                Name = $"Archetype{(EnemyArchetype)archetype}"
            };
            container.AddChild(view);
            MultiMesh multiMesh = new()
            {
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                UseColors = true,
                Mesh = mesh,
                InstanceCount = Capacity,
                VisibleInstanceCount = 0
            };
            view.Multimesh = multiMesh;
            _presentations[archetype] = new ArchetypePresentation
            {
                View = view,
                MultiMesh = multiMesh,
                Scale = scale,
                GroundOffset = groundOffset
            };
        }
    }

    private static Mesh? LoadEnemyMesh(string modelPath)
    {
        PackedScene? scene = ResourceLoader.Load<PackedScene>(modelPath);
        if (scene is null)
        {
            GD.PushWarning($"Enemy art could not be loaded: {modelPath}");
            return null;
        }

        Node root = scene.Instantiate();
        var meshInstances = new List<MeshInstance3D>();
        CollectMeshInstances(root, meshInstances);
        if (meshInstances.Count == 0)
        {
            root.Free();
            GD.PushWarning("Enemy art does not contain a MeshInstance3D; using fallback mesh.");
            return null;
        }

        Material? sharedMaterial = null;
        ArrayMesh merged = new();
        SurfaceTool tool = new();
        int appendedSurfaces = 0;
        foreach (MeshInstance3D meshInstance in meshInstances)
        {
            Mesh? mesh = meshInstance.Mesh;
            if (mesh is null)
            {
                continue;
            }
            sharedMaterial ??= mesh.SurfaceGetMaterial(0);
            Transform3D localTransform = TransformRelativeTo(meshInstance, root);
            for (int surface = 0; surface < mesh.GetSurfaceCount(); surface++)
            {
                tool.AppendFrom(mesh, surface, localTransform);
                appendedSurfaces++;
            }
        }

        if (appendedSurfaces == 0)
        {
            root.Free();
            GD.PushWarning("Enemy art has no drawable surfaces; using fallback mesh.");
            return null;
        }

        if (sharedMaterial is StandardMaterial3D material)
        {
            StandardMaterial3D tintedMaterial = (StandardMaterial3D)material.Duplicate();
            tintedMaterial.VertexColorUseAsAlbedo = true;
            tintedMaterial.VertexColorIsSrgb = true;
            tool.SetMaterial(tintedMaterial);
        }

        tool.Commit(merged);
        root.Free();
        return merged;
    }

    private static void CollectMeshInstances(Node node, List<MeshInstance3D> output)
    {
        if (node is MeshInstance3D meshInstance)
        {
            output.Add(meshInstance);
        }

        foreach (Node child in node.GetChildren())
        {
            CollectMeshInstances(child, output);
        }
    }

    private static Transform3D TransformRelativeTo(Node node, Node root)
    {
        Transform3D transform = Transform3D.Identity;
        Node? current = node;
        while (current is not null && current != root)
        {
            if (current is Node3D node3D)
            {
                transform = node3D.Transform * transform;
            }
            current = current.GetParent();
        }
        return transform;
    }

    private static Mesh BuildFallbackMesh()
    {
        BoxMesh mesh = new() { Size = new Vector3(0.9f, 1.25f, 0.9f) };
        mesh.Material = new StandardMaterial3D
        {
            AlbedoColor = Colors.White,
            Roughness = 0.8f,
            VertexColorUseAsAlbedo = true
        };
        return mesh;
    }

    private void SyncPresentation()
    {
        int[] instanceCounters = new int[ArchetypeModelPaths.Length];
        for (int denseIndex = 0; denseIndex < ActiveCount; denseIndex++)
        {
            EnemyState enemy = _states[denseIndex];
            int archetype = (int)enemy.Archetype;
            ArchetypePresentation presentation = _presentations[archetype];
            int instanceIndex = instanceCounters[archetype]++;
            if (instanceIndex >= Capacity)
            {
                continue;
            }

            float bob = Mathf.Sin(enemy.AnimationPhase) * 0.08f;
            Vector3 origin = new(
                enemy.Position.X,
                presentation.GroundOffset * enemy.VisualScale + bob,
                enemy.Position.Y);
            Vector3 scale = new(
                presentation.Scale * enemy.VisualScale * (enemy.Archetype == EnemyArchetype.Caster ? 0.72f : 1.0f),
                presentation.Scale * enemy.VisualScale * (enemy.Archetype == EnemyArchetype.Exploder ? 0.72f : 1.0f),
                presentation.Scale * enemy.VisualScale);
            float yaw = enemy.Velocity.LengthSquared() > 0.001f
                ? Mathf.Atan2(-enemy.Velocity.X, -enemy.Velocity.Y)
                : 0.0f;
            Basis basis = new Basis(Vector3.Up, yaw).Scaled(scale);
            presentation.MultiMesh.SetInstanceTransform(instanceIndex, new Transform3D(basis, origin));

            Color tint = enemy.BaseColor;
            if (enemy.Elements.Fire.Stacks > 0)
            {
                tint = tint.Lerp(new Color(1.0f, 0.28f, 0.05f), enemy.Elements.Fire.Stacks / 8.0f);
            }
            if (enemy.Elements.Frost.Stacks > 0)
            {
                tint = tint.Lerp(new Color(0.18f, 0.82f, 1.0f), enemy.Elements.Frost.Stacks / 8.0f);
            }
            if (enemy.Elements.Lightning.Stacks > 0)
            {
                tint = tint.Lerp(new Color(1.0f, 0.9f, 0.18f), enemy.Elements.Lightning.Stacks / 8.0f);
            }
            if (enemy.ResistantElement != ElementType.None)
            {
                tint = tint.Lerp(Colors.White, 0.22f);
            }
            presentation.MultiMesh.SetInstanceColor(instanceIndex, tint);
        }

        for (int archetype = 0; archetype < _presentations.Length; archetype++)
        {
            _presentations[archetype].MultiMesh.VisibleInstanceCount =
                Math.Min(instanceCounters[archetype], Capacity);
        }
    }

    private struct ArchetypePresentation
    {
        public MultiMeshInstance3D View;
        public MultiMesh MultiMesh;
        public float Scale;
        public float GroundOffset;
    }

    private static Vector2 ToSimulation(Vector3 position) => new(position.X, position.Z);
}
