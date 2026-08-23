using Catalyst.App;
using Catalyst.Elements;
using Catalyst.Player;
using Godot;

namespace Catalyst.Presentation;

public partial class CameraFeedbackSystem : Node
{
    private SpringArm3D _springArm = null!;
    private PlayerHealth _health = null!;
    private ElementSystem _elements = null!;
    private SettingsService _settings = null!;
    private Vector3 _restPosition;
    private float _remaining;
    private float _duration;
    private float _strength;
    private float _previousHealth;

    public override void _Ready()
    {
        _springArm = GetNode<SpringArm3D>("../../Player/SpringArm3D");
        _health = GetNode<PlayerHealth>("../../Player/HealthComponent");
        _elements = GetNode<ElementSystem>("../../../SimulationRoot/ElementSystem");
        _settings = GetNode<SettingsService>("/root/SettingsService");
        _restPosition = _springArm.Position;
        _previousHealth = _health.CurrentHealth;
        _health.HealthChanged += OnHealthChanged;
        _elements.ReactionTriggered += OnReactionTriggered;
    }

    public override void _Process(double deltaValue)
    {
        if (_remaining <= 0.0f)
        {
            _springArm.Position = _restPosition;
            return;
        }
        float delta = (float)deltaValue;
        _remaining = Math.Max(0.0f, _remaining - delta);
        float fade = _duration <= 0.0f ? 0.0f : _remaining / _duration;
        float time = (float)Time.GetTicksMsec() * 0.001f;
        float amplitude = _strength * fade *
            Mathf.Clamp(_settings.Current.CameraShakeStrength, 0.0f, 1.0f);
        _springArm.Position = _restPosition + new Vector3(
            Mathf.Sin(time * 83.0f),
            Mathf.Cos(time * 67.0f),
            0.0f) * amplitude;
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_health))
        {
            _health.HealthChanged -= OnHealthChanged;
        }
        if (IsInstanceValid(_elements))
        {
            _elements.ReactionTriggered -= OnReactionTriggered;
        }
    }

    private void OnHealthChanged(float current, float maximum)
    {
        if (current < _previousHealth)
        {
            Shake(0.24f, 0.22f);
        }
        _previousHealth = current;
    }

    private void OnReactionTriggered(ReactionKind kind, Vector2 position, float damage)
    {
        Shake(0.13f, kind == ReactionKind.SteamShock ? 0.18f : 0.11f);
    }

    private void Shake(float duration, float strength)
    {
        if (strength < _strength && _remaining > 0.0f)
        {
            return;
        }
        _duration = duration;
        _remaining = duration;
        _strength = strength;
    }
}
