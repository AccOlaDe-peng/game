using Catalyst.App;
using Godot;

namespace Catalyst.Player;

public partial class PlayerController : CharacterBody3D
{
    [Export]
    public float MoveSpeed { get; set; } = 6.0f;

    private Node3D? _visualRoot;
    private AnimationPlayer? _animationPlayer;
    private StringName _idleAnimation = new();
    private StringName _moveAnimation = new();
    private StringName _currentAnimation = new();
    private float _visualPhase;

    public override void _Ready()
    {
        _visualRoot = GetNodeOrNull<Node3D>("VisualRoot");
        _animationPlayer = FindAnimationPlayer(_visualRoot);
        if (_animationPlayer is not null)
        {
            _idleAnimation = FindAnimation("Spell_Simple_Idle", "Idle");
            _moveAnimation = FindAnimation("Jog_Fwd", "Walk");
            PlayAnimation(_idleAnimation);
        }
    }

    public override void _PhysicsProcess(double deltaValue)
    {
        float delta = (float)deltaValue;

        Vector2 input = Input.GetVector(
            InputBootstrap.MoveLeft,
            InputBootstrap.MoveRight,
            InputBootstrap.MoveForward,
            InputBootstrap.MoveBack);

        Vector3 moveDirection = new(input.X, 0.0f, input.Y);
        if (moveDirection.LengthSquared() > 1.0f)
        {
            moveDirection = moveDirection.Normalized();
        }

        Vector3 desiredVelocity = moveDirection * MoveSpeed;

        if (_visualRoot is not null)
        {
            float speedRatio = Mathf.Clamp(desiredVelocity.Length() / MoveSpeed, 0.0f, 2.5f);
            _visualPhase += delta * Mathf.Lerp(2.0f, 10.0f, Mathf.Min(speedRatio, 1.0f));
            _visualRoot.Position = new Vector3(
                0.0f,
                _animationPlayer is null
                    ? Mathf.Sin(_visualPhase) * 0.035f * Mathf.Min(speedRatio, 1.0f)
                    : 0.0f,
                0.0f);

            PlayAnimation(speedRatio > 0.08f ? _moveAnimation : _idleAnimation);

            if (moveDirection.LengthSquared() > 0.01f)
            {
                // Quaternius' animation character faces local +Z, while Godot's
                // conventional forward direction is -Z.
                float targetYaw = Mathf.Atan2(moveDirection.X, moveDirection.Z);
                Vector3 rotation = _visualRoot.Rotation;
                rotation.Y = Mathf.LerpAngle(rotation.Y, targetYaw, 1.0f - Mathf.Exp(-14.0f * delta));
                _visualRoot.Rotation = rotation;
            }
        }

        Velocity = new Vector3(desiredVelocity.X, Velocity.Y, desiredVelocity.Z);
        MoveAndSlide();
    }

    private StringName FindAnimation(params string[] preferredNames)
    {
        if (_animationPlayer is null)
        {
            return new StringName();
        }

        foreach (string preferredName in preferredNames)
        {
            foreach (StringName animationName in _animationPlayer.GetAnimationList())
            {
                if (animationName.ToString().EndsWith(preferredName, StringComparison.OrdinalIgnoreCase))
                {
                    return animationName;
                }
            }
        }

        return new StringName();
    }

    private void PlayAnimation(StringName animation)
    {
        if (_animationPlayer is null || animation.IsEmpty || animation == _currentAnimation)
        {
            return;
        }

        _animationPlayer.Play(animation, 0.18);
        _currentAnimation = animation;
    }

    private static AnimationPlayer? FindAnimationPlayer(Node? node)
    {
        if (node is null)
        {
            return null;
        }

        if (node is AnimationPlayer animationPlayer)
        {
            return animationPlayer;
        }

        foreach (Node child in node.GetChildren())
        {
            AnimationPlayer? match = FindAnimationPlayer(child);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}
