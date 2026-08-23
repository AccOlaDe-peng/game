using Catalyst.Enemies;
using Catalyst.Player;
using Catalyst.Run;
using Godot;

namespace Catalyst.Pickups;

public partial class PickupSystem : Node
{
    [Export(PropertyHint.Range, "1,2000,1")]
    public int Capacity { get; set; } = 512;

    [Export]
    public float AttractionRadius { get; set; } = 4.5f;

    [Export]
    public float CollectionRadius { get; set; } = 0.7f;

    [Export]
    public float AttractionSpeed { get; set; } = 13.0f;

    private PickupState[] _pickups = Array.Empty<PickupState>();
    private EnemySystem _enemies = null!;
    private CharacterBody3D _player = null!;
    private PlayerProgression _progression = null!;
    private RunController _run = null!;
    private MultiMeshInstance3D _view = null!;
    private MultiMesh _multiMesh = null!;

    public int ActiveCount { get; private set; }

    public override void _Ready()
    {
        _enemies = GetNode<EnemySystem>("../EntitySystem");
        _player = GetNode<CharacterBody3D>("../../WorldRoot/Player");
        _progression = GetNode<PlayerProgression>("../../WorldRoot/Player/Progression");
        _run = GetNode<RunController>("../RunController");
        _view = GetNode<MultiMeshInstance3D>("../../WorldRoot/PickupPresentation/ExperienceMultiMesh");
        _pickups = new PickupState[Capacity];
        _enemies.EnemyKilled += OnEnemyKilled;
        BuildPresentation();
    }

    public override void _PhysicsProcess(double deltaValue)
    {
        if (_run.State != RunState.Playing)
        {
            return;
        }

        float delta = (float)deltaValue;
        Vector2 playerPosition = new(_player.GlobalPosition.X, _player.GlobalPosition.Z);
        float attractionSquared = AttractionRadius * AttractionRadius;
        float collectionSquared = CollectionRadius * CollectionRadius;

        int index = 0;
        while (index < ActiveCount)
        {
            ref PickupState pickup = ref _pickups[index];
            Vector2 offset = playerPosition - pickup.Position;
            float distanceSquared = offset.LengthSquared();
            if (distanceSquared <= collectionSquared)
            {
                _progression.AddExperience(pickup.Value);
                RemoveAt(index);
                continue;
            }

            if (distanceSquared <= attractionSquared && distanceSquared > 0.0001f)
            {
                Vector2 desiredVelocity = offset.Normalized() * AttractionSpeed;
                pickup.Velocity = pickup.Velocity.MoveToward(desiredVelocity, AttractionSpeed * 5.0f * delta);
                pickup.Position += pickup.Velocity * delta;
            }

            index++;
        }

        SyncPresentation();
    }

    public void IncreaseAttractionRadius(float multiplier)
    {
        AttractionRadius *= Math.Max(1.0f, multiplier);
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_enemies))
        {
            _enemies.EnemyKilled -= OnEnemyKilled;
        }
    }

    private void OnEnemyKilled(Vector2 position, int value)
    {
        if (value <= 0)
        {
            return;
        }

        for (int index = 0; index < ActiveCount; index++)
        {
            if (_pickups[index].Position.DistanceSquaredTo(position) <= 0.45f * 0.45f)
            {
                _pickups[index].Value += value;
                return;
            }
        }

        if (ActiveCount >= Capacity)
        {
            if (ActiveCount > 0)
            {
                _pickups[0].Value += value;
            }

            return;
        }

        _pickups[ActiveCount++] = new PickupState
        {
            Position = position,
            Velocity = Vector2.Zero,
            Value = value
        };
    }

    private void RemoveAt(int index)
    {
        int lastIndex = ActiveCount - 1;
        if (index != lastIndex)
        {
            _pickups[index] = _pickups[lastIndex];
        }

        _pickups[lastIndex] = default;
        ActiveCount--;
    }

    private void BuildPresentation()
    {
        SphereMesh mesh = new()
        {
            Radius = 0.16f,
            Height = 0.32f,
            RadialSegments = 8,
            Rings = 4
        };
        mesh.Material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.34f, 1.0f, 0.66f, 1.0f),
            EmissionEnabled = true,
            Emission = new Color(0.08f, 0.72f, 0.35f, 1.0f),
            EmissionEnergyMultiplier = 1.8f,
            Roughness = 0.25f
        };

        _multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = mesh,
            InstanceCount = Capacity,
            VisibleInstanceCount = 0
        };
        _view.Multimesh = _multiMesh;
    }

    private void SyncPresentation()
    {
        for (int index = 0; index < ActiveCount; index++)
        {
            PickupState pickup = _pickups[index];
            float height = 0.28f + Mathf.Sin(Time.GetTicksMsec() * 0.004f + index) * 0.06f;
            _multiMesh.SetInstanceTransform(index,
                new Transform3D(Basis.Identity, new Vector3(pickup.Position.X, height, pickup.Position.Y)));
        }

        _multiMesh.VisibleInstanceCount = ActiveCount;
    }
}
